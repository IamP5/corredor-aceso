using Corredor.Contratos;
using Microsoft.Extensions.Logging;

namespace Corredor.Motor.Infra;

// O Cadastro implementa IReceptorDeContato e registra depois do Motor, entao a implementacao dele vence.
// Esta so vale enquanto o Motor roda sem o Cadastro: o pedido vira um aviso no log, sem token.
public sealed class SemReceptorDeContato(ILogger<SemReceptorDeContato> log) : IReceptorDeContato
{
    public Task PedirContatoAsync(PedidoDeContato pedido, CancellationToken ct)
    {
        log.LogWarning("Pedido de contato {Motivo} sem receptor registrado", pedido.Motivo);
        return Task.CompletedTask;
    }
}
