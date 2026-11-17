using System.Text;

namespace Corredor.Guarda;

public enum TipoDeDado { Cpf, Rg, Cns, Saude, MoraSozinho, Endereco, Telefone, Nome, Credencial }

public enum Canal { Log, RespostaSemAutenticacao, MensagemDeErro, Mqtt, GrupoDoBairro, PromptDoLlm, Vizinho, AppAutenticado, Banco }

// O valor é internal: fora da Guarda, ele só sai pela ISaidaSegura ou pelo ConversorDeTextoRotulado.
public sealed class TextoRotulado
{
    private readonly string? _mascara;

    private TextoRotulado(string valor, IEnumerable<TipoDeDado> rotulos, string? mascara)
    {
        Valor = valor;
        Rotulos = rotulos.ToHashSet();
        _mascara = mascara;
    }

    internal string Valor { get; }

    public IReadOnlySet<TipoDeDado> Rotulos { get; }

    // O rótulo vem do campo: Do(nome, TipoDeDado.Nome).
    public static TextoRotulado Do(string valor, params TipoDeDado[] rotulos) => new(valor, rotulos, null);

    internal static TextoRotulado DoBanco(string valor, TipoDeDado[] rotulosDaColuna) => new(valor, rotulosDaColuna, null);

    // O MotorDeParsing troca só o trecho achado: "ele [MORA_SOZINHO] e toma [SAUDE]".
    internal static TextoRotulado Mascarado(string valor, IEnumerable<TipoDeDado> rotulos, string mascara) => new(valor, rotulos, mascara);

    public TextoRotulado Juntar(TextoRotulado outro, string separador = " ") =>
        new(Valor + separador + outro.Valor, Rotulos.Union(outro.Rotulos), ToString() + separador + outro.ToString());

    public static TextoRotulado operator +(TextoRotulado a, TextoRotulado b) => a.Juntar(b, "");

    // Nunca devolve o valor de um texto com rótulo. Sem máscara do MotorDeParsing, o texto inteiro vira marcadores.
    public override string ToString() =>
        Rotulos.Count == 0 ? Valor : _mascara ?? string.Join(' ', Rotulos.Order().Select(Marcador));

    public static string Marcador(TipoDeDado tipo)
    {
        var nome = tipo.ToString();
        var marcador = new StringBuilder("[");
        for (var i = 0; i < nome.Length; i++)
        {
            if (i > 0 && char.IsUpper(nome[i])) marcador.Append('_');
            marcador.Append(char.ToUpperInvariant(nome[i]));
        }
        return marcador.Append(']').ToString();
    }
}
