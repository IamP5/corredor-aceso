using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Corredor.Contratos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Corredor.Api.Testes;

public class HostTestes(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Live_responde_200_sem_banco()
    {
        var resposta = await fabrica.CreateClient().GetAsync("/healthz/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Versao_responde_versao_sem_o_commit_e_o_ambiente_da_configuracao()
    {
        var versao = await fabrica.CreateClient()
            .GetFromJsonAsync<JsonElement>("/api/versao", TestContext.Current.CancellationToken);

        Assert.Matches(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$", versao.GetProperty("versao").GetString());
        Assert.False(string.IsNullOrEmpty(versao.GetProperty("commit").GetString()));
        Assert.Equal("dev", versao.GetProperty("ambiente").GetString());
    }

    [Fact]
    public async Task Swagger_publica_o_documento_openapi()
    {
        var resposta = await fabrica.CreateClient().GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public void Em_producao_vale_o_relogio_do_sistema_sem_deslocamento()
    {
        using var producao = fabrica.WithWebHostBuilder(host => host.UseEnvironment("Production"));
        var servicos = producao.Services;

        Assert.Same(TimeProvider.System, servicos.GetRequiredService<TimeProvider>());
        Assert.Equal(TimeSpan.Zero, servicos.GetRequiredService<IDeslocamentoDoRelogio>().Deslocamento);
        Assert.Equal(TimeSpan.FromMinutes(2), servicos.GetRequiredService<ParametrosDoRelogio>().ToleranciaDoRelogio);
    }

    // Vale antes e depois do RelogioDeDemo do Motor: sem PUT /api/demo/relogio, o deslocamento é zero.
    [Fact]
    public void Em_development_o_relogio_comeca_sem_deslocamento()
    {
        var servicos = fabrica.Services;
        var relogio = servicos.GetRequiredService<TimeProvider>();

        Assert.Equal(TimeSpan.Zero, servicos.GetRequiredService<IDeslocamentoDoRelogio>().Deslocamento);
        Assert.InRange(relogio.GetUtcNow() - DateTimeOffset.UtcNow, TimeSpan.FromSeconds(-5), TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(TipoDeViolacao.Invalida, 422)]
    [InlineData(TipoDeViolacao.Conflito, 409)]
    [InlineData(TipoDeViolacao.Desconhecido, 404)]
    [InlineData(TipoDeViolacao.Proibida, 403)]
    [InlineData(TipoDeViolacao.Indisponivel, 503)]
    public async Task Regra_de_dominio_violada_vira_status_e_corpo_com_codigo(TipoDeViolacao tipo, int status)
    {
        var http = new DefaultHttpContext { RequestServices = fabrica.Services };
        http.Response.Body = new MemoryStream();

        var tratada = await new TratadorDeRegraDeDominio().TryHandleAsync(
            http, new RegraDeDominioViolada("TURNO_ENCERRADO", "O turno de hoje já foi encerrado.", tipo), TestContext.Current.CancellationToken);

        Assert.True(tratada);
        Assert.Equal(status, http.Response.StatusCode);
        http.Response.Body.Position = 0;
        var corpo = await JsonSerializer.DeserializeAsync<JsonElement>(http.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("TURNO_ENCERRADO", corpo.GetProperty("codigo").GetString());
        Assert.Equal("O turno de hoje já foi encerrado.", corpo.GetProperty("mensagem").GetString());
    }

    // O Motor registra SemReceptorDeContato com TryAdd, que só loga. No host montado, o pedido de contato
    // precisa chegar ao Cadastro, senão o vizinho nunca é avisado. Visto funcionando na demo integrada.
    [Fact]
    public void No_host_o_pedido_de_contato_do_Motor_chega_ao_Cadastro()
    {
        using var escopo = fabrica.Services.CreateScope();

        var receptor = escopo.ServiceProvider.GetRequiredService<IReceptorDeContato>();

        Assert.Equal("Corredor.Cadastro", receptor.GetType().Assembly.GetName().Name);
    }

    // O alvo web do app roda em outra porta e manda Authorization. Sem CORS o navegador barra toda chamada.
    [Fact]
    public async Task Em_development_o_app_web_passa_pelo_preflight_com_authorization()
    {
        var resposta = await fabrica.CreateClient().SendAsync(Preflight("/api/radar?microarea=3"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal("*", resposta.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("authorization", string.Join(',', resposta.Headers.GetValues("Access-Control-Allow-Headers")).ToLowerInvariant());
    }

    [Fact]
    public async Task Fora_de_development_nao_ha_cors()
    {
        using var producao = fabrica.WithWebHostBuilder(host => host.UseEnvironment("Production"));

        var resposta = await producao.CreateClient().SendAsync(Preflight("/api/radar?microarea=3"), TestContext.Current.CancellationToken);

        Assert.False(resposta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // As rotas de demo do Motor e da Guarda só existem em Development. Na VM do Azure, em Production, somem.
    [Theory]
    [InlineData("GET", "/api/guarda/demo/condicoes")]
    [InlineData("GET", "/api/guarda/demo/painel")]
    [InlineData("POST", "/api/guarda/demo/grupo-do-bairro")]
    [InlineData("PUT", "/api/demo/relogio")]
    public async Task Fora_de_development_as_rotas_de_demo_nao_existem(string metodo, string rota)
    {
        // Com token válido, para o 401 da política padrão não esconder se a rota existe.
        using var producao = fabrica.WithWebHostBuilder(host => host.UseEnvironment("Production")
            .UseSetting("Autenticacao:Acs:0:Id", "acs-prod-01").UseSetting("Autenticacao:Acs:0:Microarea", "3")
            .UseSetting("Autenticacao:Acs:0:Token", "token-de-producao-no-teste"));
        var pedido = new HttpRequestMessage(new HttpMethod(metodo), rota) { Content = JsonContent.Create(new { }) };
        pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token-de-producao-no-teste");

        var resposta = await producao.CreateClient().SendAsync(pedido, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private static HttpRequestMessage Preflight(string rota)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Options, rota);
        pedido.Headers.Add("Origin", "http://localhost:8081");
        pedido.Headers.Add("Access-Control-Request-Method", "GET");
        pedido.Headers.Add("Access-Control-Request-Headers", "authorization");
        return pedido;
    }

    [Fact]
    public async Task Outra_excecao_fica_para_o_proximo_tratador()
    {
        var tratada = await new TratadorDeRegraDeDominio().TryHandleAsync(
            new DefaultHttpContext(), new InvalidOperationException("qualquer"), TestContext.Current.CancellationToken);

        Assert.False(tratada);
    }
}
