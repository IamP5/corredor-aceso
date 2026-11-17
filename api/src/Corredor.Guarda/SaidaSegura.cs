namespace Corredor.Guarda;

public abstract record Veredito
{
    private Veredito() { }

    // O texto em claro, pronto para o canal.
    public sealed record Liberada(string Texto) : Veredito;

    // Os tipos de dado que o canal não aceita, e o texto como TextoRotulado.ToString() mascara.
    public sealed record Bloqueada(IReadOnlySet<TipoDeDado> TiposDeDado, string TextoMascarado) : Veredito;
}

public interface ISaidaSegura
{
    Veredito Liberar(TextoRotulado texto, Canal canal);
}

// A PoliticaDeCanal da seção 4.4 de 03-modelo-de-dominio.md. Falta registrar SaidaBloqueada a cada bloqueio.
internal sealed class SaidaSegura : ISaidaSegura
{
    private static readonly IReadOnlySet<TipoDeDado> Nenhum = new HashSet<TipoDeDado>();

    private static readonly Dictionary<Canal, IReadOnlySet<TipoDeDado>> Aceitos = new()
    {
        [Canal.GrupoDoBairro] = Nenhum,
        [Canal.PromptDoLlm] = Nenhum,
        [Canal.Mqtt] = Nenhum,
        [Canal.MensagemDeErro] = Nenhum,
        [Canal.RespostaSemAutenticacao] = Nenhum,
        [Canal.Vizinho] = Nenhum,
        [Canal.Log] = Nenhum,
        [Canal.AppAutenticado] = new HashSet<TipoDeDado> { TipoDeDado.Nome, TipoDeDado.Endereco, TipoDeDado.Telefone },
        [Canal.Banco] = Enum.GetValues<TipoDeDado>().ToHashSet(),
    };

    public Veredito Liberar(TextoRotulado texto, Canal canal)
    {
        var recusados = texto.Rotulos.Except(Aceitos[canal]).ToHashSet();
        return recusados.Count == 0 ? new Veredito.Liberada(texto.Valor) : new Veredito.Bloqueada(recusados, texto.ToString());
    }
}
