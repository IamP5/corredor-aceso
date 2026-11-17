using System.Net;
using System.Text.Json.Nodes;
using Corredor.Guarda.Rotas;

namespace Corredor.Guarda.Testes;

/// <summary>
/// Os casos 38 a 42 da seção 7 de 03-modelo-de-dominio.md e as quatro evidências da auditoria,
/// pelo pipeline HTTP inteiro: interceptador, filtro de resposta, tratador de erros e log.
/// </summary>
public sealed class CasosDaGuardaTestes : IAsyncLifetime
{
    private HostDeTeste _host = null!;

    public async ValueTask InitializeAsync() => _host = await HostDeTeste.IniciarAsync();

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Evidencia_1_documento_do_professor_chega_mascarado_ao_binder_e_ao_log()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Post, "/api/guarda/demo/documento",
            new { documento = "123.456.789-00" });

        resposta.EnsureSuccessStatusCode();
        Assert.Equal("***.456.***-00", (string?)(await HostDeTeste.LerAsync(resposta))["documentoRecebido"]);
        await _host.EsperarEventosAsync();
        Assert.Contains("campo=documento tipo=Cpf valor=***.456.***-00", _host.Log.Tudo);
        Assert.DoesNotContain("123.456.789-00", _host.Log.Tudo);
        var evento = Assert.Single(_host.Armazem.Todos);
        Assert.Equal(TipoDeEventoDeSeguranca.MascaramentoNaEntrada, evento.Tipo);
        Assert.Equal(Canal.Banco, evento.Canal);
        Assert.Equal("/api/guarda/demo/documento", evento.Rota);
    }

    [Fact]
    public async Task Caso_38_cpf_em_condicoes_e_gravado_mascarado_com_nome_e_endereco_inteiros_e_farol_amarelo()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Post, "/api/idosos", new
        {
            microareaId = 3,
            nome = "Aparecida Souza",
            endereco = "Rua das Flores, 12",
            condicoes = "usa concentrador de oxigênio, CPF 123.456.789-00",
            medicamentos = "diurético",
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var recebido = Assert.Single(_host.Recebidos);
        Assert.Equal("usa concentrador de oxigênio, CPF ***.456.***-00", (string?)recebido["condicoes"]);
        Assert.Equal("Aparecida Souza", (string?)recebido["nome"]);
        Assert.Equal("Rua das Flores, 12", (string?)recebido["endereco"]);
        Assert.Equal("diurético", (string?)recebido["medicamentos"]);
        Assert.Contains("valor=***.456.***-00", _host.Log.Tudo);
        Assert.DoesNotContain("123.456.789-00", _host.Log.Tudo);

        var painel = await _host.PainelAsync();
        Assert.Equal("Amarelo", (string?)painel["cor"]);
        var evento = Assert.Single(painel["eventos"]!.AsArray())!;
        Assert.Equal("MascaramentoNaEntrada", (string?)evento["tipo"]);
        Assert.Equal("Cadastro", (string?)evento["modulo"]);
    }

    [Fact]
    public async Task Credencial_no_corpo_e_mascarada_pela_chave_e_casa_token_passa_inteiro()
    {
        await _host.EnviarAsync(HttpMethod.Post, "/api/idosos", new
        {
            casaToken = "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b",
            senha = "segredo-da-acs",
            observacao = "rg 12.345.678-9",
        });

        var recebido = Assert.Single(_host.Recebidos);
        Assert.Equal("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", (string?)recebido["casaToken"]);
        Assert.Equal("[CREDENCIAL]", (string?)recebido["senha"]);
        Assert.Equal("rg **.***.***-9", (string?)recebido["observacao"]);
        Assert.DoesNotContain("segredo-da-acs", _host.Log.Tudo);
    }

    [Fact]
    public async Task Evidencia_2_caso_40_gravar_o_nome_no_log_escreve_a_mascara_e_o_painel_ganha_um_evento()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Post, "/api/guarda/demo/log-nome",
            new { nome = "Aparecida Souza" });

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.Contains("Visita registrada para [NOME]", _host.Log.Tudo);
        Assert.DoesNotContain("Aparecida", _host.Log.Tudo);

        var painel = await _host.PainelAsync();
        var evento = Assert.Single(painel["eventos"]!.AsArray())!;
        Assert.Single(_host.Armazem.Todos);
        Assert.Equal("BloqueioDeSaida", (string?)evento["tipo"]);
        Assert.Equal("Log", (string?)evento["canal"]);
        Assert.Equal("Cadastro", (string?)evento["modulo"]);
        Assert.Equal("Nome", (string?)evento["tiposDeDado"]![0]);
        Assert.Equal(3, (int?)evento["severidade"]);
        Assert.Equal("Amarelo", (string?)painel["cor"]);
    }

    [Fact]
    public async Task Evidencia_3_caso_41_rota_sem_autenticacao_que_devolveria_condicoes_e_bloqueada()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Get,
            "/api/idosos/6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f/condicoes", autenticado: false);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        var corpo = await HostDeTeste.TextoAsync(resposta);
        Assert.Contains("RESPOSTA_BLOQUEADA", corpo);
        Assert.DoesNotContain("oxig", corpo);

        var painel = await _host.PainelAsync();
        var evento = Assert.Single(painel["eventos"]!.AsArray())!;
        Assert.Equal("RespostaSemAutenticacao", (string?)evento["canal"]);
        Assert.Equal("Saude", (string?)evento["tiposDeDado"]![0]);
        Assert.Equal("Vermelho", (string?)painel["cor"]);
        Assert.Equal("/api/idosos/{id:guid}/condicoes", Assert.Single(_host.Armazem.Todos).Rota);
    }

    [Fact]
    public async Task Rota_de_demonstracao_sem_autenticacao_tambem_e_bloqueada()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Get, "/api/guarda/demo/condicoes", autenticado: false);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.DoesNotContain("furosemida", await HostDeTeste.TextoAsync(resposta));
    }

    [Fact]
    public async Task Rota_autenticada_com_nome_nao_e_bloqueada_pelo_filtro()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Get, "/api/lista-de-nomes");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Empty(_host.Armazem.Todos);
    }

    [Fact]
    public async Task Evidencia_4_caso_39_mora_sozinho_e_diuretico_no_grupo_do_bairro_e_bloqueado_explicado_e_contado()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Post, "/api/guarda/demo/grupo-do-bairro",
            new { texto = "ele mora sozinho e toma diurético" });

        var corpo = await HostDeTeste.LerAsync(resposta);
        Assert.Equal("Bloqueada", (string?)corpo["veredito"]);
        Assert.Equal("ele [MORA_SOZINHO] e toma [SAUDE]", (string?)corpo["textoMascarado"]);
        Assert.Equal(
            "Essa mensagem diz que a pessoa mora sozinha e fala da saúde dela. No grupo do bairro, qualquer um lê.",
            (string?)corpo["explicacao"]);

        var painel = await _host.PainelAsync();
        Assert.Equal("Vermelho", (string?)painel["cor"]);
        Assert.Equal(12, (int?)painel["pontos24h"]);
        var canal = Assert.Single(painel["canais"]!.AsArray())!;
        Assert.Equal("GrupoDoBairro", (string?)canal["canal"]);
        Assert.Equal(1, (int?)canal["bloqueios"]);
        var evento = Assert.Single(painel["eventos"]!.AsArray())!;
        Assert.Equal(4, (int?)evento["severidade"]);
        Assert.Equal(["MoraSozinho", "Saude"], evento["tiposDeDado"]!.AsArray().Select(t => (string)t!).Order());
        Assert.False(string.IsNullOrWhiteSpace((string?)evento["mensagemAoCidadao"]));
    }

    [Fact]
    public async Task Caso_42_sem_llm_o_painel_mostra_a_frase_fixa()
    {
        await _host.EnviarAsync(HttpMethod.Post, "/api/guarda/demo/grupo-do-bairro",
            new { texto = "ele mora sozinho e toma diurético" });

        var evento = (await _host.PainelAsync())["eventos"]![0]!;

        Assert.Equal("FraseFixa", (string?)evento["origemDaMensagem"]);
        Assert.Equal(FrasesFixas.DoTipoDeEvento(TipoDeEventoDeSeguranca.BloqueioDeSaida), (string?)evento["mensagemAoCidadao"]);
    }

    [Fact]
    public async Task Mensagem_livre_sem_dado_pessoal_e_liberada_e_farol_fica_verde()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Post, "/api/guarda/demo/grupo-do-bairro",
            new { texto = "Preciso de ajuda para uma visita agora. Quem puder, me chame no privado." });

        Assert.Equal("Liberada", (string?)(await HostDeTeste.LerAsync(resposta))["veredito"]);
        var painel = await _host.PainelAsync();
        Assert.Equal("Verde", (string?)painel["cor"]);
        Assert.Empty(painel["eventos"]!.AsArray());
    }

    [Fact]
    public async Task Erro_com_dado_pessoal_mostra_texto_neutro_e_o_log_recebe_a_mascara()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Post, "/api/erro",
            new { observacao = "Dona Aparecida mora sozinha" });

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        var corpo = await HostDeTeste.TextoAsync(resposta);
        Assert.Contains(TratadorDeErros.MensagemNeutra, corpo);
        Assert.DoesNotContain("Aparecida", corpo);
        await _host.EsperarEventosAsync();
        Assert.DoesNotContain("Aparecida", _host.Log.Tudo);
        Assert.Contains(_host.Armazem.Todos, e => e.Canal == Canal.MensagemDeErro);
    }

    [Fact]
    public async Task Painel_e_aberto_e_nao_tem_rota_nem_id()
    {
        await _host.EnviarAsync(HttpMethod.Post, "/api/guarda/demo/log-nome", new { nome = "Aparecida Souza" });

        var painel = await _host.PainelAsync();

        var evento = painel["eventos"]![0]!.AsObject();
        Assert.False(evento.ContainsKey("rota"));
        Assert.False(evento.ContainsKey("id"));
        Assert.DoesNotContain("Aparecida", painel.ToJsonString());
    }

    [Fact]
    public async Task Pagina_do_painel_sai_em_development()
    {
        var resposta = await _host.EnviarAsync(HttpMethod.Get, "/api/guarda/demo/painel", autenticado: false);

        Assert.Equal("text/html", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Risco Cidadão", await HostDeTeste.TextoAsync(resposta));
    }
}
