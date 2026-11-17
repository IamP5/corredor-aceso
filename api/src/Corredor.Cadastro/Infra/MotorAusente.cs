using Corredor.Contratos;

namespace Corredor.Cadastro.Infra;

// Registrado com TryAdd depois do AddMotor: só vale quando nenhum módulo implementou IMotorDeCasas.
// Toda chamada falha como transporte, e o Cadastro responde 503 MOTOR_INDISPONIVEL com o registro gravado,
// que é o que a rota promete quando o Motor não responde. Com o Motor registrado, esta classe nunca roda.
internal sealed class MotorAusente : IMotorDeCasas
{
    public Task AtivarCasaAsync(CasaToken token, MicroareaId microarea, PerfilDaCasa perfil, CancellationToken ct) => Falhar();

    public Task DefinirPerfilAsync(CasaToken token, PerfilDaCasa perfil, CancellationToken ct) => Falhar();

    public Task ConfirmarAsync(CasaToken token, Confirmacao confirmacao, CancellationToken ct) => Falhar();

    public Task DesativarCasaAsync(CasaToken token, CancellationToken ct) => Falhar();

    private static Task Falhar() => Task.FromException(new InvalidOperationException("nenhum módulo registrou IMotorDeCasas"));
}
