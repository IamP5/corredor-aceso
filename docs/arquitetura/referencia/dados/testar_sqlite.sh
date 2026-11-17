#!/usr/bin/env bash
# Cria o banco do app em memoria e prova o cache, a juncao do radar e a fila de desfechos.
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p saida
BANCO=saida/app-teste.db
rm -f "$BANCO"
sqlite3 "$BANCO" < sqlite/app.sql
ok() { echo "ok  $1"; }
recusa() { if sqlite3 "$BANCO" "$2" 2>/dev/null; then echo "FALHOU $1: o SQLite aceitou"; exit 1; else ok "recusa  $1"; fi; }

sqlite3 "$BANCO" <<'SQL'
BEGIN;
DELETE FROM lista_de_nomes;
INSERT INTO lista_de_nomes VALUES
	('aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', 'Aparecida Jatoba', 'Rua Ficticia das Acacias, 12', 'Rosa Manaca', '(00) 90000-0001'),
	('bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb', 'Benedita Ipe', 'Rua Ficticia das Acacias, 14', NULL, NULL);
INSERT INTO sincronizacao VALUES ('lista_de_nomes', '2026-11-18T13:30:00.000Z')
	ON CONFLICT (recurso) DO UPDATE SET baixado_em = excluded.baixado_em;
COMMIT;
BEGIN;
DELETE FROM radar_card;
DELETE FROM radar_cabecalho;
INSERT INTO radar_cabecalho VALUES (1, 3, 1, '2026-11-18T13:31:00.000Z', 27);
INSERT INTO radar_card (casa_token, lista, ordem, situacao, motivo, desde, perfil_contato, eletrodependente, ultimo_pacote_em) VALUES
	('cccccccccccccccccccccccccccccccc', 'pendente', 0, 'Vermelho', 'SemMovimentoNaJanela', '2026-11-18T13:00:00.000Z', 'Sozinho', 0, '2026-11-18T13:30:52.000Z'),
	('aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', 'pendente', 1, 'Cinza', 'SemHeartbeat', '2026-11-18T12:10:00.000Z', 'Sozinho', 1, '2026-11-18T12:10:00.000Z');
INSERT INTO radar_card (casa_token, lista, ordem, situacao, saida_avisada) VALUES
	('bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb', 'aguardando', 0, 'Aguardando', 0);
INSERT INTO sincronizacao VALUES ('radar', '2026-11-18T13:31:01.000Z')
	ON CONFLICT (recurso) DO UPDATE SET baixado_em = excluded.baixado_em;
COMMIT;
INSERT INTO aviso VALUES ('5a3c1e9b-7d2f-4c0a-8e6b-2f4d6a8c0e1b', 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', 'SemHeartbeat', '2026-11-18T12:10:00.000Z', '2026-11-18T12:40:12.000Z', 'EstaBem', '2026-11-18T12:45:00.000Z');
INSERT INTO desfecho (id, casa_token, ocorrida_em, desfecho, observacao) VALUES
	('0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f', 'cccccccccccccccccccccccccccccccc', '2026-11-18T13:40:00.000Z', 'NaoAbriu', 'chamei o SAMU');
SQL

[ "$(sqlite3 "$BANCO" < sqlite/radar_na_tela.sql | wc -l | tr -d ' ')" = 3 ] && ok "confere o radar mostra os 3 cards, na ordem do servidor"
sqlite3 "$BANCO" < sqlite/radar_na_tela.sql | head -1 | grep -q '^pendente|0|cccccccc.*|||||NaoAbriu|2026-11-18T13:40:00.000Z|pendente||$' \
	&& ok "confere card sem nome na lista aparece mesmo assim, com o desfecho local" || { echo "FALHOU card sem nome"; exit 1; }
sqlite3 "$BANCO" < sqlite/radar_na_tela.sql | sed -n 2p | grep -q 'Aparecida Jatoba.*EstaBem$' \
	&& ok "confere card junta nome, vizinho e resposta do aviso do mesmo episodio" || { echo "FALHOU juncao"; exit 1; }
recusa "desfecho com o mesmo id (chave de idempotencia)" "INSERT INTO desfecho (id, casa_token, ocorrida_em, desfecho) VALUES ('0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f', 'cccccccccccccccccccccccccccccccc', '2026-11-18T13:41:00.000Z', 'EstavaBem')"
recusa "desfecho fora do enum" "INSERT INTO desfecho (id, casa_token, ocorrida_em, desfecho) VALUES ('1b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f', 'cccccccccccccccccccccccccccccccc', '2026-11-18T13:41:00.000Z', 'Resolvida')"
recusa "enviado sem registradaEm" "UPDATE desfecho SET estado = 'enviado'"
recusa "enviado que guarda a observacao" "UPDATE desfecho SET estado = 'enviado', registrada_em = '2026-11-18T14:30:00.000Z'"
recusa "card pendente sem motivo" "INSERT INTO radar_card (casa_token, lista, ordem, situacao) VALUES ('dddddddddddddddddddddddddddddddd', 'pendente', 2, 'Vermelho')"
recusa "card aguardando com cor de alerta" "INSERT INTO radar_card (casa_token, lista, ordem, situacao, saida_avisada) VALUES ('dddddddddddddddddddddddddddddddd', 'aguardando', 2, 'Vermelho', 0)"
recusa "segundo cabecalho de radar" "INSERT INTO radar_cabecalho VALUES (2, 3, 1, 'x', 0)"
[ "$(sqlite3 "$BANCO" "SELECT id FROM desfecho WHERE estado = 'pendente' ORDER BY ocorrida_em LIMIT 1")" = "0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f" ] && ok "confere a fila devolve o desfecho pendente mais antigo"
sqlite3 "$BANCO" "UPDATE desfecho SET estado = 'enviado', registrada_em = '2026-11-18T14:30:00.000Z', observacao = NULL, ultimo_erro = NULL WHERE id = '0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f'"
[ "$(sqlite3 "$BANCO" "SELECT count(*) FROM desfecho WHERE estado = 'pendente'")" = 0 ] && ok "confere depois do 201 a fila fica vazia e a observacao sai do celular"
sqlite3 "$BANCO" \
	".parameter set :casa_token \"'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'\"" \
	".parameter set :nome \"'Aparecida Jatoba'\"" \
	".parameter set :endereco \"'Rua Ficticia das Acacias, 12'\"" \
	".parameter set :vizinho_nome NULL" \
	".parameter set :vizinho_telefone NULL" \
	".parameter set :baixado_em \"'2026-11-18T14:00:00.000Z'\"" \
	".read sqlite/trocar_lista_de_nomes.sql"
[ "$(sqlite3 "$BANCO" "SELECT count(*) || ' ' || (SELECT baixado_em FROM sincronizacao WHERE recurso = 'lista_de_nomes') FROM lista_de_nomes")" = "1 2026-11-18T14:00:00.000Z" ] \
	&& ok "confere troca completa da lista tira do celular a casa que revogou e grava a hora" || { echo "FALHOU troca da lista"; exit 1; }
[ "$(sqlite3 "$BANCO" "PRAGMA user_version")" = 1 ] && ok "confere user_version = 1"
sqlite3 "$BANCO" "EXPLAIN QUERY PLAN SELECT id FROM desfecho WHERE estado = 'pendente' ORDER BY ocorrida_em LIMIT 1" | grep -q ix_desfecho_pendente && ok "confere a fila usa ix_desfecho_pendente"
rm -f "$BANCO"
