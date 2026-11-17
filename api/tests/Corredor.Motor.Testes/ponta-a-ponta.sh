#!/usr/bin/env bash
# Ponta a ponta do Motor contra o Corredor.Api de verdade, um Mosquitto e um Postgres 16.
# Publica no contrato MQTT v1, chama as rotas por HTTP e confere as tres cenas da demo.
# Pede a API em Development, com as migracoes aplicadas e o papel corredor_motor criado.
# A casa entra direto em motor.casa_monitorada, no lugar do AtivarCasaAsync do Cadastro.
# Uso: API=http://127.0.0.1:5000 PG_CONTEINER=<postgres> MQ_CONTEINER=<mosquitto> \
#   BANCO=corredor ACS_TOKEN=<token da ACS da microarea 3> bash ponta-a-ponta.sh
set -euo pipefail
API=${API:?informe a URL da API}
PG=${PG_CONTEINER:?informe o conteiner do Postgres}
MQ=${MQ_CONTEINER:?informe o conteiner do Mosquitto}
BANCO=${BANCO:-corredor}
AUT="Authorization: Bearer ${ACS_TOKEN:?informe o token da ACS da microarea 3}"
TOKEN=$(python3 -c 'import secrets;print(secrets.token_hex(16))')
DIA=$(python3 -c 'import datetime,zoneinfo;print((datetime.datetime.now(zoneinfo.ZoneInfo("America/Sao_Paulo"))+datetime.timedelta(days=1)).date())')
falhas=0

ok() { echo "  ok: $1"; }
falha() { echo "  FALHA: $1"; falhas=$((falhas + 1)); }
http() { curl -sS -H "$AUT" -H 'Content-Type: application/json' "$@"; }
relogio() { http -X PUT "$API/api/demo/relogio" -d "{\"agora\":\"${DIA}T$1:00-03:00\"}" >/dev/null; }
radar() { http "$API/api/radar?microarea=3"; }
agora_utc() { python3 -c 'import datetime,sys;print((datetime.datetime.now(datetime.timezone.utc)+datetime.timedelta(seconds=int(sys.argv[1]))).strftime("%Y-%m-%dT%H:%M:%S.%fZ"))' "${1:-0}"; }
publicar() { # seq tipo [bufferizado] [ts]
  local ts=${4:-$(agora_utc)}
  docker exec "$MQ" mosquitto_pub -h localhost -q 1 -t "corredor/v1/$TOKEN/evento" \
    -m "{\"seq\":$1,\"ts\":\"$ts\",\"tipo\":\"$2\",\"tempC\":null,\"tensaoV\":null,\"bufferizado\":${3:-false}}"
  sleep 1
}
campo() { python3 -c "import json,sys;d=json.load(sys.stdin);print(eval(sys.argv[1]))" "$1"; }

echo "saude"
[[ $(curl -s -o /dev/null -w '%{http_code}' "$API/healthz/ready") == 200 ]] && ok "/healthz/ready 200 com banco e MQTT" || falha "/healthz/ready"

echo "preparo: casa Sozinho $TOKEN na microarea 3, dia critico $DIA aberto as 05h"
docker exec "$PG" psql -q -U corredor_motor -d "$BANCO" -c \
  "INSERT INTO motor.casa_monitorada (casa_token, microarea_id, perfil_contato, perfil_eletrodependente, ativada_em) VALUES ('$TOKEN', 3, 'Sozinho', false, now())"
relogio 05:00
resposta=$(http -X PUT "$API/api/dias-criticos/$DIA" -d '{"probabilidade":0.81,"motivo":"minima de 24 graus pela segunda noite","origem":"MODELO"}')
echo "  $resposta"
[[ $(echo "$resposta" | campo 'd["abertoEm"]') == "${DIA}T05:00:00"* ]] && ok "turno aberto as 05h, -03:00" || falha "abertoEm"
codigo=$(http -o /dev/null -w '%{http_code}' -X PUT "$API/api/dias-criticos/2020-01-01" -d '{"probabilidade":0.5,"motivo":"x","origem":"MODELO"}')
[[ $codigo == 422 ]] && ok "data que nao e hoje: 422" || falha "data passada deu $codigo"
publicar 1 HEARTBEAT

