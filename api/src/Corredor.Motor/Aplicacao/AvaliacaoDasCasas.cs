using Corredor.Contratos;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;

namespace Corredor.Motor.Aplicacao;

public sealed record CasaAvaliada(CasaMonitorada Casa, HistoricoDaCasa Historico, Avaliacao Avaliacao);

public sealed record FotoDasCasas(DateTimeOffset Agora, DiaCritico? Dia, TurnoRadar? Turno, IReadOnlyList<CasaAvaliada> Casas)
{
    public bool TurnoAberto => Turno?.EstaAberto(Agora) ?? false;
}

// Carrega as casas ativas, os eventos da avaliacao e o dia critico de hoje, e avalia cada casa (secao 3.3 do modelo de dados).
// O radar filtra por microarea. O VigiaDoRadar e o encerramento leem todas.
public sealed class AvaliacaoDasCasas(MotorDbContext bd, AvaliadorDeCasas avaliador, ParametrosDoMotor parametros)
{
    public async Task<FotoDasCasas> FotografarAsync(DateTimeOffset agora, MicroareaId? microarea, CancellationToken ct)
    {
        var hoje = HorarioDeSaoPaulo.Data(agora);
        var dia = await bd.DiasCriticos.AsNoTracking().SingleOrDefaultAsync(d => d.Data == hoje, ct);
        return await FotografarAsync(agora, dia?.Turno(parametros), dia, microarea, ct);
    }

    public async Task<FotoDasCasas> FotografarAsync(DateTimeOffset agora, TurnoRadar? turno, DiaCritico? dia, MicroareaId? microarea, CancellationToken ct)
    {
        var casasAtivas = bd.Casas.AsNoTracking().Where(c => c.DesativadaEm == null);
        if (microarea is { } m)
        {
            casasAtivas = casasAtivas.Where(c => c.Microarea == m);
        }

        var casas = await casasAtivas.ToListAsync(ct);

        // As ultimas 24 horas mais as leituras de tensao baixa de qualquer idade, pelo mesmo criterio do Expurgo.
        var desde = agora - parametros.Retencao;
        var minima = parametros.LimiteDeTensao;
        var eventos = await bd.Eventos.AsNoTracking()
            .Where(e => casasAtivas.Any(c => c.Token == e.CasaToken))
            .Where(e => e.OcorridoEm >= desde || e.TensaoV < minima)
            .Select(e => new LeituraDeEvento(e.CasaToken, e.Tipo, e.OcorridoEm, e.TempC, e.TensaoV))
            .ToListAsync(ct);
        var eventosPorCasa = eventos.ToLookup(e => e.CasaToken);

        var avaliadas = casas
            .Select(casa =>
            {
                var historico = HistoricoDaCasa.De(eventosPorCasa[casa.Token], casa.Confirmacoes.Where(c => c.Quando >= desde));
                return new CasaAvaliada(casa, historico, avaliador.Avaliar(casa, historico, turno, agora, parametros));
            })
            .ToList();
        return new FotoDasCasas(agora, dia, turno, avaliadas);
    }
}
