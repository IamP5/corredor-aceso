-- arquivo: ddl/motor_gatilhos.sql. Entra na migracao Gatilhos do MotorDbContext.
CREATE FUNCTION motor.recusar_alteracao() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
	RAISE EXCEPTION 'a tabela %.% nao aceita %', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP
		USING ERRCODE = 'restrict_violation';
END $$;

CREATE TRIGGER evento_sensor_sem_update BEFORE UPDATE ON motor.evento_sensor
	FOR EACH ROW EXECUTE FUNCTION motor.recusar_alteracao();
