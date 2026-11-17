#!/usr/bin/env bash
# Sobe a demo inteira no notebook e roda as cenas contra o sistema de verdade:
# Postgres e Mosquitto do compose.dev.yml, papéis de módulo, migrações aplicadas como no deploy,
# a API em Development, a carga da demo duas vezes, o simulador das casas e o job de ML.
#
# Uso:
#   bash tools/demo.sh              sobe do zero e roda todas as cenas
#   bash tools/demo.sh subir        só sobe (banco do zero, API, carga) e deixa a API rodando
#   bash tools/demo.sh cenas [nome] roda as cenas na API que já está de pé (todas, ou uma)
#   bash tools/demo.sh app          abre o app no navegador, ligado à API (deixe rodando em outro terminal)
#   bash tools/demo.sh telas        dirige o app aberto pelo comando acima e salva os prints das telas
#   bash tools/demo.sh invariante   sorteia pacotes, visitas e saltos do relógio e confere o falso verde
#   bash tools/demo.sh api          reinicia só a API; parar-api só para a API
#   bash tools/demo.sh parar        para a API, o simulador e o app. Os contêineres ficam.
#   bash tools/demo.sh parar --tudo também para os contêineres do compose.dev.yml
#
# O banco da demo é corredor_demo, separado do corredor do npm run dev:api. Toda subida apaga e
# recria o corredor_demo, porque o relógio de demo só anda para frente (seção 8.4 de 04-modelo-de-dados.md).
# A API escuta em CORREDOR_API_PORTA (padrão 5080): no macOS a porta 5000 é do AirPlay.
# Docker antigo: exporte DOCKER_API_VERSION=1.43 antes, se o docker compose reclamar da versão.
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$RAIZ"

PORTA="${CORREDOR_API_PORTA:-5080}"
API="http://localhost:$PORTA"
BANCO="corredor_demo"
ESTADO="$RAIZ/.demo"
COMPOSE=(docker compose -f "$RAIZ/compose.dev.yml")
# Senhas de papel só para o Postgres local do compose.dev.yml, que escuta em 127.0.0.1.
SENHA_MIGRA="dev-migra"
SENHA_API="dev-api"
TOKEN_ACS_DEV="dev-acs-demo-01-so-no-notebook"
source "$RAIZ/infra/config/corredor.conf.padrao"
mkdir -p "$ESTADO"

passo() { printf '\n== %s\n' "$*"; }

psql_super() { "${COMPOSE[@]}" exec -T postgres psql -U corredor -X -q -v ON_ERROR_STOP=1 "$@"; }

esperar() { # esperar <descrição> <segundos> <comando...>
  local desc="$1" limite="$2"; shift 2
  for _ in $(seq "$limite"); do "$@" >/dev/null 2>&1 && return 0; sleep 1; done
  echo "tempo esgotado esperando $desc" >&2; return 1
}

conexao() { echo "Host=127.0.0.1;Port=5432;Database=$BANCO;Username=corredor_$1;Password=$SENHA_API"; }

ambiente_da_api() {
  export ASPNETCORE_ENVIRONMENT=Development
  ConnectionStrings__Motor="$(conexao motor)"
  ConnectionStrings__Cadastro="$(conexao cadastro)"
  ConnectionStrings__Guarda="$(conexao guarda)"
  export ConnectionStrings__Motor ConnectionStrings__Cadastro ConnectionStrings__Guarda
  export Logging__Console__FormatterName=simple Logging__Console__FormatterOptions__TimestampFormat="HH:mm:ss "
}

