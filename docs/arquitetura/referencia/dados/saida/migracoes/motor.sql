DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'motor') THEN
        CREATE SCHEMA motor;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS motor."__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'motor') THEN
            CREATE SCHEMA motor;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
    CREATE TABLE motor.casa_monitorada (
        casa_token text NOT NULL,
        microarea_id integer NOT NULL,
        ativada_em timestamp with time zone NOT NULL,
        desativada_em timestamp with time zone,
        ultimo_pacote_em timestamp with time zone,
        perfil_contato text NOT NULL,
        perfil_eletrodependente boolean NOT NULL,
        CONSTRAINT pk_casa_monitorada PRIMARY KEY (casa_token),
        CONSTRAINT ck_casa_monitorada_microarea CHECK (microarea_id > 0),
        CONSTRAINT ck_casa_monitorada_perfil_contato CHECK (perfil_contato IN ('Sozinho', 'Acamado', 'Coabitado')),
        CONSTRAINT ck_casa_monitorada_token CHECK (casa_token ~ '^[0-9a-f]{32}$')
    );
    COMMENT ON COLUMN motor.casa_monitorada.casa_token IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.casa_monitorada.microarea_id IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.casa_monitorada.ativada_em IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.casa_monitorada.desativada_em IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.casa_monitorada.ultimo_pacote_em IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.casa_monitorada.perfil_contato IS 'classe=pseudonimizado+sensivel';
    COMMENT ON COLUMN motor.casa_monitorada.perfil_eletrodependente IS 'classe=pseudonimizado+sensivel';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
    CREATE TABLE motor.dia_critico (
        data date NOT NULL,
        probabilidade numeric(4,3) NOT NULL,
        motivo character varying(200) NOT NULL,
        origem text NOT NULL,
        aberto_em timestamp with time zone NOT NULL,
        toque_pedido_em timestamp with time zone,
        encerrado_em timestamp with time zone,
        resumo_casas_ativas integer,
        resumo_com_sinal_de_vida_ate_10h integer,
        resumo_atendidas integer,
        resumo_vermelhas_pendentes integer,
        resumo_cinzas_pendentes integer,
        CONSTRAINT pk_dia_critico PRIMARY KEY (data),
        CONSTRAINT ck_dia_critico_motivo CHECK (btrim(motivo) <> ''),
        CONSTRAINT ck_dia_critico_origem CHECK (origem IN ('Modelo', 'RegraBase', 'Manual')),
        CONSTRAINT ck_dia_critico_probabilidade CHECK (probabilidade BETWEEN 0 AND 1),
        CONSTRAINT ck_dia_critico_resumo_faixa CHECK (resumo_casas_ativas >= 0 AND resumo_com_sinal_de_vida_ate_10h BETWEEN 0 AND resumo_casas_ativas AND resumo_atendidas >= 0 AND resumo_vermelhas_pendentes >= 0 AND resumo_cinzas_pendentes >= 0 AND resumo_atendidas + resumo_vermelhas_pendentes + resumo_cinzas_pendentes <= resumo_casas_ativas),
        CONSTRAINT ck_dia_critico_resumo_so_no_encerramento CHECK (num_nulls(encerrado_em, resumo_casas_ativas, resumo_com_sinal_de_vida_ate_10h, resumo_atendidas, resumo_vermelhas_pendentes, resumo_cinzas_pendentes) IN (0, 6))
    );
    COMMENT ON COLUMN motor.dia_critico.data IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.probabilidade IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.motivo IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.origem IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.aberto_em IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.toque_pedido_em IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.encerrado_em IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.resumo_casas_ativas IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.resumo_com_sinal_de_vida_ate_10h IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.resumo_atendidas IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.resumo_vermelhas_pendentes IS 'classe=nenhum';
    COMMENT ON COLUMN motor.dia_critico.resumo_cinzas_pendentes IS 'classe=nenhum';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
    CREATE TABLE motor.confirmacao (
        id uuid NOT NULL,
        origem text NOT NULL,
        resultado text NOT NULL,
        quando timestamp with time zone NOT NULL,
        casa_token text NOT NULL,
        CONSTRAINT pk_confirmacao PRIMARY KEY (id),
        CONSTRAINT ck_confirmacao_origem CHECK (origem IN ('Acs', 'Vizinho')),
        CONSTRAINT ck_confirmacao_resultado CHECK (resultado IN ('PessoaVista', 'AusenciaConfirmada')),
        CONSTRAINT ck_confirmacao_vizinho_so_ve_pessoa CHECK (origem = 'Acs' OR resultado = 'PessoaVista'),
        CONSTRAINT fk_confirmacao_casa_monitorada FOREIGN KEY (casa_token) REFERENCES motor.casa_monitorada (casa_token) ON DELETE CASCADE
    );
    COMMENT ON COLUMN motor.confirmacao.id IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.confirmacao.origem IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.confirmacao.resultado IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.confirmacao.quando IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.confirmacao.casa_token IS 'classe=pseudonimizado';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
    CREATE TABLE motor.evento_sensor (
        casa_token text NOT NULL,
        seq bigint NOT NULL,
        ocorrido_em timestamp with time zone NOT NULL,
        tipo text NOT NULL,
        recebido_em timestamp with time zone NOT NULL,
        temp_c numeric(4,1),
        tensao_v numeric(5,1),
        bufferizado boolean NOT NULL,
        CONSTRAINT pk_evento_sensor PRIMARY KEY (casa_token, ocorrido_em, seq),
        CONSTRAINT ck_evento_sensor_leitura_so_na_telemetria CHECK (tipo = 'Telemetria' OR (temp_c IS NULL AND tensao_v IS NULL)),
        CONSTRAINT ck_evento_sensor_seq CHECK (seq >= 0),
        CONSTRAINT ck_evento_sensor_temp_c CHECK (temp_c BETWEEN -10 AND 60),
        CONSTRAINT ck_evento_sensor_tensao_v CHECK (tensao_v BETWEEN 0 AND 260),
        CONSTRAINT ck_evento_sensor_tipo CHECK (tipo IN ('Movimento', 'BotaoBem', 'BotaoSair', 'Telemetria')),
        CONSTRAINT fk_evento_sensor_casa_monitorada FOREIGN KEY (casa_token) REFERENCES motor.casa_monitorada (casa_token) ON DELETE RESTRICT
    );
    COMMENT ON COLUMN motor.evento_sensor.casa_token IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.evento_sensor.seq IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.evento_sensor.ocorrido_em IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.evento_sensor.tipo IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.evento_sensor.recebido_em IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.evento_sensor.temp_c IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.evento_sensor.tensao_v IS 'classe=pseudonimizado';
    COMMENT ON COLUMN motor.evento_sensor.bufferizado IS 'classe=pseudonimizado';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
    CREATE INDEX ix_casa_monitorada_microarea_ativa ON motor.casa_monitorada (microarea_id) WHERE desativada_em IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
    CREATE INDEX ix_confirmacao_casa_quando ON motor.confirmacao (casa_token, quando);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025823_Inicial') THEN
    INSERT INTO motor."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117025823_Inicial', '10.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025834_Gatilhos') THEN
    -- arquivo: ddl/motor_gatilhos.sql. Entra na migracao Gatilhos do MotorDbContext.
    CREATE FUNCTION motor.recusar_alteracao() RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN
    	RAISE EXCEPTION 'a tabela %.% nao aceita %', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP
    		USING ERRCODE = 'restrict_violation';
    END $$;

    CREATE TRIGGER evento_sensor_sem_update BEFORE UPDATE ON motor.evento_sensor
    	FOR EACH ROW EXECUTE FUNCTION motor.recusar_alteracao();

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM motor."__EFMigrationsHistory" WHERE "migration_id" = '20261117025834_Gatilhos') THEN
    INSERT INTO motor."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117025834_Gatilhos', '10.0.15');
    END IF;
END $EF$;
