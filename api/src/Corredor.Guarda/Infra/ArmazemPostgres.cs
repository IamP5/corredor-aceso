using Microsoft.EntityFrameworkCore;

namespace Corredor.Guarda.Infra;

/// <summary>Eventos de segurança no schema guarda. Só INSERT e o anexo da mensagem.</summary>
public sealed class ArmazemPostgres(GuardaDbContext db) : IArmazemDeEventos
{
    public async Task GravarAsync(EventoDeSeguranca evento, CancellationToken ct)
    {
        db.Eventos.Add(evento);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    public async Task AnexarMensagemAsync(Guid id, MensagemAoCidadao mensagem, CancellationToken ct)
    {
        var evento = await db.Eventos.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (evento is not null && evento.AnexarMensagem(mensagem))
        {
            await db.SaveChangesAsync(ct);
        }

        db.ChangeTracker.Clear();
    }

    public async Task<IReadOnlyList<EventoDeSeguranca>> EventosDoPainelAsync(DateTimeOffset desde, int limite, CancellationToken ct) =>
        await db.Eventos.AsNoTracking()
            .Where(e => e.OcorridoEm >= desde.ToUniversalTime())
            .OrderByDescending(e => e.OcorridoEm)
            .Take(limite)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ContagemDoPainel>> ContagemDoPainelAsync(DateTimeOffset desde, CancellationToken ct) =>
        await db.Eventos
            .Where(e => e.OcorridoEm >= desde.ToUniversalTime())
            .GroupBy(e => new { e.Tipo, e.Canal, e.Severidade })
            .Select(g => new ContagemDoPainel(g.Key.Tipo, g.Key.Canal, g.Key.Severidade, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EventoDeSeguranca>> SemMensagemAsync(CancellationToken ct) =>
        await db.Eventos.AsNoTracking().Where(e => e.Mensagem == null).ToListAsync(ct);
}
