using Corredor.Cadastro.Infra;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Corredor.Cadastro.Testes.Apoio;

// Um banco Postgres novo por classe de teste. As migrações rodam como corredor_cadastro_migra, dono do schema,
// e a API conecta como corredor_cadastro, que lê e escreve mas não muda tabela: o caminho da VM e do CI.
// O servidor vem de CORREDOR_BD_TESTE, a mesma variável do tools/verificar-api.sh e do CI. Sem ela, um contêiner
// postgres:16 descartável sobe uma vez por rodada, com nome e porta sorteados pelo Testcontainers.
public sealed class BancoDeTeste : IAsyncLifetime
{
    public const string Variavel = "CORREDOR_BD_TESTE";
    private const string SenhaDosPapeis = "ci";

    private static readonly Lazy<Task<NpgsqlConnectionStringBuilder>> Servidor = new(SubirServidorAsync);
    private static readonly SemaphoreSlim Papeis = new(1, 1);
    private static bool _papeisCriados;

    private NpgsqlConnectionStringBuilder? _admin;
    private string _banco = "";

    public string ConexaoDaApi { get; private set; } = "";
    public string ConexaoDoDono { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _admin = await Servidor.Value;
        _banco = $"cadastro_testes_{Guid.NewGuid():N}";

        await ExecutarAsync(_admin.ConnectionString, $"CREATE DATABASE {_banco}");
        var noBanco = new NpgsqlConnectionStringBuilder(_admin.ConnectionString) { Database = _banco }.ConnectionString;
        // O mesmo efeito de infra/config/bd-modulo.sql, sem o psql.
        await CriarPapeisAsync(noBanco);
        await ExecutarAsync(noBanco, $"""
            REVOKE ALL ON DATABASE {_banco} FROM PUBLIC;
            GRANT CONNECT ON DATABASE {_banco} TO corredor_cadastro_migra, corredor_cadastro;
            CREATE SCHEMA cadastro AUTHORIZATION corredor_cadastro_migra;
            REVOKE ALL ON SCHEMA cadastro FROM PUBLIC;
            GRANT USAGE ON SCHEMA cadastro TO corredor_cadastro;
            ALTER DEFAULT PRIVILEGES FOR ROLE corredor_cadastro_migra IN SCHEMA cadastro
              GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO corredor_cadastro;
            """);

        ConexaoDoDono = Papel(noBanco, "corredor_cadastro_migra");
        ConexaoDaApi = Papel(noBanco, "corredor_cadastro");

        await using var db = NovoContexto(ConexaoDoDono);
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_admin is null)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        await ExecutarAsync(_admin.ConnectionString, $"DROP DATABASE IF EXISTS {_banco} WITH (FORCE)");
    }

    // Os papéis valem para o servidor inteiro. Criar ou alterar em paralelo dá "tuple concurrently updated".
    private static async Task CriarPapeisAsync(string conexao)
    {
        await Papeis.WaitAsync();
        try
        {
            if (_papeisCriados)
            {
                return;
            }

            await ExecutarAsync(conexao, $"""
                DO $$ BEGIN
                  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'corredor_cadastro_migra') THEN CREATE ROLE corredor_cadastro_migra LOGIN; END IF;
                  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'corredor_cadastro') THEN CREATE ROLE corredor_cadastro LOGIN; END IF;
                END $$;
                ALTER ROLE corredor_cadastro_migra NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD '{SenhaDosPapeis}';
                ALTER ROLE corredor_cadastro NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD '{SenhaDosPapeis}';
                """);
            _papeisCriados = true;
        }
        finally
        {
            Papeis.Release();
        }
    }

    private static async Task<NpgsqlConnectionStringBuilder> SubirServidorAsync()
    {
        if (Environment.GetEnvironmentVariable(Variavel) is { Length: > 0 } texto)
        {
            var url = new Uri(texto);
            var usuario = url.UserInfo.Split(':');
            return new NpgsqlConnectionStringBuilder
            {
                Host = url.Host,
                Port = url.Port > 0 ? url.Port : 5432,
                Username = Uri.UnescapeDataString(usuario[0]),
                Password = usuario.Length > 1 ? Uri.UnescapeDataString(usuario[1]) : null,
                Database = url.AbsolutePath.Trim('/'),
                Pooling = false,
            };
        }

        var conteiner = new PostgreSqlBuilder("postgres:16").Build();
        await conteiner.StartAsync();
        return new NpgsqlConnectionStringBuilder(conteiner.GetConnectionString()) { Pooling = false };
    }

    public static CadastroDbContext NovoContexto(string conexao) =>
        new(new DbContextOptionsBuilder<CadastroDbContext>()
            .UseNpgsql(conexao, o => o.MigrationsHistoryTable("__EFMigrationsHistory", CadastroDbContext.Esquema))
            .UseSnakeCaseNamingConvention()
            .Options);

    // SQL cru como o papel da API, para tentar o que o domínio nunca faz.
    public async Task ExecutarComoApiAsync(string sql) => await ExecutarAsync(ConexaoDaApi, sql);

    public async Task<T?> ConsultarComoApiAsync<T>(string sql)
    {
        await using var conexao = new NpgsqlConnection(ConexaoDaApi);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        var valor = await comando.ExecuteScalarAsync();
        return valor is DBNull or null ? default : (T)valor;
    }

    private static string Papel(string conexao, string papel) =>
        new NpgsqlConnectionStringBuilder(conexao) { Username = papel, Password = SenhaDosPapeis, Pooling = true }.ConnectionString;

    private static async Task ExecutarAsync(string conexao, string sql)
    {
        await using var c = new NpgsqlConnection(conexao);
        await c.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, c);
        await comando.ExecuteNonQueryAsync();
    }
}
