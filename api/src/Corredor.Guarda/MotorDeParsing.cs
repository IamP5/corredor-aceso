namespace Corredor.Guarda;

// Um dado pessoal achado num texto. Nunca tem o valor em claro.
public sealed record Achado(TipoDeDado Tipo, string Campo, string ValorMascarado, int Severidade);

public sealed record TextoAnalisado(TextoRotulado Texto, IReadOnlyList<Achado> Achados);

public interface IMotorDeParsing
{
    // Junta os rótulos do campo aos rótulos que as RegraDePii acharem no conteúdo.
    TextoAnalisado Rotular(string texto, params TipoDeDado[] rotulosDoCampo);
}
