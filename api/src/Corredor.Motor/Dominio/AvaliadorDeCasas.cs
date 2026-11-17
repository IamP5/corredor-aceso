using Corredor.Contratos;

namespace Corredor.Motor.Dominio;

// A situacao de uma casa num instante. Os construtores so montam estados que a secao 5.6 permite:
// com alerta a situacao e a do alerta, e Verde nunca tem alerta.
public sealed record Avaliacao
{
    private Avaliacao(SituacaoDaCasa situacao, Alerta? alerta, int? ordemDaRegra, bool atendida)
    {
        Situacao = situacao;
        Alerta = alerta;
        OrdemDaRegra = ordemDaRegra;
        Atendida = atendida;
    }

    public SituacaoDaCasa Situacao { get; }
    public Alerta? Alerta { get; }
    public int? OrdemDaRegra { get; }
    public bool Atendida { get; }
    public bool Pendente => Alerta is not null && !Atendida;

    public static Avaliacao ComAlerta(Alerta alerta, int ordemDaRegra, bool atendida) =>
        new(alerta.Situacao, alerta, ordemDaRegra, atendida);

    public static Avaliacao Verde() => new(SituacaoDaCasa.Verde, null, null, false);

    public static Avaliacao Aguardando() => new(SituacaoDaCasa.Aguardando, null, null, false);
}

// Servico de dominio puro: nao le banco nem relogio. Nao conhece nenhuma regra concreta.
public sealed class AvaliadorDeCasas
{
    private readonly IReadOnlyList<RegraDeContato> _regras;

    public AvaliadorDeCasas(IEnumerable<RegraDeContato> regras) =>
        _regras = regras.OrderBy(r => r.Ordem).ToList();

    public static AvaliadorDeCasas Padrao { get; } = new(
        [new EnergiaEletrodependente(), new SemHeartbeat(), new CalorInterno(), new SemMovimentoNaJanela()]);

    public Avaliacao Avaliar(CasaMonitorada casa, HistoricoDaCasa historico, TurnoRadar? turno, DateTimeOffset agora, ParametrosDoMotor parametros)
    {
        var contexto = new ContextoDaAvaliacao(casa, historico, turno, agora, parametros);

        // Passos 1 e 2: a primeira regra com alerta decide. A confirmacao atende so na janela atual e depois do Desde.
        foreach (var regra in _regras)
        {
            if (regra.Avaliar(contexto) is not { } alerta)
            {
                continue;
            }

            var limite = alerta.Desde > contexto.JanelaAtual.Inicio ? alerta.Desde : contexto.JanelaAtual.Inicio;
            var atendida = contexto.Historico.Confirmacoes.Any(c => c.Quando >= limite && regra.AceitaConfirmacao(c));
            return Avaliacao.ComAlerta(alerta, regra.Ordem, atendida);
        }

        // Passos 3 e 4: Verde so com turno aberto e sinal de vida na janela atual.
        return contexto.TurnoAberto && contexto.Historico.TemSinalDeVidaDesde(contexto.JanelaAtual.Inicio, contexto.Contato)
            ? Avaliacao.Verde()
            : Avaliacao.Aguardando();
    }
}
