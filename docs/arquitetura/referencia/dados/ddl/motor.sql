-- arquivo: ddl/motor.sql
CREATE SCHEMA IF NOT EXISTS motor;

CREATE TABLE motor.casa_monitorada (
	casa_token               text        NOT NULL,
	microarea_id             integer     NOT NULL,
	perfil_contato           text        NOT NULL,
	perfil_eletrodependente  boolean     NOT NULL,
	ativada_em               timestamptz NOT NULL,
	desativada_em            timestamptz NULL,
	ultimo_pacote_em         timestamptz NULL,
	CONSTRAINT pk_casa_monitorada PRIMARY KEY (casa_token),
	CONSTRAINT ck_casa_monitorada_token CHECK (casa_token ~ '^[0-9a-f]{32}$'),
	CONSTRAINT ck_casa_monitorada_microarea CHECK (microarea_id > 0),
	CONSTRAINT ck_casa_monitorada_perfil_contato CHECK (perfil_contato IN ('Sozinho', 'Acamado', 'Coabitado'))
);

CREATE INDEX ix_casa_monitorada_microarea_ativa
	ON motor.casa_monitorada (microarea_id)
	WHERE desativada_em IS NULL;

CREATE TABLE motor.confirmacao (
	id          uuid        NOT NULL,
	casa_token  text        NOT NULL,
	origem      text        NOT NULL,
	resultado   text        NOT NULL,
	quando      timestamptz NOT NULL,
	CONSTRAINT pk_confirmacao PRIMARY KEY (id),
	CONSTRAINT fk_confirmacao_casa_monitorada FOREIGN KEY (casa_token)
		REFERENCES motor.casa_monitorada (casa_token) ON DELETE CASCADE,
	CONSTRAINT ck_confirmacao_origem CHECK (origem IN ('Acs', 'Vizinho')),
	CONSTRAINT ck_confirmacao_resultado CHECK (resultado IN ('PessoaVista', 'AusenciaConfirmada')),
	CONSTRAINT ck_confirmacao_vizinho_so_ve_pessoa CHECK (origem = 'Acs' OR resultado = 'PessoaVista')
);

CREATE INDEX ix_confirmacao_casa_quando ON motor.confirmacao (casa_token, quando);

CREATE TABLE motor.evento_sensor (
	casa_token   text         NOT NULL,
	ocorrido_em  timestamptz  NOT NULL,
	seq          bigint       NOT NULL,
	tipo         text         NOT NULL,
	recebido_em  timestamptz  NOT NULL,
	temp_c       numeric(4,1) NULL,
	tensao_v     numeric(5,1) NULL,
	bufferizado  boolean      NOT NULL,
	CONSTRAINT pk_evento_sensor PRIMARY KEY (casa_token, ocorrido_em, seq),
	CONSTRAINT fk_evento_sensor_casa_monitorada FOREIGN KEY (casa_token)
		REFERENCES motor.casa_monitorada (casa_token) ON DELETE RESTRICT,
	CONSTRAINT ck_evento_sensor_seq CHECK (seq >= 0),
	CONSTRAINT ck_evento_sensor_tipo CHECK (tipo IN ('Movimento', 'BotaoBem', 'BotaoSair', 'Telemetria')),
	CONSTRAINT ck_evento_sensor_temp_c CHECK (temp_c BETWEEN -10 AND 60),
	CONSTRAINT ck_evento_sensor_tensao_v CHECK (tensao_v BETWEEN 0 AND 260),
	CONSTRAINT ck_evento_sensor_leitura_so_na_telemetria
		CHECK (tipo = 'Telemetria' OR (temp_c IS NULL AND tensao_v IS NULL))
);

CREATE TABLE motor.dia_critico (
	data                              date         NOT NULL,
	probabilidade                     numeric(4,3) NOT NULL,
	motivo                            varchar(200) NOT NULL,
	origem                            text         NOT NULL,
	aberto_em                         timestamptz  NOT NULL,
	toque_pedido_em                   timestamptz  NULL,
	encerrado_em                      timestamptz  NULL,
	resumo_casas_ativas               integer      NULL,
	resumo_com_sinal_de_vida_ate_10h  integer      NULL,
	resumo_atendidas                  integer      NULL,
	resumo_vermelhas_pendentes        integer      NULL,
	resumo_cinzas_pendentes           integer      NULL,
	CONSTRAINT pk_dia_critico PRIMARY KEY (data),
	CONSTRAINT ck_dia_critico_probabilidade CHECK (probabilidade BETWEEN 0 AND 1),
	CONSTRAINT ck_dia_critico_motivo CHECK (btrim(motivo) <> ''),
	CONSTRAINT ck_dia_critico_origem CHECK (origem IN ('Modelo', 'RegraBase', 'Manual')),
	CONSTRAINT ck_dia_critico_resumo_so_no_encerramento CHECK (
		num_nulls(encerrado_em, resumo_casas_ativas, resumo_com_sinal_de_vida_ate_10h,
			resumo_atendidas, resumo_vermelhas_pendentes, resumo_cinzas_pendentes) IN (0, 6)),
	CONSTRAINT ck_dia_critico_resumo_faixa CHECK (
		resumo_casas_ativas >= 0
		AND resumo_com_sinal_de_vida_ate_10h BETWEEN 0 AND resumo_casas_ativas
		AND resumo_atendidas >= 0
		AND resumo_vermelhas_pendentes >= 0
		AND resumo_cinzas_pendentes >= 0
		AND resumo_atendidas + resumo_vermelhas_pendentes + resumo_cinzas_pendentes <= resumo_casas_ativas)
);

-- Classificacao de cada coluna (secao 5). O mapeamento do EF Core grava o mesmo texto.
COMMENT ON COLUMN motor.casa_monitorada.casa_token IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.casa_monitorada.microarea_id IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.casa_monitorada.perfil_contato IS 'classe=pseudonimizado+sensivel';
COMMENT ON COLUMN motor.casa_monitorada.perfil_eletrodependente IS 'classe=pseudonimizado+sensivel';
COMMENT ON COLUMN motor.casa_monitorada.ativada_em IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.casa_monitorada.desativada_em IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.casa_monitorada.ultimo_pacote_em IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.confirmacao.id IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.confirmacao.casa_token IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.confirmacao.origem IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.confirmacao.resultado IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.confirmacao.quando IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.casa_token IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.ocorrido_em IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.seq IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.tipo IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.recebido_em IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.temp_c IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.tensao_v IS 'classe=pseudonimizado';
COMMENT ON COLUMN motor.evento_sensor.bufferizado IS 'classe=pseudonimizado';
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
