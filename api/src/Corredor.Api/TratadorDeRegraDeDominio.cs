using Corredor.Contratos;
using Microsoft.AspNetCore.Diagnostics;

namespace Corredor.Api;

// Fica no host porque a Guarda, dona do TratadorDeErros, não referencia Corredor.Contratos.
internal sealed class TratadorDeRegraDeDominio : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception excecao, CancellationToken ct)
    {
        if (excecao is not RegraDeDominioViolada regra) return false;

        http.Response.StatusCode = regra.Tipo switch
        {
            TipoDeViolacao.Conflito => StatusCodes.Status409Conflict,
            TipoDeViolacao.Desconhecido => StatusCodes.Status404NotFound,
            TipoDeViolacao.Proibida => StatusCodes.Status403Forbidden,
            TipoDeViolacao.Indisponivel => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        await http.Response.WriteAsJsonAsync(new { codigo = regra.Codigo, mensagem = regra.Message }, ct);
        return true;
    }
}
