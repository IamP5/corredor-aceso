using Corredor.Cadastro.Aplicacao;
using Corredor.Cadastro.Dominio;
using Corredor.Guarda;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Corredor.Cadastro.Rotas;

public sealed record CadastrarIdosoHttp(
    int? MicroareaId, string? Nome, string? Endereco, PerfilDaCasaHttp? Perfil, string? Condicoes, string? Medicamentos);

public sealed record IdosoCriadoHttp(Guid Id);

public sealed record AutorizacoesHttp(bool AvisarVizinho, bool AvisarFamilia);

public sealed record RegistrarConsentimentoHttp(string? VersaoDoTermo, AutorizacoesHttp? Autorizacoes);

public sealed record CasaTokenHttp(string CasaToken);

public sealed record IndicarVizinhoHttp(string? Nome, string? Telefone, DateTimeOffset? AceitouEm);

public sealed record VizinhoDaListaHttp(string Nome, string Telefone);

public sealed record ItemDaListaDeNomesHttp(string CasaToken, string Nome, string Endereco, VizinhoDaListaHttp? Vizinho);

public sealed record RegistrarVisitaHttp(Guid? Id, string? CasaToken, DateTimeOffset? OcorridaEm, string? Desfecho, string? Observacao);

public sealed record VisitaRegistradaHttp(Guid Id, DateTimeOffset RegistradaEm);

public sealed record VerificarMensagemHttp(string? Canal, string? Texto);

public sealed record MensagemVerificadaHttp(
    Guid Id, string Veredito, string TextoMascarado, IReadOnlyList<string> TiposDeDado, string? Explicacao, string? VersaoSegura);

public sealed record AvisoHttp(
    Guid Id, string CasaToken, string Motivo, DateTimeOffset Desde, DateTimeOffset RegistradoEm, string? Resposta, DateTimeOffset? RespondidoEm);

public sealed record ResponderAvisoHttp(string? Resposta);

public sealed record RespostaRegistradaHttp(Guid Id, string Resposta, DateTimeOffset RespondidoEm);

public sealed record ErroHttp(string Codigo, string Mensagem);

