using System.Net;
using Corredor.Cadastro.Rotas;
using Corredor.Cadastro.Testes.Apoio;

namespace Corredor.Cadastro.Testes;

// Agregado MensagemSaida: o veredito sai da SaidaSegura e o banco só guarda o texto mascarado.
public sealed class MensagemTestes(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    [Fact]
    public async Task Caso39_mora_sozinho_e_remedio_no_grupo_do_bairro_e_bloqueada_e_so_o_texto_mascarado_fica_gravado()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);

        var resposta = await api.PostAsync("/api/mensagens/verificar",
            new { canal = "GrupoDoBairro", texto = "ele mora sozinho e toma diurético" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.LerAsync<MensagemVerificadaHttp>();
        Assert.Equal("Bloqueada", corpo.Veredito);
        Assert.Equal(["MoraSozinho", "Saude"], corpo.TiposDeDado);
        Assert.Equal("Essa mensagem diz que a pessoa mora sozinha e fala da saúde dela. No grupo do bairro, qualquer um lê.", corpo.Explicacao);
        Assert.Equal("Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.", corpo.VersaoSegura);
        Assert.DoesNotContain("diurético", corpo.TextoMascarado);
        var gravado = await banco.ConsultarComoApiAsync<string>(
            $"SELECT texto_mascarado || '|' || veredito || '|' || array_to_string(tipos_de_dado, ',') FROM cadastro.mensagem_saida WHERE id = '{corpo.Id}'");
        Assert.Equal($"{corpo.TextoMascarado}|Bloqueada|MoraSozinho,Saude", gravado);
        Assert.DoesNotContain("sozinho", gravado);
    }

    [Fact]
    public async Task Mensagem_sem_dado_pessoal_e_liberada_sem_explicacao_nem_versao_segura()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);

        var corpo = await (await api.PostAsync("/api/mensagens/verificar",
            new { canal = "GrupoDoBairro", texto = "reunião no posto às 15h" })).LerAsync<MensagemVerificadaHttp>();

        Assert.Equal(("Liberada", "reunião no posto às 15h"), (corpo.Veredito, corpo.TextoMascarado));
        Assert.Empty(corpo.TiposDeDado);
        Assert.Null(corpo.Explicacao);
        Assert.Null(corpo.VersaoSegura);
    }

    [Fact]
    public async Task Telefone_vai_ao_app_autenticado_mas_nao_ao_grupo_do_bairro()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        const string texto = "ligar para (00) 98888-0000";

        var app = await (await api.PostAsync("/api/mensagens/verificar", new { canal = "AppAutenticado", texto })).LerAsync<MensagemVerificadaHttp>();
        var grupo = await (await api.PostAsync("/api/mensagens/verificar", new { canal = "GrupoDoBairro", texto })).LerAsync<MensagemVerificadaHttp>();

        Assert.Equal("Liberada", app.Veredito);
        Assert.Equal("Bloqueada", grupo.Veredito);
        Assert.Contains("Telefone", grupo.TiposDeDado);
        Assert.DoesNotContain("98888", grupo.TextoMascarado);
    }

    [Fact]
    public async Task Mensagem_vazia_canal_desconhecido_ou_sem_ACS_e_recusada()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);

        var vazia = await api.PostAsync("/api/mensagens/verificar", new { canal = "GrupoDoBairro", texto = "" });
        var canal = await api.PostAsync("/api/mensagens/verificar", new { canal = "Twitter", texto = "oi" });
        var anonima = await api.EnviarAsync(HttpMethod.Post, "/api/mensagens/verificar", new { canal = "GrupoDoBairro", texto = "oi" }, autenticado: false);

        Assert.Equal("CAMPO_INVALIDO", await vazia.CodigoAsync());
        Assert.Equal("CAMPO_INVALIDO", await canal.CodigoAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);
    }
}
