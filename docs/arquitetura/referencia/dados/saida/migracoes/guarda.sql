DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'guarda') THEN
        CREATE SCHEMA guarda;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS guarda."__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM guarda."__EFMigrationsHistory" WHERE "migration_id" = '20261117025827_Inicial') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'guarda') THEN
            CREATE SCHEMA guarda;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM guarda."__EFMigrationsHistory" WHERE "migration_id" = '20261117025827_Inicial') THEN
    CREATE TABLE guarda.evento_de_seguranca (
        id uuid NOT NULL,
        ocorrido_em timestamp with time zone NOT NULL,
        tipo text NOT NULL,
        tipos_de_dado text[] NOT NULL,
        canal text NOT NULL,
        modulo character varying(40) NOT NULL,
        rota character varying(200),
        severidade smallint NOT NULL,
        mensagem_texto character varying(400),
        mensagem_origem text,
        CONSTRAINT pk_evento_de_seguranca PRIMARY KEY (id),
        CONSTRAINT ck_evento_de_seguranca_canal CHECK (canal IN ('Log', 'RespostaSemAutenticacao', 'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')),
        CONSTRAINT ck_evento_de_seguranca_mensagem_inteira CHECK ((mensagem_texto IS NULL) = (mensagem_origem IS NULL)),
        CONSTRAINT ck_evento_de_seguranca_mensagem_origem CHECK (mensagem_origem IN ('Llm', 'FraseFixa')),
        CONSTRAINT ck_evento_de_seguranca_modulo CHECK (btrim(modulo) <> ''),
        CONSTRAINT ck_evento_de_seguranca_rota_sem_identificador CHECK (rota !~ '[?]|[0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-'),
        CONSTRAINT ck_evento_de_seguranca_severidade CHECK (severidade BETWEEN 1 AND 5),
        CONSTRAINT ck_evento_de_seguranca_tipo CHECK (tipo IN ('MascaramentoNaEntrada', 'BloqueioDeSaida')),
        CONSTRAINT ck_evento_de_seguranca_tipos_de_dado CHECK (cardinality(tipos_de_dado) > 0 AND tipos_de_dado <@ ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial'])
    );
    COMMENT ON COLUMN guarda.evento_de_seguranca.id IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.ocorrido_em IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.tipo IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.tipos_de_dado IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.canal IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.modulo IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.rota IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.severidade IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.mensagem_texto IS 'classe=nenhum';
    COMMENT ON COLUMN guarda.evento_de_seguranca.mensagem_origem IS 'classe=nenhum';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM guarda."__EFMigrationsHistory" WHERE "migration_id" = '20261117025827_Inicial') THEN
    CREATE INDEX ix_evento_de_seguranca_ocorrido_em ON guarda.evento_de_seguranca (ocorrido_em DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM guarda."__EFMigrationsHistory" WHERE "migration_id" = '20261117025827_Inicial') THEN
    INSERT INTO guarda."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117025827_Inicial', '10.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM guarda."__EFMigrationsHistory" WHERE "migration_id" = '20261117025837_Gatilhos') THEN
    -- arquivo: ddl/guarda_gatilhos.sql. Entra na migracao Gatilhos do GuardaDbContext.
    CREATE FUNCTION guarda.proteger_evento() RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN
    	IF TG_OP = 'DELETE' THEN
    		RAISE EXCEPTION 'guarda.evento_de_seguranca nao aceita DELETE'
    			USING ERRCODE = 'restrict_violation';
    	END IF;
    	IF OLD.mensagem_texto IS NOT NULL
    		OR (NEW.id, NEW.ocorrido_em, NEW.tipo, NEW.tipos_de_dado, NEW.canal, NEW.modulo, NEW.rota, NEW.severidade)
    			IS DISTINCT FROM
    			(OLD.id, OLD.ocorrido_em, OLD.tipo, OLD.tipos_de_dado, OLD.canal, OLD.modulo, OLD.rota, OLD.severidade)
    	THEN
    		RAISE EXCEPTION 'guarda.evento_de_seguranca so aceita anexar a mensagem uma vez'
    			USING ERRCODE = 'restrict_violation';
    	END IF;
    	RETURN NEW;
    END $$;

    CREATE TRIGGER evento_de_seguranca_protegido BEFORE UPDATE OR DELETE ON guarda.evento_de_seguranca
    	FOR EACH ROW EXECUTE FUNCTION guarda.proteger_evento();

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM guarda."__EFMigrationsHistory" WHERE "migration_id" = '20261117025837_Gatilhos') THEN
    INSERT INTO guarda."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117025837_Gatilhos', '10.0.15');
    END IF;
END $EF$;
