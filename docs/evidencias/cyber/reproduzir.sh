#!/usr/bin/env bash
# Reproduz as quatro evidências da auditoria de Cybersecurity contra a API rodando em Development
# e grava a saída de cada uma nesta pasta. As rotas /api/guarda/demo/* só existem em Development.
#
# Uso:
#   API=http://localhost:5000 LOG_DA_API=/caminho/do/log PSQL="psql postgresql://..." \
#     bash docs/evidencias/cyber/reproduzir.sh
#
# API         endereço da API (padrão http://localhost:5000)
# TOKEN       token da ACS de appsettings.Development.json
# LOG_DA_API  arquivo com a saída padrão da API, para copiar as linhas de log de cada evidência
# PSQL        comando psql conectado ao banco da API, para copiar as linhas de guarda.evento_de_seguranca
# SO          quais evidências rodar, como SO="1" ou SO="2 3 4" (padrão: todas)
set -euo pipefail
PASTA="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API="${API:-http://localhost:5000}"
TOKEN="${TOKEN:-dev-acs-demo-01-so-no-notebook}"

secao() { printf '\n## %s\n\n' "$1"; }

pedido() {
  local metodo="$1" rota="$2" corpo="${3:-}" autenticado="${4:-sim}"
  local args=(-s -X "$metodo" -H 'Content-Type: application/json' -w '\nHTTP %{http_code}\n')
  [[ $autenticado == sim ]] && args+=(-H "Authorization: Bearer $TOKEN")
  [[ -n $corpo ]] && args+=(-d "$corpo")
  printf '$ curl -X %s %s%s' "$metodo" "$rota" "${corpo:+ -d '$corpo'}"
  [[ $autenticado == sim ]] && printf ' (com token da ACS)'
  printf '\n'
  curl "${args[@]}" "$API$rota"
}

log_desde() {
  [[ -n ${LOG_DA_API:-} ]] || { echo "(LOG_DA_API não informado)"; return; }
  # O formato simples do console escreve o cabeçalho numa linha e a mensagem na seguinte.
  tail -n +"$1" "$LOG_DA_API" | grep -B1 -E "$2" | grep -v '^--$' || echo "(nenhuma linha)"
}

linhas_do_log() { [[ -n ${LOG_DA_API:-} ]] && wc -l < "$LOG_DA_API" | tr -d ' ' || echo 0; }

eventos() {
  [[ -n ${PSQL:-} ]] || { echo "(PSQL não informado)"; return; }
  $PSQL -X -c "SELECT ocorrido_em, tipo, tipos_de_dado, canal, modulo, rota, severidade, mensagem_origem, mensagem_texto FROM guarda.evento_de_seguranca ORDER BY ocorrido_em"
}

painel() { curl -s "$API/api/painel"; printf '\n'; }

esperar_traducao() { sleep "${ESPERA:-8}"; }

quero() { [[ " ${SO:-1 2 3 4} " == *" $1 "* ]]; }

quero 1 && {
  echo "# Evidência 1: CPF do professor mascarado antes do log e do banco"
  inicio=$(($(linhas_do_log) + 1))
  secao "Requisição"
  pedido POST /api/guarda/demo/documento '{"documento": "123.456.789-00"}'
  esperar_traducao
  secao "Log da API depois da requisição"
  log_desde "$inicio" 'PII mascarada|123\.456'
  secao "guarda.evento_de_seguranca"
  eventos
  secao "Painel"
  painel
} > "$PASTA/evidencia-1-documento.txt"

quero 2 && {
  echo "# Evidência 2: tentar gravar o nome da pessoa no log"
  inicio=$(($(linhas_do_log) + 1))
  secao "Requisição"
  pedido POST /api/guarda/demo/log-nome '{"nome": "Aparecida Souza"}'
  esperar_traducao
  secao "Log da API depois da requisição"
  log_desde "$inicio" 'Visita registrada|Aparecida'
  secao "guarda.evento_de_seguranca"
  eventos
} > "$PASTA/evidencia-2-log-nome.txt"

quero 3 && {
  echo "# Evidência 3: rota sem autenticação que tentaria devolver condições de saúde"
  secao "Sem token: a resposta é bloqueada"
  pedido GET /api/guarda/demo/condicoes '' nao
  secao "Com token: a rota responde, e o TextoRotulado sai sem o valor"
  pedido GET /api/guarda/demo/condicoes
  esperar_traducao
  secao "guarda.evento_de_seguranca"
  eventos
} > "$PASTA/evidencia-3-rota-aberta.txt"

quero 4 && {
  echo "# Evidência 4: \"ele mora sozinho e toma diurético\" para o grupo do bairro"
  secao "Requisição"
  pedido POST /api/guarda/demo/grupo-do-bairro '{"texto": "ele mora sozinho e toma diurético"}'
  esperar_traducao
  secao "guarda.evento_de_seguranca"
  eventos
  secao "Painel: farol, pontos, eventos por canal e mensagem traduzida"
  painel
} > "$PASTA/evidencia-4-grupo-do-bairro.txt"

echo "evidências gravadas em $PASTA"
