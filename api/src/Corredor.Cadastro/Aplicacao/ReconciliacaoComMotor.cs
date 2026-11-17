using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corredor.Cadastro.Aplicacao;

// P22. Ao iniciar, reenvia AtivarCasaAsync para todo idoso com token. AtivarCasa converge, então reenviar é inofensivo
// e conserta uma ativação que falhou depois de o Cadastro gravar o consentimento.
public sealed class ReconciliacaoComMotor(IServiceScopeFactory escopos, ILogger<ReconciliacaoComMotor> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var enviadas = await ReenviarAsync(escopos, ct);
            log.LogInformation("reconciliação com o Motor: {Casas} casas ativas reenviadas", enviadas);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError("reconciliação com o Motor falhou: {Tipo}", e.GetType().Name);
        }
    }

    public static async Task<int> ReenviarAsync(IServiceScopeFactory escopos, CancellationToken ct)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<CadastroDbContext>();
        var motor = escopo.ServiceProvider.GetRequiredService<IMotorDeCasas>();
        var casas = await db.Idosos.AsNoTracking()
            .Where(i => i.Casa.Token != null)
            .Select(i => new { i.Casa.Token, i.Microarea, i.Perfil })
            .ToListAsync(ct);
        foreach (var casa in casas)
        {
            await motor.AtivarCasaAsync(casa.Token!.Value, casa.Microarea, casa.Perfil, ct);
        }

        return casas.Count;
    }
}
