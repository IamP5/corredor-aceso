using Corredor.Contratos;

namespace Corredor.Motor.Dominio;

// Um pacote do no ja traduzido do JSON. As leituras so existem na telemetria e dentro da faixa.
public sealed record PacoteDoNo
{
    public PacoteDoNo(CasaToken token, long seq, TipoDePacote tipo, DateTimeOffset ocorridoEm, decimal? tempC, decimal? tensaoV, bool bufferizado)
    {
        Token = token;
        Seq = seq;
        Tipo = tipo;
        OcorridoEm = ocorridoEm;
        TempC = tipo == TipoDePacote.Telemetria ? Dominio.TempC.Ler(tempC) : null;
        TensaoV = tipo == TipoDePacote.Telemetria ? Dominio.TensaoV.Ler(tensaoV) : null;
        Bufferizado = bufferizado;
    }

    public CasaToken Token { get; }
    public long Seq { get; }
    public TipoDePacote Tipo { get; }
    public DateTimeOffset OcorridoEm { get; }
    public TempC? TempC { get; }
    public TensaoV? TensaoV { get; }
    public bool Bufferizado { get; }

    // Invariante 3 de EventoSensor: ts adiantado alem da tolerancia ou mais velho que a retencao recusa o pacote inteiro.
    public bool DentroDoPrazo(DateTimeOffset recebidoEm, TimeSpan toleranciaDoRelogio, TimeSpan retencao) =>
        OcorridoEm <= recebidoEm + toleranciaDoRelogio && OcorridoEm >= recebidoEm - retencao;
}

// Pacote gravado: movimento, botao ou telemetria. Nunca heartbeat. Imutavel.
public sealed class EventoSensor
{
    private EventoSensor() { }

    public CasaToken CasaToken { get; private set; }
    public long Seq { get; private set; }
    public TipoDePacote Tipo { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }
    public DateTimeOffset RecebidoEm { get; private set; }
    public TempC? TempC { get; private set; }
    public TensaoV? TensaoV { get; private set; }
    public bool Bufferizado { get; private set; }

    public static EventoSensor? De(PacoteDoNo pacote, DateTimeOffset recebidoEm) =>
        pacote.Tipo == TipoDePacote.Heartbeat
            ? null
            : new EventoSensor
            {
                CasaToken = pacote.Token,
                Seq = pacote.Seq,
                Tipo = pacote.Tipo,
                OcorridoEm = pacote.OcorridoEm,
                RecebidoEm = recebidoEm,
                TempC = pacote.TempC,
                TensaoV = pacote.TensaoV,
                Bufferizado = pacote.Bufferizado,
            };
}
