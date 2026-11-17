namespace Corredor.Guarda;

/// <summary>Onde os eventos de segurança ficam. Postgres na API, memória nos testes.</summary>
public interface IArmazemDeEventos
{
    Task GravarAsync(EventoDeSeguranca evento, CancellationToken ct);

    /// <summary>Anexa a mensagem ao cidadão, uma vez. Repetir não muda nada.</summary>
    Task AnexarMensagemAsync(Guid id, MensagemAoCidadao mensagem, CancellationToken ct);

    Task<IReadOnlyList<EventoDeSeguranca>> EventosDoPainelAsync(DateTimeOffset desde, int limite, CancellationToken ct);

    Task<IReadOnlyList<ContagemDoPainel>> ContagemDoPainelAsync(DateTimeOffset desde, CancellationToken ct);

    Task<IReadOnlyList<EventoDeSeguranca>> SemMensagemAsync(CancellationToken ct);
}
