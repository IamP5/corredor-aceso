using System.Collections.Concurrent;

namespace Corredor.Guarda.Testes;

// Os eventos na memória, para os testes do pipeline HTTP não precisarem de banco.
internal sealed class ArmazemEmMemoria : IArmazemDeEventos
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
