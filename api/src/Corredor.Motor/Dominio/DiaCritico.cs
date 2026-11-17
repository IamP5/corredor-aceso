namespace Corredor.Motor.Dominio;

public readonly record struct Probabilidade
{
    public Probabilidade(decimal valor)
    {
        if (valor is < 0m or > 1m)
        {
            throw Violacoes.ProbabilidadeForaDaFaixa();
        }

        Valor = decimal.Round(valor, 3);
    }

    public decimal Valor { get; }
}

public sealed record PrevisaoDoDia
{
    public const int TamanhoMaximoDoMotivo = 200;

    public PrevisaoDoDia(Probabilidade probabilidade, string motivo, OrigemDoDiaCritico origem)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Length > TamanhoMaximoDoMotivo)
        {
            throw Violacoes.CampoInvalido("O motivo tem de 1 a 200 caracteres.");
        }

        Probabilidade = probabilidade;
        Motivo = motivo;
        Origem = origem;
    }

    public Probabilidade Probabilidade { get; }
    public string Motivo { get; }
    public OrigemDoDiaCritico Origem { get; }
}

public sealed record ResumoDoTurno(
    int CasasAtivas,
    int ComSinalDeVidaAte10h,
    int Atendidas,
    int VermelhasPendentes,
    int CinzasPendentes);

public sealed class DiaCritico
{
    private DiaCritico() { }

    public DateOnly Data { get; private set; }
    public Probabilidade Probabilidade { get; private set; }
    public string Motivo { get; private set; } = null!;
    public OrigemDoDiaCritico Origem { get; private set; }
    public DateTimeOffset AbertoEm { get; private set; }
    public DateTimeOffset? ToquePedidoEm { get; private set; }
    public DateTimeOffset? EncerradoEm { get; private set; }
    public ResumoDoTurno? Resumo { get; private set; }
    public uint Versao { get; private set; }

    // Invariante 2: so a data de hoje, no fuso de Sao Paulo, abre turno.
    public static DiaCritico Declarar(DateOnly data, PrevisaoDoDia previsao, DateTimeOffset agora)
    {
        ExigirHoje(data, agora);
        var dia = new DiaCritico { Data = data, AbertoEm = agora };
        dia.Aplicar(previsao);
        return dia;
    }

    // Invariantes 4 e 5: declarar de novo so troca a previsao. AbertoEm fica, e o turno encerrado nao reabre.
    public void DeclararDeNovo(PrevisaoDoDia previsao, DateTimeOffset agora)
    {
        ExigirHoje(Data, agora);
        if (EncerradoEm is not null)
        {
            throw Violacoes.TurnoEncerrado();
        }

        Aplicar(previsao);
    }

    // Invariante 6: um toque por dia. Devolve verdadeiro so na primeira vez.
    public bool PedirToque(DateTimeOffset agora)
    {
        if (ToquePedidoEm is not null)
        {
            return false;
        }

        ToquePedidoEm = agora;
        return true;
    }

    // Invariante 7: encerrar de novo devolve o mesmo resumo.
    public ResumoDoTurno Encerrar(ResumoDoTurno resumo, DateTimeOffset agora)
    {
        if (EncerradoEm is null)
        {
            EncerradoEm = agora;
            Resumo = resumo;
        }

        return Resumo!;
    }

    public TurnoRadar Turno(ParametrosDoMotor parametros)
    {
        var fimDaManha = HorarioDeSaoPaulo.Em(Data, parametros.FimDaManha);
        var fimDaTarde = HorarioDeSaoPaulo.Em(Data, parametros.FimDaTarde);
        var manha = AbertoEm < fimDaManha ? new JanelaDeContato(AbertoEm, fimDaManha, fimDaManha) : null;
        var inicioDaTarde = AbertoEm > fimDaManha ? AbertoEm : fimDaManha;
        var tarde = AbertoEm < fimDaTarde ? new JanelaDeContato(inicioDaTarde, fimDaTarde, fimDaTarde) : null;
        return new TurnoRadar(AbertoEm, HorarioDeSaoPaulo.Em(Data, parametros.FimDoTurno), EncerradoEm, manha, tarde);
    }

    private static void ExigirHoje(DateOnly data, DateTimeOffset agora)
    {
        if (data != HorarioDeSaoPaulo.Data(agora))
        {
            throw Violacoes.DataNaoEHoje();
        }
    }

    private void Aplicar(PrevisaoDoDia previsao)
    {
        Probabilidade = previsao.Probabilidade;
        Motivo = previsao.Motivo;
        Origem = previsao.Origem;
    }
}