// As rotas que 03-modelo-de-dominio.md, seção 6.3, dá ao Cadastro. O host passa o grupo /api.
internal static class RotasDoCadastro
{
    public static IEndpointRouteBuilder Mapear(IEndpointRouteBuilder api)
    {
        var idosos = api.MapGroup("/idosos").WithTags("Cadastro: idosos");

        idosos.MapPost("/", CadastrarAsync)
            .WithName("CadastrarIdoso")
            .WithSummary("Cadastra um idoso, sem token")
            .WithDescription("Rota de demo, usada pelo Swagger. Repetir cria outro idoso. O idoso só ganha token com o consentimento. " +
                "Nome e endereço são gravados inteiros. CPF, RG, CNS e credencial chegam mascarados pelo interceptador da Guarda. " +
                "Limites: nome 1 a 120, endereço 1 a 200, condições e medicamentos até 500. perfil.contato aceita Sozinho, Acamado e Coabitado.")
            .Produces<IdosoCriadoHttp>(StatusCodes.Status201Created)
            .Produces<ErroHttp>(StatusCodes.Status422UnprocessableEntity);

        idosos.MapPost("/{id:guid}/consentimento", RegistrarConsentimentoAsync)
            .ExigeAcs()
            .WithName("RegistrarConsentimento")
            .WithSummary("Registra o consentimento e devolve o token da casa")
            .WithDescription("O token de 128 bits nasce aqui e a casa é ativada no Motor. A ACS vem da autenticação. " +
                "Repetir com consentimento vigente devolve o mesmo token e ativa de novo no Motor. " +
                "Com 503 MOTOR_INDISPONIVEL o consentimento já está gravado: repita o pedido.")
            .Produces<CasaTokenHttp>()
            .Produces<ErroHttp>(StatusCodes.Status404NotFound)
            .Produces<ErroHttp>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErroHttp>(StatusCodes.Status503ServiceUnavailable);

        idosos.MapDelete("/{id:guid}/consentimento", RevogarConsentimentoAsync)
            .WithName("RevogarConsentimento")
            .WithSummary("Revoga o consentimento")
            .WithDescription("Apaga o token da casa, remove o vizinho, apaga os avisos ao vizinho daquele token e desativa a casa no Motor. " +
                "As visitas ficam. Sem consentimento vigente, não faz nada.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErroHttp>(StatusCodes.Status404NotFound)
            .Produces<ErroHttp>(StatusCodes.Status503ServiceUnavailable);

        idosos.MapPut("/{id:guid}/vizinho", IndicarVizinhoAsync)
            .WithName("IndicarVizinho")
            .WithSummary("Indica o vizinho de confiança")
            .WithDescription("Exige a autorização avisarVizinho no consentimento vigente e a hora em que o vizinho aceitou. " +
                "Indicar de novo substitui o vizinho. Limites: nome 1 a 120, telefone 1 a 20.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErroHttp>(StatusCodes.Status404NotFound)
            .Produces<ErroHttp>(StatusCodes.Status422UnprocessableEntity);

        idosos.MapPut("/{id:guid}/perfil", AlterarPerfilAsync)
            .WithName("AlterarPerfil")
            .WithSummary("Troca o perfil da casa")
            .WithDescription("contato aceita Sozinho, Acamado e Coabitado. Com token ativo, o Motor recebe o perfil novo. O último valor vale.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErroHttp>(StatusCodes.Status404NotFound)
            .Produces<ErroHttp>(StatusCodes.Status422UnprocessableEntity);

        api.MapGet("/lista-de-nomes", ListaDeNomesAsync)
            .WithTags("Cadastro: app")
            .WithName("ListaDeNomes")
            .WithSummary("Token, nome, endereço e vizinho das casas ativas da microárea")
            .WithDescription("Alimenta o SQLite do celular. Só idoso com token ativo. Cada campo sai pela SaidaSegura no canal AppAutenticado. " +
                "Condições e medicamentos nunca saem. O idoso sem vizinho vem com \"vizinho\": null.")
            .Produces<IReadOnlyList<ItemDaListaDeNomesHttp>>();

        api.MapPost("/visitas", RegistrarVisitaAsync)
            .WithTags("Cadastro: app")
            .ExigeAcs()
            .WithName("RegistrarVisita")
            .WithSummary("Registra a visita da ACS com o desfecho")
            .WithDescription("O id nasce no celular e é a chave de idempotência: repetir devolve 200 com o registradaEm da primeira gravação. " +
                "desfecho aceita EstavaBem, EstavaFora, NaoAbriu e Encaminhada. EstavaBem e Encaminhada confirmam a pessoa vista no Motor. " +
                "EstavaFora é ausência confirmada, nunca sinal de vida. NaoAbriu não chama o Motor. " +
                "Com o relógio de demo deslocado, o servidor soma o deslocamento a ocorridaEm. Observação até 500. " +
                "Com 503 a visita já está gravada e o app repete o mesmo id.")
            .Produces<VisitaRegistradaHttp>(StatusCodes.Status201Created)
            .Produces<VisitaRegistradaHttp>()
            .Produces<ErroHttp>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErroHttp>(StatusCodes.Status503ServiceUnavailable);

        api.MapPost("/mensagens/verificar", VerificarMensagemAsync)
            .WithTags("Cadastro: app")
            .ExigeAcs()
            .WithName("VerificarMensagem")
            .WithSummary("Confere uma mensagem antes do grupo do bairro")
            .WithDescription("A mensagem bloqueada é um resultado normal, então a resposta é 200. Grava só o texto mascarado. " +
                "Com veredito Liberada, explicacao e versaoSegura saem null. Texto de 1 a 500.")
            .Produces<MensagemVerificadaHttp>()
            .Produces<ErroHttp>(StatusCodes.Status422UnprocessableEntity);

        var avisos = api.MapGroup("/avisos").WithTags("Cadastro: avisos ao vizinho");

        avisos.MapGet("/", AvisosAsync)
            .WithName("AvisosDoDia")
            .WithSummary("Os avisos ao vizinho das últimas 24 horas da microárea")
            .WithDescription("O app junta com o radar pelo casaToken. Cada aviso é um episódio: token, motivo e desde.")
            .Produces<IReadOnlyList<AvisoHttp>>();

        avisos.MapPost("/{id:guid}/resposta", ResponderAvisoAsync)
            .AllowAnonymous()
            .WithName("ResponderAviso")
            .WithSummary("Resposta do vizinho, pelo canal simulado")
            .WithDescription("resposta aceita EstaBem e PrecisaDeAjuda. EstaBem confirma a pessoa vista no Motor. PrecisaDeAjuda não chama o Motor. " +
                "A mesma resposta repetida não muda nada. Uma resposta diferente é recusada com 409 AVISO_JA_RESPONDIDO.")
            .Produces<RespostaRegistradaHttp>()
            .Produces<ErroHttp>(StatusCodes.Status404NotFound)
            .Produces<ErroHttp>(StatusCodes.Status409Conflict)
            .Produces<ErroHttp>(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Created<IdosoCriadoHttp>> CadastrarAsync(CadastrarIdosoHttp corpo, CasosDoIdoso casos, CancellationToken ct)
    {
        var novo = new NovoIdoso(
            Entrada.Microarea(corpo.MicroareaId, "microareaId"),
            string.IsNullOrWhiteSpace(corpo.Nome) ? throw Erros.NomeObrigatorio() : Entrada.Texto(corpo.Nome, "nome", 120),
            string.IsNullOrWhiteSpace(corpo.Endereco) ? throw Erros.EnderecoObrigatorio() : Entrada.Texto(corpo.Endereco, "endereco", 200),
            Entrada.Perfil(corpo.Perfil, "perfil"),
            Entrada.Opcional(corpo.Condicoes, "condicoes", 500),
            Entrada.Opcional(corpo.Medicamentos, "medicamentos", 500));
        var id = await casos.CadastrarAsync(novo, ct);
        return TypedResults.Created($"/api/idosos/{id.Valor}", new IdosoCriadoHttp(id.Valor));
    }

    private static async Task<Ok<CasaTokenHttp>> RegistrarConsentimentoAsync(
        Guid id, RegistrarConsentimentoHttp corpo, HttpContext http, CasosDoIdoso casos, CancellationToken ct)
    {
        var versao = Entrada.Texto(corpo.VersaoDoTermo, "versaoDoTermo", 20);
        var autorizacoes = corpo.Autorizacoes ?? throw Erros.CampoInvalido("autorizacoes");
        var token = await casos.RegistrarConsentimentoAsync(
            new IdosoId(id), Entrada.Acs(http)!.Value, versao, new Autorizacoes(autorizacoes.AvisarVizinho, autorizacoes.AvisarFamilia), ct);
        return TypedResults.Ok(new CasaTokenHttp(token.Valor));
    }

    private static async Task<NoContent> RevogarConsentimentoAsync(Guid id, CasosDoIdoso casos, CancellationToken ct)
    {
        await casos.RevogarConsentimentoAsync(new IdosoId(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> IndicarVizinhoAsync(Guid id, IndicarVizinhoHttp corpo, CasosDoIdoso casos, CancellationToken ct)
    {
        await casos.IndicarVizinhoAsync(
            new IdosoId(id),
            Entrada.Texto(corpo.Nome, "nome", 120),
            Entrada.Texto(corpo.Telefone, "telefone", 20),
            corpo.AceitouEm ?? throw Erros.CampoInvalido("aceitouEm"),
            ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> AlterarPerfilAsync(Guid id, PerfilDaCasaHttp corpo, CasosDoIdoso casos, CancellationToken ct)
    {
        await casos.AlterarPerfilAsync(new IdosoId(id), Entrada.Perfil(corpo, "perfil"), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<ItemDaListaDeNomesHttp>>> ListaDeNomesAsync(int? microarea, Leituras leituras, CancellationToken ct)
    {
        var itens = await leituras.ListaDeNomesAsync(Entrada.Microarea(microarea, "microarea"), ct);
        return TypedResults.Ok<IReadOnlyList<ItemDaListaDeNomesHttp>>(itens
            .Select(i => new ItemDaListaDeNomesHttp(
                i.CasaToken.Valor, i.Nome, i.Endereco, i.Vizinho is { } v ? new VizinhoDaListaHttp(v.Nome, v.Telefone) : null))
            .ToList());
    }

    private static async Task<Results<Created<VisitaRegistradaHttp>, Ok<VisitaRegistradaHttp>>> RegistrarVisitaAsync(
        RegistrarVisitaHttp corpo, HttpContext http, CasosDaVisita casos, CancellationToken ct)
    {
        var nova = new NovaVisita(
            corpo.Id is { } id && id != Guid.Empty ? id : throw Erros.CampoInvalido("id"),
            Entrada.Token(corpo.CasaToken, "casaToken"),
            corpo.OcorridaEm ?? throw Erros.CampoInvalido("ocorridaEm"),
            Entrada.Enum<Desfecho>(corpo.Desfecho, "desfecho"),
            Entrada.Opcional(corpo.Observacao, "observacao", 500));
        var gravada = await casos.RegistrarAsync(nova, Entrada.Acs(http)!.Value, ct);
        var resposta = new VisitaRegistradaHttp(gravada.Id, Entrada.Hora(gravada.RegistradaEm));
        return gravada.Repeticao ? TypedResults.Ok(resposta) : TypedResults.Created($"/api/visitas/{gravada.Id}", resposta);
    }

    private static async Task<Ok<MensagemVerificadaHttp>> VerificarMensagemAsync(
        VerificarMensagemHttp corpo, HttpContext http, Leituras leituras, CancellationToken ct)
    {
        var canal = Entrada.Enum<Canal>(corpo.Canal, "canal");
        var texto = Entrada.Texto(corpo.Texto, "texto", 500);
        var mensagem = await leituras.VerificarMensagemAsync(texto, canal, Entrada.Acs(http)!.Value, ct);
        var bloqueada = mensagem.Veredito == VereditoDaMensagem.Bloqueada;
        return TypedResults.Ok(new MensagemVerificadaHttp(
            mensagem.Id,
            mensagem.Veredito.ToString(),
            mensagem.TextoMascarado,
            mensagem.TiposDeDado.Select(t => t.ToString()).ToList(),
            mensagem.Explicacao(),
            bloqueada ? MensagemSaida.VersaoSegura : null));
    }

    private static async Task<Ok<IReadOnlyList<AvisoHttp>>> AvisosAsync(int? microarea, Leituras leituras, CancellationToken ct)
    {
        var avisos = await leituras.AvisosDoDiaAsync(Entrada.Microarea(microarea, "microarea"), ct);
        return TypedResults.Ok<IReadOnlyList<AvisoHttp>>(avisos.Select(Aviso).ToList());
    }

    private static async Task<Ok<RespostaRegistradaHttp>> ResponderAvisoAsync(
        Guid id, ResponderAvisoHttp corpo, CasosDoAviso casos, CancellationToken ct)
    {
        var aviso = await casos.RegistrarRespostaAsync(id, Entrada.Enum<RespostaDoVizinho>(corpo.Resposta, "resposta"), ct);
        return TypedResults.Ok(new RespostaRegistradaHttp(aviso.Id, aviso.Resposta!.Value.ToString(), Entrada.Hora(aviso.RespondidoEm!.Value)));
    }

    private static AvisoHttp Aviso(AvisoAoVizinho a) =>
        new(a.Id, a.CasaToken.Valor, a.Motivo.ToString(), Entrada.Hora(a.Desde), Entrada.Hora(a.RegistradoEm),
            a.Resposta?.ToString(), Entrada.Hora(a.RespondidoEm));
}
