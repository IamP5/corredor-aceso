// Cópia de docs/arquitetura/referencia/dados/sqlite/app.sql (04-modelo-de-dados.md, seção 6.1).
// O teste de esquema confere que as duas ficam iguais.
export const ESQUEMA_V1 = `
CREATE TABLE sincronizacao (
	recurso     TEXT PRIMARY KEY CHECK (recurso IN ('lista_de_nomes', 'radar', 'avisos')),
	baixado_em  TEXT NOT NULL
);

CREATE TABLE lista_de_nomes (
	casa_token        TEXT PRIMARY KEY CHECK (length(casa_token) = 32),
	nome              TEXT NOT NULL,
	endereco          TEXT NOT NULL,
	vizinho_nome      TEXT,
	vizinho_telefone  TEXT,
	CHECK ((vizinho_nome IS NULL) = (vizinho_telefone IS NULL))
);

CREATE TABLE radar_cabecalho (
	id                    INTEGER PRIMARY KEY CHECK (id = 1),
	microarea_id          INTEGER NOT NULL,
	turno_aberto          INTEGER NOT NULL CHECK (turno_aberto IN (0, 1)),
	agora_do_servidor     TEXT NOT NULL,
	verdes                INTEGER NOT NULL CHECK (verdes >= 0)
);

CREATE TABLE radar_card (
	casa_token        TEXT PRIMARY KEY CHECK (length(casa_token) = 32),
	lista             TEXT NOT NULL CHECK (lista IN ('pendente', 'atendida', 'aguardando')),
	ordem             INTEGER NOT NULL,
	situacao          TEXT NOT NULL CHECK (situacao IN ('Vermelho', 'Cinza', 'Aguardando')),
	motivo            TEXT CHECK (motivo IN ('EnergiaEletrodependente', 'SemHeartbeat', 'CalorInterno', 'SemMovimentoNaJanela')),
	desde             TEXT,
	perfil_contato    TEXT CHECK (perfil_contato IN ('Sozinho', 'Acamado', 'Coabitado')),
	eletrodependente  INTEGER CHECK (eletrodependente IN (0, 1)),
	ultimo_pacote_em  TEXT,
	saida_avisada     INTEGER CHECK (saida_avisada IN (0, 1)),
	CHECK ((lista = 'aguardando') = (situacao = 'Aguardando')),
	CHECK ((lista = 'aguardando') = (motivo IS NULL)),
	CHECK ((lista = 'aguardando') = (desde IS NULL)),
	CHECK ((lista = 'aguardando') = (perfil_contato IS NULL)),
	CHECK ((lista = 'aguardando') = (saida_avisada IS NOT NULL))
);

CREATE INDEX ix_radar_card_lista_ordem ON radar_card (lista, ordem);

CREATE TABLE aviso (
	id             TEXT PRIMARY KEY,
	casa_token     TEXT NOT NULL,
	motivo         TEXT NOT NULL,
	desde          TEXT NOT NULL,
	registrado_em  TEXT NOT NULL,
	resposta       TEXT CHECK (resposta IN ('EstaBem', 'PrecisaDeAjuda')),
	respondido_em  TEXT
);

CREATE INDEX ix_aviso_episodio ON aviso (casa_token, motivo, desde);

CREATE TABLE desfecho (
	id             TEXT PRIMARY KEY CHECK (length(id) = 36),
	casa_token     TEXT NOT NULL CHECK (length(casa_token) = 32),
	ocorrida_em    TEXT NOT NULL,
	desfecho       TEXT NOT NULL CHECK (desfecho IN ('EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada')),
	observacao     TEXT CHECK (length(observacao) <= 500),
	estado         TEXT NOT NULL DEFAULT 'pendente' CHECK (estado IN ('pendente', 'enviado', 'recusado')),
	tentativas     INTEGER NOT NULL DEFAULT 0,
	ultimo_erro    TEXT,
	registrada_em  TEXT,
	CHECK ((estado = 'enviado') = (registrada_em IS NOT NULL)),
	CHECK (estado <> 'enviado' OR observacao IS NULL)
);

CREATE INDEX ix_desfecho_pendente ON desfecho (ocorrida_em) WHERE estado = 'pendente';
CREATE INDEX ix_desfecho_casa ON desfecho (casa_token, ocorrida_em);
`;
