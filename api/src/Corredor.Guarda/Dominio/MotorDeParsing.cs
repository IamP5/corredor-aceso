using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Corredor.Guarda;

/// <summary>O resultado de analisar um texto.</summary>
/// <param name="Texto">O texto rotulado, com a máscara de cada achado no ToString().</param>
/// <param name="Achados">Os dados pessoais achados, sem o valor em claro.</param>
/// <param name="TextoDeEntrada">O texto com CPF, RG, CNS e credencial mascarados e o resto intacto.</param>
public sealed record AnaliseDeTexto(TextoRotulado Texto, IReadOnlyList<Achado> Achados, string TextoDeEntrada)
{
    public bool MascarouNaEntrada => Achados.Any(a => TiposDeDado.MascaradosNaEntrada.Contains(a.Tipo));
}

/// <summary>O resultado de analisar um corpo JSON.</summary>
/// <param name="JsonDeEntrada">O corpo com CPF, RG, CNS e credencial mascarados em qualquer campo.</param>
public sealed record AnaliseDeJson(string JsonDeEntrada, IReadOnlyList<Achado> Achados)
{
    public IReadOnlyList<Achado> Mascarados =>
        Achados.Where(a => TiposDeDado.MascaradosNaEntrada.Contains(a.Tipo)).ToList();
}

/// <summary>Aplica as RegraDePii a um texto ou a um JSON. Puro: não grava nada.</summary>
public sealed class MotorDeParsing(RegrasDePii regras) : IMotorDeParsing
{
    public MotorDeParsing() : this(RegrasDePii.Padrao) { }

    public RegrasDePii Regras { get; } = regras;

    public AnaliseDeTexto Analisar(string texto, string campo = "texto")
    {
        var trechos = Trechos(texto);
        if (trechos.Count == 0)
        {
            return new AnaliseDeTexto(TextoRotulado.Do(texto), [], texto);
        }

        var mascarado = new StringBuilder(texto.Length);
        var entrada = new StringBuilder(texto.Length);
        var achados = new List<Achado>(trechos.Count);
        var posicao = 0;
        foreach (var (inicio, tamanho, regra) in trechos)
        {
            var antes = texto[posicao..inicio];
            var valor = texto.Substring(inicio, tamanho);
            var mascara = regra.Mascarar(valor);
            mascarado.Append(antes).Append(mascara);
            entrada.Append(antes).Append(regra.NaEntrada == AcaoNaEntrada.Mascarar ? mascara : valor);
            achados.Add(new Achado(regra.Tipo, campo, mascara, regra.SeveridadeDe(valor)));
            posicao = inicio + tamanho;
        }

        mascarado.Append(texto[posicao..]);
        entrada.Append(texto[posicao..]);
        var rotulado = TextoRotulado.Mascarado(texto, achados.Select(a => a.Tipo), mascarado.ToString());
        return new AnaliseDeTexto(rotulado, achados, entrada.ToString());
    }

    /// <summary>
    /// Os rótulos do campo mais os rótulos que as regras acharem no conteúdo. Com rótulo de campo,
    /// o texto inteiro é dado pessoal, então a máscara cobre o texto inteiro.
    /// </summary>
    public TextoAnalisado Rotular(string texto, params TipoDeDado[] rotulosDoCampo)
    {
        var analise = Analisar(texto);
        var rotulado = rotulosDoCampo.Length == 0
            ? analise.Texto
            : TextoRotulado.Do(texto, [.. analise.Texto.Rotulos.Union(rotulosDoCampo)]);
        return new TextoAnalisado(rotulado, analise.Achados);
    }

    /// <summary>
    /// Percorre todo valor de texto do JSON. Mascara CPF, RG, CNS e credencial em qualquer campo.
    /// A credencial por chave casa com o nome inteiro da chave, então casaToken nunca é mascarado.
    /// </summary>
    public AnaliseDeJson AnalisarJson(string json)
    {
        var raiz = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true });
        var achados = new List<Achado>();
        var mudou = false;
        var nova = Visitar(raiz, "$", null, achados, ref mudou);
        return new AnaliseDeJson(mudou ? nova?.ToJsonString() ?? "null" : json, achados);
    }

    private JsonNode? Visitar(JsonNode? no, string caminho, string? chave, List<Achado> achados, ref bool mudou)
    {
        switch (no)
        {
            case JsonObject objeto:
                foreach (var (nome, filho) in objeto.ToList())
                {
                    var novo = Visitar(filho, Juntar(caminho, nome), nome, achados, ref mudou);
                    if (!ReferenceEquals(novo, filho))
                    {
                        objeto[nome] = novo;
                    }
                }

                return objeto;
            case JsonArray lista:
                for (var i = 0; i < lista.Count; i++)
                {
                    var filho = lista[i];
                    var novo = Visitar(filho, $"{caminho}[{i}]", chave, achados, ref mudou);
                    if (!ReferenceEquals(novo, filho))
                    {
                        lista[i] = novo;
                    }
                }

                return lista;
            case JsonValue valor:
                var texto = valor.GetValueKind() switch
                {
                    JsonValueKind.String => valor.GetValue<string>(),
                    JsonValueKind.Number => valor.ToJsonString(),
                    _ => null,
                };
                if (string.IsNullOrEmpty(texto))
                {
                    return valor;
                }

                var campo = caminho.StartsWith("$.", StringComparison.Ordinal) ? caminho[2..] : caminho;
                if (chave is not null && Regras.ChavesDeCredencial.Contains(chave))
                {
                    var marca = TextoRotulado.Marcador(TipoDeDado.Credencial);
                    achados.Add(new Achado(TipoDeDado.Credencial, campo, marca, Regras.SeveridadeDe(TipoDeDado.Credencial)));
                    mudou = true;
                    return JsonValue.Create(marca);
                }

                var analise = Analisar(texto, campo);
                achados.AddRange(analise.Achados);
                if (analise.TextoDeEntrada == texto)
                {
                    return valor;
                }

                mudou = true;
                return JsonValue.Create(analise.TextoDeEntrada);
            default:
                return no;
        }
    }

    private static string Juntar(string caminho, string nome) => $"{caminho}.{nome}";

    // Todos os casamentos de todas as regras. Na sobreposição, fica o de maior severidade,
    // depois o mais longo, depois o que começa antes.
    private List<(int Inicio, int Tamanho, RegraDePii Regra)> Trechos(string texto)
    {
        var candidatos = new List<(int Inicio, int Tamanho, RegraDePii Regra, int Severidade)>();
        foreach (var regra in Regras.Regras)
        {
            foreach (System.Text.RegularExpressions.Match m in regra.Padrao.Matches(texto))
            {
                if (m.Length > 0)
                {
                    candidatos.Add((m.Index, m.Length, regra, regra.SeveridadeDe(m.Value)));
                }
            }
        }

        var escolhidos = new List<(int Inicio, int Tamanho, RegraDePii Regra)>();
        foreach (var c in candidatos.OrderByDescending(c => c.Severidade).ThenByDescending(c => c.Tamanho).ThenBy(c => c.Inicio))
        {
            var fim = c.Inicio + c.Tamanho;
            if (escolhidos.All(e => fim <= e.Inicio || c.Inicio >= e.Inicio + e.Tamanho))
            {
                escolhidos.Add((c.Inicio, c.Tamanho, c.Regra));
            }
        }

        escolhidos.Sort((a, b) => a.Inicio.CompareTo(b.Inicio));
        return escolhidos;
    }
}
