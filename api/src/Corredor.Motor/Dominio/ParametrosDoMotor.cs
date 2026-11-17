namespace Corredor.Motor.Dominio;

// Todos os numeros das regras (secao 5.9 do modelo de dominio). Os valores iniciais sao os de producao.
public sealed class ParametrosDoMotor
{
    public const string Secao = "Motor";

    public TimeSpan IntervaloDoHeartbeat { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan SilencioMaximo { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan SilencioMaximoEletrodependente { get; init; } = TimeSpan.FromSeconds(60);
    public decimal TensaoMinima { get; init; } = 100m;
    public decimal TemperaturaDeCalor { get; init; } = 34m;
    public TimeSpan DuracaoDoCalor { get; init; } = TimeSpan.FromHours(2);
    public TimeOnly FimDaManha { get; init; } = new(10, 0);
    public TimeOnly FimDaTarde { get; init; } = new(15, 0);
    public TimeOnly HoraDoPedidoDeToque { get; init; } = new(14, 30);
    public TimeOnly FimDoTurno { get; init; } = new(18, 0);
    public TimeSpan Retencao { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan IntervaloDoVigia { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan IntervaloDoExpurgo { get; init; } = TimeSpan.FromHours(1);

    // Os valores de demo vem de appsettings.Development.json, secao Motor.
    public static ParametrosDoMotor Producao => new();

    public TensaoV LimiteDeTensao => new(TensaoMinima);

    public TempC LimiteDeCalor => new(TemperaturaDeCalor);

    // Conferencia de partida: o eletrodependente nao pode ficar vermelho entre dois heartbeats.
    public bool Coerente => SilencioMaximoEletrodependente >= IntervaloDoHeartbeat * 2
        && FimDaManha < FimDaTarde
        && FimDaTarde <= FimDoTurno;
}
