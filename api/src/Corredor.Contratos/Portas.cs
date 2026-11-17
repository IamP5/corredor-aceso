namespace Corredor.Contratos;

// Implementada pelo Motor e chamada pelo Cadastro. O host liga as duas pontas.
public interface IMotorDeCasas
{
    Task AtivarCasaAsync(CasaToken token, MicroareaId microarea, PerfilDaCasa perfil, CancellationToken ct);
    Task DefinirPerfilAsync(CasaToken token, PerfilDaCasa perfil, CancellationToken ct);
    Task ConfirmarAsync(CasaToken token, Confirmacao confirmacao, CancellationToken ct);
    Task DesativarCasaAsync(CasaToken token, CancellationToken ct);
}

// Declarada pelo Motor e implementada pelo Cadastro: inversão de dependência.
public interface IReceptorDeContato
{
    Task PedirContatoAsync(PedidoDeContato pedido, CancellationToken ct);
}
