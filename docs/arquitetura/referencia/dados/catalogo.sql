-- Descreve o catalogo dos tres schemas em linhas de texto comparaveis:
-- colunas, restricoes, indices, comentarios de classificacao, gatilhos, funcoes e a carga da microarea.
-- O corpo da funcao entra pelo md5 sem os espacos, porque o script idempotente recua cada linha.
SELECT 'coluna ' || table_schema || '.' || table_name || '.' || column_name || ' ' ||
	CASE WHEN data_type = 'ARRAY' THEN udt_name ELSE data_type END ||
	coalesce('(' || character_maximum_length || ')', '') ||
	CASE WHEN data_type = 'numeric' THEN '(' || numeric_precision || ',' || numeric_scale || ')' ELSE '' END ||
	CASE WHEN is_nullable = 'NO' THEN ' NOT NULL' ELSE ' NULL' END ||
	coalesce(' DEFAULT ' || column_default, '')
FROM information_schema.columns
WHERE table_schema IN ('motor', 'cadastro', 'guarda') AND table_name <> '__EFMigrationsHistory'
UNION ALL
SELECT 'restricao ' || n.nspname || '.' || c.relname || ' ' || k.conname || ' ' || pg_get_constraintdef(k.oid)
FROM pg_constraint k
JOIN pg_class c ON c.oid = k.conrelid
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname IN ('motor', 'cadastro', 'guarda') AND c.relname <> '__EFMigrationsHistory'
UNION ALL
SELECT 'indice ' || schemaname || '.' || tablename || ' ' || indexdef
FROM pg_indexes
WHERE schemaname IN ('motor', 'cadastro', 'guarda') AND tablename <> '__EFMigrationsHistory'
UNION ALL
SELECT 'comentario ' || n.nspname || '.' || c.relname || '.' || a.attname || ' ' || d.description
FROM pg_description d
JOIN pg_class c ON c.oid = d.objoid AND d.classoid = 'pg_class'::regclass
JOIN pg_namespace n ON n.oid = c.relnamespace
JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = d.objsubid
WHERE n.nspname IN ('motor', 'cadastro', 'guarda') AND d.objsubid > 0
UNION ALL
SELECT 'gatilho ' || n.nspname || '.' || c.relname || ' ' || pg_get_triggerdef(t.oid)
FROM pg_trigger t
JOIN pg_class c ON c.oid = t.tgrelid
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname IN ('motor', 'cadastro', 'guarda') AND NOT t.tgisinternal
UNION ALL
SELECT 'funcao ' || p.oid::regprocedure::text || ' md5=' || md5(btrim(regexp_replace(p.prosrc, '\s+', ' ', 'g')))
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname IN ('motor', 'cadastro', 'guarda')
UNION ALL
SELECT 'linha cadastro.microarea ' || id || ' ' || nome || ' ' || acs_responsavel FROM cadastro.microarea
ORDER BY 1;
