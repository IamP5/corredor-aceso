using Corredor.Contratos;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Corredor.Motor.Aplicacao;

// Os quatro comandos que o Cadastro manda. Todos convergem, entao o Cadastro pode repetir qualquer um.
public sealed class MotorDeCasas(MotorDbContext bd, TimeProvider relogio, ParametrosDoRelogio relogioDoContrato, ParametrosDoMotor parametros)
    : IMotorDeCasas
{
    public Task AtivarCasaAsync(CasaToken token, MicroareaId microarea, PerfilDaCasa perfil, CancellationToken ct) =>
        ComUmaRepeticao(async () =>
        {
            if (await Carregar(token, ct) is { } casa)
            {
                casa.DefinirPerfil(perfil);
            }
            else
            {
                bd.Casas.Add(CasaMonitorada.Ativar(token, microarea, perfil, relogio.GetUtcNow()));
            }

            await bd.SaveChangesAsync(ct);
        });

    public Task DefinirPerfilAsync(CasaToken token, PerfilDaCasa perfil, CancellationToken ct) =>
        ComUmaRepeticao(async () =>
        {
            var casa = await Carregar(token, ct) ?? throw Violacoes.CasaDesconhecida();
            casa.DefinirPerfil(perfil);
            await bd.SaveChangesAsync(ct);
        });

    public Task ConfirmarAsync(CasaToken token, Confirmacao confirmacao, CancellationToken ct) =>
        ComUmaRepeticao(async () =>
        {
            var casa = await Carregar(token, ct) ?? throw Violacoes.CasaDesconhecida();
            if (casa.Confirmar(confirmacao, relogio.GetUtcNow(), relogioDoContrato.ToleranciaDoRelogio, parametros.Retencao))
            {
                await bd.SaveChangesAsync(ct);
            }
        });

    // Grava a desativacao e apaga confirmacoes e eventos na mesma transacao. Casa desconhecida nao tem o que desligar.
    public Task DesativarCasaAsync(CasaToken token, CancellationToken ct) =>
        ComUmaRepeticao(async () =>
        {
            await using var tx = await bd.Database.BeginTransactionAsync(ct);
            if (await Carregar(token, ct) is not { } casa)
            {
                return;
            }

            casa.Desativar(relogio.GetUtcNow());
            await bd.SaveChangesAsync(ct);
            await bd.Eventos.Where(e => e.CasaToken == token).ExecuteDeleteAsync(ct);
            await tx.CommitAsync(ct);
        });

    private Task<CasaMonitorada?> Carregar(CasaToken token, CancellationToken ct) =>
        bd.Casas.SingleOrDefaultAsync(c => c.Token == token, ct);

    // Secao 2.6 do modelo de dados: com chave repetida no banco, outro pedido igual ganhou a corrida.
    // Limpa o rastreador e roda o comando de novo uma vez. A segunda rodada encontra o que ja existe.
    private async Task ComUmaRepeticao(Func<Task> comando)
    {
        try
        {
            await comando();
        }
        catch (DbUpdateException erro) when (erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            bd.ChangeTracker.Clear();
            await comando();
        }
    }
}
