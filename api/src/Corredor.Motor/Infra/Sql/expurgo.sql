-- O texto conferido de docs/arquitetura/referencia/dados/consultas/expurgo.sql, sem BEGIN e COMMIT.
-- O Expurgo roda numa transacao. {0} = agora - Retencao, em UTC. {1} = ParametrosDoMotor.TensaoMinima.
-- Os eventos saem antes das confirmacoes: sem a confirmacao da ACS, a leitura baixa que ela soltou nunca mais sairia.
DELETE FROM motor.evento_sensor e
WHERE e.ocorrido_em < {0}
	AND NOT (
		coalesce(e.tensao_v < {1}, false)
		AND EXISTS (
			SELECT 1 FROM motor.casa_monitorada m
			WHERE m.casa_token = e.casa_token
				AND m.desativada_em IS NULL
				AND m.perfil_eletrodependente)
		AND NOT EXISTS (
			SELECT 1 FROM motor.confirmacao c
			WHERE c.casa_token = e.casa_token
				AND c.origem = 'Acs'
				AND c.resultado = 'PessoaVista'
				AND c.quando > e.ocorrido_em));

DELETE FROM motor.confirmacao
WHERE quando < {0};

DELETE FROM motor.evento_sensor e
USING motor.casa_monitorada m
WHERE m.casa_token = e.casa_token AND m.desativada_em IS NOT NULL;

DELETE FROM motor.confirmacao c
USING motor.casa_monitorada m
WHERE m.casa_token = c.casa_token AND m.desativada_em IS NOT NULL;
