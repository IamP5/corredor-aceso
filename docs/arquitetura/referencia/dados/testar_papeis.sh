#!/usr/bin/env bash
# Prova, no banco migrado, que cada papel do bd-modulo.sql so enxerga o proprio schema
# e que os gatilhos valem tambem para o papel da API. Cada comando entra pela rede, como a API.
set -euo pipefail
cd "$(dirname "$0")"
C=corredor-dados
como() { docker exec -e PGPASSWORD=corredor $C psql -X -q -v ON_ERROR_STOP=1 -h 127.0.0.1 -d migrado -U "$1" -Atc "$2" 2>&1; }
ok() { echo "ok  $1"; }
nega() {
	local saida
	if saida=$(como "$1" "$2"); then echo "FALHOU $3: o banco aceitou"; exit 1; fi
	echo "$saida" | grep -q "$4" || { echo "FALHOU $3: veio $saida"; exit 1; }
	ok "recusa  $3"
}
aceita() { como "$1" "$2" >/dev/null || { echo "FALHOU $3"; exit 1; }; ok "aceita  $3"; }

aceita corredor_motor "SELECT count(*) FROM motor.casa_monitorada" "corredor_motor le motor"
nega corredor_motor "SELECT 1 FROM cadastro.idoso" "corredor_motor le cadastro.idoso" "permission denied for schema cadastro"
nega corredor_motor "SELECT 1 FROM guarda.evento_de_seguranca" "corredor_motor le guarda" "permission denied for schema guarda"
nega corredor_cadastro "SELECT 1 FROM motor.casa_monitorada" "corredor_cadastro le motor" "permission denied for schema motor"
nega corredor_guarda "SELECT 1 FROM cadastro.idoso" "corredor_guarda le cadastro" "permission denied for schema cadastro"
nega corredor_motor_migra "SELECT 1 FROM cadastro.idoso" "dono do schema motor le cadastro" "permission denied for schema cadastro"
nega corredor_motor "CREATE TABLE motor.extra (a int)" "papel da API cria tabela" "permission denied for schema motor"
nega corredor_motor "TRUNCATE motor.evento_sensor" "papel da API faz TRUNCATE" "permission denied"
aceita corredor_cadastro "INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente, casa_endereco) VALUES ('0d0d0d0d-0000-4000-8000-000000000001', 3, 'Pessoa Ficticia', 'Sozinho', false, 'Rua Ficticia, 1'); INSERT INTO cadastro.visita VALUES ('0d0d0d0d-0000-4000-8000-000000000002', '0d0d0d0d-0000-4000-8000-000000000001', 'acs-demo-01', '2026-11-18 13:40+00', '2026-11-18 14:30+00', 'EstavaBem', NULL)" "corredor_cadastro grava idoso e visita"
nega corredor_cadastro "UPDATE cadastro.visita SET desfecho = 'NaoAbriu'" "papel da API altera visita" "nao aceita UPDATE"
nega corredor_cadastro "UPDATE cadastro.idoso SET casa_token = 'abababababababababababababababab' WHERE id = '0d0d0d0d-0000-4000-8000-000000000001'" "papel da API grava token sem consentimento" "token e consentimento vigente"
aceita corredor_guarda "INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, severidade) VALUES ('0d0d0d0d-0000-4000-8000-000000000003', '2026-11-18 13:40+00', 'BloqueioDeSaida', ARRAY['Saude'], 'Log', 'Cadastro', 4)" "corredor_guarda grava evento"
nega corredor_guarda "DELETE FROM guarda.evento_de_seguranca" "papel da API apaga evento de seguranca" "nao aceita DELETE"
