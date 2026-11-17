#!/usr/bin/env bash
# Funções usadas por todos os scripts de infra/.
# Regra de todas elas: ler o estado atual e mudar só a diferença.
# Quem chama já definiu INFRA_DIR e rodou `set -Eeuo pipefail`.
# shellcheck disable=SC2034  # ALTEROU e MUDANCAS são lidas por quem inclui este arquivo

CORREDOR_RAIZ="${CORREDOR_RAIZ:-/opt/corredor}"
CORREDOR_ETC="${CORREDOR_ETC:-/etc/corredor}"
CORREDOR_LOG="${CORREDOR_LOG:-/var/log/corredor}"
MUDANCAS=0
ALTEROU=0
APT_ATUALIZADO=0

info() { printf '  [..] %s\n' "$*"; }
feito() { printf '  [ok] %s\n' "$*"; }
mudou() {
  MUDANCAS=$((MUDANCAS + 1))
  printf '  [++] %s\n' "$*"
}
erro() {
  printf '  [ERRO] %s\n' "$*" >&2
  exit 1
}

exigir_root() {
  [[ $EUID -eq 0 ]] || erro "rode com sudo: sudo bash $0"
}

# Só um provisionamento ou deploy por vez. Os scripts filhos herdam a trava.
travar() {
  [[ ${CORREDOR_TRAVA:-} == "$1" ]] && return 0
  exec 9>"/run/lock/corredor-$1.lock"
  flock -n 9 || erro "outro '$1' está em andamento nesta VM"
  export CORREDOR_TRAVA="$1"
}

detectar_ambiente() {
  if [[ -s $CORREDOR_ETC/ambiente ]]; then
    cat "$CORREDOR_ETC/ambiente"
  elif command -v cloud-id >/dev/null 2>&1 && [[ "$(cloud-id 2>/dev/null)" == azure ]]; then
    echo producao
  else
    echo local
  fi
}

carregar_config() {
  CORREDOR_AMBIENTE="$(detectar_ambiente)"
  set -a
  # shellcheck source=/dev/null
  source "$INFRA_DIR/config/corredor.conf.padrao"
  # shellcheck source=/dev/null
  source "$INFRA_DIR/config/ambientes/$CORREDOR_AMBIENTE.conf"
  if [[ -f $CORREDOR_ETC/corredor.conf ]]; then
    # shellcheck source=/dev/null
    source "$CORREDOR_ETC/corredor.conf"
  fi
  set +a
  export CORREDOR_AMBIENTE CORREDOR_RAIZ CORREDOR_ETC CORREDOR_LOG
}

