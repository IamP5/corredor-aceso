using Corredor.Cadastro.Dominio;
using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Corredor.Cadastro.Aplicacao;

internal static class Persistencia
{
    // Seção 2.6: com conflito de versão, limpa o rastreador e roda o comando de novo, uma vez.
    // Todo comando do Cadastro converge, então a segunda rodada devolve o que já existe.
    public static async Task<T> ComUmaRepeticaoAsync<T>(CadastroDbContext db, Func<Task<T>> comando)
    {
        try
        {
            return await comando();
        }
        catch (Exception e) when (e is DbUpdateConcurrencyException || ChaveRepetida(e))
        {
            db.ChangeTracker.Clear();
            return await comando();
        }
    }

    public static bool ChaveRepetida(Exception e) =>
        e is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } };

    public static async Task<Idoso> IdosoAsync(this CadastroDbContext db, IdosoId id, CancellationToken ct) =>
        await db.Idosos.SingleOrDefaultAsync(i => i.Id == id, ct) ?? throw Erros.IdosoDesconhecido();

    public static Task<Idoso?> IdosoDoTokenAsync(this CadastroDbContext db, CasaToken token, CancellationToken ct) =>
        db.Idosos.SingleOrDefaultAsync(i => i.Casa.Token == token, ct);
}

// O Cadastro grava primeiro e chama o Motor depois. Os dois schemas não têm transação comum.
// Uma falha de transporte vira MOTOR_INDISPONIVEL, e repetir o comando conserta, porque IMotorDeCasas converge.
// Uma recusa de regra do Motor (RegraDeDominioViolada) segue para o chamador decidir.
internal static class ChamadaAoMotor
{
    public static async Task ExecutarAsync(Func<Task> chamada)
    {
        try
        {
            await chamada();
        }
        catch (Exception e) when (e is not RegraDeDominioViolada and not OperationCanceledException)
        {
            throw Erros.MotorIndisponivel();
        }
    }
}
