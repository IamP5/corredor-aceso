#!/usr/bin/env bash
# Roda cada cenário do simulador contra um Mosquitto em contêiner, captura o fio com
# mosquitto_sub e confere a captura com conferir_fio.py. Usa tokens sorteados.
#
# Uso: provar_no_fio.sh CONTEINER PORTA PASTA_DE_SAIDA [PYTHON]
#   CONTEINER  nome de um contêiner eclipse-mosquitto:2 que já está rodando
#   PORTA      porta do host ligada à 1883 desse contêiner
#   PYTHON     um Python com paho-mqtt (padrão: python3)
set -euo pipefail
conteiner=$1
porta=$2
saida="$(mkdir -p "$3" && cd "$3" && pwd)"
python=${4:-python3}
aqui="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# rodar NOME DURACAO ARGS...: captura, roda o simulador e confere.
rodar() {
  local nome=$1 duracao=$2
  shift 2
  echo "== $nome =="
  docker exec "$conteiner" mosquitto_sub -h 127.0.0.1 -t 'corredor/v1/#' -q 1 -F '%U %t %p' \
    -W "$((duracao + 8))" >"$saida/$nome.captura.txt" 2>/dev/null &
  local assinante=$!
  sleep 1
  "$python" "$aqui/simulador.py" --host 127.0.0.1 --porta "$porta" --duracao "$duracao" "$@" >"$saida/$nome.log.txt" 2>&1 &
  local simulador=$!
  if [[ $nome == manha || $nome == demo ]]; then
    comandos "$nome"
  fi
  wait "$simulador"
  wait "$assinante" || true
  (cd "$aqui" && "$python" conferir_fio.py "$saida/$nome.captura.txt" --log "$saida/$nome.log.txt") \
    | tee "$saida/$nome.conferencia.txt"
}

# Comandos do Motor no meio da rodada: DIA_CRITICO para todas, PEDIR_TOQUE para uma casa.
comandos() {
  local nome=$1 alvo token
  sleep 20
  alvo=$([[ $nome == demo ]] && echo toque || echo normal)
  token="$(awk -v c="$alvo" '$1 == "CASA" && $4 == c {print $3; exit}' "$saida/$nome.log.txt")"
  while read -r t; do
    docker exec "$conteiner" mosquitto_pub -h 127.0.0.1 -q 1 -t "corredor/v1/$t/comando" -m '{"tipo":"DIA_CRITICO"}'
  done < <(awk '$1 == "CASA" {print $3}' "$saida/$nome.log.txt")
  docker exec "$conteiner" mosquitto_pub -h 127.0.0.1 -q 1 -t "corredor/v1/$token/comando" -m '{"tipo":"PEDIR_TOQUE"}'
}

rodar manha 70 --cenario manha --tokens-ficticios --semente 1
rodar silencio 70 --cenario silencio --tokens-ficticios --inicio 20 --semente 2
rodar mudo 70 --cenario mudo --tokens-ficticios --inicio 20 --semente 3
rodar queda-de-energia 80 --cenario queda-de-energia --tokens-ficticios --inicio 20 --janela 30 --semente 4
rodar reenvio 80 --cenario reenvio --tokens-ficticios --inicio 20 --janela 30 --semente 5
# O cenário demo lê o papel de cada casa. Monta um tokens.local.json com os papéis da semente
# e tokens sorteados, como o --semear escreveria.
"$python" - "$aqui/../../docs/arquitetura/referencia/dados/semente/casas.json" "$saida/tokens.demo.json" <<'PY'
import json, secrets, sys
casas = json.load(open(sys.argv[1], encoding="utf-8"))["casas"]
itens = [{"n": c["n"], "casaToken": secrets.token_hex(16), "papelNaDemo": c["papelNaDemo"]} for c in casas]
json.dump(itens, open(sys.argv[2], "w", encoding="utf-8"), ensure_ascii=False, indent=1)
PY
rodar demo 70 --cenario demo --semente 6 --tokens "$saida/tokens.demo.json"
