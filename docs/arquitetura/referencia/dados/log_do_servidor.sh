#!/usr/bin/env bash
# Mostra o que o Postgres grava no proprio log quando uma restricao recusa uma linha com dado pessoal.
set -uo pipefail
p() { docker exec -i corredor-dados psql -U postgres -d teste -q "$@"; }
recusada="INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente, casa_endereco, condicoes) VALUES (gen_random_uuid(), 3, 'Dirce Aroeira', 'Sozinho', false, 'Rua do Sabia, 30 - Vila Exemplo', 'doc 123.456.789-00')"
p -c "ALTER SYSTEM RESET log_error_verbosity" -c "ALTER SYSTEM RESET log_min_error_statement" -c "SELECT pg_reload_conf()" >/dev/null; sleep 1
p -c "$recusada" >/dev/null 2>&1
sleep 1; echo "--- log_error_verbosity padrao:"; docker logs corredor-dados 2>&1 | grep -A2 "ck_idoso_sem_cpf_em_claro" | tail -3
p -c "ALTER SYSTEM SET log_error_verbosity = 'terse'" -c "ALTER SYSTEM SET log_min_error_statement = 'panic'" -c "SELECT pg_reload_conf()" >/dev/null; sleep 1
p -c "$recusada" >/dev/null 2>&1
sleep 1; echo "--- log_error_verbosity = terse e log_min_error_statement = panic:"; docker logs corredor-dados 2>&1 | tail -1
p -c "ALTER SYSTEM RESET log_error_verbosity" -c "ALTER SYSTEM RESET log_min_error_statement" -c "SELECT pg_reload_conf()" >/dev/null
