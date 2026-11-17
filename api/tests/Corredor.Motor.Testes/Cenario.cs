using Corredor.Contratos;
using Corredor.Motor.Dominio;

namespace Corredor.Motor.Testes;

public enum Desfecho { EstavaBem, EstavaFora, NaoAbriu, Encaminhada }

// Monta os fatos de uma casa em 18/11/2026 e avalia pela superficie publica do dominio.
// Sem Mudo() ou NuncaMandouPacote(), o aparelho esta ligado: o ultimo pacote chegou 5 segundos antes da avaliacao.
public sealed class Cenario
{
    public static readonly DateOnly Dia = new(2026, 11, 18);
    public static readonly ParametrosDoMotor Parametros = ParametrosDoMotor.Producao;

    private readonly PerfilDaCasa _perfil;
    private readonly List<LeituraDeEvento> _eventos = [];
    private readonly List<Confirmacao> _confirmacoes = [];
    private DateTimeOffset? _abertura = H(5);
    private DateTimeOffset? _ultimoPacoteFixo;
    private bool _nuncaMandouPacote;

    private Cenario(PerfilDaCasa perfil) => _perfil = perfil;

    public static CasaToken Token { get; } = CasaToken.Novo();

    public static DateTimeOffset AtivadaEm => H(0).AddHours(-12);

    public static DateTimeOffset H(int hora, int minuto = 0, int segundo = 0) =>
        HorarioDeSaoPaulo.Em(Dia, new TimeOnly(hora, minuto, segundo));

    public static Cenario Sozinho() => new(new PerfilDaCasa(PerfilDeContato.Sozinho, false));

    public static Cenario Acamado() => new(new PerfilDaCasa(PerfilDeContato.Acamado, false));

    public static Cenario Coabitado() => new(new PerfilDaCasa(PerfilDeContato.Coabitado, false));

    public static Cenario Eletrodependente(PerfilDeContato contato = PerfilDeContato.Sozinho) => new(new PerfilDaCasa(contato, true));

    public Cenario DiaComum()
    {
        _abertura = null;
        return this;
    }

    public Cenario TurnoAbertoAs(DateTimeOffset abertura)
    {
        _abertura = abertura;
        return this;
    }

    public Cenario Movimento(DateTimeOffset quando) => Evento(TipoDePacote.Movimento, quando);

    public Cenario BotaoBem(DateTimeOffset quando) => Evento(TipoDePacote.BotaoBem, quando);

    public Cenario SaidaAvisada(DateTimeOffset quando) => Evento(TipoDePacote.BotaoSair, quando);

    public Cenario Telemetria(DateTimeOffset quando, decimal? tempC = null, decimal? tensaoV = null)
    {
        _eventos.Add(new LeituraDeEvento(Token, TipoDePacote.Telemetria, quando, TempC.Ler(tempC), TensaoV.Ler(tensaoV)));
        return this;
    }

    // Uma leitura a cada 5 minutos, de inicio ate fim, inclusive.
    public Cenario TelemetriaDe(DateTimeOffset inicio, DateTimeOffset fim, decimal? tempC = null, decimal? tensaoV = null)
    {
        for (var t = inicio; t <= fim; t = t.AddMinutes(5))
        {
            Telemetria(t, tempC, tensaoV);
        }

        return this;
    }

    // O TradutorDeDesfecho do Cadastro: EstavaBem e Encaminhada viram PessoaVista, EstavaFora vira AusenciaConfirmada, NaoAbriu nao chama o Motor.
    public Cenario Visita(DateTimeOffset quando, Desfecho desfecho)
    {
        ResultadoDaConfirmacao? resultado = desfecho switch
        {
            Desfecho.EstavaBem or Desfecho.Encaminhada => ResultadoDaConfirmacao.PessoaVista,
            Desfecho.EstavaFora => ResultadoDaConfirmacao.AusenciaConfirmada,
            _ => null,
        };
        if (resultado is { } r)
        {
            _confirmacoes.Add(new Confirmacao(Guid.NewGuid(), OrigemDaConfirmacao.Acs, r, quando));
        }

        return this;
    }

    public Cenario VizinhoRespondeEstaBem(DateTimeOffset quando)
    {
        _confirmacoes.Add(new Confirmacao(Guid.NewGuid(), OrigemDaConfirmacao.Vizinho, ResultadoDaConfirmacao.PessoaVista, quando));
        return this;
    }

    public Cenario Confirmacao(OrigemDaConfirmacao origem, ResultadoDaConfirmacao resultado, DateTimeOffset quando)
    {
        _confirmacoes.Add(new Confirmacao(Guid.NewGuid(), origem, resultado, quando));
        return this;
    }

    public Cenario Mudo(DateTimeOffset ultimoPacote)
    {
        _ultimoPacoteFixo = ultimoPacote;
        return this;
    }

    public Cenario NuncaMandouPacote()
    {
        _nuncaMandouPacote = true;
        return this;
    }

    public Avaliacao Em(DateTimeOffset agora)
    {
        var casa = CasaMonitorada.Ativar(Token, new MicroareaId(3), _perfil, AtivadaEm);
        if (!_nuncaMandouPacote)
        {
            casa.RegistrarPacote(_ultimoPacoteFixo ?? agora.AddSeconds(-5));
        }

        return AvaliadorDeCasas.Padrao.Avaliar(casa, HistoricoDaCasa.De(_eventos, _confirmacoes), Turno(), agora, Parametros);
    }

    private TurnoRadar? Turno()
    {
        if (_abertura is not { } abertura)
        {
            return null;
        }

        var previsao = new PrevisaoDoDia(new Probabilidade(0.81m), "minima alta pela segunda noite", OrigemDoDiaCritico.Modelo);
        return DiaCritico.Declarar(Dia, previsao, abertura).Turno(Parametros);
    }

    private Cenario Evento(TipoDePacote tipo, DateTimeOffset quando)
    {
        _eventos.Add(new LeituraDeEvento(Token, tipo, quando, null, null));
        return this;
    }
}
