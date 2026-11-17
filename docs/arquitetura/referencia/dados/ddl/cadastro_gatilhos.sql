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