instalar_pacotes() {
  local p faltam=()
  for p in "$@"; do
    dpkg-query -W -f='${Status}' "$p" 2>/dev/null | grep -q 'install ok installed' || faltam+=("$p")
  done
  if ((${#faltam[@]} == 0)); then
    feito "pacotes presentes: $*"
    return 0
  fi
  if ((APT_ATUALIZADO == 0)); then
    apt-get update -qq
    APT_ATUALIZADO=1
  fi
  # Termina uma instalação que tenha sido cortada no meio, por queda ou reboot.
  DEBIAN_FRONTEND=noninteractive dpkg --configure -a >/dev/null
  DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends "${faltam[@]}" >/dev/null
  mudou "pacotes instalados: ${faltam[*]}"
}

garantir_usuario() {
  local nome=$1 casa=${2:-}
  if id -u "$nome" >/dev/null 2>&1; then
    feito "usuário $nome"
  elif [[ -n $casa ]]; then
    useradd --create-home --home-dir "$casa" --shell /bin/bash "$nome"
    mudou "usuário $nome criado"
  else
    useradd --system --no-create-home --shell /usr/sbin/nologin "$nome"
    mudou "usuário de serviço $nome criado"
  fi
}

garantir_dir() {
  local dir=$1 modo=$2 dono=$3
  if [[ -d $dir && "$(stat -c '%a %U:%G' "$dir")" == "$modo $dono" ]]; then
    return 0
  fi
  install -d -m "$modo" -o "${dono%:*}" -g "${dono#*:}" "$dir"
  mudou "diretório $dir ($modo $dono)"
}

# Copia origem para destino só se o conteúdo, o modo ou o dono mudarem.
# Grava em um arquivo vizinho e renomeia, então o destino nunca fica pela metade.
# Deixa ALTEROU=1 quando mexeu, para quem chama saber se recarrega o serviço.
instalar_arquivo() {
  local origem=$1 destino=$2 modo=$3 dono=$4
  ALTEROU=0
  if [[ -f $destino ]] && cmp -s "$origem" "$destino" &&
    [[ "$(stat -c '%a %U:%G' "$destino")" == "$modo $dono" ]]; then
    feito "$destino"
    return 0
  fi
  install -D -m "$modo" -o "${dono%:*}" -g "${dono#*:}" "$origem" "$destino.novo"
  mv -f "$destino.novo" "$destino"
  ALTEROU=1
  mudou "$destino"
}

# Troca ${VAR} só das variáveis listadas. O resto do modelo fica intacto,
# o que protege as variáveis do Nginx, como $host.
instalar_modelo() {
  local modelo=$1 destino=$2 modo=$3 dono=$4 lista="" v tmp
  shift 4
  for v in "$@"; do lista+="\${$v} "; done
  tmp="$(mktemp)"
  envsubst "$lista" <"$modelo" >"$tmp"
  instalar_arquivo "$tmp" "$destino" "$modo" "$dono"
  rm -f "$tmp"
}

# Mantém um bloco marcado dentro de um arquivo que tem outros donos,
# como /etc/hosts e authorized_keys. Fora das marcas, nada muda.
bloco_gerenciado() {
  local arquivo=$1 conteudo=$2 tmp
  local ini="# >>> corredor-aceso (gerenciado por infra/, não edite)" fim="# <<< corredor-aceso"
  ALTEROU=0
  tmp="$(mktemp)"
  if [[ -f $arquivo ]]; then
    awk -v ini="$ini" -v fim="$fim" '$0==ini{pula=1} !pula{print} $0==fim{pula=0}' "$arquivo" >"$tmp"
  fi
  printf '%s\n%s\n%s\n' "$ini" "$conteudo" "$fim" >>"$tmp"
  if [[ -f $arquivo ]] && cmp -s "$tmp" "$arquivo"; then
    feito "bloco gerenciado em $arquivo"
  else
    cat "$tmp" >"$arquivo"
    ALTEROU=1
    mudou "bloco gerenciado em $arquivo"
  fi
  rm -f "$tmp"
}

psql_admin() {
  (cd / && runuser -u postgres -- psql -X -q -v ON_ERROR_STOP=1 "$@")
}

# Devolve a senha de um papel do banco: segredo_bd motor (API) ou segredo_bd motor-migra
# (dono do schema). Gera na primeira vez e guarda só na VM.
segredo_bd() {
  local arquivo="$CORREDOR_ETC/segredos/bd-$1.senha"
  if [[ ! -s $arquivo ]]; then
    (umask 077 && head -c 24 /dev/urandom | od -An -tx1 | tr -d ' \n' >"$arquivo")
  fi
  cat "$arquivo"
}

ativar_servico() {
  local unidade=$1
  if systemctl is-enabled --quiet "$unidade" 2>/dev/null && systemctl is-active --quiet "$unidade"; then
    feito "serviço $unidade ativo"
  else
    systemctl enable --now "$unidade" >/dev/null 2>&1
    mudou "serviço $unidade habilitado e iniciado"
  fi
}

# Igual a instalar_arquivo, com o conteúdo vindo da entrada padrão.
instalar_texto() {
  local destino=$1 modo=$2 dono=$3 tmp
  tmp="$(mktemp)"
  cat >"$tmp"
  instalar_arquivo "$tmp" "$destino" "$modo" "$dono"
  rm -f "$tmp"
}
