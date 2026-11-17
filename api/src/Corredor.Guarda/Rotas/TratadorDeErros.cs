using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Corredor.Guarda.Rotas;

/// <summary>
/// Roda depois do tratador de RegraDeDominioViolada do host, então só vê erro inesperado. A tela
/// do app recebe sempre um texto neutro. Se a mensagem da exceção tinha dado pessoal, a saída
/// pelo canal MensagemDeErro é bloqueada e registrada, e o log recebe a mensagem mascarada.
/// </summary>
public sealed class TratadorDeErros(MotorDeParsing motor, SaidaSegura saida, ILogger<TratadorDeErros> log) : IExceptionHandler
{
    public const string MensagemNeutra = "Algo deu errado. Tente de novo em instantes.";

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception excecao, CancellationToken ct)
    {
        var (status, codigo) = excecao is BadHttpRequestException
            ? (StatusCodes.Status422UnprocessableEntity, "CAMPO_INVALIDO")
            : (StatusCodes.Status500InternalServerError, "ERRO_INTERNO");

        var mensagem = motor.Rotular(excecao.Message).Texto;
        var naTela = saida.Liberar(mensagem, Canal.MensagemDeErro);
        log.LogError(
            "Erro {Codigo} em {Rota}: {Tipo}: {Mensagem}",
            codigo, RotaAtual.ModeloDe(http), excecao.GetType().Name, naTela.ParaOCanal());

        if (http.Response.HasStarted)
        {
            return true;
        }

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(
            new { codigo, mensagem = codigo == "CAMPO_INVALIDO" ? "Um campo veio fora do formato." : MensagemNeutra },
            ct);
        return true;
    }
}
