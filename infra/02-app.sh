#!/usr/bin/env bash
# Rubrica: script de provisionamento da aplicação (dependências, configurações,
# orquestração). 2 pontos.
# Instala Postgres, Mosquitto com ponte e Nginx. Cria usuários, banco, arquivos
# de ambiente e a unit da API. Implanta a última release se ainda não há versão.
# Pode rodar quantas vezes for preciso: cada função confere antes de mudar.
set -Eeuo pipefail
INFRA_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/comum.sh
source "$INFRA_DIR/lib/comum.sh"
# shellcheck source=lib/auditoria.sh
source "$INFRA_DIR/lib/auditoria.sh"
exigir_root
carregar_config
auditoria_iniciar "$@"
travar provisionamento

REPO_DIR="$(dirname "$INFRA_DIR")"
REINICIAR_API=0

garantir_swap() {
  local mem_kb
  mem_kb="$(awk '/^MemTotal:/ {print $2}' /proc/meminfo)"
  if ((mem_kb >= 3000000)) || swapon --noheadings --show | grep -q .; then
    feito "memória ou swap suficientes"
    return 0
  fi
  if [[ ! -f /swapfile ]]; then
    fallocate -l 2G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile >/dev/null
  fi
  swapon /swapfile
  grep -q '^/swapfile ' /etc/fstab || echo '/swapfile none swap sw 0 0' >>/etc/fstab
  mudou "swap de 2 GB criada, porque a VM tem menos de 3 GB de memória"
}

instalar_dependencias() {
  instalar_pacotes ca-certificates curl jq gettext-base logrotate libicu74 \
    postgresql mosquitto mosquitto-clients nginx
}

garantir_usuarios_e_pastas() {
  garantir_usuario corredor
  garantir_dir "$CORREDOR_RAIZ" 755 root:root
  garantir_dir "$CORREDOR_RAIZ/releases" 755 root:root
  garantir_dir "$CORREDOR_ETC" 750 root:corredor
  garantir_dir "$CORREDOR_ETC/segredos" 700 root:root
  if [[ ! -f $CORREDOR_ETC/segredos.env ]]; then
    instalar_texto "$CORREDOR_ETC/segredos.env" 640 root:corredor <<'SEGREDOS'
# Segredos digitados à mão nesta VM. Este arquivo nunca entra no Git.
# Depois de editar: sudo corredor reiniciar api
# Llm__ApiKey=
SEGREDOS
  fi
}

# Usuário que o GitHub Actions usa para implantar. Só existe se a chave pública
# estiver versionada em infra/chaves/deploy.pub.
garantir_usuario_de_deploy() {
  local chave="$INFRA_DIR/chaves/deploy.pub"
  if [[ ! -f $chave ]]; then
    info "sem infra/chaves/deploy.pub: deploy pelo GitHub Actions desligado"
    return 0
  fi
  instalar_pacotes sudo openssh-server
  garantir_usuario deploy /home/deploy
  garantir_dir /home/deploy/.ssh 700 deploy:deploy
  instalar_arquivo "$chave" /home/deploy/.ssh/authorized_keys 600 deploy:deploy
  visudo -cf "$INFRA_DIR/config/sudoers-deploy" >/dev/null
  instalar_arquivo "$INFRA_DIR/config/sudoers-deploy" /etc/sudoers.d/corredor-deploy 440 root:root
}

# Um banco, um schema por módulo e dois papéis por módulo, criados por
# config/bd-modulo.sql. corredor_<m>_migra é dono do schema e roda as migrações do
# módulo. corredor_<m> é o papel da API: lê e escreve, não muda tabela. Nenhum dos
# dois enxerga o schema de outro módulo, e nenhum é superusuário.
configurar_postgres() {
  local m
  ativar_servico postgresql
  for _ in $(seq 1 30); do
    pg_isready -q && break
    sleep 1
  done
  pg_isready -q || erro "o Postgres não respondeu em 30 s"
  if ! psql_admin -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$BD_NOME'" | grep -q 1; then
    psql_admin -d postgres -c "CREATE DATABASE \"$BD_NOME\""
    mudou "banco $BD_NOME criado"
  fi
  for m in $MODULOS_BD; do
    psql_admin -d "$BD_NOME" -v banco="$BD_NOME" -v esquema="$m" \
      -v dono="corredor_${m}_migra" -v senha_dono="$(segredo_bd "$m-migra")" \
      -v papel="corredor_$m" -v senha="$(segredo_bd "$m")" <"$INFRA_DIR/config/bd-modulo.sql"
  done
  feito "banco $BD_NOME: papéis, schemas e permissões de ${MODULOS_BD// /, }"
}

