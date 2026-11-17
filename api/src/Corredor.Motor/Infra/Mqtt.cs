using System.Buffers;
using System.Text;
using Corredor.Contratos;
using Corredor.Motor.Aplicacao;
using Corredor.Motor.Dominio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace Corredor.Motor.Infra;

public sealed record OpcoesDoMqtt(string Host, int Porta, string ClienteId)
{
    // A sessao sobrevive uma queda de ate um dia: o broker guarda os eventos QoS 1 enquanto o Motor reconecta.
    public static readonly TimeSpan ValidadeDaSessao = TimeSpan.FromDays(1);

    public static OpcoesDoMqtt De(IConfiguration configuracao) => new(
        configuracao["Mqtt:Host"] ?? "127.0.0.1",
        configuracao.GetValue("Mqtt:Porta", 1883),
        configuracao["Mqtt:ClienteId"] ?? "corredor-motor");
}

// Um cliente MQTT para o processo: o consumidor assina os eventos e o publicador manda os comandos pela mesma conexao.
public sealed class ConexaoMqtt : IDisposable
{
    public IMqttClient Cliente { get; } = new MqttClientFactory().CreateMqttClient();

    public void Dispose() => Cliente.Dispose();
}

public sealed class PublicadorDeComandos(ConexaoMqtt conexao, ILogger<PublicadorDeComandos> log) : IPublicadorDeComandos
{
    public async Task PublicarAsync(CasaToken token, ComandoDoNo comando, CancellationToken ct)
    {
        if (!conexao.Cliente.IsConnected)
        {
            log.LogWarning("Comando {Comando} nao publicado: broker desconectado", comando);
            return;
        }

        var tipo = comando switch
        {
            ComandoDoNo.DiaCritico => "DIA_CRITICO",
            _ => "PEDIR_TOQUE",
        };
        var mensagem = new MqttApplicationMessageBuilder()
            .WithTopic(TradutorDePacote.TopicoDeComando(token))
            .WithPayload(Encoding.UTF8.GetBytes($$"""{"tipo":"{{tipo}}"}"""))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();
        await conexao.Cliente.PublishAsync(mensagem, ct);
    }
}

// Assina corredor/v1/+/evento com client id fixo, QoS 1 e sessao persistente, e reconecta sozinho.
public sealed class ConsumidorMqtt(
    ConexaoMqtt conexao,
    OpcoesDoMqtt opcoes,
    IServiceScopeFactory escopos,
    IDeslocamentoDoRelogio relogio,
    ILogger<ConsumidorMqtt> log) : BackgroundService
{
    private static readonly TimeSpan IntervaloDeReconexao = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        conexao.Cliente.ApplicationMessageReceivedAsync += e => ReceberAsync(e, ct);
        var conectar = new MqttClientOptionsBuilder()
            .WithTcpServer(opcoes.Host, opcoes.Porta)
            .WithClientId(opcoes.ClienteId)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithCleanStart(false)
            .WithSessionExpiryInterval((uint)OpcoesDoMqtt.ValidadeDaSessao.TotalSeconds)
            .Build();

        while (!ct.IsCancellationRequested)
        {
            if (!conexao.Cliente.IsConnected)
            {
                await ConectarAsync(conectar, ct);
            }

            await Esperar(IntervaloDeReconexao, ct);
        }

        if (conexao.Cliente.IsConnected)
        {
            await conexao.Cliente.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), CancellationToken.None);
        }
    }

    private async Task ConectarAsync(MqttClientOptions conectar, CancellationToken ct)
    {
        try
        {
            var resultado = await conexao.Cliente.ConnectAsync(conectar, ct);
            await conexao.Cliente.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(TradutorDePacote.FiltroDeEventos).WithAtLeastOnceQoS())
                .Build(), ct);
            log.LogInformation("MQTT conectado em {Host}:{Porta}, sessao retomada: {Retomada}", opcoes.Host, opcoes.Porta, resultado.IsSessionPresent);
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            log.LogWarning("MQTT indisponivel em {Host}:{Porta}: {Erro}", opcoes.Host, opcoes.Porta, erro.GetType().Name);
        }
    }

    private async Task ReceberAsync(MqttApplicationMessageReceivedEventArgs e, CancellationToken ct)
    {
        var corpo = e.ApplicationMessage.Payload.ToArray();
        if (TradutorDePacote.Traduzir(e.ApplicationMessage.Topic, corpo, relogio.Deslocamento, out var recusa) is not { } pacote)
        {
            log.LogWarning("Pacote descartado: {Recusa}", recusa);
            return;
        }

        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var resultado = await escopo.ServiceProvider.GetRequiredService<RegistrarPacote>().ExecutarAsync(pacote, ct);
            if (resultado is ResultadoDoPacote.ForaDoPrazo or ResultadoDoPacote.CasaInativa)
            {
                log.LogWarning("Pacote {Seq} descartado: {Resultado}", pacote.Seq, resultado);
            }
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            log.LogError(erro, "Falha ao registrar o pacote {Seq}", pacote.Seq);
        }
    }

    private static async Task Esperar(TimeSpan intervalo, CancellationToken ct)
    {
        try
        {
            await Task.Delay(intervalo, ct);
        }
        catch (OperationCanceledException)
        {
        }
    }
}

public sealed class SaudeDoMqtt(ConexaoMqtt conexao) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(conexao.Cliente.IsConnected ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("broker MQTT desconectado"));
}
