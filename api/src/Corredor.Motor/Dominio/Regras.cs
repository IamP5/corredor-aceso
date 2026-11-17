using Corredor.Contratos;

namespace Corredor.Motor.Dominio;

// O que uma regra devolve quando pega a casa. Calculado, nunca gravado.
public sealed record Alerta(SituacaoDaCasa Situacao, MotivoDoAlerta Motivo, DateTimeOffset Desde);

// Tudo o que uma regra pode ler, ja cortado no instante da avaliacao.
public sealed class ContextoDaAvaliacao
{
    public ContextoDaAvaliacao(CasaMonitorada casa, HistoricoDaCasa historico, TurnoRadar? turno, DateTimeOffset agora, ParametrosDoMotor parametros)
    {
        Casa = casa;
        Historico = historico.Ate(agora);
        Turno = turno;
        Agora = agora;
        Parametros = parametros;
        TurnoAberto = turno?.EstaAberto(agora) ?? false;
        Calendario = CalendarioDoDia.De(HorarioDeSaoPaulo.Data(agora), turno, parametros);
        JanelaAtual = Calendario.JanelaAtual(agora);
    }

    public CasaMonitorada Casa { get; }
    public HistoricoDaCasa Historico { get; }
    public TurnoRadar? Turno { get; }
    public DateTimeOffset Agora { get; }
    public ParametrosDoMotor Parametros { get; }
    public bool TurnoAberto { get; }
    public CalendarioDoDia Calendario { get; }
    public JanelaDeContato JanelaAtual { get; }

    public PerfilDeContato Contato => Casa.Perfil.Contato;

    public bool Silencioso(TimeSpan limite) =>
        Casa.UltimoPacoteEm is not { } ultimo || Agora - ultimo > limite;
}

public abstract class RegraDeContato
{
    public abstract int Ordem { get; }

    public abstract MotivoDoAlerta Motivo { get; }

    public abstract bool ExigeTurnoAberto { get; }

    public Alerta? Avaliar(ContextoDaAvaliacao contexto) =>
        ExigeTurnoAberto && !contexto.TurnoAberto ? null : AvaliarNucleo(contexto);

    // Coluna "Confirmacao que atende" da secao 5.4. A maioria das regras aceita quem viu a pessoa.
    public virtual bool AceitaConfirmacao(Confirmacao confirmacao) =>
        confirmacao.Resultado == ResultadoDaConfirmacao.PessoaVista;

    protected abstract Alerta? AvaliarNucleo(ContextoDaAvaliacao contexto);

    protected Alerta Alerta(SituacaoDaCasa situacao, DateTimeOffset desde) => new(situacao, Motivo, desde);
}

// Regra 1. A casa eletrodependente fica vermelha com tensao baixa, com a trava, ou com o aparelho mudo por 60 segundos.
public sealed class EnergiaEletrodependente : RegraDeContato
{
    public override int Ordem => 1;
    public override MotivoDoAlerta Motivo => MotivoDoAlerta.EnergiaEletrodependente;
    public override bool ExigeTurnoAberto => false;

    public override bool AceitaConfirmacao(Confirmacao confirmacao) =>
        confirmacao is { Origem: OrigemDaConfirmacao.Acs, Resultado: ResultadoDaConfirmacao.PessoaVista };

    protected override Alerta? AvaliarNucleo(ContextoDaAvaliacao c)
    {
        if (!c.Casa.Perfil.Eletrodependente || c.Casa.UltimoPacoteEm is not { } ultimoPacote)
        {
            return null;
        }

        var minima = c.Parametros.LimiteDeTensao;
        if (c.Historico.UltimaTensao < minima && c.Historico.InicioDaSequenciaBaixa(minima) is { } inicioBaixa)
        {
            return Alerta(SituacaoDaCasa.Vermelho, inicioBaixa);
        }

        if (c.Historico.PrimeiraLeituraBaixaSemConfirmacao(minima) is { } trava)
        {
            return Alerta(SituacaoDaCasa.Vermelho, trava);
        }

        return c.Silencioso(c.Parametros.SilencioMaximoEletrodependente)
            ? Alerta(SituacaoDaCasa.Vermelho, ultimoPacote)
            : null;
    }
}

// Regra 2. Vem antes de toda regra que olha sinal de vida: o aparelho mudo nunca vira verde.
public sealed class SemHeartbeat : RegraDeContato
{
    public override int Ordem => 2;
    public override MotivoDoAlerta Motivo => MotivoDoAlerta.SemHeartbeat;
    public override bool ExigeTurnoAberto => false;

    protected override Alerta? AvaliarNucleo(ContextoDaAvaliacao c) =>
        c.Silencioso(c.Parametros.SilencioMaximo)
            ? Alerta(SituacaoDaCasa.Cinza, c.Casa.UltimoPacoteEm ?? c.Casa.AtivadaEm)
            : null;
}

// Regra 3. Temperatura em TemperaturaDeCalor ou mais, sem interrupcao, ha DuracaoDoCalor ou mais.
public sealed class CalorInterno : RegraDeContato
{
    public override int Ordem => 3;
    public override MotivoDoAlerta Motivo => MotivoDoAlerta.CalorInterno;
    public override bool ExigeTurnoAberto => true;

    protected override Alerta? AvaliarNucleo(ContextoDaAvaliacao c) =>
        c.Historico.InicioDaSequenciaQuente(c.Parametros.LimiteDeCalor) is { } inicio
        && c.Agora - inicio >= c.Parametros.DuracaoDoCalor
            ? Alerta(SituacaoDaCasa.Vermelho, inicio + c.Parametros.DuracaoDoCalor)
            : null;
}

// Regra 4. Sozinho ou Acamado sem sinal de vida desde o inicio da ultima janela vencida.
public sealed class SemMovimentoNaJanela : RegraDeContato
{
    public override int Ordem => 4;
    public override MotivoDoAlerta Motivo => MotivoDoAlerta.SemMovimentoNaJanela;
    public override bool ExigeTurnoAberto => true;

    // So a ausencia confirmada pela ACS atende. PessoaVista e sinal de vida e desfaz o alerta.
    public override bool AceitaConfirmacao(Confirmacao confirmacao) =>
        confirmacao is { Origem: OrigemDaConfirmacao.Acs, Resultado: ResultadoDaConfirmacao.AusenciaConfirmada };

    protected override Alerta? AvaliarNucleo(ContextoDaAvaliacao c)
    {
        if (c.Contato == PerfilDeContato.Coabitado)
        {
            return null;
        }

        // A saida avisada vale so para o Sozinho e suspende esta regra ate o fim do turno.
        if (c.Contato == PerfilDeContato.Sozinho && c.Historico.TeveSaidaAvisadaDesde(c.Turno!.AbertoEm))
        {
            return null;
        }

        var acamado = c.Contato == PerfilDeContato.Acamado;
        if (c.Calendario.UltimaVencida(c.Agora, venceNoInicio: acamado) is not { } vencida
            || c.Historico.TemSinalDeVidaDesde(vencida.Inicio, c.Contato))
        {
            return null;
        }

        return Alerta(SituacaoDaCasa.Vermelho, acamado ? vencida.Inicio : vencida.Prazo!.Value);
    }
}
