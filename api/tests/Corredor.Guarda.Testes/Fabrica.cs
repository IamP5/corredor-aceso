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

// Um CPF com dígito verificador válido, montado na hora a partir de uma base sintética. Nenhum
// CPF válido fica escrito no repositório, que é público.
internal static class CpfDeTeste
{
    public const string Base = "000000001";

    public static string Digitos { get; } = Base + Digito(Base) + Digito(Base + Digito(Base));

    public static string Formatado => $"{Digitos[..3]}.{Digitos[3..6]}.{Digitos[6..9]}-{Digitos[9..]}";

    public static string Mascara => $"***.{Digitos[3..6]}.***-{Digitos[9..]}";

    private static char Digito(string d)
    {
        var soma = d.Select((c, i) => (c - '0') * (d.Length + 1 - i)).Sum();
        var resto = soma * 10 % 11;
        return (char)('0' + (resto == 10 ? 0 : resto));
    }
}
