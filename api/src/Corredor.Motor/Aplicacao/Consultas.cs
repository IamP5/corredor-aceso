using Corredor.Contratos;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;

namespace Corredor.Motor.Aplicacao;

public sealed record CardDoRadar(
    CasaToken CasaToken,
    SituacaoDaCasa Situacao,
    MotivoDoAlerta Motivo,
    DateTimeOffset Desde,
    bool Atendida,
    PerfilDaCasa Perfil,
    DateTimeOffset? UltimoPacoteEm);

public sealed record CasaAguardando(CasaToken CasaToken, bool SaidaAvisada);

public sealed record Radar(
    bool TurnoAberto,
    DateTimeOffset Agora,
    IReadOnlyList<CardDoRadar> Pendentes,
    IReadOnlyList<CardDoRadar> Atendidas,
    IReadOnlyList<CasaAguardando> Aguardando,
    int Verdes);

public sealed record LinhaDoTempo(
    CasaToken CasaToken,
    DateTimeOffset? UltimoPacoteEm,
    IReadOnlyList<EventoSensor> Eventos,
    IReadOnlyList<Confirmacao> Confirmacoes);

public sealed class ConsultarRadar(AvaliacaoDasCasas avaliacao, TimeProvider relogio)
{
    public async Task<Radar> ExecutarAsync(MicroareaId microarea, CancellationToken ct)
    {
        var foto = await avaliacao.FotografarAsync(relogio.GetUtcNow(), microarea, ct);

        // Vermelho antes de Cinza, depois a ordem da regra, depois o Desde mais antigo.
        var comAlerta =
            from c in foto.Casas
            let alerta = c.Avaliacao.Alerta
            where alerta is not null
            orderby alerta.Situacao == SituacaoDaCasa.Vermelho ? 0 : 1, c.Avaliacao.OrdemDaRegra, alerta.Desde
            select (c.Avaliacao.Atendida, Card: new CardDoRadar(
                c.Casa.Token, alerta.Situacao, alerta.Motivo, alerta.Desde, c.Avaliacao.Atendida, c.Casa.Perfil, c.Casa.UltimoPacoteEm));
        var cards = comAlerta.ToList();

        var aguardando = foto.Casas
            .Where(c => c.Avaliacao.Situacao == SituacaoDaCasa.Aguardando)
            .Select(c => new CasaAguardando(c.Casa.Token, foto.Turno is { } t && c.Historico.TeveSaidaAvisadaDesde(t.AbertoEm)))
            .ToList();

        return new Radar(
            foto.TurnoAberto,
            foto.Agora,
            cards.Where(c => !c.Atendida).Select(c => c.Card).ToList(),
            cards.Where(c => c.Atendida).Select(c => c.Card).ToList(),
            aguardando,
            foto.Casas.Count(c => c.Avaliacao.Situacao == SituacaoDaCasa.Verde));
    }
}

// Eventos do sensor e confirmacoes das ultimas 24 horas de uma casa, em ordem de OcorridoEm.
// Casa desativada responde como desconhecida: uma linha vazia confirmaria que o token existiu.
public sealed class ConsultarLinhaDoTempo(MotorDbContext bd, TimeProvider relogio, ParametrosDoMotor parametros)
{
    public async Task<LinhaDoTempo> ExecutarAsync(CasaToken token, CancellationToken ct)
    {
        var desde = relogio.GetUtcNow() - parametros.Retencao;
        var casa = await bd.Casas.AsNoTracking().SingleOrDefaultAsync(c => c.Token == token && c.DesativadaEm == null, ct)
            ?? throw Violacoes.CasaDesconhecida();
        var eventos = await bd.Eventos.AsNoTracking()
            .Where(e => e.CasaToken == token && e.OcorridoEm >= desde)
            .OrderBy(e => e.OcorridoEm).ThenBy(e => e.Seq)
            .ToListAsync(ct);
        var confirmacoes = casa.Confirmacoes.Where(c => c.Quando >= desde).OrderBy(c => c.Quando).ToList();
        return new LinhaDoTempo(token, casa.UltimoPacoteEm, eventos, confirmacoes);
    }
}
