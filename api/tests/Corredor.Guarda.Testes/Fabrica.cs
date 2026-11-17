namespace Corredor.Guarda.Testes;

// Relógio parado, para os testes não dependerem da hora em que rodam.
internal sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;

    public override DateTimeOffset GetUtcNow() => Agora;
}

internal sealed class RotaFixa(string? modelo, string? modulo) : IRotaAtual
{
    public string? Modelo => modelo;

    public string? Modulo => modulo;
}

internal static class Fabrica
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 18, 20, 10, 0, TimeSpan.Zero);

    public static SaidaSegura Saida(FilaDeSeguranca? fila = null, IRotaAtual? rota = null) =>
        new(fila ?? new FilaDeSeguranca(), RegrasDePii.Padrao, new RelogioFixo(Agora), rota ?? new RotaFixa(null, null));

    public static List<EventoDeSeguranca> Drenar(this FilaDeSeguranca fila)
    {
        var eventos = new List<EventoDeSeguranca>();
        while (fila.Leitor.TryRead(out var evento))
        {
            eventos.Add(evento);
            fila.Concluir();
        }

        return eventos;
    }
}
