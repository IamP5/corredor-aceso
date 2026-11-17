using System.Net;
using Corredor.Cadastro.Dominio;
using Corredor.Cadastro.Rotas;
using Corredor.Cadastro.Testes.Apoio;
using Corredor.Contratos;
using Microsoft.Extensions.DependencyInjection;

namespace Corredor.Cadastro.Testes;

// Pedido de contato do Motor (P19), avisos do dia e resposta do vizinho (P20).
public sealed class AvisoTestes(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    private static PedidoDeContato Pedido(string token, MotivoDoAlerta motivo = MotivoDoAlerta.SemMovimentoNaJanela) =>
        new(CasaToken.TryParse(token, out var t) ? t : throw new ArgumentException(token), motivo, ApiDeTeste.Inicio.AddTicks(7));

    private static async Task<List<AvisoHttp>> AvisosDaCasaAsync(ApiDeTeste api, string token) =>
        (await (await api.GetAsync("/api/avisos?microarea=3")).LerAsync<List<AvisoHttp>>()).Where(a => a.CasaToken == token).ToList();

    [Fact]
    public async Task Caso35_pedido_de_contato_repetido_no_mesmo_episodio_gera_um_aviso()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        var receptor = api.Servicos.GetRequiredService<IReceptorDeContato>();

        await receptor.PedirContatoAsync(Pedido(token), TestContext.Current.CancellationToken);
        await receptor.PedirContatoAsync(Pedido(token), TestContext.Current.CancellationToken);
        await receptor.PedirContatoAsync(Pedido(token, MotivoDoAlerta.CalorInterno), TestContext.Current.CancellationToken);

        var avisos = await AvisosDaCasaAsync(api, token);
        Assert.Equal(2, avisos.Count);
        Assert.Single(avisos, a => a.Motivo == "SemMovimentoNaJanela");
        Assert.All(avisos, a => Assert.Null(a.Resposta));
    }

    [Fact]
    public async Task Caso36_pedido_de_contato_sem_AvisarVizinho_ou_sem_vizinho_nao_gera_aviso()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var semAutorizacao = await api.ConsentirAsync(await api.CadastrarAsync(), avisarVizinho: false);
        var semVizinho = await api.ConsentirAsync(await api.CadastrarAsync());
        var receptor = api.Servicos.GetRequiredService<IReceptorDeContato>();

        await receptor.PedirContatoAsync(Pedido(semAutorizacao), TestContext.Current.CancellationToken);
        await receptor.PedirContatoAsync(Pedido(semVizinho), TestContext.Current.CancellationToken);
        await receptor.PedirContatoAsync(Pedido(CasaToken.Novo().Valor), TestContext.Current.CancellationToken);

        Assert.Empty(await AvisosDaCasaAsync(api, semAutorizacao));
        Assert.Empty(await AvisosDaCasaAsync(api, semVizinho));
    }

    [Fact]
    public void Caso34_destinatarios_do_aviso_nunca_incluem_a_familia_sem_AvisarFamilia()
    {
        var agora = ApiDeTeste.Inicio;
        var (semFamilia, _) = Idoso.Cadastrar(IdosoId.Novo(), new MicroareaId(3), "Efigênia Jatobá", "Rua do Sabiá, 12",
            new PerfilDaCasa(PerfilDeContato.Sozinho, false), null, null);
        semFamilia.RegistrarConsentimento(new AcsId("acs-demo-01"), "2026-11", new Autorizacoes(true, false), agora);
        semFamilia.IndicarVizinho("Odete Embaúba", "(00) 90000-0001", agora);
        var (comFamilia, _) = Idoso.Cadastrar(IdosoId.Novo(), new MicroareaId(3), "Geraldo Jequitibá", "Rua do Sabiá, 14",
            new PerfilDaCasa(PerfilDeContato.Sozinho, false), null, null);
        comFamilia.RegistrarConsentimento(new AcsId("acs-demo-01"), "2026-11", new Autorizacoes(false, true), agora);

        Assert.Equal([DestinatarioDoAviso.Vizinho], semFamilia.DestinatariosDoAviso());
        Assert.Equal([DestinatarioDoAviso.Familia], comFamilia.DestinatariosDoAviso());
    }

    [Fact]
    public async Task Caso33_revogar_apaga_os_avisos_daquele_token()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (idoso, token) = await api.CasaAtivaComVizinhoAsync();
        await api.Servicos.GetRequiredService<IReceptorDeContato>().PedirContatoAsync(Pedido(token), TestContext.Current.CancellationToken);

        await api.DeleteAsync($"/api/idosos/{idoso}/consentimento");

        Assert.Empty(await AvisosDaCasaAsync(api, token));
    }

    [Fact]
    public async Task Avisos_do_dia_trazem_so_as_ultimas_24_horas_com_horas_em_menos_tres()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        var receptor = api.Servicos.GetRequiredService<IReceptorDeContato>();
        api.Relogio.Agora = ApiDeTeste.Inicio.AddHours(-30);
        await receptor.PedirContatoAsync(Pedido(token, MotivoDoAlerta.CalorInterno), TestContext.Current.CancellationToken);
        api.Relogio.Agora = ApiDeTeste.Inicio;
        await receptor.PedirContatoAsync(Pedido(token), TestContext.Current.CancellationToken);

        var aviso = Assert.Single(await AvisosDaCasaAsync(api, token));

        Assert.Equal("SemMovimentoNaJanela", aviso.Motivo);
        Assert.Equal(TimeSpan.FromHours(-3), aviso.Desde.Offset);
    }

    [Fact]
    public async Task Resposta_EstaBem_confirma_a_pessoa_vista_no_Motor_com_o_id_do_aviso()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        await api.Servicos.GetRequiredService<IReceptorDeContato>().PedirContatoAsync(Pedido(token), TestContext.Current.CancellationToken);
        var aviso = Assert.Single(await AvisosDaCasaAsync(api, token));
        api.Relogio.Agora = ApiDeTeste.Inicio.AddMinutes(5);

        var resposta = await api.PostAsync($"/api/avisos/{aviso.Id}/resposta", new { resposta = "EstaBem" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.LerAsync<RespostaRegistradaHttp>();
        Assert.Equal(("EstaBem", ApiDeTeste.Inicio.AddMinutes(5)), (corpo.Resposta, corpo.RespondidoEm));
        var (casa, confirmacao) = api.Motor.Confirmacoes[aviso.Id];
        Assert.Equal(token, casa.Valor);
        Assert.Equal((OrigemDaConfirmacao.Vizinho, ResultadoDaConfirmacao.PessoaVista), (confirmacao.Origem, confirmacao.Resultado));
    }

    [Fact]
    public async Task Resposta_igual_nao_muda_nada_resposta_diferente_responde_409_e_PrecisaDeAjuda_nao_chama_o_Motor()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        await api.Servicos.GetRequiredService<IReceptorDeContato>().PedirContatoAsync(Pedido(token), TestContext.Current.CancellationToken);
        var aviso = Assert.Single(await AvisosDaCasaAsync(api, token));

        var primeira = await api.PostAsync($"/api/avisos/{aviso.Id}/resposta", new { resposta = "PrecisaDeAjuda" });
        api.Relogio.Agora = ApiDeTeste.Inicio.AddMinutes(9);
        var igual = await api.PostAsync($"/api/avisos/{aviso.Id}/resposta", new { resposta = "PrecisaDeAjuda" });
        var diferente = await api.PostAsync($"/api/avisos/{aviso.Id}/resposta", new { resposta = "EstaBem" });

        Assert.Equal(HttpStatusCode.OK, igual.StatusCode);
        Assert.Equal((await primeira.LerAsync<RespostaRegistradaHttp>()).RespondidoEm, (await igual.LerAsync<RespostaRegistradaHttp>()).RespondidoEm);
        Assert.Equal(HttpStatusCode.Conflict, diferente.StatusCode);
        Assert.Equal("AVISO_JA_RESPONDIDO", await diferente.CodigoAsync());
        Assert.Equal(0, api.Motor.Contar("confirmar"));
    }

    [Fact]
    public async Task Resposta_a_aviso_que_nao_existe_responde_404()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);

        var resposta = await api.PostAsync($"/api/avisos/{Guid.NewGuid()}/resposta", new { resposta = "EstaBem" });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("AVISO_DESCONHECIDO", await resposta.CodigoAsync());
    }
}
