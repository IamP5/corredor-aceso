using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Corredor.Guarda.Rotas;

public static class RotasDaGuarda
{
    /// <summary>GET /api/painel, aberto. Nenhum campo carrega o valor de um dado pessoal.</summary>
    public static void MapPainel(IEndpointRouteBuilder api) =>
        api.MapGet("/painel", async (Painel painel, CancellationToken ct) => TypedResults.Ok(await painel.MontarAsync(ct)))
            .AllowAnonymous()
            .WithTags("Guarda")
            .WithSummary("Painel Risco Cidadão: farol, pontos e eventos de segurança das últimas 24 horas");

    /// <summary>
    /// Rotas da auditoria de Cybersecurity, só em Development. Cada uma reproduz uma evidência
    /// pedida pelo professor sem depender das rotas do Cadastro.
    /// </summary>
    public static void MapDemonstracao(IEndpointRouteBuilder api)
    {
        var demo = api.MapGroup("/guarda/demo").WithTags("Guarda: demonstração");

        // Evidência 1. O corpo chega aqui depois do InterceptadorDeEntrada: o CPF já vem mascarado.
        demo.MapPost("/documento", (DocumentoRecebido corpo) => TypedResults.Ok(new { documentoRecebido = corpo.Documento }))
            .WithSummary("Devolve o documento como o binder recebeu, depois do interceptador");

        // Evidência 2. Tentar gravar o nome no log escreve [NOME] e registra SaidaBloqueada no canal Log.
        demo.MapPost("/log-nome", (NomeRecebido corpo, ILoggerFactory logs) =>
            {
                var nome = TextoRotulado.Do(corpo.Nome, TipoDeDado.Nome);
                logs.CreateLogger("Corredor.Cadastro.Demonstracao").LogInformation("Visita registrada para {Nome}", nome);
                return TypedResults.Accepted((string?)null, new { logado = $"Visita registrada para {nome}" });
            })
            .WithSummary("Loga Idoso.Nome como TextoRotulado");

        // Evidência 3. Rota sem autenticação que tentaria devolver Idoso.Condicoes.
        demo.MapGet("/condicoes", () => TypedResults.Ok(new
            {
                casa = "demo",
                condicoes = TextoRotulado.Do("usa concentrador de oxigênio e toma furosemida", TipoDeDado.Saude),
            }))
            .AllowAnonymous()
            .WithSummary("Rota aberta que tentaria devolver condições de saúde");

        // Evidência 4. A mensagem para o grupo do bairro passa pela SaidaSegura.
        demo.MapPost("/grupo-do-bairro", (MensagemRecebida corpo, IMotorDeParsing motor, ISaidaSegura saida) =>
            {
                var texto = motor.Rotular(corpo.Texto).Texto;
                return saida.Liberar(texto, Canal.GrupoDoBairro) switch
                {
                    Veredito.Bloqueada b => TypedResults.Ok(new RespostaDoGrupo(
                        "Bloqueada", b.TextoMascarado, [.. b.TiposDeDado.Order()],
                        FrasesFixas.Explicacao(b.TiposDeDado, Canal.GrupoDoBairro))),
                    Veredito.Liberada l => TypedResults.Ok(new RespostaDoGrupo("Liberada", l.Texto, [], null)),
                    _ => throw new InvalidOperationException("veredito desconhecido"),
                };
            })
            .WithSummary("Verifica uma mensagem para o grupo do bairro");

        // O Nginx serve o painel em /painel/ na VM. No notebook, a página sai daqui.
        demo.MapGet("/painel", () => TypedResults.Content(PaginaDoPainel.Value, "text/html; charset=utf-8"))
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    private static readonly Lazy<string> PaginaDoPainel = new(() =>
    {
        using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream("painel.html")
            ?? throw new InvalidOperationException("painel/index.html não está embutido no assembly");
        using var leitor = new StreamReader(fluxo);
        return leitor.ReadToEnd();
    });

    public sealed record DocumentoRecebido(string Documento);

    public sealed record NomeRecebido(string Nome);

    public sealed record MensagemRecebida(string Texto);

    public sealed record RespostaDoGrupo(string Veredito, string TextoMascarado, IReadOnlyList<TipoDeDado> TiposDeDado, string? Explicacao);
}
