namespace Corredor.Motor.Dominio;

public enum TipoDePacote { Movimento, BotaoBem, BotaoSair, Telemetria, Heartbeat }

public enum OrigemDoDiaCritico { Modelo, RegraBase, Manual }

public enum SituacaoDaCasa { Verde, Aguardando, Vermelho, Cinza }

// TempC e TensaoV declaram os operadores de comparacao para o LINQ traduzir e.TensaoV < minima.
public readonly record struct TempC(decimal Valor)
{
    public const decimal Minima = -10m;
    public const decimal Maxima = 60m;

    // Invariante 5 de EventoSensor: a leitura fora da faixa fica ausente.
    public static TempC? Ler(decimal? valor) =>
        valor is { } v && v >= Minima && v <= Maxima ? new TempC(decimal.Round(v, 1)) : null;

    public static bool operator <(TempC a, TempC b) => a.Valor < b.Valor;
    public static bool operator >(TempC a, TempC b) => a.Valor > b.Valor;
    public static bool operator <=(TempC a, TempC b) => a.Valor <= b.Valor;
    public static bool operator >=(TempC a, TempC b) => a.Valor >= b.Valor;
}

public readonly record struct TensaoV(decimal Valor)
{
    public const decimal Minima = 0m;
    public const decimal Maxima = 260m;

    public static TensaoV? Ler(decimal? valor) =>
        valor is { } v && v >= Minima && v <= Maxima ? new TensaoV(decimal.Round(v, 1)) : null;

    public static bool operator <(TensaoV a, TensaoV b) => a.Valor < b.Valor;
    public static bool operator >(TensaoV a, TensaoV b) => a.Valor > b.Valor;
    public static bool operator <=(TensaoV a, TensaoV b) => a.Valor <= b.Valor;
    public static bool operator >=(TensaoV a, TensaoV b) => a.Valor >= b.Valor;
}
