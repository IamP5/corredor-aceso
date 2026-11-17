using Corredor.Guarda.Infra;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Corredor.Guarda.Testes;

/// <summary>
/// Um banco descartável num Postgres 16. Com CORREDOR_BD_TESTE, a variável do check:api, cria um
/// banco novo naquele servidor. Sem ela, sobe um contêiner pelo Testcontainers.
/// </summary>
internal sealed class BancoDeTeste : IAsyncDisposable
{
    private readonly PostgreSqlContainer? _conteiner;
    private readonly string? _administracao;
    private readonly string? _nome;

    private BancoDeTeste(string conexao, PostgreSqlContainer? conteiner, string? administracao, string? nome)
    {
        Conexao = conexao;
        _conteiner = conteiner;
        _administracao = administracao;
        _nome = nome;
    }

    public string Conexao { get; }

    public static async Task<BancoDeTeste> CriarAsync(CancellationToken ct)
    {
        if (Environment.GetEnvironmentVariable("CORREDOR_BD_TESTE") is { Length: > 0 } url)
        {
            var uri = new Uri(url);
            var usuario = uri.UserInfo.Split(':');
            var administracao = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host, Port = uri.Port, Username = Uri.UnescapeDataString(usuario[0]),
                Password = usuario.Length > 1 ? Uri.UnescapeDataString(usuario[1]) : null, Database = uri.AbsolutePath.Trim('/'),
            };
            var nome = $"guarda_teste_{Guid.NewGuid():N}";
            await using (var conexao = new NpgsqlConnection(administracao.ConnectionString))
            {
                await conexao.OpenAsync(ct);
                await new NpgsqlCommand($"CREATE DATABASE {nome}", conexao).ExecuteNonQueryAsync(ct);
            }

            var banco = new NpgsqlConnectionStringBuilder(administracao.ConnectionString) { Database = nome, Pooling = false };
            return new BancoDeTeste(banco.ConnectionString, null, administracao.ConnectionString, nome);
        }

        PostgreSqlContainer conteiner;
        try
        {
            conteiner = new PostgreSqlBuilder("postgres:16").Build();
        }
        catch (DockerUnavailableException e)
        {
            Assert.Skip($"sem Docker compatível com o Testcontainers ({Raiz(e).Message.Trim()}). Rode com CORREDOR_BD_TESTE.");
            throw;
        }

        await conteiner.StartAsync(ct);
        return new BancoDeTeste(conteiner.GetConnectionString(), conteiner, null, null);

        static Exception Raiz(Exception e) => e.InnerException is null ? e : Raiz(e.InnerException);
    }

    public async ValueTask DisposeAsync()
    {
        if (_conteiner is not null)
        {
            await _conteiner.DisposeAsync();
            return;
        }

        await using var conexao = new NpgsqlConnection(_administracao);
        await conexao.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {_nome} WITH (FORCE)", conexao).ExecuteNonQueryAsync();
    }
}

