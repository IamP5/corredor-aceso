using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Corredor.Guarda.Rotas;

/// <summary>
/// Middleware. Lê o corpo de toda requisição e passa pelo MotorDeParsing. Mascara CPF, RG, CNS e
/// credencial em qualquer campo antes do log e do banco, e registra PiiMascaradaNaEntrada com o
/// canal Banco. Os outros tipos de dado não mudam o corpo: viram rótulo do campo, guardado em
/// HttpContext.Items para quem quiser ler. Nome e endereço chegam inteiros ao Cadastro.
/// </summary>
public sealed class InterceptadorDeEntrada(
    RequestDelegate proximo,
    MotorDeParsing motor,
    FilaDeSeguranca fila,
    TimeProvider relogio,
    ILogger<InterceptadorDeEntrada> log)
{
    /// <summary>Chave de HttpContext.Items com os achados da requisição, sem valor em claro.</summary>
    public const string ChaveDosAchados = "Corredor.Guarda.Achados";

    private const int LimiteDoCorpo = 1024 * 1024;

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (await LerCorpoAsync(contexto.Request) is { } corpo)
        {
            var (novoCorpo, achados) = Analisar(corpo, contexto.Request.ContentType);
            contexto.Items[ChaveDosAchados] = achados;
            if (!ReferenceEquals(novoCorpo, corpo))
            {
                var bytes = Encoding.UTF8.GetBytes(novoCorpo);
                contexto.Request.Body = new MemoryStream(bytes);
                contexto.Request.ContentLength = bytes.Length;
            }
            else
            {
                contexto.Request.Body.Position = 0;
            }

            Registrar(contexto, achados);
        }

        await proximo(contexto);
    }

    private (string Corpo, IReadOnlyList<Achado> Achados) Analisar(string corpo, string? tipo)
    {
        if (tipo?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var json = motor.AnalisarJson(corpo);
                return (json.JsonDeEntrada == corpo ? corpo : json.JsonDeEntrada, json.Achados);
            }
            catch (JsonException)
            {
                // Corpo que não é JSON válido: o binder vai recusar, mas o texto ainda é mascarado.
            }
        }

        var texto = motor.Analisar(corpo, "corpo");
        return (texto.TextoDeEntrada == corpo ? corpo : texto.TextoDeEntrada, texto.Achados);
    }

    private void Registrar(HttpContext contexto, IReadOnlyList<Achado> achados)
    {
        var mascarados = achados.Where(a => TiposDeDado.MascaradosNaEntrada.Contains(a.Tipo)).ToList();
        if (mascarados.Count == 0)
        {
            return;
        }

        var rota = RotaAtual.ModeloDe(contexto);
        var modulo = rota is null ? "Api" : RotaAtual.ModuloDaRota(rota);
        foreach (var a in mascarados)
        {
            log.LogInformation(
                "PII mascarada na entrada: campo={Campo} tipo={Tipo} valor={ValorMascarado} severidade={Severidade} rota={Rota}",
                a.Campo, a.Tipo, a.ValorMascarado, a.Severidade, rota);
        }

        fila.Enfileirar(EventoDeSeguranca.Registrar(
            TipoDeEventoDeSeguranca.MascaramentoNaEntrada,
            Canal.Banco,
            mascarados.Select(a => (a.Tipo, a.Severidade)),
            modulo,
            rota,
            relogio.GetUtcNow()));
    }

    private static async Task<string?> LerCorpoAsync(HttpRequest pedido)
    {
        if (pedido.ContentLength == 0 || !(pedido.ContentLength > 0 || pedido.Headers.TransferEncoding.Count > 0))
        {
            return null;
        }

        var tipo = pedido.ContentType ?? string.Empty;
        if (!(tipo.Contains("json", StringComparison.OrdinalIgnoreCase)
            || tipo.StartsWith("text/", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        pedido.EnableBuffering(LimiteDoCorpo);
        using var leitor = new StreamReader(pedido.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var corpo = await leitor.ReadToEndAsync();
        return corpo.Length == 0 ? null : corpo;
    }
}
