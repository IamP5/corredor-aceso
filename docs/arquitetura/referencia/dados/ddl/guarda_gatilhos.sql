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
