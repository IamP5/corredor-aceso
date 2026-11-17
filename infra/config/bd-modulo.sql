-- Papéis e schema de um módulo. Roda como superusuário, com as mesmas variáveis,
-- no infra/02-app.sh (VM) e no tools/verificar-api.sh (CI).
--   :dono   dono do schema. Só ele roda as migrações do módulo. Não enxerga outro schema.
--   :papel  papel da API para o módulo. Lê e escreve, não muda tabela.
-- Pode rodar quantas vezes for preciso: cada comando confere antes de criar.
SET client_min_messages = warning;
SELECT format('CREATE ROLE %I LOGIN', :'dono')
 WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'dono') \gexec
SELECT format('CREATE ROLE %I LOGIN', :'papel')
 WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'papel') \gexec
ALTER ROLE :"dono" NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD :'senha_dono';
ALTER ROLE :"papel" NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD :'senha';
REVOKE ALL ON DATABASE :"banco" FROM PUBLIC;
GRANT CONNECT ON DATABASE :"banco" TO :"dono", :"papel";
CREATE SCHEMA IF NOT EXISTS :"esquema" AUTHORIZATION :"dono";
ALTER SCHEMA :"esquema" OWNER TO :"dono";
REVOKE ALL ON SCHEMA :"esquema" FROM PUBLIC;
GRANT USAGE ON SCHEMA :"esquema" TO :"papel";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA :"esquema" TO :"papel";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA :"esquema" TO :"papel";
ALTER DEFAULT PRIVILEGES FOR ROLE :"dono" IN SCHEMA :"esquema"
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"papel";
ALTER DEFAULT PRIVILEGES FOR ROLE :"dono" IN SCHEMA :"esquema"
  GRANT USAGE, SELECT ON SEQUENCES TO :"papel";