# Monta /etc/corredor/api.env a cada execução, a partir da configuração
# versionada e das senhas guardadas na VM.
escrever_ambiente_da_api() {
  local m nome tmp
  tmp="$(mktemp)"
  {
    echo "# Gerado por infra/02-app.sh. Não edite: mude infra/config/ ou segredos.env."
    echo "ASPNETCORE_ENVIRONMENT=$API_AMBIENTE_ASPNET"
    echo "ASPNETCORE_URLS=http://127.0.0.1:$API_PORTA"
    echo "Corredor__Ambiente=$CORREDOR_AMBIENTE"
    echo "Mqtt__Host=127.0.0.1"
    echo "Mqtt__Porta=1883"
    echo "Mqtt__ClienteId=corredor-motor"
    for m in $MODULOS_BD; do
      nome="${m^}"
      echo "ConnectionStrings__$nome=Host=127.0.0.1;Port=5432;Database=$BD_NOME;Username=corredor_$m;Password=$(segredo_bd "$m")"
    done
  } >"$tmp"
  instalar_arquivo "$tmp" "$CORREDOR_ETC/api.env" 640 root:corredor
  rm -f "$tmp"
  if ((ALTEROU)); then REINICIAR_API=1; fi
}

configurar_mosquitto() {
  MQTT_CLIENTE_PONTE="corredor-ponte-$(head -c 8 /etc/machine-id)"
  export MQTT_CLIENTE_PONTE
  instalar_modelo "$INFRA_DIR/config/mosquitto-corredor.conf.modelo" /etc/mosquitto/conf.d/corredor.conf \
    644 root:root MQTT_PUBLICO_HOST MQTT_PUBLICO_PORTA MQTT_PREFIXO_PUBLICO MQTT_CLIENTE_PONTE
  if ((ALTEROU)) && systemctl is-active --quiet mosquitto; then
    systemctl restart mosquitto
  fi
  ativar_servico mosquitto
}

configurar_nginx() {
  local recarregar=0
  garantir_dir /var/www/certbot 755 root:root
  if [[ -e /etc/nginx/sites-enabled/default ]]; then
    rm -f /etc/nginx/sites-enabled/default
    recarregar=1
    mudou "site padrão do Nginx removido"
  fi
  instalar_modelo "$INFRA_DIR/config/nginx-rotas.conf.modelo" /etc/nginx/snippets/corredor-rotas.conf \
    644 root:root API_PORTA CORREDOR_RAIZ
  if ((ALTEROU)); then recarregar=1; fi
  instalar_modelo "$INFRA_DIR/config/nginx-site.conf.modelo" /etc/nginx/conf.d/corredor.conf \
    644 root:root CORREDOR_DOMINIO_INTERNO
  if ((ALTEROU)); then recarregar=1; fi
  ativar_servico nginx
  if ((recarregar)); then
    nginx -t -q
    systemctl reload nginx
  fi
}

# Tentativa de HTTPS com Let's Encrypt, só quando o ambiente tem nome público.
# Nunca bloqueia: se a emissão falhar, o provisionamento segue e a API responde em
# HTTP no mesmo nome. O HTTPS dos vídeos vem do túnel (infra/tunel.sh).
garantir_tls() {
  local dom="$CORREDOR_DOMINIO_PUBLICO" conta=(--register-unsafely-without-email)
  if [[ -z $dom ]]; then
    info "ambiente sem nome público: TLS não configurado"
    return 0
  fi
  if [[ -f /etc/letsencrypt/live/$dom/fullchain.pem ]]; then
    feito "certificado TLS de $dom"
  else
    instalar_pacotes certbot
    if [[ -n $CORREDOR_EMAIL_TLS ]]; then conta=(-m "$CORREDOR_EMAIL_TLS"); fi
    if certbot certonly --webroot -w /var/www/certbot -d "$dom" --non-interactive --agree-tos \
      "${conta[@]}" --deploy-hook 'systemctl reload nginx' >/dev/null 2>&1; then
      mudou "certificado TLS emitido para $dom"
    else
      info "AVISO: o certbot não emitiu o certificado de $dom. A API segue em HTTP."
      info "Causa em /var/log/letsencrypt/letsencrypt.log. Rode este script de novo depois."
      return 0
    fi
  fi
  instalar_modelo "$INFRA_DIR/config/nginx-tls.conf.modelo" /etc/nginx/conf.d/corredor-tls.conf \
    644 root:root CORREDOR_DOMINIO_PUBLICO
  if ((ALTEROU)); then
    nginx -t -q
    systemctl reload nginx
  fi
}

