namespace Corredor.Guarda;

/// <summary>Um evento como o painel mostra: sem id, sem rota e sem valor.</summary>
public sealed record EventoNoPainel(
    DateTimeOffset OcorridoEm,
    TipoDeEventoDeSeguranca Tipo,
    Canal Canal,
    string Modulo,
    IReadOnlyList<TipoDeDado> TiposDeDado,
    int Severidade,
    string MensagemAoCidadao,
    OrigemDaMensagem OrigemDaMensagem);

/// <summary>Quantos eventos e quantos pontos cada canal teve nas últimas 24 horas.</summary>
public sealed record CanalNoPainel(Canal Canal, int Eventos, int Bloqueios, int Pontos);

/// <summary>A resposta de GET /api/painel. Nenhum campo carrega o valor de um dado pessoal.</summary>
public sealed record RespostaDoPainel(
    CorDoFarol Cor,
    int Pontos24h,
    IReadOnlyList<CanalNoPainel> Canais,
    IReadOnlyList<EventoNoPainel> Eventos);

/// <summary>Monta o painel Risco Cidadão a partir dos eventos das últimas 24 horas.</summary>
public sealed class Painel(IArmazemDeEventos armazem, TimeProvider relogio)
{
    public const int LimiteDeEventos = 50;

    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<RespostaDoPainel> MontarAsync(CancellationToken ct)
    {
        var desde = relogio.GetUtcNow() - Farol.Janela;
        var contagem = await armazem.ContagemDoPainelAsync(desde, ct);
        var eventos = await armazem.EventosDoPainelAsync(desde, LimiteDeEventos, ct);

        var canais = contagem
            .GroupBy(c => c.Canal)
            .Select(g => new CanalNoPainel(
                g.Key,
                g.Sum(c => c.Quantidade),
                g.Where(c => c.Tipo == TipoDeEventoDeSeguranca.BloqueioDeSaida).Sum(c => c.Quantidade),
                g.Sum(c => c.Pontos)))
            .OrderByDescending(c => c.Pontos)
            .ToList();

        return new RespostaDoPainel(
            Farol.Cor(contagem),
            Farol.Pontos(contagem),
            canais,
            eventos.Select(e => new EventoNoPainel(
                TimeZoneInfo.ConvertTime(e.OcorridoEm, SaoPaulo),
                e.Tipo,
                e.Canal,
                e.Modulo,
                e.TiposDeDado,
                e.Severidade,
                e.Mensagem?.Texto ?? FrasesFixas.DoTipoDeEvento(e.Tipo),
                e.Mensagem?.Origem ?? OrigemDaMensagem.FraseFixa)).ToList());
    }
}
