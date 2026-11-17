-- arquivo: consultas/reiniciar_demo.sql
-- Roda como postgres, depois de PUT /api/demo/relogio com uma hora anterior.
-- :agora = a hora que o RelogioDeDemo acabou de receber, em UTC.
-- Apaga toda linha que uma regra compara com agora e que agora ficou no futuro.
BEGIN;

TRUNCATE motor.evento_sensor, motor.confirmacao;

UPDATE motor.casa_monitorada
SET ultimo_pacote_em = NULL,
	ativada_em = least(ativada_em, :agora)
WHERE desativada_em IS NULL;

DELETE FROM motor.dia_critico;

DELETE FROM cadastro.aviso_ao_vizinho;

TRUNCATE guarda.evento_de_seguranca;

COMMIT;
