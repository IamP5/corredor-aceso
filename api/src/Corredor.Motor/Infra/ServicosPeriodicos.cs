using System.Reflection;
using Corredor.Motor.Aplicacao;
using Corredor.Motor.Dominio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corredor.Motor.Infra;

// O SQL dos gatilhos e do expurgo mora em Infra/Sql, embutido no assembly.
public static class RecursoDoMotor
{
    public static string Ler(string nome)
    {
        using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream(nome)
            ?? throw new InvalidOperationException($"recurso ausente: {nome}");
        using var leitor = new StreamReader(fluxo);
        return leitor.ReadToEnd();
    }
}

// Roda uma tarefa num escopo novo a cada intervalo. Uma rodada que falha vira log e nao derruba o host.
public abstract class ServicoPeriodico<T>(IServiceScopeFactory escopos, TimeSpan intervalo, ILogger log) : BackgroundService
    where T : notnull
{
    protected abstract Task RodarAsync(T tarefa, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var temporizador = new PeriodicTimer(intervalo);
        do
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                await RodarAsync(escopo.ServiceProvider.GetRequiredService<T>(), ct);
            }
            catch (Exception erro) when (!ct.IsCancellationRequested)
            {
                log.LogError(erro, "Rodada de {Servico} falhou", typeof(T).Name);
            }
        }
        while (await EsperarAsync(temporizador, ct));
    }

    private static async Task<bool> EsperarAsync(PeriodicTimer temporizador, CancellationToken ct)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

public sealed class VigiaDoRadar(IServiceScopeFactory escopos, ParametrosDoMotor parametros, ILogger<VigiaDoRadar> log)
    : ServicoPeriodico<CicloDoVigia>(escopos, parametros.IntervaloDoVigia, log)
{
    protected override Task RodarAsync(CicloDoVigia ciclo, CancellationToken ct) => ciclo.RodarAsync(ct);
}

public sealed class Expurgo(IServiceScopeFactory escopos, ParametrosDoMotor parametros, ILogger<Expurgo> log)
    : ServicoPeriodico<ExpurgarDados>(escopos, parametros.IntervaloDoExpurgo, log)
{
    protected override Task RodarAsync(ExpurgarDados expurgo, CancellationToken ct) => expurgo.ExecutarAsync(ct);
}