echo "cena 1: o movimento tira a casa da lista de pendentes"
relogio 10:01
publicar 2 HEARTBEAT
r=$(radar)
echo "  $(echo "$r" | campo '[(c["situacao"], c["motivo"], c["desde"]) for c in d["pendentes"] if c["casaToken"]=="'$TOKEN'"]')"
[[ $(echo "$r" | campo 'any(c["casaToken"]=="'$TOKEN'" and c["situacao"]=="Vermelho" and c["motivo"]=="SemMovimentoNaJanela" for c in d["pendentes"])') == True ]] \
  && ok "10h01 sem movimento: Vermelho, SemMovimentoNaJanela, pendente" || falha "vermelho das 10h"
publicar 3 MOVIMENTO
r=$(radar)
[[ $(echo "$r" | campo 'all(c["casaToken"]!="'$TOKEN'" for c in d["pendentes"]) and d["verdes"]>=1') == True ]] \
  && ok "depois do MOVIMENTO: fora dos pendentes, verdes=$(echo "$r" | campo 'd["verdes"]')" || falha "movimento"

echo "cena 2: o aparelho mudo fica Cinza e continua Cinza ate um pacote novo"
relogio 10:03
r=$(radar)
[[ $(echo "$r" | campo 'any(c["casaToken"]=="'$TOKEN'" and c["situacao"]=="Cinza" for c in d["pendentes"])') == True ]] \
  && ok "10h03, 2 min sem pacote: Cinza pendente" || falha "cinza das 10h03"
relogio 10:20
r=$(radar)
[[ $(echo "$r" | campo 'any(c["casaToken"]=="'$TOKEN'" and c["situacao"]=="Cinza" for c in d["pendentes"])') == True ]] \
  && ok "10h20: continua Cinza, nunca Verde" || falha "cinza das 10h20"
publicar 4 HEARTBEAT
r=$(radar)
[[ $(echo "$r" | campo 'all(c["casaToken"]!="'$TOKEN'" for c in d["pendentes"]+d["atendidas"])') == True ]] \
  && ok "pacote novo: sai do Cinza (movimento das 10h01 vale na janela)" || falha "volta do cinza"

echo "cena 3: o evento reenviado do buffer e gravado uma vez com a hora original"
original=$(agora_utc -120)
publicar 500 MOVIMENTO false "$original"
publicar 500 MOVIMENTO true "$original"
linha=$(http "$API/api/casas/$TOKEN/linha-do-tempo")
echo "  $(echo "$linha" | campo '[(e["seq"], e["tipo"], e["ocorridoEm"], e["recebidoEm"], e["bufferizado"]) for e in d["eventos"] if e["seq"]==500]')"
[[ $(echo "$linha" | campo 'sum(1 for e in d["eventos"] if e["seq"]==500)') == 1 ]] && ok "um evento seq 500" || falha "duplicado"
[[ $(echo "$linha" | campo '[e for e in d["eventos"] if e["seq"]==500][0]["ocorridoEm"] < [e for e in d["eventos"] if e["seq"]==500][0]["recebidoEm"]') == True ]] \
  && ok "ocorridoEm e a hora do ts, dois minutos antes da chegada" || falha "hora original"
[[ $(echo "$linha" | campo 'len([e for e in d["eventos"] if e["tipo"]=="HEARTBEAT"])') == 0 ]] && ok "heartbeat nao vira evento" || falha "heartbeat gravado"

echo "encerramento"
relogio 17:30
enc=$(http -X POST "$API/api/dias-criticos/$DIA/encerramento")
enc2=$(http -X POST "$API/api/dias-criticos/$DIA/encerramento")
echo "  $enc"
[[ "$enc" == "$enc2" ]] && ok "encerrar de novo devolve o mesmo resumo" || falha "resumo mudou"
codigo=$(http -o /dev/null -w '%{http_code}' -X PUT "$API/api/dias-criticos/$DIA" -d '{"probabilidade":0.5,"motivo":"x","origem":"MODELO"}')
[[ $codigo == 409 ]] && ok "declarar depois do encerramento: 409" || falha "turno encerrado deu $codigo"

echo "falhas: $falhas"
exit $falhas
