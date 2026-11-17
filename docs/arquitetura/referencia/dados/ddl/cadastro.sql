-- arquivo: ddl/cadastro.sql
CREATE SCHEMA IF NOT EXISTS cadastro;

CREATE TABLE cadastro.microarea (
	id               integer     NOT NULL,
	nome             varchar(80) NOT NULL,
	acs_responsavel  varchar(40) NOT NULL,
	CONSTRAINT pk_microarea PRIMARY KEY (id),
	CONSTRAINT ck_microarea_id CHECK (id > 0),
	CONSTRAINT ck_microarea_nome CHECK (btrim(nome) <> ''),
	CONSTRAINT ck_microarea_acs_responsavel CHECK (acs_responsavel ~ '^[a-z0-9-]{3,40}$')
);

CREATE TABLE cadastro.idoso (
	id                       uuid         NOT NULL,
	microarea_id             integer      NOT NULL,
	nome                     varchar(120) NOT NULL,
	condicoes                varchar(500) NULL,
	medicamentos             varchar(500) NULL,
	perfil_contato           text         NOT NULL,
	perfil_eletrodependente  boolean      NOT NULL,
	casa_endereco            varchar(200) NOT NULL,
	casa_token               text         NULL,
	CONSTRAINT pk_idoso PRIMARY KEY (id),
	CONSTRAINT fk_idoso_microarea FOREIGN KEY (microarea_id)
		REFERENCES cadastro.microarea (id) ON DELETE RESTRICT,
	CONSTRAINT ck_idoso_nome CHECK (btrim(nome) <> ''),
	CONSTRAINT ck_idoso_casa_endereco CHECK (btrim(casa_endereco) <> ''),
	CONSTRAINT ck_idoso_perfil_contato CHECK (perfil_contato IN ('Sozinho', 'Acamado', 'Coabitado')),
	CONSTRAINT ck_idoso_casa_token CHECK (casa_token ~ '^[0-9a-f]{32}$'),
	CONSTRAINT ck_idoso_sem_cpf_em_claro CHECK (
		concat_ws(' ', nome, casa_endereco, condicoes, medicamentos)
			!~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}')
);

CREATE UNIQUE INDEX ux_idoso_casa_token ON cadastro.idoso (casa_token);

CREATE INDEX ix_idoso_microarea_com_token
	ON cadastro.idoso (microarea_id)
	WHERE casa_token IS NOT NULL;

CREATE TABLE cadastro.consentimento (
	id                           uuid        NOT NULL,
	idoso_id                     uuid        NOT NULL,
	colhido_por                  varchar(40) NOT NULL,
	colhido_em                   timestamptz NOT NULL,
	versao_do_termo              varchar(20) NOT NULL,
	autorizacoes_avisar_vizinho  boolean     NOT NULL,
	autorizacoes_avisar_familia  boolean     NOT NULL,
	revogado_em                  timestamptz NULL,
	CONSTRAINT pk_consentimento PRIMARY KEY (id),
	CONSTRAINT fk_consentimento_idoso FOREIGN KEY (idoso_id)
		REFERENCES cadastro.idoso (id) ON DELETE CASCADE,
	CONSTRAINT ck_consentimento_colhido_por CHECK (colhido_por ~ '^[a-z0-9-]{3,40}$'),
	CONSTRAINT ck_consentimento_versao_do_termo CHECK (btrim(versao_do_termo) <> '')
);

CREATE UNIQUE INDEX ux_consentimento_vigente
	ON cadastro.consentimento (idoso_id)
	WHERE revogado_em IS NULL;

