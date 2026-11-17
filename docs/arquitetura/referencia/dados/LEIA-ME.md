# Arquivos conferidos do modelo de dados

Esta pasta tem o código que rodou por trás de [04-modelo-de-dados.md](../../04-modelo-de-dados.md): o DDL, o mapeamento do EF Core com as migrações, o SQLite do app, o gerador da semente e os scripts de conferência. O documento é montado a partir destes arquivos por `montar.py`, então nenhum bloco de código de lá é cópia manual.

## O que tem aqui

| Pasta ou arquivo | Conteúdo |
|---|---|
| `ddl/` | O DDL dos três schemas, com os comentários de classificação, e os gatilhos de cada schema |
| `ef/` | Projeto .NET 10 com os três `DbContext`, os tipos mínimos do domínio, as consultas da seção 3 e as seis migrações em `ef/Migrations/` |
| `consultas/` | O `Expurgo` e o reinício da demo, em SQL |
| `sqlite/` | O esquema e as consultas do app |
| `semente/` | O gerador das 40 casas, a semente gerada e a varredura de dado pessoal |
| `saida/` | A saída da última rodada |
| `conferir_migracao_aditiva.sh` | A conferência de migração destrutiva para o CI |
| `modelo-de-dados.fonte.md`, `montar.py` | A fonte do documento e o script que monta |

Os tipos de `ef/Tipos.cs` são o mínimo para o mapeamento compilar. Eles servem de ponto de partida para os projetos `Corredor.Motor`, `Corredor.Cadastro` e `Corredor.Guarda`, não são o domínio completo.

## Como rodar as conferências

Você precisa de Docker, do SDK do .NET 10, de `sqlite3` e de Python 3. O papel de cada módulo vem de `../repo/infra/config/bd-modulo.sql`, da referência da infra.

1. Suba um Postgres 16 descartável:

	```bash
	docker run -d --rm --name corredor-dados -e POSTGRES_PASSWORD=corredor -p 127.0.0.1:55470:5432 postgres:16
	```

2. Rode tudo a partir desta pasta:

	```bash
	./verificar_tudo.sh
	```

3. Para ver o log do Postgres com e sem as duas linhas do `postgresql.conf`, rode `./log_do_servidor.sh`. Ele usa o banco `teste`, que `verificar_tudo.sh` cria.

4. Remova o contêiner:

	```bash
	docker rm -f corredor-dados
	```

Na última rodada, `verificar_tudo.sh` imprimiu:

| Etapa | Resultado |
|---|---|
| Catálogo do EF Core contra o DDL | 279 linhas iguais, com 88 comentários de classificação |
| Migrações | conferem com o modelo, rodam duas vezes como `corredor_<modulo>_migra` e deixam o catálogo do DDL |
| SQL | 86 conferências |
| Papéis | 13 conferências |
| EF Core, como os papéis da API | 32 conferências |
| SQLite | 15 conferências |
| Semente, varredura e montagem do documento | passam |

Se você mudar um arquivo daqui, rode `python3 montar.py` para refazer o documento e `./verificar_tudo.sh` para conferir. O último passo, `montar.py --conferir`, falha quando o documento e os arquivos divergem.

## Como mudar o modelo

1. Mude o mapeamento em `ef/Contextos.cs` e a classificação em `ef/Classificacao.cs`. Uma coluna sem classificação derruba a montagem do modelo.
2. Gere a migração a partir de `ef/`:

	```bash
	dotnet ef migrations add <Nome> --context MotorDbContext --output-dir Migrations/Motor
	```

3. Mude o DDL em `ddl/` para o catálogo continuar igual.
4. Rode `./verificar_tudo.sh`.
