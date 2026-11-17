using System.Net;
using Corredor.Cadastro.Rotas;

namespace Corredor.Cadastro.Testes.Apoio;

// Passos que vários testes repetem, sempre pelas rotas públicas.
public static class Cenario
{
    public static object NovoIdoso(string nome = "Efigênia Jatobá", string contato = "Sozinho", bool eletrodependente = false,
        string? condicoes = "usa concentrador de oxigênio", string? medicamentos = "diurético") => new
        {
            microareaId = 3,
            nome,
            endereco = "Rua do Sabiá, 12 - Vila Exemplo",
            perfil = new { contato, eletrodependente },
            condicoes,
            medicamentos,
        };

    public static async Task<Guid> CadastrarAsync(this ApiDeTeste api, object? corpo = null)
    {
        var resposta = await api.PostAsync("/api/idosos", corpo ?? NovoIdoso());
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.LerAsync<IdosoCriadoHttp>()).Id;
    }

    public static async Task<string> ConsentirAsync(this ApiDeTeste api, Guid idoso, bool avisarVizinho = true, bool avisarFamilia = false)
    {
        var resposta = await api.PostAsync($"/api/idosos/{idoso}/consentimento",
            new { versaoDoTermo = "2026-11", autorizacoes = new { avisarVizinho, avisarFamilia } });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.LerAsync<CasaTokenHttp>()).CasaToken;
    }

    public static async Task IndicarVizinhoAsync(this ApiDeTeste api, Guid idoso, string nome = "Odete Embaúba")
    {
        var resposta = await api.PutAsync($"/api/idosos/{idoso}/vizinho",
            new { nome, telefone = "(00) 90000-0001", aceitouEm = ApiDeTeste.Inicio.AddDays(-1) });
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    // Idoso com consentimento, autorização para avisar o vizinho e vizinho indicado.
    public static async Task<(Guid Idoso, string Token)> CasaAtivaComVizinhoAsync(this ApiDeTeste api)
    {
        var idoso = await api.CadastrarAsync();
        var token = await api.ConsentirAsync(idoso);
        await api.IndicarVizinhoAsync(idoso);
        return (idoso, token);
    }

    public static object Visita(Guid id, string token, string desfecho, DateTimeOffset? ocorridaEm = null, string? observacao = null) =>
        new { id, casaToken = token, ocorridaEm = ocorridaEm ?? ApiDeTeste.Inicio.AddMinutes(-5), desfecho, observacao };
}
