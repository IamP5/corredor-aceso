using Corredor.Contratos;
using Corredor.Motor.Aplicacao;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Corredor.Motor.Testes.Banco;

[CollectionDefinition(Nome)]
public sealed class ColecaoDoBanco : ICollectionFixture<PostgresDoMotor>
{
    public const string Nome = "Postgres do Motor";
}

// Um Postgres 16 descartavel para os testes que precisam do banco. Cada teste ganha um banco novo com as migracoes do Motor.
public sealed class PostgresDoMotor : IAsyncLifetime
{
    private readonly PostgreSqlContainer _conteiner = new PostgreSqlBuilder("postgres:16").Build();

    public string Conexao(string banco) =>
        new NpgsqlConnectionStringBuilder(_conteiner.GetConnectionString()) { Database = banco }.ConnectionString;

    public async Task<MotorDeTeste> NovoMotorAsync(DateTimeOffset agora)
    {
        var banco = "motor_" + Guid.NewGuid().ToString("N");
        await using (var comando = new NpgsqlConnection(_conteiner.GetConnectionString()))
        {
            await comando.OpenAsync();
            await using var criar = new NpgsqlCommand($"CREATE DATABASE {banco}", comando);
            await criar.ExecuteNonQueryAsync();
        }

        var motor = new MotorDeTeste(Conexao(banco), agora);
        await using var escopo = motor.Escopo();
        await escopo.ServiceProvider.GetRequiredService<MotorDbContext>().Database.MigrateAsync();
        return motor;
    }

    public async ValueTask InitializeAsync() => await _conteiner.StartAsync();

    public async ValueTask DisposeAsync() => await _conteiner.DisposeAsync();
}

// O modulo como o host o monta, com relogio parado, publicador e receptor que guardam o que receberam.
// O host nao e iniciado: os servicos em segundo plano nao rodam, e cada teste chama o caso de uso.
public sealed class MotorDeTeste : IAsyncDisposable
{
    private readonly IHost _host;

    public MotorDeTeste(string conexao, DateTimeOffset agora)
    {
        Relogio = new RelogioParado(agora);
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration["ConnectionStrings:Motor"] = conexao;
        builder.Services.AddSingleton(new ParametrosDoRelogio(TimeSpan.FromMinutes(2)));
        builder.AddMotor();
        builder.Services.AddSingleton<TimeProvider>(Relogio);
        builder.Services.AddSingleton<IPublicadorDeComandos>(Publicador);
        builder.Services.AddSingleton<IReceptorDeContato>(Receptor);
        _host = builder.Build();
    }

    public RelogioParado Relogio { get; }
    public PublicadorQueGuarda Publicador { get; } = new();
    public ReceptorQueGuarda Receptor { get; } = new();

    public AsyncServiceScope Escopo() => _host.Services.CreateAsyncScope();

    public async Task<T> Executar<TServico, T>(Func<TServico, Task<T>> acao) where TServico : notnull
    {
        await using var escopo = Escopo();
        return await acao(escopo.ServiceProvider.GetRequiredService<TServico>());
    }

    public async Task Executar<TServico>(Func<TServico, Task> acao) where TServico : notnull
    {
        await using var escopo = Escopo();
        await acao(escopo.ServiceProvider.GetRequiredService<TServico>());
    }

    public Task<T> Ler<T>(Func<MotorDbContext, Task<T>> consulta) => Executar(consulta);

    public ValueTask DisposeAsync()
    {
        _host.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class RelogioParado(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;

    public override DateTimeOffset GetUtcNow() => Agora.ToUniversalTime();
}

public sealed class PublicadorQueGuarda : IPublicadorDeComandos
{
    public List<(CasaToken Token, ComandoDoNo Comando)> Publicados { get; } = [];

    public Task PublicarAsync(CasaToken token, ComandoDoNo comando, CancellationToken ct)
    {
        lock (Publicados)
        {
            Publicados.Add((token, comando));
        }

        return Task.CompletedTask;
    }
}

public sealed class ReceptorQueGuarda : IReceptorDeContato
{
    public List<PedidoDeContato> Pedidos { get; } = [];

    public Task PedirContatoAsync(PedidoDeContato pedido, CancellationToken ct)
    {
        lock (Pedidos)
        {
            Pedidos.Add(pedido);
        }

        return Task.CompletedTask;
    }
}
