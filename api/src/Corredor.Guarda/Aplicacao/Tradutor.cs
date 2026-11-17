using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corredor.Guarda;

/// <summary>Configuração do LLM. A chave vem de Llm__ApiKey ou de OPENROUTER_API_KEY e nunca é logada.</summary>
public sealed class OpcoesDoLlm
{
    public string? ApiKey { get; set; }
    public string Modelo { get; set; } = "openrouter/auto";
    public string UrlBase { get; set; } = "https://openrouter.ai/api/v1";
    public TimeSpan Limite { get; set; } = TimeSpan.FromSeconds(5);

    public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);

    public override string ToString() => $"OpcoesDoLlm {{ Modelo = {Modelo}, UrlBase = {UrlBase}, ApiKey = {(Configurado ? "***" : "ausente")} }}";
}

/// <summary>
/// Camada anticorrupção entre a Guarda e o LLM. Monta o prompt só com o tipo de evento, os tipos
/// de dado, o canal e o módulo, passa o prompt pela SaidaSegura com o canal PromptDoLlm e chama o
/// endpoint de chat completions do OpenRouter, compatível com o da OpenAI. Sem chave, com erro,
/// sem resposta no limite ou com dado pessoal na resposta, devolve a frase fixa do tipo de evento.
/// </summary>
public sealed class Tradutor(
    HttpClient http,
    IOptions<OpcoesDoLlm> opcoes,
    SaidaSegura saida,
    MotorDeParsing motor,
    ILogger<Tradutor> log)
{
    public const int TamanhoMaximo = 400;

    public async Task<MensagemAoCidadao> TraduzirAsync(EventoDeSeguranca evento, CancellationToken ct)
    {
        var fixa = new MensagemAoCidadao(FrasesFixas.DoTipoDeEvento(evento.Tipo), OrigemDaMensagem.FraseFixa);
        var op = opcoes.Value;
        if (!op.Configurado)
        {
            return fixa;
        }

        if (saida.Liberar(Prompt(evento), Canal.PromptDoLlm, "Guarda") is not Veredito.Liberada prompt)
        {
            return fixa;
        }

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(op.Limite);
        try
        {
            using var pedido = new HttpRequestMessage(HttpMethod.Post, op.UrlBase.TrimEnd('/') + "/chat/completions")
            {
                Content = JsonContent.Create(new PedidoDeChat(
                    op.Modelo,
                    [new("system", Sistema), new("user", prompt.Texto)],
                    MaxTokens: 200,
                    Temperature: 0.3)),
            };
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", op.ApiKey);
            pedido.Headers.Add("X-Title", "Corredor Aceso");

            using var resposta = await http.SendAsync(pedido, limite.Token);
            if (!resposta.IsSuccessStatusCode)
            {
                log.LogWarning("LLM respondeu {Status}. Entra a frase fixa.", (int)resposta.StatusCode);
                return fixa;
            }

            var corpo = await resposta.Content.ReadFromJsonAsync<RespostaDeChat>(limite.Token);
            var texto = corpo?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            return Aceitavel(texto) ? new MensagemAoCidadao(texto!, OrigemDaMensagem.Llm) : fixa;
        }
        catch (Exception e) when (e is OperationCanceledException or HttpRequestException or JsonException)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }

            log.LogWarning("LLM sem resposta em {Limite}s ({Erro}). Entra a frase fixa.", op.Limite.TotalSeconds, e.GetType().Name);
            return fixa;
        }
    }

    /// <summary>O prompt leva só nomes de enum traduzidos. Nunca o valor que foi bloqueado.</summary>
    public static TextoRotulado Prompt(EventoDeSeguranca evento)
    {
        var oQue = evento.Tipo == TipoDeEventoDeSeguranca.BloqueioDeSaida
            ? $"O sistema impediu que uma informação saísse pelo canal \"{DescricaoDoCanal(evento.Canal)}\"."
            : "O sistema recebeu um número de documento num cadastro e escondeu o número antes de guardar.";
        var tipos = string.Join(", ", evento.TiposDeDado.Select(DescricaoDoTipo));
        return TextoRotulado.Do(
            $"Evento de segurança: {evento.Tipo}. {oQue} Tipo de informação protegida: {tipos}. " +
            $"Parte do sistema: módulo {evento.Modulo}. Gravidade de 1 a 5: {evento.Severidade}. " +
            "Explique o que aconteceu e por que a pessoa idosa está protegida.");
    }

    private const string Sistema =
        "Você explica eventos de segurança de um sistema de saúde da prefeitura para um cidadão leigo de 60 anos. " +
        "Responda em português do Brasil, com exatamente 2 frases curtas e simples, sem termos técnicos, sem siglas, " +
        "sem nomes de pessoas, sem números de documento, sem nomes de doenças ou remédios e sem dizer com quem a pessoa mora.";

    private bool Aceitavel(string? texto) =>
        !string.IsNullOrWhiteSpace(texto)
        && texto.Length <= TamanhoMaximo
        && motor.Analisar(texto).Achados.Count == 0;

    private static string DescricaoDoCanal(Canal canal) => canal switch
    {
        Canal.GrupoDoBairro => "grupo de mensagens do bairro",
        Canal.Vizinho => "aviso ao vizinho",
        Canal.Mqtt => "aparelho instalado na casa",
        Canal.PromptDoLlm => "inteligência artificial",
        Canal.Log => "registro interno do sistema",
        Canal.MensagemDeErro => "mensagem de erro na tela do aplicativo",
        Canal.RespostaSemAutenticacao => "página aberta a quem não entrou no sistema",
        Canal.AppAutenticado => "aplicativo da agente de saúde",
        Canal.Banco => "banco de dados",
        _ => canal.ToString(),
    };

    private static string DescricaoDoTipo(TipoDeDado tipo) => tipo switch
    {
        TipoDeDado.Cpf => "número de CPF",
        TipoDeDado.Rg => "número de RG",
        TipoDeDado.Cns => "número do cartão do SUS",
        TipoDeDado.Saude => "informação de saúde",
        TipoDeDado.MoraSozinho => "informação sobre com quem a pessoa vive",
        TipoDeDado.Endereco => "endereço",
        TipoDeDado.Telefone => "telefone",
        TipoDeDado.Nome => "nome da pessoa",
        TipoDeDado.Credencial => "senha ou chave de acesso",
        _ => tipo.ToString(),
    };

    private sealed record MensagemDeChat(string Role, string Content);

    private sealed record PedidoDeChat(
        string Model,
        IReadOnlyList<MensagemDeChat> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        double Temperature);

    private sealed record RespostaDeChat(IReadOnlyList<Escolha>? Choices);

    private sealed record Escolha(MensagemDeChat? Message);
}
