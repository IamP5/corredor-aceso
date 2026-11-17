namespace Corredor.Guarda;

/// <summary>O que um canal aceita e quanto ele expõe, de 1 a 3.</summary>
public sealed record RegraDoCanal(IReadOnlySet<TipoDeDado> Aceita, int Exposicao);

/// <summary>Para cada canal, os tipos de dado aceitos e a exposição. Uma tabela, não uma sequência de if.</summary>
public static class PoliticaDeCanal
{
    private static readonly IReadOnlySet<TipoDeDado> Nenhum = new HashSet<TipoDeDado>();

    public static readonly IReadOnlyDictionary<Canal, RegraDoCanal> Tabela = new Dictionary<Canal, RegraDoCanal>
    {
        [Canal.GrupoDoBairro] = new(Nenhum, 3),
        [Canal.PromptDoLlm] = new(Nenhum, 3),
        [Canal.Mqtt] = new(Nenhum, 3),
        [Canal.MensagemDeErro] = new(Nenhum, 3),
        [Canal.RespostaSemAutenticacao] = new(Nenhum, 3),
        [Canal.Vizinho] = new(Nenhum, 3),
        [Canal.Log] = new(Nenhum, 2),
        [Canal.AppAutenticado] = new(new HashSet<TipoDeDado> { TipoDeDado.Nome, TipoDeDado.Endereco, TipoDeDado.Telefone }, 1),
        [Canal.Banco] = new(Enum.GetValues<TipoDeDado>().ToHashSet(), 1),
    };

    public static int Exposicao(Canal canal) => Tabela[canal].Exposicao;

    /// <summary>Os rótulos do texto que o canal não aceita.</summary>
    public static IReadOnlySet<TipoDeDado> Proibidos(Canal canal, IReadOnlySet<TipoDeDado> rotulos) =>
        rotulos.Except(Tabela[canal].Aceita).ToHashSet();
}

public static class Vereditos
{
    /// <summary>O que pode ir ao canal em qualquer caso: o valor, se liberado, ou a máscara.</summary>
    public static string ParaOCanal(this Veredito veredito) => veredito switch
    {
        Veredito.Liberada l => l.Texto,
        Veredito.Bloqueada b => b.TextoMascarado,
        _ => throw new InvalidOperationException("veredito desconhecido"),
    };
}