CREATE TABLE cadastro.vizinho_de_confianca (
	idoso_id    uuid         NOT NULL,
	nome        varchar(120) NOT NULL,
	telefone    varchar(20)  NOT NULL,
	aceitou_em  timestamptz  NOT NULL,
	CONSTRAINT pk_vizinho_de_confianca PRIMARY KEY (idoso_id),
	CONSTRAINT fk_vizinho_de_confianca_idoso FOREIGN KEY (idoso_id)
		REFERENCES cadastro.idoso (id) ON DELETE CASCADE,
	CONSTRAINT ck_vizinho_de_confianca_nome CHECK (btrim(nome) <> ''),
	CONSTRAINT ck_vizinho_de_confianca_telefone CHECK (btrim(telefone) <> ''),
	CONSTRAINT ck_vizinho_de_confianca_sem_cpf_em_claro CHECK (
		concat_ws(' ', nome, telefone) !~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}')
);

CREATE TABLE cadastro.visita (
	id             uuid         NOT NULL,
	idoso_id       uuid         NOT NULL,
	acs_id         varchar(40)  NOT NULL,
	ocorrida_em    timestamptz  NOT NULL,
	registrada_em  timestamptz  NOT NULL,
	desfecho       text         NOT NULL,
	observacao     varchar(500) NULL,
	CONSTRAINT pk_visita PRIMARY KEY (id),
	CONSTRAINT fk_visita_idoso FOREIGN KEY (idoso_id)
		REFERENCES cadastro.idoso (id) ON DELETE RESTRICT,
	CONSTRAINT ck_visita_acs_id CHECK (acs_id ~ '^[a-z0-9-]{3,40}$'),
	CONSTRAINT ck_visita_desfecho CHECK (desfecho IN ('EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada')),
	CONSTRAINT ck_visita_sem_cpf_em_claro CHECK (
		observacao !~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}')
);

CREATE INDEX ix_visita_idoso_ocorrida ON cadastro.visita (idoso_id, ocorrida_em);

CREATE TABLE cadastro.mensagem_saida (
	id               uuid          NOT NULL,
	canal            text          NOT NULL,
	acs_id           varchar(40)   NOT NULL,
	texto_mascarado  varchar(1000) NOT NULL,
	veredito         text          NOT NULL,
	tipos_de_dado    text[]        NOT NULL,
	verificada_em    timestamptz   NOT NULL,
	CONSTRAINT pk_mensagem_saida PRIMARY KEY (id),
	CONSTRAINT ck_mensagem_saida_canal CHECK (canal IN ('Log', 'RespostaSemAutenticacao',
		'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')),
	CONSTRAINT ck_mensagem_saida_acs_id CHECK (acs_id ~ '^[a-z0-9-]{3,40}$'),
	CONSTRAINT ck_mensagem_saida_veredito CHECK (veredito IN ('Liberada', 'Bloqueada')),
	CONSTRAINT ck_mensagem_saida_tipos_de_dado CHECK (tipos_de_dado <@ ARRAY['Cpf', 'Rg', 'Cns',
		'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial']),
	CONSTRAINT ck_mensagem_saida_bloqueada_tem_tipo
		CHECK (veredito = 'Liberada' OR cardinality(tipos_de_dado) > 0),
	CONSTRAINT ck_mensagem_saida_sem_cpf_em_claro CHECK (
		texto_mascarado !~ '[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}')
);

CREATE TABLE cadastro.aviso_ao_vizinho (
	id             uuid        NOT NULL,
	casa_token     text        NOT NULL,
	microarea_id   integer     NOT NULL,
	motivo         text        NOT NULL,
	desde          timestamptz NOT NULL,
	registrado_em  timestamptz NOT NULL,
	resposta       text        NULL,
	respondido_em  timestamptz NULL,
	CONSTRAINT pk_aviso_ao_vizinho PRIMARY KEY (id),
	CONSTRAINT fk_aviso_ao_vizinho_microarea FOREIGN KEY (microarea_id)
		REFERENCES cadastro.microarea (id) ON DELETE RESTRICT,
	CONSTRAINT ck_aviso_ao_vizinho_casa_token CHECK (casa_token ~ '^[0-9a-f]{32}$'),
	CONSTRAINT ck_aviso_ao_vizinho_motivo CHECK (motivo IN ('EnergiaEletrodependente',
		'SemHeartbeat', 'CalorInterno', 'SemMovimentoNaJanela')),
	CONSTRAINT ck_aviso_ao_vizinho_resposta CHECK (resposta IN ('EstaBem', 'PrecisaDeAjuda')),
	CONSTRAINT ck_aviso_ao_vizinho_resposta_com_hora
		CHECK ((resposta IS NULL) = (respondido_em IS NULL))
);

