using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Corredor.Guarda.Rotas;

/// <summary>
/// Filtro de endpoint. Numa requisição sem autenticação, um TextoRotulado com rótulo dentro do
/// resultado bloqueia a resposta inteira pela SaidaSegura com o canal RespostaSemAutenticacao.
/// </summary>
public sealed class FiltroDeResposta(SaidaSegura saida) : IEndpointFilter
{
    private const int ProfundidadeMaxima = 6;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext contexto, EndpointFilterDelegate proximo)
    {
        var resultado = await proximo(contexto);
        if (contexto.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return resultado;
        }

        var textos = new List<TextoRotulado>();
        Coletar(resultado is IValueHttpResult comValor ? comValor.Value : resultado, textos, 0);
        var rotulados = textos.Where(t => t.Rotulos.Count > 0).ToList();
        if (rotulados.Count == 0)
        {
            return resultado;
        }

        var veredito = saida.Liberar(rotulados.Aggregate((a, b) => a + b), Canal.RespostaSemAutenticacao);
        return veredito is Veredito.Liberada
            ? resultado
            : TypedResults.Json(
                new { codigo = "RESPOSTA_BLOQUEADA", mensagem = "Esta resposta tinha dado pessoal e foi bloqueada para quem não entrou no sistema." },
                statusCode: StatusCodes.Status403Forbidden);
    }

    private static void Coletar(object? valor, List<TextoRotulado> textos, int profundidade)
    {
        switch (valor)
        {
            case null or string or ValueType:
                return;
            case TextoRotulado texto:
                textos.Add(texto);
                return;
            case IDictionary dicionario when profundidade < ProfundidadeMaxima:
                foreach (var item in dicionario.Values)
                {
                    Coletar(item, textos, profundidade + 1);
                }

                return;
            case IEnumerable lista when profundidade < ProfundidadeMaxima:
                foreach (var item in lista)
                {
                    Coletar(item, textos, profundidade + 1);
                }

                return;
        }

        if (profundidade >= ProfundidadeMaxima || valor is IResult)
        {
            return;
        }

        foreach (var propriedade in valor.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (propriedade.GetIndexParameters().Length == 0)
            {
                Coletar(propriedade.GetValue(valor), textos, profundidade + 1);
            }
        }
    }
}
