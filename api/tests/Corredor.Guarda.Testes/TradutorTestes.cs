using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Corredor.Guarda.Testes;

public sealed class TradutorTestes
{
    private const string ChaveFalsa = "chave-falsa-de-teste-0000";

    private static readonly DateTimeOffset Agora = new(2026, 11, 18, 20, 10, 0, TimeSpan.Zero);

    private static readonly EventoDeSeguranca Bloqueio = EventoDeSeguranca.Registrar(
        TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.GrupoDoBairro,
        [(TipoDeDado.MoraSozinho, 4), (TipoDeDado.Saude, 4)], "Cadastro", "/api/mensagens/verificar", Agora);

    private sealed class Servidor(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public List<string> Corpos { get; } = [];
        public List<string?> Autorizacoes { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken ct)
        {
            Corpos.Add(await pedido.Content!.ReadAsStringAsync(ct));
            Autorizacoes.Add(pedido.Headers.Authorization?.ToString());
            return await responder(pedido, ct);
        }
    }

    private sealed class LogNaMemoria : ILogger
    {
        public List<string> Linhas { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel l, EventId id, TState s, Exception? e, Func<TState, Exception?, string> f) => Linhas.Add(f(s, e));
    }

    private sealed class LoggerDe<T>(LogNaMemoria interno) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => interno.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel l, EventId id, TState s, Exception? e, Func<TState, Exception?, string> f) => interno.Log(l, id, s, e, f);
    }

    private static Tradutor Novo(Servidor servidor, string? chave = ChaveFalsa, LogNaMemoria? log = null, TimeSpan? limite = null)
    {
        var saida = Fabrica.Saida();
        var opcoes = Options.Create(new OpcoesDoLlm
        {
            ApiKey = chave,
            Modelo = "modelo/teste",
            UrlBase = "https://llm.teste/api/v1",
            Limite = limite ?? TimeSpan.FromSeconds(5),
        });
        ILogger<Tradutor> logger = log is null ? NullLogger<Tradutor>.Instance : new LoggerDe<Tradutor>(log);
        return new Tradutor(new HttpClient(servidor), opcoes, saida, new MotorDeParsing(), logger);
    }

    private static HttpResponseMessage Resposta(string conteudo, string fim = "stop") => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            new JsonObject
            {
                ["choices"] = new JsonArray(new JsonObject
                {
                    ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = conteudo },
                    ["finish_reason"] = fim,
                }),
            }.ToJsonString(),
            Encoding.UTF8, "application/json"),
    };

    // Visto na integração: openrouter/auto escolheu um modelo que pensa antes, o raciocínio gastou os
    // 200 tokens e o painel mostrou "impediu que dados sobre a sua saúde e a sua", cortado.
    [Fact]
    public async Task Resposta_cortada_no_limite_de_tokens_vira_a_frase_fixa()
    {
        var servidor = new Servidor((_, _) => Task.FromResult(Resposta(
            "O sistema da prefeitura impediu que dados sobre a sua saúde e a sua", fim: "length")));

        var mensagem = await Novo(servidor).TraduzirAsync(Bloqueio, CancellationToken.None);

        Assert.Equal(OrigemDaMensagem.FraseFixa, mensagem.Origem);
        Assert.Equal(FrasesFixas.DoTipoDeEvento(TipoDeEventoDeSeguranca.BloqueioDeSaida), mensagem.Texto);
    }

    [Fact]
    public async Task Pede_tokens_para_o_raciocinio_e_as_duas_frases()
    {
        var servidor = new Servidor((_, _) => Task.FromResult(Resposta("Uma frase. Outra frase.")));

        await Novo(servidor).TraduzirAsync(Bloqueio, CancellationToken.None);

        Assert.True((int)JsonNode.Parse(Assert.Single(servidor.Corpos))!["max_tokens"]! >= 1000);
    }

    [Fact]
    public async Task Chama_chat_completions_com_prompt_sem_o_valor_e_devolve_a_traducao_do_llm()
    {
        var servidor = new Servidor((_, _) => Task.FromResult(Resposta(
            "Uma mensagem com informações pessoais de um idoso foi barrada antes de ir para o grupo do bairro. Assim, ninguém de fora fica sabendo da vida dele.")));
        var tradutor = Novo(servidor);

        var mensagem = await tradutor.TraduzirAsync(Bloqueio, CancellationToken.None);

        Assert.Equal(OrigemDaMensagem.Llm, mensagem.Origem);
        Assert.StartsWith("Uma mensagem", mensagem.Texto);
        var corpo = JsonNode.Parse(Assert.Single(servidor.Corpos))!;
        Assert.Equal("modelo/teste", (string?)corpo["model"]);
        var prompt = (string?)corpo["messages"]![1]!["content"];
        Assert.Contains("grupo de mensagens do bairro", prompt);
        Assert.Contains("informação de saúde", prompt);
        Assert.Contains("módulo Cadastro", prompt);
        Assert.Contains("2 frases", (string?)corpo["messages"]![0]!["content"]);
        Assert.DoesNotContain("diur", prompt);
        Assert.DoesNotContain("sozinho", prompt);
        Assert.Equal($"Bearer {ChaveFalsa}", Assert.Single(servidor.Autorizacoes));
    }

    [Fact]
    public async Task Caso_42_llm_fora_do_ar_entra_a_frase_fixa()
    {
        var servidor = new Servidor((_, _) => throw new HttpRequestException("conexão recusada"));

        var mensagem = await Novo(servidor).TraduzirAsync(Bloqueio, CancellationToken.None);

        Assert.Equal(OrigemDaMensagem.FraseFixa, mensagem.Origem);
        Assert.Equal(FrasesFixas.DoTipoDeEvento(TipoDeEventoDeSeguranca.BloqueioDeSaida), mensagem.Texto);
    }

    [Fact]
    public async Task Sem_resposta_no_limite_entra_a_frase_fixa()
    {
        var servidor = new Servidor(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return Resposta("tarde demais");
        });

        var mensagem = await Novo(servidor, limite: TimeSpan.FromMilliseconds(200)).TraduzirAsync(Bloqueio, CancellationToken.None);

        Assert.Equal(OrigemDaMensagem.FraseFixa, mensagem.Origem);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Erro_http_entra_a_frase_fixa(HttpStatusCode status)
    {
        var servidor = new Servidor((_, _) => Task.FromResult(new HttpResponseMessage(status)));

        Assert.Equal(OrigemDaMensagem.FraseFixa, (await Novo(servidor).TraduzirAsync(Bloqueio, CancellationToken.None)).Origem);
    }

    [Fact]
    public async Task Sem_chave_nao_chama_o_llm()
    {
        var servidor = new Servidor((_, _) => Task.FromResult(Resposta("não deveria chamar")));

        var mensagem = await Novo(servidor, chave: null).TraduzirAsync(Bloqueio, CancellationToken.None);

        Assert.Equal(OrigemDaMensagem.FraseFixa, mensagem.Origem);
        Assert.Empty(servidor.Corpos);
    }

    [Theory]
    [InlineData("A senhora mora sozinha e toma diurético. Cuidado.")]
    [InlineData("Bloqueamos o CPF 123.456.789-00. Fique tranquila.")]
    [InlineData("")]
    public async Task Resposta_com_dado_pessoal_ou_vazia_entra_a_frase_fixa(string resposta)
    {
        var servidor = new Servidor((_, _) => Task.FromResult(Resposta(resposta)));

        Assert.Equal(OrigemDaMensagem.FraseFixa, (await Novo(servidor).TraduzirAsync(Bloqueio, CancellationToken.None)).Origem);
    }

    [Fact]
    public async Task A_chave_nunca_vai_ao_log()
    {
        var log = new LogNaMemoria();
        var servidor = new Servidor((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        await Novo(servidor, log: log).TraduzirAsync(Bloqueio, CancellationToken.None);

        Assert.NotEmpty(log.Linhas);
        Assert.DoesNotContain(log.Linhas, l => l.Contains(ChaveFalsa));
        Assert.DoesNotContain(ChaveFalsa, new OpcoesDoLlm { ApiKey = ChaveFalsa }.ToString());
    }
}
