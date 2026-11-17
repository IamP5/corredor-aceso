using System.Text;
using Corredor.Contratos;
using Corredor.Motor.Infra;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Protocol;

namespace Corredor.Motor.Testes.Banco;

// O consumidor MQTT de verdade contra um Mosquitto e um Postgres: contrato v1, reenvio e sessao que sobrevive a queda.
[Collection(ColecaoDoBanco.Nome)]
public sealed class ConsumidorMqttTestes(PostgresDoMotor postgres) : IAsyncLifetime
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(15);

    private readonly IContainer _mosquitto = new ContainerBuilder("eclipse-mosquitto:2")
        .WithPortBinding(1883, true)
        .WithCommand("mosquitto", "-c", "/mosquitto-no-auth.conf")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("running"))
        .Build();

    private readonly string _clienteId = "corredor-motor-teste-" + Guid.NewGuid().ToString("N")[..8];

    public async ValueTask InitializeAsync() => await _mosquitto.StartAsync();

    public async ValueTask DisposeAsync() => await _mosquitto.DisposeAsync();

    [Fact]
    public async Task EventoReenviadoDoBufferEGravadoUmaVezComAHoraOriginal()
    {
        var (conexao, token) = await BancoComCasaAsync();
        using var host = await IniciarMotorAsync(conexao);
        var ts = DateTimeOffset.UtcNow.AddMinutes(-7);

        await PublicarAsync(token, Corpo(1842, ts, "MOVIMENTO", bufferizado: false));
        await PublicarAsync(token, Corpo(1842, ts, "MOVIMENTO", bufferizado: true));
        // As mensagens chegam em ordem: quando o heartbeat avanca o ultimo pacote, o reenvio ja passou.
        var antesDoHeartbeat = DateTimeOffset.UtcNow;
        await PublicarAsync(token, Corpo(1843, antesDoHeartbeat, "HEARTBEAT", bufferizado: false));
        await EsperarAsync(conexao, bd => bd.Casas.AnyAsync(c => c.Token == token && c.UltimoPacoteEm >= antesDoHeartbeat));

        var eventos = await LerAsync(conexao, bd => bd.Eventos.Where(e => e.CasaToken == token).ToListAsync());
        var evento = Assert.Single(eventos);
        Assert.Equal(1842, evento.Seq);
        Assert.Equal(TruncarNoMicrossegundo(ts), evento.OcorridoEm);
        Assert.True(evento.RecebidoEm > ts.AddMinutes(6));
    }

    [Fact]
    public async Task PacoteForaDoContratoEDescartadoSemDerrubarOConsumidor()
    {
        var (conexao, token) = await BancoComCasaAsync();
        using var host = await IniciarMotorAsync(conexao);

        await PublicarAsync(token, "nao e json");
        await PublicarAsync(token, """{"seq":1,"ts":"2026-11-18T09:12:40Z","tipo":"PULO"}""");
        await PublicarAsync(token, Corpo(5, DateTimeOffset.UtcNow.AddMinutes(3), "MOVIMENTO", false));
        await PublicarAsync(token, Corpo(6, DateTimeOffset.UtcNow, "MOVIMENTO", false));
        await EsperarAsync(conexao, bd => bd.Eventos.AnyAsync(e => e.CasaToken == token));

        var eventos = await LerAsync(conexao, bd => bd.Eventos.Where(e => e.CasaToken == token).ToListAsync());
        Assert.Equal(6, Assert.Single(eventos).Seq);
    }

    [Fact]
    public async Task SessaoSobreviveAQuedaEOsEventosPublicadosNoIntervaloChegamNaVolta()
    {
        var (conexao, token) = await BancoComCasaAsync();
        using (await IniciarMotorAsync(conexao))
        {
        }

        var ts = DateTimeOffset.UtcNow.AddSeconds(-30);
        await PublicarAsync(token, Corpo(77, ts, "BOTAO_BEM", bufferizado: true));

        using var deVolta = await IniciarMotorAsync(conexao);
        await EsperarAsync(conexao, bd => bd.Eventos.AnyAsync(e => e.CasaToken == token));

        var evento = Assert.Single(await LerAsync(conexao, bd => bd.Eventos.Where(e => e.CasaToken == token).ToListAsync()));
        Assert.Equal(77, evento.Seq);
        Assert.True(evento.Bufferizado);
        Assert.Equal(TruncarNoMicrossegundo(ts), evento.OcorridoEm);
    }

    private async Task<(string Conexao, CasaToken Token)> BancoComCasaAsync()
    {
        await using var motor = await postgres.NovoMotorAsync(DateTimeOffset.UtcNow);
        var token = CasaToken.Novo();
        await motor.Executar<IMotorDeCasas>(m => m.AtivarCasaAsync(token, new MicroareaId(3), new PerfilDaCasa(PerfilDeContato.Sozinho, false), CancellationToken.None));
        return (motor.Conexao, token);
    }

    // Sobe o modulo com o ConsumidorMqtt e espera a conexao com o broker.
    private async Task<IHost> IniciarMotorAsync(string conexao)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration["ConnectionStrings:Motor"] = conexao;
        builder.Configuration["Mqtt:Host"] = _mosquitto.Hostname;
        builder.Configuration["Mqtt:Porta"] = _mosquitto.GetMappedPublicPort(1883).ToString();
        builder.Configuration["Mqtt:ClienteId"] = _clienteId;
        builder.Services.AddSingleton(new ParametrosDoRelogio(TimeSpan.FromMinutes(2)));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDeslocamentoDoRelogio, SemDeslocamento>();
        builder.AddMotor();
        var host = builder.Build();
        await host.StartAsync();
        var mqtt = host.Services.GetRequiredService<ConexaoMqtt>();
        await EsperarAsync(() => Task.FromResult(mqtt.Cliente.IsConnected));
        return host;
    }

    private async Task PublicarAsync(CasaToken token, string corpo)
    {
        using var cliente = new MqttClientFactory().CreateMqttClient();
        await cliente.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(_mosquitto.Hostname, _mosquitto.GetMappedPublicPort(1883))
            .WithClientId("simulador-" + Guid.NewGuid().ToString("N")[..8])
            .Build());
        await cliente.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"corredor/v1/{token.Valor}/evento")
            .WithPayload(Encoding.UTF8.GetBytes(corpo))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
        await cliente.DisconnectAsync();
    }

    private static string Corpo(long seq, DateTimeOffset ts, string tipo, bool bufferizado) =>
        $$"""{"seq":{{seq}},"ts":"{{ts.UtcDateTime:yyyy-MM-ddTHH:mm:ss.ffffffZ}}","tipo":"{{tipo}}","tempC":null,"tensaoV":null,"bufferizado":{{(bufferizado ? "true" : "false")}}}""";

    private static DateTimeOffset TruncarNoMicrossegundo(DateTimeOffset t) => t.AddTicks(-(t.Ticks % 10));

    private static async Task<T> LerAsync<T>(string conexao, Func<MotorDbContext, Task<T>> consulta)
    {
        await using var bd = new MotorDbContext(new DbContextOptionsBuilder<MotorDbContext>().UseNpgsql(conexao).UseSnakeCaseNamingConvention().Options);
        return await consulta(bd);
    }

    private static Task EsperarAsync(string conexao, Func<MotorDbContext, Task<bool>> condicao) =>
        EsperarAsync(() => LerAsync(conexao, condicao));

    private static async Task EsperarAsync(Func<Task<bool>> condicao)
    {
        var limite = DateTime.UtcNow + Prazo;
        while (!await condicao())
        {
            Assert.True(DateTime.UtcNow < limite, "a condicao nao aconteceu no prazo");
            await Task.Delay(200);
        }
    }

    private sealed class SemDeslocamento : IDeslocamentoDoRelogio
    {
        public TimeSpan Deslocamento => TimeSpan.Zero;
    }
}
