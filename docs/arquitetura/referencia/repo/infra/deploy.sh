#!/usr/bin/env bash
# Implanta uma versão da API e volta sozinho para a anterior se ela não ficar
# saudável. Também faz o rollback manual.
#
# Uso:
#   deploy.sh vX.Y.Z              baixa a release do GitHub e implanta
#   deploy.sh --ultima            implanta a release mais recente
#   deploy.sh --arquivo PACOTE    implanta um pacote local feito por tools/empacotar.sh
#   deploy.sh --voltar            troca para a versão anterior, sem tocar no banco
#   opções: --simular-falha (ensaio do rollback), --forcar (reimplanta a mesma versão)
#
# Seguro para repetir: a release só aparece em releases/ depois de conferida, o
# link `current` troca de forma atômica, as migrações são idempotentes e
# implantar a versão que já está no ar não faz nada.
set -Eeuo pipefail
INFRA_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/comum.sh
source "$INFRA_DIR/lib/comum.sh"
# shellcheck source=lib/auditoria.sh
source "$INFRA_DIR/lib/auditoria.sh"
exigir_root
carregar_config
auditoria_iniciar "$@"
travar deploy

RELEASES="$CORREDOR_RAIZ/releases"
ATUAL="$CORREDOR_RAIZ/current"
ANTERIOR="$CORREDOR_RAIZ/previous"
REGISTRO="$CORREDOR_LOG/deploy.log"
SAUDE="http://127.0.0.1:$API_PORTA/healthz/ready"
VERSAO_URL="http://127.0.0.1:$API_PORTA/api/versao"
PADRAO_TAG='^v[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?$'
SIMULAR_FALHA=0
FORCAR=0

registrar() {
  printf '%s usuario=%s %s\n' "$(date -Is)" "$AUD_USUARIO" "$*" | tee -a "$REGISTRO"
}

arquitetura() {
  case "$(uname -m)" in
    x86_64) echo x64 ;;
    aarch64 | arm64) echo arm64 ;;
    *) erro "arquitetura sem pacote: $(uname -m)" ;;
  esac
}

