-- arquivo: consultas/expurgo.sql
-- :corte = agora - Retencao. :tensao_minima = ParametrosDoMotor.TensaoMinima.
BEGIN;

DELETE FROM motor.evento_sensor e
WHERE e.ocorrido_em < :corte
	AND NOT (
		coalesce(e.tensao_v < :tensao_minima, false)
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
WHERE quando < :corte;

DELETE FROM motor.evento_sensor e
USING motor.casa_monitorada m
WHERE m.casa_token = e.casa_token AND m.desativada_em IS NOT NULL;

DELETE FROM motor.confirmacao c
USING motor.casa_monitorada m
WHERE m.casa_token = c.casa_token AND m.desativada_em IS NOT NULL;

COMMIT;
