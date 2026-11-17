-- Volume para os planos: 20 microareas x 40 casas, 24 h de eventos. Dados inventados por generate_series.
-- Os tokens saem de md5() so para este teste. Na aplicacao o token vem de CasaToken.Novo().
\set agora '''2026-11-18 13:31:00+00'''
-- Uma transacao: o gatilho de restricao confere token e consentimento no COMMIT.
BEGIN;

INSERT INTO cadastro.microarea (id, nome, acs_responsavel)
SELECT m, 'Microarea ' || m, 'acs-volume-' || lpad(m::text, 2, '0')
FROM generate_series(1, 20) m WHERE m <> 3;

CREATE TEMP TABLE casas AS
SELECT m, n, md5('casa-' || m || '-' || n) AS token, gen_random_uuid() AS idoso_id,
	CASE WHEN n % 10 = 0 THEN 'Acamado' WHEN n % 7 = 0 THEN 'Coabitado' ELSE 'Sozinho' END AS contato,
	n % 13 = 0 AS eletro
FROM generate_series(1, 20) m, generate_series(1, 40) n;

INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente, casa_endereco, casa_token)
SELECT idoso_id, m, 'Pessoa Ficticia ' || m || '-' || n, contato, eletro, 'Rua Ficticia ' || n || ', ' || m, token FROM casas;

INSERT INTO cadastro.consentimento (id, idoso_id, colhido_por, colhido_em, versao_do_termo,
	autorizacoes_avisar_vizinho, autorizacoes_avisar_familia)
SELECT gen_random_uuid(), idoso_id, 'acs-volume-' || lpad(m::text, 2, '0'), :agora::timestamptz - interval '3 days', '2026-11', n % 4 <> 0, false FROM casas;

INSERT INTO cadastro.vizinho_de_confianca (idoso_id, nome, telefone, aceitou_em)
SELECT idoso_id, 'Vizinho Ficticio ' || n, '(00) 90000-' || lpad(n::text, 4, '0'), :agora::timestamptz - interval '3 days' FROM casas WHERE n % 4 <> 0;

INSERT INTO cadastro.aviso_ao_vizinho (id, casa_token, microarea_id, motivo, desde, registrado_em)
SELECT gen_random_uuid(), token, m, 'SemMovimentoNaJanela', :agora::timestamptz - d * interval '1 day' - interval '31 minutes', :agora::timestamptz - d * interval '1 day' - interval '30 minutes'
FROM casas, generate_series(0, 29) d WHERE n % 8 = 0;

INSERT INTO motor.casa_monitorada (casa_token, microarea_id, perfil_contato, perfil_eletrodependente, ativada_em, ultimo_pacote_em)
SELECT token, m, contato, eletro, :agora::timestamptz - interval '3 days', :agora::timestamptz - interval '10 seconds' FROM casas;

INSERT INTO motor.evento_sensor (casa_token, ocorrido_em, seq, tipo, recebido_em, temp_c, tensao_v, bufferizado)
SELECT token, t, row_number() OVER (PARTITION BY token ORDER BY t), 'Telemetria', t + interval '1 second', 29.5 + (n % 9), 127.0, false
FROM casas, generate_series(:agora::timestamptz - interval '24 hours' + interval '1 minute', :agora::timestamptz, interval '5 minutes') t;

INSERT INTO motor.evento_sensor (casa_token, ocorrido_em, seq, tipo, recebido_em, bufferizado)
SELECT token, t + (n || ' seconds')::interval, 100000 + row_number() OVER (PARTITION BY token ORDER BY t), 'Movimento', t + (n || ' seconds')::interval, false
FROM casas, generate_series(:agora::timestamptz - interval '24 hours' + interval '7 minutes', :agora::timestamptz - interval '1 minute', interval '37 minutes') t;

INSERT INTO motor.confirmacao (id, casa_token, origem, resultado, quando)
SELECT gen_random_uuid(), token, 'Acs', 'PessoaVista', :agora::timestamptz - interval '3 hours' FROM casas WHERE n % 9 = 0;

INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, rota, severidade)
SELECT gen_random_uuid(), :agora::timestamptz - (i * interval '13 minutes'),
	CASE WHEN i % 3 = 0 THEN 'BloqueioDeSaida' ELSE 'MascaramentoNaEntrada' END,
	CASE WHEN i % 3 = 0 THEN ARRAY['MoraSozinho', 'Saude'] ELSE ARRAY['Cpf'] END,
	CASE WHEN i % 3 = 0 THEN 'GrupoDoBairro' ELSE 'Banco' END,
	'Cadastro', '/api/idosos', CASE WHEN i % 3 = 0 THEN 4 ELSE 5 END
FROM generate_series(1, 3000) i;

COMMIT;

ANALYZE;
SELECT 'motor.casa_monitorada' AS tabela, count(*) FROM motor.casa_monitorada
UNION ALL SELECT 'motor.evento_sensor', count(*) FROM motor.evento_sensor
UNION ALL SELECT 'cadastro.idoso', count(*) FROM cadastro.idoso
UNION ALL SELECT 'cadastro.aviso_ao_vizinho', count(*) FROM cadastro.aviso_ao_vizinho
UNION ALL SELECT 'guarda.evento_de_seguranca', count(*) FROM guarda.evento_de_seguranca;
