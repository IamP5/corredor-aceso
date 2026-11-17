-- Prova o Expurgo e a excecao da leitura de tensao baixa (caso 31). Agora = 2026-11-19 03:40 UTC.
\set ON_ERROR_STOP on
\set QUIET on
\set corte '''2026-11-18 03:40:00+00'''
\set tensao_minima 100

INSERT INTO motor.casa_monitorada (casa_token, microarea_id, perfil_contato, perfil_eletrodependente, ativada_em, desativada_em) VALUES
	('e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1', 3, 'Sozinho', true, '2026-11-10 12:00+00', NULL),
	('e2e2e2e2e2e2e2e2e2e2e2e2e2e2e2e2', 3, 'Sozinho', true, '2026-11-10 12:00+00', NULL),
	('e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3', 3, 'Acamado', true, '2026-11-10 12:00+00', NULL),
	('a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1', 3, 'Sozinho', false, '2026-11-10 12:00+00', NULL),
	('d1d1d1d1d1d1d1d1d1d1d1d1d1d1d1d1', 3, 'Sozinho', true, '2026-11-10 12:00+00', '2026-11-19 01:00+00');

INSERT INTO motor.evento_sensor (casa_token, ocorrido_em, seq, tipo, recebido_em, temp_c, tensao_v, bufferizado) VALUES
	('e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1', '2026-11-18 02:40+00', 1, 'Telemetria', '2026-11-18 02:40:01+00', 30.0, 0.0, false),
	('e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1', '2026-11-18 02:45+00', 2, 'Telemetria', '2026-11-18 02:45:01+00', 30.0, 127.0, false),
	('e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1', '2026-11-18 02:50+00', 3, 'Movimento', '2026-11-18 02:50:01+00', NULL, NULL, false),
	('e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1', '2026-11-19 03:00+00', 4, 'Movimento', '2026-11-19 03:00:01+00', NULL, NULL, false),
	('e2e2e2e2e2e2e2e2e2e2e2e2e2e2e2e2', '2026-11-18 02:40+00', 1, 'Telemetria', '2026-11-18 02:40:01+00', 30.0, 0.0, false),
	('e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3', '2026-11-18 02:40+00', 1, 'Telemetria', '2026-11-18 02:40:01+00', 30.0, 0.0, false),
	('a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1', '2026-11-18 02:40+00', 1, 'Telemetria', '2026-11-18 02:40:01+00', 30.0, 0.0, false),
	('d1d1d1d1d1d1d1d1d1d1d1d1d1d1d1d1', '2026-11-19 00:30+00', 1, 'Telemetria', '2026-11-19 00:30:01+00', 30.0, 0.0, false);

INSERT INTO motor.confirmacao (id, casa_token, origem, resultado, quando) VALUES
	(gen_random_uuid(), 'e2e2e2e2e2e2e2e2e2e2e2e2e2e2e2e2', 'Acs', 'PessoaVista', '2026-11-18 02:50+00'),
	(gen_random_uuid(), 'e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3', 'Vizinho', 'PessoaVista', '2026-11-18 02:55+00'),
	(gen_random_uuid(), 'e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3', 'Acs', 'AusenciaConfirmada', '2026-11-18 23:00+00'),
	(gen_random_uuid(), 'd1d1d1d1d1d1d1d1d1d1d1d1d1d1d1d1', 'Acs', 'PessoaVista', '2026-11-19 00:40+00');

\i consultas/expurgo.sql
\i consultas/expurgo.sql

SELECT CASE WHEN ok THEN 'ok  confere ' ELSE 'FALHOU ' END || descricao FROM (VALUES
	('caso 31: leitura baixa de 25 h sem confirmacao da ACS fica',
		EXISTS (SELECT 1 FROM motor.evento_sensor WHERE casa_token LIKE 'e1%' AND seq = 1)),
	('leitura normal e movimento com mais de 24 h somem',
		NOT EXISTS (SELECT 1 FROM motor.evento_sensor WHERE casa_token LIKE 'e1%' AND seq IN (2, 3))),
	('evento com menos de 24 h fica',
		EXISTS (SELECT 1 FROM motor.evento_sensor WHERE casa_token LIKE 'e1%' AND seq = 4)),
	('leitura baixa com confirmacao da ACS depois dela some, mesmo com a confirmacao vencendo na mesma rodada',
		NOT EXISTS (SELECT 1 FROM motor.evento_sensor WHERE casa_token LIKE 'e2%')),
	('confirmacao com mais de 24 h some',
		NOT EXISTS (SELECT 1 FROM motor.confirmacao WHERE casa_token LIKE 'e2%')),
	('vizinho e AusenciaConfirmada nao soltam a trava: a leitura fica',
		EXISTS (SELECT 1 FROM motor.evento_sensor WHERE casa_token LIKE 'e3%')),
	('leitura baixa de casa que nao e eletrodependente some',
		NOT EXISTS (SELECT 1 FROM motor.evento_sensor WHERE casa_token LIKE 'a1%')),
	('casa desativada fica sem evento e sem confirmacao',
		NOT EXISTS (SELECT 1 FROM motor.evento_sensor WHERE casa_token LIKE 'd1%')
		AND NOT EXISTS (SELECT 1 FROM motor.confirmacao WHERE casa_token LIKE 'd1%')),
	('rodar duas vezes deixa 3 eventos e 1 confirmacao',
		(SELECT count(*) FROM motor.evento_sensor WHERE casa_token ~ '^(e|a|d)') = 3
		AND (SELECT count(*) FROM motor.confirmacao WHERE casa_token ~ '^(e|a|d)') = 1)
) AS t(descricao, ok);

\set agora '''2026-11-18 12:58:00+00'''
\i consultas/reiniciar_demo.sql
SELECT CASE WHEN ok THEN 'ok  confere ' ELSE 'FALHOU ' END || descricao FROM (VALUES
	('reinicio da demo: sem eventos, confirmacoes, dia critico, avisos e eventos de seguranca, ultimo pacote vazio',
		NOT EXISTS (SELECT 1 FROM motor.evento_sensor) AND NOT EXISTS (SELECT 1 FROM motor.confirmacao)
		AND NOT EXISTS (SELECT 1 FROM motor.dia_critico) AND NOT EXISTS (SELECT 1 FROM cadastro.aviso_ao_vizinho)
		AND NOT EXISTS (SELECT 1 FROM guarda.evento_de_seguranca)
		AND NOT EXISTS (SELECT 1 FROM motor.casa_monitorada WHERE desativada_em IS NULL AND ultimo_pacote_em IS NOT NULL)),
	('reinicio da demo: nenhuma casa ativa fica com ativada_em depois do agora novo',
		NOT EXISTS (SELECT 1 FROM motor.casa_monitorada WHERE desativada_em IS NULL AND ativada_em > :agora)),
	('reinicio da demo: visitas, idosos e mensagens ficam',
		EXISTS (SELECT 1 FROM cadastro.visita) AND EXISTS (SELECT 1 FROM cadastro.idoso) AND EXISTS (SELECT 1 FROM cadastro.mensagem_saida))
) AS t(descricao, ok);
