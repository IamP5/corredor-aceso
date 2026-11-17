using Corredor.Guarda;

namespace Corredor.Cadastro.Dominio;

// Nasce com o veredito, que não muda. Guarda só o texto mascarado: o texto bloqueado é o próprio vazamento.
public sealed class MensagemSaida
{
    public const string VersaoSegura = "Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.";

    private MensagemSaida() { }

    public Guid Id { get; private set; }
    public Canal Canal { get; private set; }
    public AcsId AcsId { get; private set; }
    public string TextoMascarado { get; private set; } = null!;
    public VereditoDaMensagem Veredito { get; private set; }
    public List<TipoDeDado> TiposDeDado { get; private set; } = [];
    public DateTimeOffset VerificadaEm { get; private set; }

    // Bloqueada se, e somente se, a SaidaSegura achou um tipo de dado que o canal não aceita (invariante 2).
    public static MensagemSaida Verificar(TextoRotulado texto, Canal canal, AcsId acs, ISaidaSegura saidaSegura, DateTimeOffset agora)
    {
        var (tipos, mascarado) = saidaSegura.Liberar(texto, canal) switch
        {
            Veredito.Bloqueada b => (b.TiposDeDado.Order().ToList(), b.TextoMascarado),
            _ => (new List<TipoDeDado>(), texto.ToString()),
        };
        return new MensagemSaida
        {
            Id = Guid.NewGuid(),
            Canal = canal,
            AcsId = acs,
            TextoMascarado = mascarado,
            Veredito = tipos.Count == 0 ? VereditoDaMensagem.Liberada : VereditoDaMensagem.Bloqueada,
            TiposDeDado = tipos,
            VerificadaEm = agora,
        };
    }

    // A frase fixa do tipo de dado, montada só com o tipo, nunca com o valor.
    public string? Explicacao() =>
        Veredito == VereditoDaMensagem.Liberada
            ? null
            : $"Essa mensagem {string.Join(" e ", TiposDeDado.Select(FraseDoTipo).Distinct())}. {FraseDoCanal(Canal)}";

    private static string FraseDoTipo(TipoDeDado tipo) => tipo switch
    {
        TipoDeDado.MoraSozinho => "diz que a pessoa mora sozinha",
        TipoDeDado.Saude => "fala da saúde dela",
        TipoDeDado.Nome => "diz o nome da pessoa",
        TipoDeDado.Endereco => "diz onde a pessoa mora",
        TipoDeDado.Telefone => "traz um telefone",
        _ => "traz um documento ou uma senha",
    };

    private static string FraseDoCanal(Canal canal) => canal switch
    {
        Canal.GrupoDoBairro => "No grupo do bairro, qualquer um lê.",
        _ => "Esse canal não aceita dado pessoal.",
    };
}
