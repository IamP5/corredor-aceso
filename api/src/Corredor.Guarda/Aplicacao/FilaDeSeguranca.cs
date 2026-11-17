using System.Threading.Channels;

namespace Corredor.Guarda;

/// <summary>
/// Fila em memória entre quem registra um evento de segurança e o ProcessadorDeEventos, que
/// grava e traduz depois da resposta HTTP. Enfileirar nunca bloqueia nem toca o banco, então a
/// SaidaSegura pode rodar dentro de um logger sem recursão.
/// </summary>
public sealed class FilaDeSeguranca
{
    private readonly Channel<EventoDeSeguranca> _canal =
        Channel.CreateUnbounded<EventoDeSeguranca>(new UnboundedChannelOptions { SingleReader = true });

    private int _pendentes;

    public void Enfileirar(EventoDeSeguranca evento)
    {
        Interlocked.Increment(ref _pendentes);
        _canal.Writer.TryWrite(evento);
    }

    internal ChannelReader<EventoDeSeguranca> Leitor => _canal.Reader;

    internal void Concluir() => Interlocked.Decrement(ref _pendentes);

    /// <summary>Quantos eventos ainda não foram gravados. Os testes esperam chegar a zero.</summary>
    public int Pendentes => Volatile.Read(ref _pendentes);
}
