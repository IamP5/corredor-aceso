namespace Corredor.Guarda;

/// <summary>As frases que entram quando o LLM falha e a explicação do bloqueio para a ACS.</summary>
public static class FrasesFixas
{
    private static readonly IReadOnlyDictionary<TipoDeEventoDeSeguranca, string> DoEvento =
        new Dictionary<TipoDeEventoDeSeguranca, string>
        {
            [TipoDeEventoDeSeguranca.MascaramentoNaEntrada] =
                "Um número de documento chegou ao sistema e foi escondido antes de ser guardado. Ninguém consegue ler o número inteiro.",
            [TipoDeEventoDeSeguranca.BloqueioDeSaida] =
                "O sistema barrou uma mensagem com dados pessoais de um idoso antes que ela saísse. Os dados continuam protegidos.",
        };

    // A ordem em que os tipos aparecem na explicação. "Mora sozinha" vem antes da saúde.
    private static readonly (TipoDeDado Tipo, string Parte)[] Partes =
    [
        (TipoDeDado.MoraSozinho, "diz que a pessoa mora sozinha"),
        (TipoDeDado.Saude, "fala da saúde dela"),
        (TipoDeDado.Nome, "diz o nome da pessoa"),
        (TipoDeDado.Endereco, "diz onde a pessoa mora"),
        (TipoDeDado.Telefone, "mostra um telefone"),
        (TipoDeDado.Cpf, "mostra um CPF"),
        (TipoDeDado.Rg, "mostra um RG"),
        (TipoDeDado.Cns, "mostra o cartão do SUS"),
        (TipoDeDado.Credencial, "mostra uma senha"),
    ];

    private static readonly IReadOnlyDictionary<Canal, string> OndeSai = new Dictionary<Canal, string>
    {
        [Canal.GrupoDoBairro] = "No grupo do bairro, qualquer um lê.",
        [Canal.Vizinho] = "O vizinho não precisa saber disso para ajudar.",
        [Canal.Mqtt] = "O aparelho da casa não pode receber esse dado.",
        [Canal.PromptDoLlm] = "A inteligência artificial não pode receber esse dado.",
        [Canal.Log] = "O registro do sistema não pode guardar esse dado.",
        [Canal.MensagemDeErro] = "Uma mensagem de erro não pode mostrar esse dado.",
        [Canal.RespostaSemAutenticacao] = "Quem não entrou no sistema não pode ver esse dado.",
        [Canal.AppAutenticado] = "Esse dado não aparece no aplicativo.",
        [Canal.Banco] = "Esse dado não pode ser guardado.",
    };

    public static string DoTipoDeEvento(TipoDeEventoDeSeguranca tipo) => DoEvento[tipo];

    /// <summary>
    /// A explicação de um bloqueio pelos tipos de dado. "ele mora sozinho e toma diurético" no
    /// grupo do bairro dá "Essa mensagem diz que a pessoa mora sozinha e fala da saúde dela. No
    /// grupo do bairro, qualquer um lê."
    /// </summary>
    public static string Explicacao(IEnumerable<TipoDeDado> tipos, Canal canal)
    {
        var conjunto = tipos.ToHashSet();
        var partes = Partes.Where(p => conjunto.Contains(p.Tipo)).Select(p => p.Parte).ToList();
        var oQue = partes.Count switch
        {
            0 => "tem um dado pessoal",
            1 => partes[0],
            _ => string.Join(", ", partes[..^1]) + " e " + partes[^1],
        };
        return $"Essa mensagem {oQue}. {OndeSai[canal]}";
    }
}
