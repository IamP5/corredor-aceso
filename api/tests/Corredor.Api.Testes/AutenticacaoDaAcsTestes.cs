using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Corredor.Api.Testes;

public class AutenticacaoDaAcsTestes : IAsyncLifetime
{
    private const string Token = "token-da-acs-de-teste-01";
    private WebApplication _app = null!;
    private HttpClient _cliente = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Autenticacao:Acs:0:Id"] = "acs-teste-01";
        builder.Configuration["Autenticacao:Acs:0:Microarea"] = "3";
        builder.Configuration["Autenticacao:Acs:0:Token"] = Token;
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<TratadorDeRegraDeDominio>();
        builder.AddAutenticacaoDaAcs();

        _app = builder.Build();
        _app.UseExceptionHandler();
        _app.UseAuthentication();
        _app.UseAuthorization();
        var api = _app.MapGroup("/api").ExigirMicroareaDaAcs();
        api.MapGet("/fechada", (ClaimsPrincipal acs) => new { acs = acs.FindFirstValue("acs"), microarea = acs.FindFirstValue("microarea") });
        api.MapGet("/aberta", () => "ok").AllowAnonymous();
        api.MapGet("/radar", (int microarea) => microarea);
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _cliente = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Rota_sem_AllowAnonymous_recusa_quem_nao_manda_token()
    {
        var resposta = await _cliente.GetAsync("/api/fechada", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_desconhecido_e_recusado()
    {
        var resposta = await Enviar("/api/fechada", "token-que-ninguem-tem-000");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_da_acs_vira_as_claims_acs_e_microarea()
    {
        var corpo = await (await Enviar("/api/fechada", Token)).Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal("acs-teste-01", corpo.GetProperty("acs").GetString());
        Assert.Equal("3", corpo.GetProperty("microarea").GetString());
    }

    [Fact]
    public async Task Rota_com_AllowAnonymous_responde_sem_token()
    {
        var resposta = await _cliente.GetAsync("/api/aberta", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Microarea_de_outra_acs_responde_403_com_codigo()
    {
        var resposta = await Enviar("/api/radar?microarea=4", Token);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal("MICROAREA_DE_OUTRA_ACS", corpo.GetProperty("codigo").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Enviar("/api/radar?microarea=3", Token)).StatusCode);
    }

    private Task<HttpResponseMessage> Enviar(string rota, string token)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, rota);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _cliente.SendAsync(pedido, TestContext.Current.CancellationToken);
    }
}

public class RotasAbertasDoHostTestes(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/healthz/live")]
    [InlineData("/api/versao")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    public async Task Rota_do_host_responde_sem_token(string rota)
    {
        var resposta = await fabrica.CreateClient().GetAsync(rota, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Swagger_declara_o_esquema_bearer()
    {
        var documento = await fabrica.CreateClient().GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Contains("\"bearer\"", documento);
    }
}
