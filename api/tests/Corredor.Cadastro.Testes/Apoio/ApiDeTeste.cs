using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Corredor.Guarda;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corredor.Cadastro.Testes.Apoio;

// Um host mínimo com AddCadastro e MapCadastro, como o Program.cs faz, num banco real.
// O Motor é o dublê que registra o que recebeu. A Guarda é a do esqueleto, registrada por AddGuarda.
public sealed class ApiDeTeste : IAsyncDisposable
{
    public const string Acs = "acs-demo-01";
    public static readonly DateTimeOffset Inicio = new(2026, 11, 18, 10, 0, 0, TimeSpan.FromHours(-3));

    private readonly WebApplication _app;

    private ApiDeTeste(WebApplication app, HttpClient cliente)
    {
        _app = app;
        Cliente = cliente;
    }

    public HttpClient Cliente { get; }
    public MotorFalso Motor => _app.Services.GetRequiredService<MotorFalso>();
    public RelogioDeTeste Relogio => _app.Services.GetRequiredService<RelogioDeTeste>();
    public DeslocamentoDeTeste Deslocamento => _app.Services.GetRequiredService<DeslocamentoDeTeste>();
    public IServiceProvider Servicos => _app.Services;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<ApiDeTeste> IniciarAsync(BancoDeTeste banco, bool comReconciliacao = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testes" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration["ConnectionStrings:Cadastro"] = banco.ConexaoDaApi;
        builder.Services.ConfigureHttpJsonOptions(j => j.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddSingleton(new RelogioDeTeste(Inicio));
        builder.Services.AddSingleton<TimeProvider>(s => s.GetRequiredService<RelogioDeTeste>());
        builder.Services.AddSingleton<DeslocamentoDeTeste>();
        builder.Services.AddSingleton<IDeslocamentoDoRelogio>(s => s.GetRequiredService<DeslocamentoDeTeste>());
        builder.Services.AddSingleton(new ParametrosDoRelogio(TimeSpan.FromMinutes(2)));
        builder.Services.AddSingleton<MotorFalso>();
        builder.Services.AddSingleton<IMotorDeCasas>(s => s.GetRequiredService<MotorFalso>());
        builder.Services.AddAuthentication("Teste").AddScheme<AuthenticationSchemeOptions, AutenticacaoDeTeste>("Teste", null);

        builder.AddGuarda();
        // O parser provisório do esqueleto rotula todo texto. Este dublê acha só as palavras da tabela,
        // para o teste ver os dois vereditos. A política de canal é a SaidaSegura real da Guarda.
        builder.Services.AddSingleton<IMotorDeParsing, ParsingPorPalavra>();
        builder.AddCadastro();
        if (!comReconciliacao)
        {
            builder.Services.RemoveAll<IHostedService>();
        }

        var app = builder.Build();
        app.UseExceptionHandler(e => e.Run(TratarErroAsync));
        app.UseAuthentication();
        app.UseGuarda();
        app.MapGroup("/api").MapCadastro();
        await app.StartAsync();
        return new ApiDeTeste(app, app.GetTestClient());
    }

    public CadastroDbContext NovoContexto() => _app.Services.CreateScope().ServiceProvider.GetRequiredService<CadastroDbContext>();

    public async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string rota, object? corpo = null, bool autenticado = true)
    {
        using var pedido = new HttpRequestMessage(metodo, rota);
        if (corpo is not null)
        {
            pedido.Content = JsonContent.Create(corpo, options: Json);
        }

        if (autenticado)
        {
            pedido.Headers.Add("Authorization", $"Bearer {Acs}");
        }

        return await Cliente.SendAsync(pedido);
    }

    public Task<HttpResponseMessage> PostAsync(string rota, object corpo) => EnviarAsync(HttpMethod.Post, rota, corpo);

    public Task<HttpResponseMessage> PutAsync(string rota, object corpo) => EnviarAsync(HttpMethod.Put, rota, corpo);

    public Task<HttpResponseMessage> DeleteAsync(string rota) => EnviarAsync(HttpMethod.Delete, rota);

    public Task<HttpResponseMessage> GetAsync(string rota) => EnviarAsync(HttpMethod.Get, rota);

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    // O mesmo contrato do TratadorDeRegraDeDominio do host: {"codigo", "mensagem"} com 422, 409, 404 ou 503.
    private static async Task TratarErroAsync(HttpContext http)
    {
        var excecao = http.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (excecao is not RegraDeDominioViolada regra)
        {
            http.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        http.Response.StatusCode = regra.Tipo switch
        {
            TipoDeViolacao.Conflito => StatusCodes.Status409Conflict,
            TipoDeViolacao.Desconhecido => StatusCodes.Status404NotFound,
            TipoDeViolacao.Indisponivel => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        await http.Response.WriteAsJsonAsync(new { codigo = regra.Codigo, mensagem = regra.Message });
    }

    // "Authorization: Bearer <acs>" vira o claim acs, como o handler mínimo do host.
    private sealed class AutenticacaoDeTeste(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder u)
        : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, u)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var cabecalho = Request.Headers.Authorization.ToString();
            if (!cabecalho.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identidade = new ClaimsIdentity([new Claim("acs", cabecalho["Bearer ".Length..])], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Scheme.Name)));
        }
    }
}

public sealed class ParsingPorPalavra : IMotorDeParsing
{
    private static readonly (string Palavra, TipoDeDado Tipo)[] Tabela =
    [
        ("mora sozinho", TipoDeDado.MoraSozinho),
        ("diurético", TipoDeDado.Saude),
        ("Aparecida", TipoDeDado.Nome),
    ];

    public TextoAnalisado Rotular(string texto, params TipoDeDado[] rotulosDoCampo)
    {
        var achados = Tabela.Where(t => texto.Contains(t.Palavra, StringComparison.OrdinalIgnoreCase)).Select(t => t.Tipo);
        return new TextoAnalisado(TextoRotulado.Do(texto, rotulosDoCampo.Union(achados).ToArray()), []);
    }
}

public static class Respostas
{
    public static async Task<T> LerAsync<T>(this HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>(ApiDeTeste.Json))!;

    public static async Task<string> CodigoAsync(this HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString()!;
}