# Uma linha por tabela dos três schemas: linhas e md5 do conteúdo. Prova que a segunda carga não muda nada.
impressao_do_banco() {
  psql_super -d "$BANCO" -At <<'SQL'
SELECT format('SELECT %L, count(*), md5(coalesce(string_agg(t::text, ''|'' ORDER BY t::text), '''')) FROM %I.%I t',
              table_schema || '.' || table_name, table_schema, table_name)
  FROM information_schema.tables
 WHERE table_schema IN ('motor', 'cadastro', 'guarda') AND table_type = 'BASE TABLE'
 ORDER BY 1 \gexec
SQL
}

python_da_demo() {
  if [[ ! -x "$ESTADO/venv/bin/python" ]]; then
    python3 -m venv "$ESTADO/venv"
    "$ESTADO/venv/bin/pip" install -q -r iot/simulador/requirements.txt
  fi
  echo "$ESTADO/venv/bin/python"
}

parar_api() {
  if [[ -f "$ESTADO/api.pid" ]] && kill -0 "$(cat "$ESTADO/api.pid")" 2>/dev/null; then
    kill "$(cat "$ESTADO/api.pid")"; sleep 2
  fi
  rm -f "$ESTADO/api.pid"
}

iniciar_api() {
  ambiente_da_api
  dotnet build api/Corredor.slnx --nologo -v q >/dev/null
  nohup dotnet run --no-build --no-launch-profile --project api/src/Corredor.Api --urls "$API" \
    >>"$ESTADO/api.log" 2>&1 &
  echo $! >"$ESTADO/api.pid"
  esperar "a API ficar pronta em $API/healthz/ready" 90 curl -sf "$API/healthz/ready"
  echo "API pronta em $API (log em .demo/api.log)"
}

subir() {
  passo "Postgres e Mosquitto (npm run dev:deps)"
  "${COMPOSE[@]}" up -d
  esperar "o Postgres" 60 psql_super -d corredor -c 'select 1'

  passo "banco $BANCO do zero, com os papéis de infra/config/bd-modulo.sql"
  parar_api
  # Um simulador de uma rodada anterior brigaria pelo mesmo client id do MQTT com o das cenas.
  pkill -f "iot/simulador/simulador.py" 2>/dev/null || true
  psql_super -d corredor -c "DROP DATABASE IF EXISTS $BANCO WITH (FORCE)" -c "CREATE DATABASE $BANCO"
  for m in $MODULOS_BD; do
    psql_super -d "$BANCO" -v banco="$BANCO" -v esquema="$m" -v dono="corredor_${m}_migra" \
      -v senha_dono="$SENHA_MIGRA" -v papel="corredor_$m" -v senha="$SENHA_API" -f - <infra/config/bd-modulo.sql
    echo "papéis corredor_${m}_migra e corredor_$m prontos"
  done

  passo "migrações: SQL do tools/migracoes.sh, aplicado duas vezes como corredor_<modulo>_migra"
  bash tools/migracoes.sh "$ESTADO/migracoes" >/dev/null
  for rodada in 1 2; do
    for m in $MODULOS_BD; do
      "${COMPOSE[@]}" exec -T postgres psql "postgresql://corredor_${m}_migra:$SENHA_MIGRA@127.0.0.1:5432/$BANCO" \
        -X -q -v ON_ERROR_STOP=1 --single-transaction -c 'SET client_min_messages = warning' -f - <"$ESTADO/migracoes/$m.sql"
      echo "rodada $rodada: $m.sql aplicado como corredor_${m}_migra"
    done
  done

  passo "carga da demo, duas vezes (Corredor.Api --semear api/semente/casas.json)"
  ambiente_da_api
  dotnet build api/Corredor.slnx --nologo -v q >/dev/null
  for rodada in 1 2; do
    dotnet run --no-build --no-launch-profile --project api/src/Corredor.Api -- --semear "$RAIZ/api/semente/casas.json" 2>&1 \
      | grep -E "semente:|erro|Erro|fail" | sed "s/^/rodada $rodada: /"
    cp api/semente/tokens.local.json "$ESTADO/tokens.rodada$rodada.json"
    impressao_do_banco >"$ESTADO/banco.rodada$rodada.txt"
  done
  if cmp -s "$ESTADO/tokens.rodada1.json" "$ESTADO/tokens.rodada2.json" \
    && cmp -s "$ESTADO/banco.rodada1.txt" "$ESTADO/banco.rodada2.txt"; then
    echo "a segunda carga não mudou nada: mesmos 40 tokens e as $(wc -l <"$ESTADO/banco.rodada2.txt" | tr -d ' ') tabelas iguais, linha a linha"
  else
    diff "$ESTADO/banco.rodada1.txt" "$ESTADO/banco.rodada2.txt" >&2 || true
    echo "a segunda carga mudou o banco ou os tokens" >&2; exit 1
  fi

  passo "API em Development"
  : >"$ESTADO/api.log"
  iniciar_api
}

cenas() {
  local py; py="$(python_da_demo)"
  CORREDOR_API_URL="$API" CORREDOR_TOKEN_ACS="$TOKEN_ACS_DEV" CORREDOR_DEMO_ESTADO="$ESTADO" \
    "$py" tools/demo/cenas.py "$@"
}

app() {
  passo "app no navegador, ligado a $API"
  [[ -d app/node_modules ]] || npm --prefix app ci
  echo "Na entrada, use o código de acesso $TOKEN_ACS_DEV e a microárea 3."
  cd app && EXPO_PUBLIC_API_URL="$API" exec npx expo start --web --port 8081
}

parar() {
  parar_api
  pkill -f "iot/simulador/simulador.py" 2>/dev/null || true
  pkill -f "expo start --web --port 8081" 2>/dev/null || true
  if [[ "${1:-}" == "--tudo" ]]; then "${COMPOSE[@]}" stop; fi
  echo "parado"
}

case "${1:-tudo}" in
  tudo) subir; cenas ;;
  subir) subir ;;
  api) parar_api; iniciar_api ;;
  parar-api) parar_api; echo "API parada" ;;
  telas) CORREDOR_API_URL="$API" CORREDOR_TOKEN_ACS="$TOKEN_ACS_DEV" node tools/demo/telas.mjs ;;
  invariante) shift; py="$(python_da_demo)"; CORREDOR_API_URL="$API" CORREDOR_TOKEN_ACS="$TOKEN_ACS_DEV" "$py" tools/demo/invariante.py "$@" ;;
  cenas) shift; cenas "$@" ;;
  app) app ;;
  parar) shift; parar "$@" ;;
  *) sed -n '2,20p' "$0"; exit 2 ;;
esac
