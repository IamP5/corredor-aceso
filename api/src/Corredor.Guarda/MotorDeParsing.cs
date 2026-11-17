namespace Corredor.Guarda;

// Um dado pessoal achado num texto. Nunca tem o valor em claro.
public sealed record Achado(TipoDeDado Tipo, string Campo, string ValorMascarado, int Severidade);

public sealed record TextoAnalisado(TextoRotulado Texto, IReadOnlyList<Achado> Achados);

public interface IMotorDeParsing
{
    // Junta os rótulos do campo aos rótulos que as RegraDePii acharem no conteúdo.
    TextoAnalisado Rotular(string texto, params TipoDeDado[] rotulosDoCampo);
}

// Sem as regras do pii.json, um texto livre pode carregar qualquer um dos cinco tipos de dado de texto livre.
// Rotular com os cinco faz todo canal aberto bloquear até a regra de verdade existir.
internal sealed class MotorDeParsingProvisorio : IMotorDeParsing
{
    private static readonly TipoDeDado[] DeTextoLivre =
        [TipoDeDado.Saude, TipoDeDado.MoraSozinho, TipoDeDado.Nome, TipoDeDado.Endereco, TipoDeDado.Telefone];

    public TextoAnalisado Rotular(string texto, params TipoDeDado[] rotulosDoCampo)
    {
        var rotulos = string.IsNullOrWhiteSpace(texto) ? rotulosDoCampo : rotulosDoCampo.Union(DeTextoLivre).ToArray();
        return new TextoAnalisado(TextoRotulado.Do(texto, rotulos), []);
    }
}
