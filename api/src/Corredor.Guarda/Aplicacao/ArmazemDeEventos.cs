using System.Collections.Concurrent;

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

public sealed class ArmazemEmMemoria : IArmazemDeEventos
{
    private readonly ConcurrentDictionary<Guid, EventoDeSeguranca> _eventos = new();

    public IReadOnlyCollection<EventoDeSeguranca> Todos => _eventos.Values.ToList();

    public Task GravarAsync(EventoDeSeguranca evento, CancellationToken ct)
    {
        _eventos.TryAdd(evento.Id, evento);
        return Task.CompletedTask;
    }

    public Task AnexarMensagemAsync(Guid id, MensagemAoCidadao mensagem, CancellationToken ct)
    {
        if (_eventos.TryGetValue(id, out var evento))
        {
            lock (evento)
            {
                evento.AnexarMensagem(mensagem);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EventoDeSeguranca>> EventosDoPainelAsync(DateTimeOffset desde, int limite, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<EventoDeSeguranca>>(
            _eventos.Values.Where(e => e.OcorridoEm >= desde).OrderByDescending(e => e.OcorridoEm).Take(limite).ToList());

    public Task<IReadOnlyList<ContagemDoPainel>> ContagemDoPainelAsync(DateTimeOffset desde, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ContagemDoPainel>>(
            _eventos.Values.Where(e => e.OcorridoEm >= desde)
                .GroupBy(e => (e.Tipo, e.Canal, e.Severidade))
                .Select(g => new ContagemDoPainel(g.Key.Tipo, g.Key.Canal, g.Key.Severidade, g.Count()))
                .ToList());

    public Task<IReadOnlyList<EventoDeSeguranca>> SemMensagemAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<EventoDeSeguranca>>(_eventos.Values.Where(e => e.Mensagem is null).ToList());
}