/// <summary>O schema guarda com as migrações do módulo aplicadas.</summary>
public sealed class ArmazemPostgresTestes : IAsyncLifetime
{
    private BancoDeTeste _banco = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _banco = await BancoDeTeste.CriarAsync(Ct);
        await using var bd = NovoContexto();
        await bd.Database.MigrateAsync(Ct);
    }

    public async ValueTask DisposeAsync() => await _banco.DisposeAsync();

    private GuardaDbContext NovoContexto() => new(new DbContextOptionsBuilder<GuardaDbContext>()
        .UseNpgsql(_banco.Conexao, o => o.MigrationsHistoryTable("__EFMigrationsHistory", GuardaDbContext.Esquema))
        .UseSnakeCaseNamingConvention()
        .Options);

    private static EventoDeSeguranca Evento(TipoDeEventoDeSeguranca tipo, Canal canal, TipoDeDado dado, int severidade, DateTimeOffset quando) =>
        EventoDeSeguranca.Registrar(tipo, canal, [(dado, severidade)], "Cadastro", "/api/mensagens/verificar", quando);

    [Fact]
    public async Task Grava_conta_e_anexa_a_mensagem_uma_vez()
    {
        var agora = DateTimeOffset.UtcNow;
        var bloqueio = Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.GrupoDoBairro, TipoDeDado.Saude, 4, agora);
        var mascaramento = Evento(TipoDeEventoDeSeguranca.MascaramentoNaEntrada, Canal.Banco, TipoDeDado.Cpf, 4, agora.AddMinutes(-1));
        var velho = Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.Log, TipoDeDado.Nome, 3, agora.AddHours(-30));
        await using (var bd = NovoContexto())
        {
            var armazem = new ArmazemPostgres(bd);
            foreach (var e in new[] { bloqueio, mascaramento, velho })
            {
                await armazem.GravarAsync(e, Ct);
            }

            await armazem.AnexarMensagemAsync(bloqueio.Id, new MensagemAoCidadao("traduzida", OrigemDaMensagem.Llm), Ct);
            await armazem.AnexarMensagemAsync(bloqueio.Id, new MensagemAoCidadao("de novo", OrigemDaMensagem.FraseFixa), Ct);
        }

        await using var leitura = NovoContexto();
        var lidos = new ArmazemPostgres(leitura);
        var desde = agora - Farol.Janela;
        var eventos = await lidos.EventosDoPainelAsync(desde, 50, Ct);
        var contagem = await lidos.ContagemDoPainelAsync(desde, Ct);

        Assert.Equal([bloqueio.Id, mascaramento.Id], eventos.Select(e => e.Id));
        Assert.Equal("traduzida", eventos[0].Mensagem!.Texto);
        Assert.Equal([TipoDeDado.Saude], eventos[0].TiposDeDado);
        Assert.Equal(CorDoFarol.Vermelho, Farol.Cor(contagem));
        Assert.Equal((4 * 3) + (4 * 1), Farol.Pontos(contagem));
        var semMensagem = (await lidos.SemMensagemAsync(Ct)).Select(e => e.Id).ToHashSet();
        Assert.Equal(new HashSet<Guid> { mascaramento.Id, velho.Id }, semMensagem);
    }

    [Fact]
    public async Task O_banco_recusa_apagar_evento_e_trocar_a_mensagem_anexada()
    {
        var evento = Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.GrupoDoBairro, TipoDeDado.Saude, 4, DateTimeOffset.UtcNow);
        await using (var bd = NovoContexto())
        {
            var armazem = new ArmazemPostgres(bd);
            await armazem.GravarAsync(evento, Ct);
            await armazem.AnexarMensagemAsync(evento.Id, new MensagemAoCidadao("primeira", OrigemDaMensagem.Llm), Ct);
        }

        await using var conexao = new NpgsqlConnection(_banco.Conexao);
        await conexao.OpenAsync(Ct);

        var apagar = await Assert.ThrowsAsync<PostgresException>(() =>
            new NpgsqlCommand($"DELETE FROM guarda.evento_de_seguranca WHERE id = '{evento.Id}'", conexao).ExecuteNonQueryAsync(Ct));
        var trocar = await Assert.ThrowsAsync<PostgresException>(() =>
            new NpgsqlCommand($"UPDATE guarda.evento_de_seguranca SET mensagem_texto = 'outra' WHERE id = '{evento.Id}'", conexao).ExecuteNonQueryAsync(Ct));
        var rotaComId = await Assert.ThrowsAsync<PostgresException>(() => new NpgsqlCommand(
            "INSERT INTO guarda.evento_de_seguranca (id, ocorrido_em, tipo, tipos_de_dado, canal, modulo, rota, severidade) " +
            "VALUES (gen_random_uuid(), now(), 'BloqueioDeSaida', ARRAY['Nome'], 'Log', 'Cadastro', " +
            "'/api/idosos/6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f/vizinho', 3)", conexao).ExecuteNonQueryAsync(Ct));

        Assert.Equal(PostgresErrorCodes.RestrictViolation, apagar.SqlState);
        Assert.Equal(PostgresErrorCodes.RestrictViolation, trocar.SqlState);
        Assert.Equal(PostgresErrorCodes.CheckViolation, rotaComId.SqlState);
    }

    [Fact]
    public async Task Toda_coluna_tem_classificacao_no_comentario()
    {
        await using var conexao = new NpgsqlConnection(_banco.Conexao);
        await conexao.OpenAsync(Ct);
        await using var comando = new NpgsqlCommand(
            "SELECT a.attname, col_description(a.attrelid, a.attnum) FROM pg_attribute a " +
            "WHERE a.attrelid = 'guarda.evento_de_seguranca'::regclass AND a.attnum > 0 AND NOT a.attisdropped", conexao);
        await using var leitor = await comando.ExecuteReaderAsync(Ct);
        var colunas = new Dictionary<string, string?>();
        while (await leitor.ReadAsync(Ct))
        {
            colunas[leitor.GetString(0)] = leitor.IsDBNull(1) ? null : leitor.GetString(1);
        }

        Assert.Equal(10, colunas.Count);
        Assert.All(colunas.Values, classe => Assert.Equal("classe=nenhum", classe));
    }
}
