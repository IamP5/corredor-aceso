-- arquivo: ddl/guarda.sql
CREATE SCHEMA IF NOT EXISTS guarda;

CREATE TABLE guarda.evento_de_seguranca (
	id               uuid         NOT NULL,
	ocorrido_em      timestamptz  NOT NULL,
	tipo             text         NOT NULL,
	tipos_de_dado    text[]       NOT NULL,
	canal            text         NOT NULL,
	modulo           varchar(40)  NOT NULL,
	rota             varchar(200) NULL,
	severidade       smallint     NOT NULL,
	mensagem_texto   varchar(400) NULL,
	mensagem_origem  text         NULL,
	CONSTRAINT pk_evento_de_seguranca PRIMARY KEY (id),
	CONSTRAINT ck_evento_de_seguranca_tipo CHECK (tipo IN ('MascaramentoNaEntrada', 'BloqueioDeSaida')),
	CONSTRAINT ck_evento_de_seguranca_tipos_de_dado CHECK (
		cardinality(tipos_de_dado) > 0
		AND tipos_de_dado <@ ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco',
			'Telefone', 'Nome', 'Credencial']),
	CONSTRAINT ck_evento_de_seguranca_canal CHECK (canal IN ('Log', 'RespostaSemAutenticacao',
		'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')),
	CONSTRAINT ck_evento_de_seguranca_modulo CHECK (btrim(modulo) <> ''),
	CONSTRAINT ck_evento_de_seguranca_rota_sem_identificador CHECK (
		rota !~ '[?]|[0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-'),
	CONSTRAINT ck_evento_de_seguranca_severidade CHECK (severidade BETWEEN 1 AND 5),
	CONSTRAINT ck_evento_de_seguranca_mensagem_origem CHECK (mensagem_origem IN ('Llm', 'FraseFixa')),
	CONSTRAINT ck_evento_de_seguranca_mensagem_inteira
		CHECK ((mensagem_texto IS NULL) = (mensagem_origem IS NULL))
);

CREATE INDEX ix_evento_de_seguranca_ocorrido_em
	ON guarda.evento_de_seguranca (ocorrido_em DESC);

-- Classificacao de cada coluna (secao 5). O mapeamento do EF Core grava o mesmo texto.
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