# O GitHub redireciona /releases/latest para /releases/tag/<tag>.
resolver_ultima() {
  local url
  url="$(curl -fsSLI -o /dev/null -w '%{url_effective}' "$CORREDOR_RELEASES_URL/latest" 2>/dev/null)" || return 1
  [[ $url == */tag/* ]] || return 1
  echo "${url##*/tag/}"
}

# Confere o pacote e só então o move para releases/<tag>. Um download ou uma
# extração interrompidos ficam em .baixando-* e são apagados na execução seguinte.
instalar_pacote() {
  local pacote=$1 tmp=$2 tag
  mkdir "$tmp/conteudo"
  tar -xzf "$pacote" -C "$tmp/conteudo"
  tag="$(cat "$tmp/conteudo/VERSION" 2>/dev/null || true)"
  [[ $tag =~ $PADRAO_TAG ]] || erro "pacote sem arquivo VERSION válido"
  [[ -f $tmp/conteudo/api/Corredor.Api ]] || erro "pacote sem api/Corredor.Api"
  chmod 755 "$tmp/conteudo/api/Corredor.Api"
  chown -R root:root "$tmp/conteudo"
  chmod -R a+rX,go-w "$tmp/conteudo"
  if [[ -d $RELEASES/$tag ]]; then
    feito "release $tag já está em disco" >&2
  else
    mv -T "$tmp/conteudo" "$RELEASES/$tag"
  fi
  rm -rf "$tmp"
  echo "$tag"
}

baixar() {
  local tag=$1 asset tmp
  if [[ -d $RELEASES/$tag ]]; then
    feito "release $tag já está em disco"
    return 0
  fi
  asset="corredor-linux-$(arquitetura).tar.gz"
  tmp="$(mktemp -d "$RELEASES/.baixando-XXXXXX")"
  info "baixando $asset de $CORREDOR_RELEASES_URL/download/$tag"
  curl -fsSL --retry 3 -o "$tmp/$asset" "$CORREDOR_RELEASES_URL/download/$tag/$asset" ||
    erro "não achei o pacote $asset da release $tag"
  curl -fsSL --retry 3 -o "$tmp/SHA256SUMS" "$CORREDOR_RELEASES_URL/download/$tag/SHA256SUMS"
  (cd "$tmp" && sha256sum --ignore-missing --check --quiet SHA256SUMS) || erro "o hash do pacote não confere"
  [[ "$(instalar_pacote "$tmp/$asset" "$tmp")" == "$tag" ]] || erro "o pacote baixado não é da release $tag"
}

# Aplica os scripts SQL idempotentes que vieram na release. Antes, guarda um
# pg_dump. Cada script roda em uma transação: ou entra inteiro, ou não entra.
# O script <modulo>.sql conecta como corredor_<modulo>_migra, o dono do schema do
# módulo. Esse papel não é superusuário, então um RESET ROLE no SQL não sobe de
# privilégio, e ele não enxerga o schema de outro módulo.
migrar() {
  local dir=$1 tag=$2 sql copia m
  compgen -G "$dir/migracoes/*.sql" >/dev/null || {
    info "release sem migrações"
    return 0
  }
  install -d -m 0700 -o postgres -g postgres /var/backups/corredor
  copia="/var/backups/corredor/$(date +%Y%m%dT%H%M%S)-antes-de-$tag.dump"
  (cd / && runuser -u postgres -- pg_dump -Fc -d "$BD_NOME" -f "$copia")
  find /var/backups/corredor -name '*.dump' -printf '%T@ %p\n' | sort -rn | tail -n +6 | cut -d' ' -f2- | xargs -r rm -f
  for sql in "$dir"/migracoes/*.sql; do
    m="$(basename "$sql" .sql)"
    [[ " $MODULOS_BD " == *" $m "* ]] || erro "migração de módulo desconhecido: $m (MODULOS_BD=$MODULOS_BD)"
    PGPASSWORD="$(segredo_bd "$m-migra")" psql -X -q -v ON_ERROR_STOP=1 --single-transaction \
      -h 127.0.0.1 -U "corredor_${m}_migra" -d "$BD_NOME" \
      -c 'SET client_min_messages = warning' -f "$sql" >/dev/null
    feito "migração $(basename "$sql") aplicada como corredor_${m}_migra"
  done
}

ativar() {
  ln -sfn "$1" "$ATUAL.novo"
  mv -Tf "$ATUAL.novo" "$ATUAL"
}

versao_de() { sed 's/^v//' "$1/VERSION"; }

# Zera o contador de partidas antes de reiniciar. Sem isto, uma versão que caiu
# várias vezes esgota o StartLimitBurst da unit e o systemd recusa o reinício
# da versão boa durante o rollback.
reiniciar_api() {
  systemctl reset-failed corredor-api 2>/dev/null || true
  systemctl restart corredor-api || true
}

reinicios() { systemctl show -p NRestarts --value corredor-api; }

# Pronta quer dizer: /healthz/ready responde 200 e /api/versao diz a versão esperada.
# Desiste antes do limite se o processo já caiu três vezes.
saudavel() {
  local esperada="${1//./\\.}" limite=${2:-$ESPERA_SAUDE_S} inicio
  inicio="$(reinicios)"
  for _ in $(seq 1 "$limite"); do
    if curl -fsS --max-time 2 "$SAUDE" >/dev/null 2>&1 &&
      curl -fsS --max-time 2 "$VERSAO_URL" 2>/dev/null | grep -Eq "\"versao\"[[:space:]]*:[[:space:]]*\"$esperada\""; then
      return 0
    fi
    if (($(reinicios) - inicio >= 3)); then return 1; fi
    sleep 1
  done
  return 1
}

limpar_antigas() {
  local dir manter
  manter="$(readlink -f "$ATUAL" 2>/dev/null || true) $(readlink -f "$ANTERIOR" 2>/dev/null || true)"
  find "$RELEASES" -mindepth 1 -maxdepth 1 -type d -name 'v*' -printf '%T@ %p\n' | sort -rn |
    tail -n +"$((RELEASES_MANTIDAS + 1))" | cut -d' ' -f2- | while read -r dir; do
    [[ " $manter " == *" $dir "* ]] || rm -rf "$dir"
  done
}

implantar() {
  local tag=$1 destino="$RELEASES/$1" anterior="" volta=""
  if [[ -L $ATUAL ]]; then anterior="$(readlink -f "$ATUAL")"; fi
  if [[ $anterior == "$destino" ]] && ((FORCAR == 0 && SIMULAR_FALHA == 0)) && saudavel "$(versao_de "$destino")" 3; then
    registrar "evento=nada_a_fazer versao=$tag"
    return 0
  fi
  if [[ -n $anterior && $anterior != "$destino" ]]; then
    volta="$anterior"
  elif [[ -L $ANTERIOR ]]; then
    volta="$(readlink -f "$ANTERIOR")"
  fi

  registrar "evento=inicio versao=$tag de=$(basename "${anterior:-nenhuma}")"
  migrar "$destino" "$tag"
  ativar "$destino"
  systemctl enable corredor-api >/dev/null 2>&1
  reiniciar_api

  if ((SIMULAR_FALHA == 0)) && saudavel "$(versao_de "$destino")"; then
    if [[ -n $anterior && $anterior != "$destino" ]]; then ln -sfn "$anterior" "$ANTERIOR"; fi
    limpar_antigas
    registrar "evento=deploy_ok versao=$tag"
    return 0
  fi

  registrar "evento=deploy_falhou versao=$tag motivo=$( ((SIMULAR_FALHA)) && echo falha_simulada || echo nao_ficou_saudavel)"
  journalctl -u corredor-api -n 40 --no-pager >>"$REGISTRO" 2>&1 || true
  if [[ -n $volta && -d $volta ]]; then
    ativar "$volta"
    reiniciar_api
    if saudavel "$(versao_de "$volta")"; then
      registrar "evento=rollback_ok versao=$(basename "$volta")"
    else
      registrar "evento=rollback_falhou versao=$(basename "$volta")"
    fi
  else
    systemctl stop corredor-api
    rm -f "$ATUAL"
    registrar "evento=sem_versao_anterior acao=api_parada"
  fi
  exit 1
}

# Rollback manual: troca current e previous. Rodar duas vezes volta ao começo.
voltar() {
  local de para
  [[ -L $ANTERIOR && -L $ATUAL ]] || erro "não há versão anterior registrada em $ANTERIOR"
  de="$(readlink -f "$ATUAL")"
  para="$(readlink -f "$ANTERIOR")"
  [[ -d $para ]] || erro "a pasta da versão anterior sumiu: $para"
  ativar "$para"
  ln -sfn "$de" "$ANTERIOR"
  reiniciar_api
  if saudavel "$(versao_de "$para")"; then
    registrar "evento=rollback_manual_ok de=$(basename "$de") para=$(basename "$para")"
  else
    registrar "evento=rollback_manual_falhou de=$(basename "$de") para=$(basename "$para")"
    exit 1
  fi
}

ALVO=""
ARQUIVO=""
while (($#)); do
  case "$1" in
    --simular-falha) SIMULAR_FALHA=1 ;;
    --forcar) FORCAR=1 ;;
    --ultima | --voltar) ALVO="$1" ;;
    --arquivo)
      ALVO="--arquivo"
      ARQUIVO="${2:?informe o caminho do pacote}"
      shift
      ;;
    *) ALVO="$1" ;;
  esac
  shift
done

rm -rf "$RELEASES"/.baixando-*
case "$ALVO" in
  --voltar) voltar ;;
  --ultima)
    TAG="$(resolver_ultima)" || {
      info "o repositório $CORREDOR_REPO_GH ainda não tem release publicada"
      exit 3
    }
    baixar "$TAG"
    implantar "$TAG"
    ;;
  --arquivo)
    [[ -f $ARQUIVO ]] || erro "arquivo não encontrado: $ARQUIVO"
    TAG="$(instalar_pacote "$ARQUIVO" "$(mktemp -d "$RELEASES/.baixando-XXXXXX")")"
    implantar "$TAG"
    ;;
  *)
    [[ $ALVO =~ $PADRAO_TAG ]] || erro "informe uma tag como v0.3.0, ou --ultima, --arquivo PACOTE, --voltar"
    baixar "$ALVO"
    implantar "$ALVO"
    ;;
esac
