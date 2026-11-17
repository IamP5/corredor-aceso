DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'cadastro') THEN
        CREATE SCHEMA cadastro;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS cadastro."__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'cadastro') THEN
            CREATE SCHEMA cadastro;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE TABLE cadastro.mensagem_saida (
        id uuid NOT NULL,
        canal text NOT NULL,
        acs_id character varying(40) NOT NULL,
        texto_mascarado character varying(1000) NOT NULL,
        veredito text NOT NULL,
        tipos_de_dado text[] NOT NULL,
        verificada_em timestamp with time zone NOT NULL,
        CONSTRAINT pk_mensagem_saida PRIMARY KEY (id),
        CONSTRAINT ck_mensagem_saida_acs_id CHECK (acs_id ~ '^[a-z0-9-]{3,40}$'),
        CONSTRAINT ck_mensagem_saida_bloqueada_tem_tipo CHECK (veredito = 'Liberada' OR cardinality(tipos_de_dado) > 0),
        CONSTRAINT ck_mensagem_saida_canal CHECK (canal IN ('Log', 'RespostaSemAutenticacao', 'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')),
        CONSTRAINT ck_mensagem_saida_sem_cpf_em_claro CHECK (texto_mascarado !~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}'),
        CONSTRAINT ck_mensagem_saida_tipos_de_dado CHECK (tipos_de_dado <@ ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial']),
        CONSTRAINT ck_mensagem_saida_veredito CHECK (veredito IN ('Liberada', 'Bloqueada'))
    );
    COMMENT ON COLUMN cadastro.mensagem_saida.id IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.mensagem_saida.canal IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.mensagem_saida.acs_id IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.mensagem_saida.texto_mascarado IS 'classe=pessoal; mascarado';
    COMMENT ON COLUMN cadastro.mensagem_saida.veredito IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.mensagem_saida.tipos_de_dado IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.mensagem_saida.verificada_em IS 'classe=nenhum';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE TABLE cadastro.microarea (
        id integer NOT NULL,
        nome character varying(80) NOT NULL,
        acs_responsavel character varying(40) NOT NULL,
        CONSTRAINT pk_microarea PRIMARY KEY (id),
        CONSTRAINT ck_microarea_acs_responsavel CHECK (acs_responsavel ~ '^[a-z0-9-]{3,40}$'),
        CONSTRAINT ck_microarea_id CHECK (id > 0),
        CONSTRAINT ck_microarea_nome CHECK (btrim(nome) <> '')
    );
    COMMENT ON COLUMN cadastro.microarea.id IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.microarea.nome IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.microarea.acs_responsavel IS 'classe=pessoal';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE TABLE cadastro.aviso_ao_vizinho (
        id uuid NOT NULL,
        casa_token text NOT NULL,
        microarea_id integer NOT NULL,
        motivo text NOT NULL,
        desde timestamp with time zone NOT NULL,
        registrado_em timestamp with time zone NOT NULL,
        resposta text,
        respondido_em timestamp with time zone,
        CONSTRAINT pk_aviso_ao_vizinho PRIMARY KEY (id),
        CONSTRAINT ck_aviso_ao_vizinho_casa_token CHECK (casa_token ~ '^[0-9a-f]{32}$'),
        CONSTRAINT ck_aviso_ao_vizinho_motivo CHECK (motivo IN ('EnergiaEletrodependente', 'SemHeartbeat', 'CalorInterno', 'SemMovimentoNaJanela')),
        CONSTRAINT ck_aviso_ao_vizinho_resposta CHECK (resposta IN ('EstaBem', 'PrecisaDeAjuda')),
        CONSTRAINT ck_aviso_ao_vizinho_resposta_com_hora CHECK ((resposta IS NULL) = (respondido_em IS NULL)),
        CONSTRAINT fk_aviso_ao_vizinho_microarea FOREIGN KEY (microarea_id) REFERENCES cadastro.microarea (id) ON DELETE RESTRICT
    );
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.id IS 'classe=pseudonimizado';
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.casa_token IS 'classe=pseudonimizado';
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.microarea_id IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.motivo IS 'classe=pseudonimizado+sensivel';
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.desde IS 'classe=pseudonimizado';
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.registrado_em IS 'classe=pseudonimizado';
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.resposta IS 'classe=pseudonimizado';
    COMMENT ON COLUMN cadastro.aviso_ao_vizinho.respondido_em IS 'classe=pseudonimizado';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE TABLE cadastro.idoso (
        id uuid NOT NULL,
        microarea_id integer NOT NULL,
        nome character varying(120) NOT NULL,
        condicoes character varying(500),
        medicamentos character varying(500),
        casa_endereco character varying(200) NOT NULL,
        casa_token text,
        perfil_contato text NOT NULL,
        perfil_eletrodependente boolean NOT NULL,
        CONSTRAINT pk_idoso PRIMARY KEY (id),
        CONSTRAINT ck_idoso_casa_endereco CHECK (btrim(casa_endereco) <> ''),
        CONSTRAINT ck_idoso_casa_token CHECK (casa_token ~ '^[0-9a-f]{32}$'),
        CONSTRAINT ck_idoso_nome CHECK (btrim(nome) <> ''),
        CONSTRAINT ck_idoso_perfil_contato CHECK (perfil_contato IN ('Sozinho', 'Acamado', 'Coabitado')),
        CONSTRAINT ck_idoso_sem_cpf_em_claro CHECK (concat_ws(' ', nome, casa_endereco, condicoes, medicamentos) !~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}'),
        CONSTRAINT fk_idoso_microarea FOREIGN KEY (microarea_id) REFERENCES cadastro.microarea (id) ON DELETE RESTRICT
    );
    COMMENT ON COLUMN cadastro.idoso.id IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.idoso.microarea_id IS 'classe=nenhum';
    COMMENT ON COLUMN cadastro.idoso.nome IS 'classe=pessoal; rotulo=Nome';
    COMMENT ON COLUMN cadastro.idoso.condicoes IS 'classe=sensivel; rotulo=Saude';
    COMMENT ON COLUMN cadastro.idoso.medicamentos IS 'classe=sensivel; rotulo=Saude';
    COMMENT ON COLUMN cadastro.idoso.casa_endereco IS 'classe=pessoal; rotulo=Endereco';
    COMMENT ON COLUMN cadastro.idoso.casa_token IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.idoso.perfil_contato IS 'classe=sensivel';
    COMMENT ON COLUMN cadastro.idoso.perfil_eletrodependente IS 'classe=sensivel';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE TABLE cadastro.consentimento (
        id uuid NOT NULL,
        colhido_por character varying(40) NOT NULL,
        colhido_em timestamp with time zone NOT NULL,
        versao_do_termo character varying(20) NOT NULL,
        autorizacoes_avisar_vizinho boolean NOT NULL,
        autorizacoes_avisar_familia boolean NOT NULL,
        revogado_em timestamp with time zone,
        idoso_id uuid NOT NULL,
        CONSTRAINT pk_consentimento PRIMARY KEY (id),
        CONSTRAINT ck_consentimento_colhido_por CHECK (colhido_por ~ '^[a-z0-9-]{3,40}$'),
        CONSTRAINT ck_consentimento_versao_do_termo CHECK (btrim(versao_do_termo) <> ''),
        CONSTRAINT fk_consentimento_idoso FOREIGN KEY (idoso_id) REFERENCES cadastro.idoso (id) ON DELETE CASCADE
    );
    COMMENT ON COLUMN cadastro.consentimento.id IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.consentimento.colhido_por IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.consentimento.colhido_em IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.consentimento.versao_do_termo IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.consentimento.autorizacoes_avisar_vizinho IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.consentimento.autorizacoes_avisar_familia IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.consentimento.revogado_em IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.consentimento.idoso_id IS 'classe=pessoal';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE TABLE cadastro.visita (
        id uuid NOT NULL,
        idoso_id uuid NOT NULL,
        acs_id character varying(40) NOT NULL,
        ocorrida_em timestamp with time zone NOT NULL,
        registrada_em timestamp with time zone NOT NULL,
        desfecho text NOT NULL,
        observacao character varying(500),
        CONSTRAINT pk_visita PRIMARY KEY (id),
        CONSTRAINT ck_visita_acs_id CHECK (acs_id ~ '^[a-z0-9-]{3,40}$'),
        CONSTRAINT ck_visita_desfecho CHECK (desfecho IN ('EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada')),
        CONSTRAINT ck_visita_sem_cpf_em_claro CHECK (observacao !~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}'),
        CONSTRAINT fk_visita_idoso FOREIGN KEY (idoso_id) REFERENCES cadastro.idoso (id) ON DELETE RESTRICT
    );
    COMMENT ON COLUMN cadastro.visita.id IS 'classe=sensivel';
    COMMENT ON COLUMN cadastro.visita.idoso_id IS 'classe=sensivel';
    COMMENT ON COLUMN cadastro.visita.acs_id IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.visita.ocorrida_em IS 'classe=sensivel';
    COMMENT ON COLUMN cadastro.visita.registrada_em IS 'classe=sensivel';
    COMMENT ON COLUMN cadastro.visita.desfecho IS 'classe=sensivel';
    COMMENT ON COLUMN cadastro.visita.observacao IS 'classe=sensivel; rotulo=Saude+MoraSozinho+Nome+Endereco+Telefone';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE TABLE cadastro.vizinho_de_confianca (
        idoso_id uuid NOT NULL,
        nome character varying(120) NOT NULL,
        telefone character varying(20) NOT NULL,
        aceitou_em timestamp with time zone NOT NULL,
        CONSTRAINT pk_vizinho_de_confianca PRIMARY KEY (idoso_id),
        CONSTRAINT ck_vizinho_de_confianca_nome CHECK (btrim(nome) <> ''),
        CONSTRAINT ck_vizinho_de_confianca_sem_cpf_em_claro CHECK (concat_ws(' ', nome, telefone) !~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}'),
        CONSTRAINT ck_vizinho_de_confianca_telefone CHECK (btrim(telefone) <> ''),
        CONSTRAINT fk_vizinho_de_confianca_idoso FOREIGN KEY (idoso_id) REFERENCES cadastro.idoso (id) ON DELETE CASCADE
    );
    COMMENT ON COLUMN cadastro.vizinho_de_confianca.idoso_id IS 'classe=pessoal';
    COMMENT ON COLUMN cadastro.vizinho_de_confianca.nome IS 'classe=pessoal; rotulo=Nome';
    COMMENT ON COLUMN cadastro.vizinho_de_confianca.telefone IS 'classe=pessoal; rotulo=Telefone';
    COMMENT ON COLUMN cadastro.vizinho_de_confianca.aceitou_em IS 'classe=pessoal';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    INSERT INTO cadastro.microarea (id, acs_responsavel, nome)
    VALUES (3, 'acs-demo-01', 'Microárea 3 - Vila Exemplo');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE INDEX ix_aviso_ao_vizinho_microarea_registrado ON cadastro.aviso_ao_vizinho (microarea_id, registrado_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE UNIQUE INDEX ux_aviso_ao_vizinho_episodio ON cadastro.aviso_ao_vizinho (casa_token, motivo, desde);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE UNIQUE INDEX ux_consentimento_vigente ON cadastro.consentimento (idoso_id) WHERE revogado_em IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE INDEX ix_idoso_microarea_com_token ON cadastro.idoso (microarea_id) WHERE casa_token IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE UNIQUE INDEX ux_idoso_casa_token ON cadastro.idoso (casa_token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    CREATE INDEX ix_visita_idoso_ocorrida ON cadastro.visita (idoso_id, ocorrida_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025825_Inicial') THEN
    INSERT INTO cadastro."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117025825_Inicial', '10.0.15');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025836_Gatilhos') THEN
    -- arquivo: ddl/cadastro_gatilhos.sql. Entra na migracao Gatilhos do CadastroDbContext.
    CREATE FUNCTION cadastro.recusar_alteracao() RETURNS trigger LANGUAGE plpgsql AS $$
    BEGIN
    	RAISE EXCEPTION 'a tabela %.% nao aceita %', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP
    		USING ERRCODE = 'restrict_violation';
    END $$;

    CREATE TRIGGER visita_so_insert BEFORE UPDATE OR DELETE ON cadastro.visita
    	FOR EACH ROW EXECUTE FUNCTION cadastro.recusar_alteracao();

    CREATE TRIGGER mensagem_saida_so_insert BEFORE UPDATE OR DELETE ON cadastro.mensagem_saida
    	FOR EACH ROW EXECUTE FUNCTION cadastro.recusar_alteracao();

    -- Invariantes 1 e 8 de Idoso: o idoso tem token se, e somente se, tem consentimento vigente.
    -- A regra cruza duas tabelas, entao um CHECK nao alcanca. O gatilho de restricao confere no COMMIT,
    -- depois que o caso de uso gravou o idoso e o consentimento na mesma transacao.
    CREATE FUNCTION cadastro.conferir_token_com_consentimento() RETURNS trigger LANGUAGE plpgsql AS $$
    DECLARE
    	alvo uuid;
    BEGIN
    	IF TG_TABLE_NAME = 'idoso' THEN
    		alvo := NEW.id;
    	ELSIF TG_OP = 'DELETE' THEN
    		alvo := OLD.idoso_id;
    	ELSE
    		alvo := NEW.idoso_id;
    	END IF;
    	IF EXISTS (
    		SELECT 1 FROM cadastro.idoso i
    		WHERE i.id = alvo
    			AND (i.casa_token IS NOT NULL) <> EXISTS (
    				SELECT 1 FROM cadastro.consentimento c
    				WHERE c.idoso_id = i.id AND c.revogado_em IS NULL))
    	THEN
    		RAISE EXCEPTION 'token e consentimento vigente precisam existir juntos'
    			USING ERRCODE = 'check_violation';
    	END IF;
    	RETURN NULL;
    END $$;

    CREATE CONSTRAINT TRIGGER idoso_token_com_consentimento
    	AFTER INSERT OR UPDATE OF casa_token ON cadastro.idoso
    	DEFERRABLE INITIALLY DEFERRED
    	FOR EACH ROW EXECUTE FUNCTION cadastro.conferir_token_com_consentimento();

    CREATE CONSTRAINT TRIGGER consentimento_token_com_consentimento
    	AFTER INSERT OR UPDATE OF revogado_em OR DELETE ON cadastro.consentimento
    	DEFERRABLE INITIALLY DEFERRED
    	FOR EACH ROW EXECUTE FUNCTION cadastro.conferir_token_com_consentimento();

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM cadastro."__EFMigrationsHistory" WHERE "migration_id" = '20261117025836_Gatilhos') THEN
    INSERT INTO cadastro."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117025836_Gatilhos', '10.0.15');
    END IF;
END $EF$;
