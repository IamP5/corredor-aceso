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
            Veredito.Bloqueada b => (b.TiposDeDado.OrderBy(t => t.ToString(), StringComparer.Ordinal).ToList(), b.TextoMascarado),
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

    // A frase fixa da Guarda para os tipos de dado e o canal, nunca com o valor.
    public string? Explicacao() =>
        Veredito == VereditoDaMensagem.Liberada ? null : FrasesFixas.Explicacao(TiposDeDado, Canal);
}
