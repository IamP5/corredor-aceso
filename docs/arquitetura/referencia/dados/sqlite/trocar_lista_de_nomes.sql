-- arquivo: sqlite/trocar_lista_de_nomes.sql
-- Uma transacao. O INSERT em lista_de_nomes repete para cada item da resposta.
BEGIN;
DELETE FROM lista_de_nomes;
INSERT INTO lista_de_nomes (casa_token, nome, endereco, vizinho_nome, vizinho_telefone)
VALUES (:casa_token, :nome, :endereco, :vizinho_nome, :vizinho_telefone);
INSERT INTO sincronizacao (recurso, baixado_em) VALUES ('lista_de_nomes', :baixado_em)
	ON CONFLICT (recurso) DO UPDATE SET baixado_em = excluded.baixado_em;
COMMIT;
