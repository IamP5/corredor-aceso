using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corredor.Guarda.Testes;

/// <summary>Guarda o texto de cada linha de log, como o console escreveria.</summary>
internal sealed class LogCapturado : ILoggerProvider
{
    public ConcurrentQueue<string> Linhas { get; } = new();

    public string Tudo => string.Join('\n', Linhas);

    public ILogger CreateLogger(string categoria) => new Logger(this, categoria);

    public void Dispose() { }

    private sealed class Logger(LogCapturado dono, string categoria) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel nivel) => nivel >= LogLevel.Information;

        public void Log<TState>(LogLevel nivel, EventId id, TState state, Exception? excecao, Func<TState, Exception?, string> formatar)
        {
            if (IsEnabled(nivel))
            {
                dono.Linhas.Enqueue($"{nivel} {categoria}: {formatar(state, excecao)}{(excecao is null ? "" : "\n" + excecao)}");
            }
        }
    }
}

/// <summary>
/// Um host com o pipeline do Corredor.Api: UseGuarda depois do tratador de exceção, autenticação
/// antes das rotas, grupo /api com ComGuarda e MapGuarda. A autenticação de teste aceita o
/// cabeçalho Authorization: Bearer teste. Os eventos ficam na memória.
/// </summary>
internal sealed class HostDeTeste : IAsyncDisposable
{
    public const string Token = "teste";

    private readonly WebApplication _app;

    private HostDeTeste(WebApplication app, LogCapturado log, ArmazemEmMemoria armazem)
    {
        _app = app;
        Log = log;
        Armazem = armazem;
    }

    public HttpClient Cliente { get; private set; } = null!;

    public LogCapturado Log { get; }

    public ArmazemEmMemoria Armazem { get; }

    /// <summary>Os corpos que chegaram à rota de cadastro de teste, depois do interceptador.</summary>
    public ConcurrentQueue<JsonObject> Recebidos { get; } = new();

    public static async Task<HostDeTeste> IniciarAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        var log = new LogCapturado();
        builder.Logging.AddProvider(log);
        builder.Configuration["ConnectionStrings:Guarda"] = "Host=127.0.0.1;Database=sem_conexao";
        builder.Configuration["Llm:ApiKey"] = "";
        builder.Services.AddProblemDetails();
        builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.AddGuarda();
        var armazem = new ArmazemEmMemoria();
        builder.Services.AddSingleton<IArmazemDeEventos>(armazem);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseGuarda();
        app.Use((http, proximo) =>
        {
            if (http.Request.Headers.Authorization == $"Bearer {Token}")
            {
                http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("acs", "acs-demo-01")], "teste"));
            }

            return proximo(http);
        });

        var host = new HostDeTeste(app, log, armazem);
        var api = app.MapGroup("/api").ComGuarda();
        api.MapGuarda();

        // Fazem o papel das rotas do Cadastro: guardam o corpo como o binder recebeu.
        api.MapPost("/idosos", (JsonObject corpo) =>
        {
            host.Recebidos.Enqueue(corpo);
            return TypedResults.Created("/api/idosos/1", new { id = Guid.NewGuid() });
        });
        api.MapGet("/idosos/{id:guid}/condicoes", (Guid id) => TypedResults.Ok(new
        {
            id,
            condicoes = TextoRotulado.Do("usa concentrador de oxigênio", TipoDeDado.Saude),
        })).AllowAnonymous();
        api.MapGet("/lista-de-nomes", () => TypedResults.Ok(new[]
        {
            new { nome = TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome) },
        }));
        api.MapPost("/erro", (JsonObject corpo) =>
        {
            throw new InvalidOperationException($"falhou ao gravar {corpo["observacao"]}");
        });

        await app.StartAsync();
        host.Cliente = app.GetTestClient();
        return host;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string rota, object? corpo = null, bool autenticado = true) =>
        Cliente.SendAsync(Pedido(metodo, rota, corpo, autenticado), Ct);

    public static Task<string> TextoAsync(HttpResponseMessage resposta) => resposta.Content.ReadAsStringAsync(Ct);

    private static HttpRequestMessage Pedido(HttpMethod metodo, string rota, object? corpo, bool autenticado)
    {
        var pedido = new HttpRequestMessage(metodo, rota);
        if (autenticado)
        {
            pedido.Headers.Authorization = new("Bearer", Token);
        }

        if (corpo is not null)
        {
            pedido.Content = JsonContent.Create(corpo);
        }

        return pedido;
    }

    /// <summary>Espera a fila gravar e o tradutor anexar a mensagem a todo evento.</summary>
    public async Task EsperarEventosAsync()
    {
        var fila = _app.Services.GetRequiredService<FilaDeSeguranca>();
        for (var i = 0; i < 100; i++)
        {
            if (fila.Pendentes == 0 && Armazem.Todos.All(e => e.Mensagem is not null))
            {
                return;
            }

            await Task.Delay(50, Ct);
        }

        throw new TimeoutException("os eventos de segurança não foram gravados e traduzidos");
    }

    public async Task<JsonObject> PainelAsync()
    {
        await EsperarEventosAsync();
        var resposta = await Cliente.GetAsync("/api/painel", Ct);
        resposta.EnsureSuccessStatusCode();
        return await LerAsync(resposta);
    }

    public static async Task<JsonObject> LerAsync(HttpResponseMessage resposta) =>
        JsonNode.Parse(await TextoAsync(resposta))!.AsObject();

    public async ValueTask DisposeAsync()
    {
        Cliente.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
