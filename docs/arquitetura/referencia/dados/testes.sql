-- Prova, num banco novo com ddl/*.sql, que cada restricao recusa o que a invariante proibe.
\set ON_ERROR_STOP on
\set QUIET on
SET client_min_messages = warning;

CREATE FUNCTION pg_temp.recusa(descricao text, comando text, estado text) RETURNS text LANGUAGE plpgsql AS $$
DECLARE obtido text;
BEGIN
	BEGIN
		EXECUTE comando;
	EXCEPTION WHEN OTHERS THEN
		GET STACKED DIAGNOSTICS obtido = RETURNED_SQLSTATE;
		IF obtido = estado THEN RETURN 'ok  recusa  ' || descricao; END IF;
		RAISE EXCEPTION 'FALHOU %: esperava SQLSTATE %, veio %', descricao, estado, obtido;
	END;
	RAISE EXCEPTION 'FALHOU %: o banco aceitou', descricao;
END $$;

CREATE FUNCTION pg_temp.aceita(descricao text, comando text) RETURNS text LANGUAGE plpgsql AS $$
BEGIN
	EXECUTE comando;
	RETURN 'ok  aceita  ' || descricao;
END $$;

CREATE FUNCTION pg_temp.confere(descricao text, condicao boolean) RETURNS text LANGUAGE plpgsql AS $$
BEGIN
	IF condicao IS NOT TRUE THEN RAISE EXCEPTION 'FALHOU %', descricao; END IF;
	RETURN 'ok  confere ' || descricao;
END $$;

\set t1 '''11111111111111111111111111111111'''
\set t2 '''22222222222222222222222222222222'''

-- ===== motor =====
SELECT pg_temp.aceita('casa ativa', $$INSERT INTO motor.casa_monitorada VALUES ('11111111111111111111111111111111', 3, 'Sozinho', true, '2026-11-16 12:00+00', NULL, NULL)$$);
SELECT pg_temp.recusa('Motor: token repetido (AtivarCasa, chave casaToken)', $$INSERT INTO motor.casa_monitorada VALUES ('11111111111111111111111111111111', 3, 'Sozinho', false, now(), NULL, NULL)$$, '23505');
SELECT pg_temp.recusa('Motor: token fora de 32 hex minusculos', $$INSERT INTO motor.casa_monitorada VALUES ('3F9A1C0E5B7D4A2E8C6F1B0D9E7A5C3B', 3, 'Sozinho', false, now(), NULL, NULL)$$, '23514');
SELECT pg_temp.recusa('Motor: perfil de contato desconhecido', $$INSERT INTO motor.casa_monitorada VALUES ('22222222222222222222222222222222', 3, 'Eletrodependente', false, now(), NULL, NULL)$$, '23514');
SELECT pg_temp.aceita('evento de movimento', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:12:40+00', 1842, 'Movimento', '2026-11-18 09:12:41+00', NULL, NULL, false)$$);
SELECT pg_temp.recusa('EventoSensor inv. 1: (token, seq, ts) repetido', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:12:40+00', 1842, 'Movimento', '2026-11-18 09:13:00+00', NULL, NULL, true)$$, '23505');
SELECT pg_temp.aceita('caso 26: o mesmo pacote com ON CONFLICT DO NOTHING conta como sucesso', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:12:40+00', 1842, 'Movimento', '2026-11-18 09:13:00+00', NULL, NULL, true) ON CONFLICT (casa_token, ocorrido_em, seq) DO NOTHING$$);
SELECT pg_temp.aceita('caso 27: seq 1 de novo com outro ts', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:20:00+00', 1, 'Movimento', '2026-11-18 09:20:01+00', NULL, NULL, false), ('11111111111111111111111111111111', '2026-11-18 09:30:00+00', 1, 'Movimento', '2026-11-18 09:30:01+00', NULL, NULL, false)$$);
SELECT pg_temp.confere('um evento por chave', (SELECT count(*) = 3 FROM motor.evento_sensor));
SELECT pg_temp.recusa('EventoSensor inv. 2: heartbeat nunca vira linha', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:13:00+00', 1843, 'Heartbeat', '2026-11-18 09:13:01+00', NULL, NULL, false)$$, '23514');
SELECT pg_temp.recusa('EventoSensor inv. 4: evento de token desconhecido', $$INSERT INTO motor.evento_sensor VALUES ('99999999999999999999999999999999', '2026-11-18 09:13:00+00', 1, 'Movimento', '2026-11-18 09:13:01+00', NULL, NULL, false)$$, '23503');
SELECT pg_temp.recusa('EventoSensor inv. 5: temperatura fora de -10 a 60', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:14:00+00', 1844, 'Telemetria', '2026-11-18 09:14:01+00', 85.0, 127.0, false)$$, '23514');
SELECT pg_temp.recusa('EventoSensor inv. 5: tensao fora de 0 a 260', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:14:00+00', 1844, 'Telemetria', '2026-11-18 09:14:01+00', 31.5, 380.0, false)$$, '23514');
SELECT pg_temp.recusa('contrato 6.2: leitura so em TELEMETRIA', $$INSERT INTO motor.evento_sensor VALUES ('11111111111111111111111111111111', '2026-11-18 09:15:00+00', 1845, 'Movimento', '2026-11-18 09:15:01+00', 31.5, 126.8, false)$$, '23514');
SELECT pg_temp.recusa('EventoSensor imutavel: UPDATE', $$UPDATE motor.evento_sensor SET tipo = 'BotaoBem' WHERE seq = 1842$$, '23001');
SELECT pg_temp.recusa('casa com evento nao pode ser apagada', $$DELETE FROM motor.casa_monitorada$$, '23503');
SELECT pg_temp.aceita('confirmacao da ACS', $$INSERT INTO motor.confirmacao VALUES ('0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f', '11111111111111111111111111111111', 'Acs', 'AusenciaConfirmada', '2026-11-18 13:40+00')$$);
SELECT pg_temp.recusa('CasaMonitorada inv. 4: Confirmacao.Id repetido', $$INSERT INTO motor.confirmacao VALUES ('0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f', '11111111111111111111111111111111', 'Acs', 'PessoaVista', '2026-11-18 13:41+00')$$, '23505');
SELECT pg_temp.recusa('secao 5.7: vizinho nunca manda AusenciaConfirmada', $$INSERT INTO motor.confirmacao VALUES (gen_random_uuid(), '11111111111111111111111111111111', 'Vizinho', 'AusenciaConfirmada', '2026-11-18 13:41+00')$$, '23514');
SELECT pg_temp.aceita('dia critico', $$INSERT INTO motor.dia_critico (data, probabilidade, motivo, origem, aberto_em) VALUES ('2026-11-18', 0.81, 'minima de 24 C pela segunda noite', 'Modelo', '2026-11-18 08:00:03+00')$$);
SELECT pg_temp.recusa('DiaCritico inv. 1: segundo dia critico na mesma data', $$INSERT INTO motor.dia_critico (data, probabilidade, motivo, origem, aberto_em) VALUES ('2026-11-18', 0.5, 'outro', 'Manual', now())$$, '23505');
SELECT pg_temp.recusa('DiaCritico inv. 3: probabilidade fora de 0 a 1', $$UPDATE motor.dia_critico SET probabilidade = 1.2$$, '23514');
SELECT pg_temp.recusa('DiaCritico inv. 3: probabilidade negativa', $$UPDATE motor.dia_critico SET probabilidade = -0.1$$, '23514');
SELECT pg_temp.recusa('DiaCritico inv. 3: motivo vazio', $$UPDATE motor.dia_critico SET motivo = '  '$$, '23514');
SELECT pg_temp.recusa('DiaCritico inv. 3: motivo com 201 caracteres', $$UPDATE motor.dia_critico SET motivo = repeat('a', 201)$$, '22001');
SELECT pg_temp.recusa('DiaCritico inv. 7: encerramento sem resumo', $$UPDATE motor.dia_critico SET encerrado_em = now()$$, '23514');
SELECT pg_temp.recusa('DiaCritico inv. 7: resumo sem encerramento', $$UPDATE motor.dia_critico SET resumo_casas_ativas = 40, resumo_com_sinal_de_vida_ate_10h = 34, resumo_atendidas = 4, resumo_vermelhas_pendentes = 1, resumo_cinzas_pendentes = 1$$, '23514');
SELECT pg_temp.recusa('resumo com mais casas com sinal de vida do que casas ativas', $$UPDATE motor.dia_critico SET encerrado_em = now(), resumo_casas_ativas = 40, resumo_com_sinal_de_vida_ate_10h = 41, resumo_atendidas = 4, resumo_vermelhas_pendentes = 1, resumo_cinzas_pendentes = 1$$, '23514');
SELECT pg_temp.aceita('encerramento com o resumo inteiro', $$UPDATE motor.dia_critico SET encerrado_em = now(), resumo_casas_ativas = 40, resumo_com_sinal_de_vida_ate_10h = 34, resumo_atendidas = 4, resumo_vermelhas_pendentes = 1, resumo_cinzas_pendentes = 1$$);

-- ===== cadastro =====
SELECT pg_temp.aceita('idoso sem consentimento e sem token (caso 32)', $$INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente, casa_endereco) VALUES ('6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 3, 'Aparecida Jatoba', 'Sozinho', true, 'Rua Ficticia das Acacias, 12')$$);
SELECT pg_temp.aceita('segundo idoso', $$INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente, casa_endereco) VALUES ('7d2a1c8f-3e5b-4f9c-8a4d-2b6e8f0a1d3a', 3, 'Benedita Ipe', 'Acamado', false, 'Rua Ficticia das Acacias, 14')$$);
SELECT pg_temp.recusa('Idoso inv. 9: nome vazio', $$INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente, casa_endereco) VALUES (gen_random_uuid(), 3, ' ', 'Sozinho', false, 'Rua Ficticia, 1')$$, '23514');
SELECT pg_temp.recusa('Idoso inv. 9: endereco ausente', $$INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente) VALUES (gen_random_uuid(), 3, 'Carmem Aroeira', 'Sozinho', false)$$, '23502');
SELECT pg_temp.recusa('microarea desconhecida', $$INSERT INTO cadastro.idoso (id, microarea_id, nome, perfil_contato, perfil_eletrodependente, casa_endereco) VALUES (gen_random_uuid(), 99, 'Carmem Aroeira', 'Sozinho', false, 'Rua Ficticia, 1')$$, '23503');
SELECT pg_temp.recusa('caso 38: CPF em claro em condicoes nao entra no banco', $$UPDATE cadastro.idoso SET condicoes = 'documento 123.456.789-00' WHERE nome = 'Aparecida Jatoba'$$, '23514');
SELECT pg_temp.aceita('caso 38: CPF mascarado entra', $$UPDATE cadastro.idoso SET condicoes = 'documento ***.456.***-00' WHERE nome = 'Aparecida Jatoba'$$);
SELECT pg_temp.aceita('consentimento vigente e token', $$INSERT INTO cadastro.consentimento VALUES ('aaaaaaaa-0000-4000-8000-000000000001', '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'acs-demo-01', '2026-11-16 17:00+00', '2026-11', true, false, NULL); UPDATE cadastro.idoso SET casa_token = '11111111111111111111111111111111' WHERE id = '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f'$$);
SELECT pg_temp.recusa('Idoso inv. 2: segundo consentimento vigente', $$INSERT INTO cadastro.consentimento VALUES (gen_random_uuid(), '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'acs-demo-01', now(), '2026-11', true, true, NULL)$$, '23505');
SELECT pg_temp.recusa('o mesmo token em dois idosos', $$UPDATE cadastro.idoso SET casa_token = '11111111111111111111111111111111' WHERE nome = 'Benedita Ipe'$$, '23505');
SELECT pg_temp.aceita('Idoso inv. 2: revogado fica guardado e um novo vigente entra', $$UPDATE cadastro.consentimento SET revogado_em = '2026-11-17 12:00+00'; INSERT INTO cadastro.consentimento VALUES (gen_random_uuid(), '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'acs-demo-01', '2026-11-17 13:00+00', '2026-11', true, false, NULL)$$);
SELECT pg_temp.aceita('vizinho com a data em que aceitou', $$INSERT INTO cadastro.vizinho_de_confianca VALUES ('6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'Rosa Manaca', '(00) 90000-0001', '2026-11-16 17:00+00')$$);
SELECT pg_temp.recusa('Idoso: no maximo um vizinho', $$INSERT INTO cadastro.vizinho_de_confianca VALUES ('6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'Outra Pessoa', '(00) 90000-0002', now())$$, '23505');
SELECT pg_temp.recusa('Idoso inv. 6: vizinho sem a data em que aceitou', $$INSERT INTO cadastro.vizinho_de_confianca (idoso_id, nome, telefone) VALUES ('7d2a1c8f-3e5b-4f9c-8a4d-2b6e8f0a1d3a', 'Outra Pessoa', '(00) 90000-0002')$$, '23502');
SELECT pg_temp.aceita('visita', $$INSERT INTO cadastro.visita VALUES ('0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f', '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'acs-demo-01', '2026-11-18 13:40+00', '2026-11-18 14:30+00', 'EstavaFora', 'vizinha disse que foi a feira')$$);
SELECT pg_temp.recusa('Visita inv. 2 e caso 37: Visita.Id repetido', $$INSERT INTO cadastro.visita VALUES ('0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f', '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'acs-demo-01', now(), now(), 'EstavaBem', NULL)$$, '23505');
SELECT pg_temp.recusa('Visita inv. 1: UPDATE', $$UPDATE cadastro.visita SET desfecho = 'EstavaBem'$$, '23001');
SELECT pg_temp.recusa('Visita inv. 1: DELETE', $$DELETE FROM cadastro.visita$$, '23001');
SELECT pg_temp.recusa('Visita inv. 6: apagar o idoso nao leva a visita junto', $$DELETE FROM cadastro.idoso WHERE id = '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f'$$, '23503');
SELECT pg_temp.recusa('desfecho desconhecido', $$INSERT INTO cadastro.visita VALUES (gen_random_uuid(), '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'acs-demo-01', now(), now(), 'Resolvida', NULL)$$, '23514');
SELECT pg_temp.recusa('CPF em claro na observacao', $$INSERT INTO cadastro.visita VALUES (gen_random_uuid(), '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f', 'acs-demo-01', now(), now(), 'EstavaBem', 'cpf dela 123.456.789-00')$$, '23514');
SELECT pg_temp.aceita('caso 39: mensagem bloqueada so com o texto mascarado', $$INSERT INTO cadastro.mensagem_saida VALUES ('9e7c5a3b-1d0f-4b2e-8c6a-4f2d0b8e6c4a', 'GrupoDoBairro', 'acs-demo-01', 'ele [MORA_SOZINHO] e toma [SAUDE]', 'Bloqueada', ARRAY['MoraSozinho', 'Saude'], '2026-11-18 20:10+00')$$);
SELECT pg_temp.recusa('MensagemSaida inv. 2: bloqueada sem tipo de dado', $$INSERT INTO cadastro.mensagem_saida VALUES (gen_random_uuid(), 'GrupoDoBairro', 'acs-demo-01', 'texto', 'Bloqueada', '{}', now())$$, '23514');
SELECT pg_temp.recusa('tipo de dado fora do enum', $$INSERT INTO cadastro.mensagem_saida VALUES (gen_random_uuid(), 'GrupoDoBairro', 'acs-demo-01', 'texto', 'Bloqueada', ARRAY['Email'], now())$$, '23514');
SELECT pg_temp.recusa('MensagemSaida inv. 4: veredito nao muda', $$UPDATE cadastro.mensagem_saida SET veredito = 'Liberada'$$, '23001');
SELECT pg_temp.aceita('aviso ao vizinho', $$INSERT INTO cadastro.aviso_ao_vizinho (id, casa_token, microarea_id, motivo, desde, registrado_em) VALUES ('5a3c1e9b-7d2f-4c0a-8e6b-2f4d6a8c0e1b', '11111111111111111111111111111111', 3, 'SemMovimentoNaJanela', '2026-11-18 13:00+00', '2026-11-18 13:00:12+00')$$);
SELECT pg_temp.recusa('AvisoAoVizinho inv. 1 e caso 35: episodio repetido', $$INSERT INTO cadastro.aviso_ao_vizinho (id, casa_token, microarea_id, motivo, desde, registrado_em) VALUES (gen_random_uuid(), '11111111111111111111111111111111', 3, 'SemMovimentoNaJanela', '2026-11-18 13:00+00', now())$$, '23505');
SELECT pg_temp.aceita('fluxo 8 passo 7: mesmo motivo com outro Desde e outro episodio', $$INSERT INTO cadastro.aviso_ao_vizinho (id, casa_token, microarea_id, motivo, desde, registrado_em) VALUES (gen_random_uuid(), '11111111111111111111111111111111', 3, 'SemMovimentoNaJanela', '2026-11-18 18:00+00', '2026-11-18 18:00:05+00')$$);
SELECT pg_temp.recusa('resposta sem a hora', $$UPDATE cadastro.aviso_ao_vizinho SET resposta = 'EstaBem' WHERE id = '5a3c1e9b-7d2f-4c0a-8e6b-2f4d6a8c0e1b'$$, '23514');
SELECT pg_temp.recusa('Idoso inv. 1: token sem consentimento vigente, conferido no COMMIT', $$UPDATE cadastro.idoso SET casa_token = '22222222222222222222222222222222' WHERE nome = 'Benedita Ipe'; SET CONSTRAINTS ALL IMMEDIATE$$, '23514');
SELECT pg_temp.recusa('Idoso inv. 1: consentimento vigente sem token', $$INSERT INTO cadastro.consentimento VALUES (gen_random_uuid(), '7d2a1c8f-3e5b-4f9c-8a4d-2b6e8f0a1d3a', 'acs-demo-01', '2026-11-17 13:00+00', '2026-11', false, false, NULL); SET CONSTRAINTS ALL IMMEDIATE$$, '23514');
SELECT pg_temp.recusa('Idoso inv. 8: revogar e deixar o token', $$UPDATE cadastro.consentimento SET revogado_em = '2026-11-18 12:00+00' WHERE idoso_id = '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f' AND revogado_em IS NULL; SET CONSTRAINTS ALL IMMEDIATE$$, '23514');
SELECT pg_temp.aceita('Idoso inv. 8: revogar e apagar o token na mesma transacao', $$UPDATE cadastro.consentimento SET revogado_em = '2026-11-18 12:00+00' WHERE idoso_id = '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f' AND revogado_em IS NULL; UPDATE cadastro.idoso SET casa_token = NULL WHERE id = '6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f'; SET CONSTRAINTS ALL IMMEDIATE$$);
SELECT pg_temp.confere('toda coluna dos tres schemas tem a classificacao no comentario',
	(SELECT count(*) = 0 FROM information_schema.columns c
	 WHERE c.table_schema IN ('motor', 'cadastro', 'guarda')
		AND coalesce(col_description(format('%I.%I', c.table_schema, c.table_name)::regclass, c.ordinal_position), '') !~ '^classe=(pessoal|sensivel|pseudonimizado|pseudonimizado\+sensivel|nenhum)'));

-- ===== guarda =====
SELECT pg_temp.aceita('evento de seguranca', $$INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, rota, severidade) VALUES ('cccccccc-0000-4000-8000-000000000001', '2026-11-18 20:10+00', 'BloqueioDeSaida', ARRAY['MoraSozinho', 'Saude'], 'GrupoDoBairro', 'Cadastro', '/api/mensagens/verificar', 4)$$);
SELECT pg_temp.recusa('EventoDeSeguranca inv. 2: lista de tipos vazia', $$INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, severidade) VALUES (gen_random_uuid(), now(), 'BloqueioDeSaida', '{}', 'Log', 'Cadastro', 3)$$, '23514');
SELECT pg_temp.recusa('EventoDeSeguranca inv. 2: severidade 6', $$INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, severidade) VALUES (gen_random_uuid(), now(), 'BloqueioDeSaida', ARRAY['Cpf'], 'Log', 'Cadastro', 6)$$, '23514');
SELECT pg_temp.recusa('EventoDeSeguranca inv. 1: rota com o id do idoso', $$INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, rota, severidade) VALUES (gen_random_uuid(), now(), 'MascaramentoNaEntrada', ARRAY['Cpf'], 'Banco', 'Cadastro', '/api/idosos/6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f/vizinho', 5)$$, '23514');
SELECT pg_temp.recusa('EventoDeSeguranca inv. 1: rota com o token', $$INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, rota, severidade) VALUES (gen_random_uuid(), now(), 'MascaramentoNaEntrada', ARRAY['Cpf'], 'Banco', 'Motor', '/api/casas/3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b/linha-do-tempo', 5)$$, '23514');
SELECT pg_temp.aceita('rota como modelo', $$INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, rota, severidade) VALUES (gen_random_uuid(), now(), 'MascaramentoNaEntrada', ARRAY['Cpf'], 'Banco', 'Cadastro', '/api/idosos/{id}/vizinho', 5)$$);
SELECT pg_temp.recusa('mensagem sem origem', $$UPDATE guarda.evento_de_seguranca SET mensagem_texto = 'texto' WHERE id = 'cccccccc-0000-4000-8000-000000000001'$$, '23514');
SELECT pg_temp.aceita('EventoDeSeguranca inv. 3: anexar a mensagem', $$UPDATE guarda.evento_de_seguranca SET mensagem_texto = 'Uma mensagem foi barrada.', mensagem_origem = 'FraseFixa' WHERE id = 'cccccccc-0000-4000-8000-000000000001'$$);
SELECT pg_temp.recusa('EventoDeSeguranca inv. 3: anexar de novo', $$UPDATE guarda.evento_de_seguranca SET mensagem_texto = 'Outra.', mensagem_origem = 'Llm' WHERE id = 'cccccccc-0000-4000-8000-000000000001'$$, '23001');
SELECT pg_temp.recusa('EventoDeSeguranca inv. 5: mudar a severidade', $$UPDATE guarda.evento_de_seguranca SET severidade = 1 WHERE mensagem_texto IS NULL$$, '23001');
SELECT pg_temp.recusa('EventoDeSeguranca inv. 5: DELETE', $$DELETE FROM guarda.evento_de_seguranca$$, '23001');
