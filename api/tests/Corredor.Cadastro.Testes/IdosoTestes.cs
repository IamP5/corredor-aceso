using System.Net;
using Corredor.Cadastro.Rotas;
using Corredor.Cadastro.Testes.Apoio;
using Corredor.Contratos;

namespace Corredor.Cadastro.Testes;

// Agregado Idoso: cadastro, consentimento, token, revogação, vizinho e perfil, pelas rotas.
public sealed class IdosoTestes(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    [Fact]
    public async Task Caso32_idoso_sem_consentimento_nao_tem_token_e_nao_entra_na_lista_de_nomes()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync(Cenario.NovoIdoso(nome: "Dirce Aroeira"));

        var lista = await (await api.GetAsync("/api/lista-de-nomes?microarea=3")).LerAsync<List<ItemDaListaDeNomesHttp>>();

        Assert.DoesNotContain(lista, i => i.Nome == "Dirce Aroeira");
        Assert.Empty(api.Motor.Chamadas);
        await using var db = api.NovoContexto();
        Assert.Null(db.Idosos.Single(i => i.Id == new Dominio.IdosoId(idoso)).TokenAtivo);
    }

    [Fact]
    public async Task Consentimento_gera_token_de_128_bits_e_ativa_a_casa_no_Motor_com_microarea_e_perfil()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync(Cenario.NovoIdoso(contato: "Acamado", eletrodependente: true));

        var token = await api.ConsentirAsync(idoso);

        Assert.Matches("^[0-9a-f]{32}$", token);
        Assert.True(CasaToken.TryParse(token, out var casa));
        Assert.Equal(new PerfilDaCasa(PerfilDeContato.Acamado, true), api.Motor.Ativas[casa]);
    }

    [Fact]
    public async Task Consentimento_repetido_devolve_o_mesmo_token_e_repete_a_ativacao_no_Motor()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync();

        var primeiro = await api.ConsentirAsync(idoso);
        var segundo = await api.ConsentirAsync(idoso);

        Assert.Equal(primeiro, segundo);
        Assert.Equal(2, api.Motor.Contar($"ativar {primeiro}"));
    }

    [Fact]
    public async Task Consentimento_com_Motor_fora_do_ar_fica_gravado_responde_503_e_a_repeticao_conserta()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync();
        api.Motor.ForaDoAr = true;

        var falhou = await api.PostAsync($"/api/idosos/{idoso}/consentimento",
            new { versaoDoTermo = "2026-11", autorizacoes = new { avisarVizinho = true, avisarFamilia = false } });
        api.Motor.ForaDoAr = false;
        var token = await api.ConsentirAsync(idoso);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, falhou.StatusCode);
        Assert.Equal("MOTOR_INDISPONIVEL", await falhou.CodigoAsync());
        Assert.True(CasaToken.TryParse(token, out var casa));
        Assert.True(api.Motor.Ativas.ContainsKey(casa));
        await using var db = api.NovoContexto();
        Assert.Single(db.Idosos.Single(i => i.Id == new Dominio.IdosoId(idoso)).Consentimentos);
    }

    [Fact]
    public async Task Consentimento_sem_ACS_autenticada_e_recusado_com_401()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync();

        var resposta = await api.EnviarAsync(HttpMethod.Post, $"/api/idosos/{idoso}/consentimento",
            new { versaoDoTermo = "2026-11", autorizacoes = new { avisarVizinho = true, avisarFamilia = false } }, autenticado: false);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Empty(api.Motor.Chamadas);
    }

    [Fact]
    public async Task Consentimento_de_idoso_que_nao_existe_responde_404()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);

        var resposta = await api.PostAsync($"/api/idosos/{Guid.NewGuid()}/consentimento",
            new { versaoDoTermo = "2026-11", autorizacoes = new { avisarVizinho = true, avisarFamilia = false } });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("IDOSO_DESCONHECIDO", await resposta.CodigoAsync());
    }

    [Theory]
    [InlineData("", "Rua do Sabiá, 12", "NOME_OBRIGATORIO")]
    [InlineData("Efigênia Jatobá", " ", "ENDERECO_OBRIGATORIO")]
    public async Task Cadastro_sem_nome_ou_sem_endereco_e_recusado(string nome, string endereco, string codigo)
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);

        var resposta = await api.PostAsync("/api/idosos",
            new { microareaId = 3, nome, endereco, perfil = new { contato = "Sozinho", eletrodependente = false } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(codigo, await resposta.CodigoAsync());
    }

    [Fact]
    public async Task Cadastro_com_campo_fora_do_limite_ou_perfil_desconhecido_responde_CAMPO_INVALIDO()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);

        var longo = await api.PostAsync("/api/idosos", Cenario.NovoIdoso(nome: new string('a', 121)));
        var perfil = await api.PostAsync("/api/idosos", Cenario.NovoIdoso(contato: "Sozinha"));

        Assert.Equal("CAMPO_INVALIDO", await longo.CodigoAsync());
        Assert.Equal("CAMPO_INVALIDO", await perfil.CodigoAsync());
    }

    [Fact]
    public async Task Caso33_revogar_apaga_o_token_e_o_vizinho_desativa_no_Motor_e_as_visitas_ficam()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (idoso, token) = await api.CasaAtivaComVizinhoAsync();
        var visita = Guid.NewGuid();
        await api.PostAsync("/api/visitas", Cenario.Visita(visita, token, "EstavaBem"));

        var resposta = await api.DeleteAsync($"/api/idosos/{idoso}/consentimento");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(1, api.Motor.Contar($"desativar {token}"));
        var lista = await (await api.GetAsync("/api/lista-de-nomes?microarea=3")).LerAsync<List<ItemDaListaDeNomesHttp>>();
        Assert.DoesNotContain(lista, i => i.CasaToken == token);
        await using var db = api.NovoContexto();
        var gravado = db.Idosos.Single(i => i.Id == new Dominio.IdosoId(idoso));
        Assert.Null(gravado.TokenAtivo);
        Assert.Null(gravado.Vizinho);
        Assert.NotNull(Assert.Single(gravado.Consentimentos).RevogadoEm);
        Assert.Contains(db.Visitas, v => v.Id == visita);
    }

    [Fact]
    public async Task Revogar_sem_consentimento_vigente_nao_faz_nada_e_consentir_de_novo_gera_outro_token()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync();
        var primeiro = await api.ConsentirAsync(idoso);
        await api.DeleteAsync($"/api/idosos/{idoso}/consentimento");

        var repetida = await api.DeleteAsync($"/api/idosos/{idoso}/consentimento");
        var segundo = await api.ConsentirAsync(idoso);

        Assert.Equal(HttpStatusCode.NoContent, repetida.StatusCode);
        Assert.Equal(1, api.Motor.Contar("desativar"));
        Assert.NotEqual(primeiro, segundo);
    }

    [Fact]
    public async Task Vizinho_sem_autorizacao_AvisarVizinho_e_recusado()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var semConsentimento = await api.CadastrarAsync();
        var semAutorizacao = await api.CadastrarAsync();
        await api.ConsentirAsync(semAutorizacao, avisarVizinho: false);

        foreach (var idoso in new[] { semConsentimento, semAutorizacao })
        {
            var resposta = await api.PutAsync($"/api/idosos/{idoso}/vizinho",
                new { nome = "Odete Embaúba", telefone = "(00) 90000-0001", aceitouEm = ApiDeTeste.Inicio });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
            Assert.Equal("AUTORIZACAO_AUSENTE", await resposta.CodigoAsync());
        }
    }

    [Fact]
    public async Task Perfil_novo_vai_ao_Motor_so_com_token_ativo()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var semToken = await api.CadastrarAsync();
        var comToken = await api.CadastrarAsync();
        var token = await api.ConsentirAsync(comToken);

        await api.PutAsync($"/api/idosos/{semToken}/perfil", new { contato = "Coabitado", eletrodependente = false });
        var resposta = await api.PutAsync($"/api/idosos/{comToken}/perfil", new { contato = "Acamado", eletrodependente = true });

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(1, api.Motor.Contar("perfil"));
        Assert.True(CasaToken.TryParse(token, out var casa));
        Assert.Equal(new PerfilDaCasa(PerfilDeContato.Acamado, true), api.Motor.Ativas[casa]);
    }

    [Fact]
    public async Task Lista_de_nomes_traz_nome_endereco_e_vizinho_e_nunca_condicoes_ou_medicamentos()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, comVizinho) = await api.CasaAtivaComVizinhoAsync();
        var semVizinho = await api.ConsentirAsync(await api.CadastrarAsync(Cenario.NovoIdoso(nome: "Geraldo Jequitibá")));

        var resposta = await api.GetAsync("/api/lista-de-nomes?microarea=3");
        var corpo = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var lista = await resposta.LerAsync<List<ItemDaListaDeNomesHttp>>();

        var item = Assert.Single(lista, i => i.CasaToken == comVizinho);
        Assert.Equal("Efigênia Jatobá", item.Nome);
        Assert.Equal("Rua do Sabiá, 12 - Vila Exemplo", item.Endereco);
        Assert.Equal(new VizinhoDaListaHttp("Odete Embaúba", "(00) 90000-0001"), item.Vizinho);
        Assert.Null(Assert.Single(lista, i => i.CasaToken == semVizinho).Vizinho);
        Assert.DoesNotContain("concentrador", corpo);
        Assert.DoesNotContain("diurético", corpo);
    }

    [Fact]
    public async Task Reconciliacao_ao_iniciar_reenvia_ao_Motor_toda_casa_com_token()
    {
        string token;
        await using (var api = await ApiDeTeste.IniciarAsync(banco))
        {
            token = await api.ConsentirAsync(await api.CadastrarAsync());
        }

        await using var reiniciada = await ApiDeTeste.IniciarAsync(banco, comReconciliacao: true);
        var prazo = DateTime.UtcNow.AddSeconds(10);
        while (reiniciada.Motor.Contar($"ativar {token}") == 0 && DateTime.UtcNow < prazo)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, reiniciada.Motor.Contar($"ativar {token}"));
    }
}
