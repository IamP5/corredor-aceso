namespace Corredor.Motor.Dominio;

// O unico lugar do fuso America/Sao_Paulo: a data de hoje e o corte das janelas.
public static class HorarioDeSaoPaulo
{
    public static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTimeOffset Local(DateTimeOffset instante) => TimeZoneInfo.ConvertTime(instante, Fuso);

    public static DateOnly Data(DateTimeOffset instante) => DateOnly.FromDateTime(Local(instante).DateTime);

    public static DateTimeOffset Em(DateOnly data, TimeOnly hora)
    {
        var local = data.ToDateTime(hora);
        return new DateTimeOffset(local, Fuso.GetUtcOffset(local));
    }

    public static DateTimeOffset InicioDoDia(DateOnly data) => Em(data, TimeOnly.MinValue);
}

public sealed record JanelaDeContato(DateTimeOffset Inicio, DateTimeOffset Fim, DateTimeOffset? Prazo)
{
    public bool Contem(DateTimeOffset instante) => instante >= Inicio && instante < Fim;

    // Para o Acamado a janela vence no inicio: ele nao passa no corredor.
    public bool VencidaEm(DateTimeOffset agora, bool venceNoInicio) =>
        Prazo is { } prazo && (venceNoInicio ? Inicio : prazo) <= agora;
}

public sealed record TurnoRadar(
    DateTimeOffset AbertoEm,
    DateTimeOffset FimPrevisto,
    DateTimeOffset? EncerradoEm,
    JanelaDeContato? Manha,
    JanelaDeContato? Tarde)
{
    public bool EstaAberto(DateTimeOffset agora) => EncerradoEm is null && agora >= AbertoEm && agora < FimPrevisto;

    public IEnumerable<JanelaDeContato> JanelasComPrazo =>
        new[] { Manha, Tarde }.OfType<JanelaDeContato>();
}

// Corta o dia de Sao Paulo em janelas: 00h, a abertura do turno, FimDaManha, FimDaTarde e 24h (secao 5.2).
public sealed class CalendarioDoDia
{
    private CalendarioDoDia(IReadOnlyList<JanelaDeContato> janelas) => Janelas = janelas;

    public IReadOnlyList<JanelaDeContato> Janelas { get; }

    public static CalendarioDoDia De(DateOnly dia, TurnoRadar? turno, ParametrosDoMotor parametros)
    {
        var inicio = HorarioDeSaoPaulo.InicioDoDia(dia);
        var fim = HorarioDeSaoPaulo.InicioDoDia(dia.AddDays(1));
        var cortes = new SortedSet<DateTimeOffset>
        {
            inicio,
            HorarioDeSaoPaulo.Em(dia, parametros.FimDaManha),
            HorarioDeSaoPaulo.Em(dia, parametros.FimDaTarde),
            fim,
        };
        if (turno is not null && turno.AbertoEm > inicio && turno.AbertoEm < fim)
        {
            cortes.Add(turno.AbertoEm);
        }

        var comPrazo = turno?.JanelasComPrazo.ToList() ?? [];
        var janelas = cortes.Zip(cortes.Skip(1), (de, ate) =>
            comPrazo.FirstOrDefault(j => j.Inicio == de && j.Fim == ate) ?? new JanelaDeContato(de, ate, null));
        return new CalendarioDoDia(janelas.ToList());
    }

    public JanelaDeContato JanelaAtual(DateTimeOffset agora) => Janelas.First(j => j.Contem(agora));

    public JanelaDeContato? UltimaVencida(DateTimeOffset agora, bool venceNoInicio) =>
        Janelas.LastOrDefault(j => j.VencidaEm(agora, venceNoInicio));
}
