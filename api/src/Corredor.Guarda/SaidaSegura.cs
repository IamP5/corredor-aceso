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