CREATE UNIQUE INDEX ux_aviso_ao_vizinho_episodio
	ON cadastro.aviso_ao_vizinho (casa_token, motivo, desde);

CREATE INDEX ix_aviso_ao_vizinho_microarea_registrado
	ON cadastro.aviso_ao_vizinho (microarea_id, registrado_em);

INSERT INTO cadastro.microarea (id, nome, acs_responsavel)
VALUES (3, 'Microárea 3 - Vila Exemplo', 'acs-demo-01');

-- Classificacao de cada coluna (secao 5). O mapeamento do EF Core grava o mesmo texto.
COMMENT ON COLUMN cadastro.microarea.id IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.microarea.nome IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.microarea.acs_responsavel IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.idoso.id IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.idoso.microarea_id IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.idoso.nome IS 'classe=pessoal; rotulo=Nome';
COMMENT ON COLUMN cadastro.idoso.condicoes IS 'classe=sensivel; rotulo=Saude';
COMMENT ON COLUMN cadastro.idoso.medicamentos IS 'classe=sensivel; rotulo=Saude';
COMMENT ON COLUMN cadastro.idoso.perfil_contato IS 'classe=sensivel';
COMMENT ON COLUMN cadastro.idoso.perfil_eletrodependente IS 'classe=sensivel';
COMMENT ON COLUMN cadastro.idoso.casa_endereco IS 'classe=pessoal; rotulo=Endereco';
COMMENT ON COLUMN cadastro.idoso.casa_token IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.id IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.idoso_id IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.colhido_por IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.colhido_em IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.versao_do_termo IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.autorizacoes_avisar_vizinho IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.autorizacoes_avisar_familia IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.consentimento.revogado_em IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.vizinho_de_confianca.idoso_id IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.vizinho_de_confianca.nome IS 'classe=pessoal; rotulo=Nome';
COMMENT ON COLUMN cadastro.vizinho_de_confianca.telefone IS 'classe=pessoal; rotulo=Telefone';
COMMENT ON COLUMN cadastro.vizinho_de_confianca.aceitou_em IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.visita.id IS 'classe=sensivel';
COMMENT ON COLUMN cadastro.visita.idoso_id IS 'classe=sensivel';
COMMENT ON COLUMN cadastro.visita.acs_id IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.visita.ocorrida_em IS 'classe=sensivel';
COMMENT ON COLUMN cadastro.visita.registrada_em IS 'classe=sensivel';
COMMENT ON COLUMN cadastro.visita.desfecho IS 'classe=sensivel';
COMMENT ON COLUMN cadastro.visita.observacao IS 'classe=sensivel; rotulo=Saude+MoraSozinho+Nome+Endereco+Telefone';
COMMENT ON COLUMN cadastro.mensagem_saida.id IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.mensagem_saida.canal IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.mensagem_saida.acs_id IS 'classe=pessoal';
COMMENT ON COLUMN cadastro.mensagem_saida.texto_mascarado IS 'classe=pessoal; mascarado';
COMMENT ON COLUMN cadastro.mensagem_saida.veredito IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.mensagem_saida.tipos_de_dado IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.mensagem_saida.verificada_em IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.id IS 'classe=pseudonimizado';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.casa_token IS 'classe=pseudonimizado';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.microarea_id IS 'classe=nenhum';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.motivo IS 'classe=pseudonimizado+sensivel';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.desde IS 'classe=pseudonimizado';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.registrado_em IS 'classe=pseudonimizado';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.resposta IS 'classe=pseudonimizado';
COMMENT ON COLUMN cadastro.aviso_ao_vizinho.respondido_em IS 'classe=pseudonimizado';
