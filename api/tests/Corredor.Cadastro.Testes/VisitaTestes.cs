using System.Net;
using Corredor.Cadastro.Rotas;
using Corredor.Cadastro.Testes.Apoio;
using Corredor.Contratos;

namespace Corredor.Cadastro.Testes;

// Agregado Visita e a tabela de desfechos, pelas rotas. O que o Motor recebe é o comportamento observado.
public sealed class VisitaTestes(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    [Theory]
    [InlineData("EstavaBem", ResultadoDaConfirmacao.PessoaVista)]
    [InlineData("Encaminhada", ResultadoDaConfirmacao.PessoaVista)]
    [InlineData("EstavaFora", ResultadoDaConfirmacao.AusenciaConfirmada)]
    public async Task Desfecho_vira_a_confirmacao_da_tabela_com_o_id_e_a_hora_da_visita(string desfecho, ResultadoDaConfirmacao esperado)
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        var id = Guid.NewGuid();
        var ocorridaEm = ApiDeTeste.Inicio.AddMinutes(-20);

        var resposta = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, desfecho, ocorridaEm));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var (casa, confirmacao) = api.Motor.Confirmacoes[id];
        Assert.Equal(token, casa.Valor);
        Assert.Equal((OrigemDaConfirmacao.Acs, esperado), (confirmacao.Origem, confirmacao.Resultado));
        Assert.Equal(ocorridaEm.ToUniversalTime(), confirmacao.Quando.ToUniversalTime());
    }

    [Fact]
    public async Task Caso21_NaoAbriu_grava_a_visita_e_nao_chama_o_Motor()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        var id = Guid.NewGuid();

        var resposta = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "NaoAbriu"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(0, api.Motor.Contar("confirmar"));
        await using var db = api.NovoContexto();
        Assert.Contains(db.Visitas, v => v.Id == id);
    }

    [Fact]
    public async Task Caso22_EstavaFora_nunca_e_sinal_de_vida()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        var id = Guid.NewGuid();

        await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "EstavaFora"));

        Assert.NotEqual(ResultadoDaConfirmacao.PessoaVista, api.Motor.Confirmacoes[id].Confirmacao.Resultado);
    }

    [Fact]
    public async Task Caso37_visita_repetida_com_o_mesmo_id_grava_uma_vez_e_o_Motor_guarda_uma_confirmacao()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        var id = Guid.NewGuid();

        var primeira = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "EstavaBem"));
        api.Relogio.Agora = ApiDeTeste.Inicio.AddMinutes(30);
        var segunda = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "EstavaBem"));

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        var (a, b) = (await primeira.LerAsync<VisitaRegistradaHttp>(), await segunda.LerAsync<VisitaRegistradaHttp>());
        Assert.Equal(a.RegistradaEm, b.RegistradaEm);
        Assert.Equal(TimeSpan.FromHours(-3), a.RegistradaEm.Offset);
        await using var db = api.NovoContexto();
        Assert.Single(db.Visitas, v => v.Id == id);
        Assert.Single(api.Motor.Confirmacoes.Values, c => c.Confirmacao.Id == id);
    }

    [Fact]
    public async Task Token_desconhecido_ou_revogado_responde_TOKEN_INATIVO()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (idoso, revogado) = await api.CasaAtivaComVizinhoAsync();
        await api.DeleteAsync($"/api/idosos/{idoso}/consentimento");

        foreach (var token in new[] { CasaToken.Novo().Valor, revogado })
        {
            var resposta = await api.PostAsync("/api/visitas", Cenario.Visita(Guid.NewGuid(), token, "EstavaBem"));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
            Assert.Equal("TOKEN_INATIVO", await resposta.CodigoAsync());
        }
    }

    [Fact]
    public async Task Visita_alem_da_tolerancia_do_relogio_responde_VISITA_NO_FUTURO()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();

        var adiantada = await api.PostAsync("/api/visitas", Cenario.Visita(Guid.NewGuid(), token, "EstavaBem", ApiDeTeste.Inicio.AddMinutes(3)));
        var tolerada = await api.PostAsync("/api/visitas", Cenario.Visita(Guid.NewGuid(), token, "EstavaBem", ApiDeTeste.Inicio.AddMinutes(1)));

        Assert.Equal("VISITA_NO_FUTURO", await adiantada.CodigoAsync());
        Assert.Equal(HttpStatusCode.Created, tolerada.StatusCode);
    }

    [Fact]
    public async Task Com_o_relogio_de_demo_deslocado_a_hora_da_visita_anda_junto()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        // O relógio de demo foi para as 09h58 de um servidor que está às 20h00: deslocamento de -10h02.
        api.Deslocamento.Deslocamento = TimeSpan.FromSeconds(-36120);
        api.Relogio.Agora = ApiDeTeste.Inicio.AddHours(10);
        var id = Guid.NewGuid();
        var noCelular = ApiDeTeste.Inicio.AddHours(10).AddMinutes(-1);

        var resposta = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "EstavaBem", noCelular));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(noCelular.AddSeconds(-36120).ToUniversalTime(), api.Motor.Confirmacoes[id].Confirmacao.Quando.ToUniversalTime());
    }

    [Fact]
    public async Task Motor_que_recusa_a_confirmacao_mantem_a_visita_e_responde_201()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        api.Motor.RecusaDeConfirmacao = new RegraDeDominioViolada("CONFIRMACAO_VELHA", "confirmação com mais de 24 horas");
        var id = Guid.NewGuid();

        var resposta = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "EstavaBem", ApiDeTeste.Inicio.AddHours(-25)));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        await using var db = api.NovoContexto();
        Assert.Contains(db.Visitas, v => v.Id == id);
    }

    [Fact]
    public async Task Motor_fora_do_ar_responde_503_com_a_visita_gravada_e_o_mesmo_id_conserta()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        var id = Guid.NewGuid();
        api.Motor.ForaDoAr = true;

        var falhou = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "EstavaBem"));
        api.Motor.ForaDoAr = false;
        var repetida = await api.PostAsync("/api/visitas", Cenario.Visita(id, token, "EstavaBem"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, falhou.StatusCode);
        Assert.Equal("MOTOR_INDISPONIVEL", await falhou.CodigoAsync());
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        Assert.True(api.Motor.Confirmacoes.ContainsKey(id));
    }

    [Fact]
    public async Task Visita_sem_ACS_autenticada_e_recusada_com_401()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();

        var resposta = await api.EnviarAsync(HttpMethod.Post, "/api/visitas", Cenario.Visita(Guid.NewGuid(), token, "EstavaBem"), autenticado: false);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Desfecho_desconhecido_responde_CAMPO_INVALIDO()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();

        var resposta = await api.PostAsync("/api/visitas", Cenario.Visita(Guid.NewGuid(), token, "Sumiu"));

        Assert.Equal("CAMPO_INVALIDO", await resposta.CodigoAsync());
    }
}