# Ambiente Python do job de ML e do simulador. Só existe se as pastas existirem.
instalar_python() {
  local requisitos=() r soma
  for r in "$REPO_DIR/ml/requirements-vm.txt" "$REPO_DIR/iot/simulador/requirements.txt"; do
    if [[ -f $r ]]; then requisitos+=("$r"); fi
  done
  if ((${#requisitos[@]} == 0)); then
    info "sem ml/requirements-vm.txt nem iot/simulador/requirements.txt: Python não instalado"
    return 0
  fi
  instalar_pacotes python3-venv
  [[ -x $CORREDOR_RAIZ/venv/bin/python ]] || python3 -m venv "$CORREDOR_RAIZ/venv"
  soma="$(cat "${requisitos[@]}" | sha256sum | cut -d' ' -f1)"
  if [[ "$(cat "$CORREDOR_RAIZ/venv/.requisitos" 2>/dev/null)" == "$soma" ]]; then
    feito "ambiente Python em $CORREDOR_RAIZ/venv"
  else
    for r in "${requisitos[@]}"; do
      "$CORREDOR_RAIZ/venv/bin/pip" install --quiet -r "$r"
    done
    echo "$soma" >"$CORREDOR_RAIZ/venv/.requisitos"
    mudou "dependências Python instaladas em $CORREDOR_RAIZ/venv"
  fi
}

instalar_unidades() {
  local u recarregar=0 unidades=(corredor-api.service)
  if [[ -f $REPO_DIR/ml/prever_hoje.py ]]; then unidades+=(corredor-ml.service corredor-ml.timer); fi
  if [[ -f $REPO_DIR/iot/simulador/simulador.py ]]; then unidades+=(corredor-simulador.service); fi
  for u in "${unidades[@]}"; do
    instalar_arquivo "$INFRA_DIR/systemd/$u" "/etc/systemd/system/$u" 644 root:root
    if ((ALTEROU)); then
      recarregar=1
      if [[ $u == corredor-api.service ]]; then REINICIAR_API=1; fi
    fi
  done
  if ((recarregar)); then systemctl daemon-reload; fi
  if [[ " ${unidades[*]} " == *" corredor-ml.timer "* ]]; then ativar_servico corredor-ml.timer; fi
}

instalar_comando() {
  instalar_texto /usr/local/sbin/corredor 755 root:root <<CMD
#!/bin/sh
exec /bin/bash "$INFRA_DIR/bin/corredor" "\$@"
CMD
}

# Com versão implantada: garante a API no ar e reinicia se a configuração mudou.
# Sem versão: busca a última release. Se ainda não existe release, só avisa.
garantir_api() {
  if [[ -L $CORREDOR_RAIZ/current ]]; then
    if ((REINICIAR_API)) && systemctl is-active --quiet corredor-api; then
      systemctl restart corredor-api
      mudou "API reiniciada, porque a unit ou o api.env mudou"
    fi
    ativar_servico corredor-api
  elif bash "$INFRA_DIR/deploy.sh" --ultima; then
    mudou "primeira versão da API implantada"
  else
    info "AVISO: nenhuma release implantada ainda. Depois rode: sudo corredor deploy vX.Y.Z"
  fi
}

echo "== 02-app: ambiente $CORREDOR_AMBIENTE =="
garantir_swap
instalar_dependencias
garantir_usuarios_e_pastas
garantir_usuario_de_deploy
configurar_postgres
escrever_ambiente_da_api
configurar_mosquitto
configurar_nginx
garantir_tls
instalar_python
instalar_unidades
instalar_comando
garantir_api
echo "== 02-app: $MUDANCAS mudança(s) =="
