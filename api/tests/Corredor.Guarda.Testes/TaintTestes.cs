using System.Text.Json;

namespace Corredor.Guarda.Testes;

public sealed class TaintTestes
{
    private static (SaidaSegura Saida, FilaDeSeguranca Fila) NovaSaida()
    {
        var fila = new FilaDeSeguranca();
        return (Fabrica.Saida(fila, new RotaFixa("/api/mensagens/verificar", "Cadastro")), fila);
    }

    [Fact]
    public void Juntar_dois_textos_une_os_rotulos()
    {
        var nome = TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome);
        var condicao = TextoRotulado.Do("usa concentrador de oxigênio", TipoDeDado.Saude);

        var junto = nome.Juntar(condicao, ", ");

        Assert.Equal(new HashSet<TipoDeDado> { TipoDeDado.Nome, TipoDeDado.Saude }, junto.Rotulos);
        Assert.Equal("[NOME], [SAUDE]", junto.ToString());
    }

    [Fact]
    public void Texto_rotulado_nunca_mostra_o_valor_em_interpolacao_nem_em_json()
    {
        var nome = TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome);

        Assert.Equal("Olá [NOME]", $"Olá {nome}");
        Assert.DoesNotContain("Aparecida", JsonSerializer.Serialize(new { nome }));
    }

    [Theory]
    [InlineData(Canal.GrupoDoBairro, 3)]
    [InlineData(Canal.PromptDoLlm, 3)]
    [InlineData(Canal.Mqtt, 3)]
    [InlineData(Canal.MensagemDeErro, 3)]
    [InlineData(Canal.RespostaSemAutenticacao, 3)]
    [InlineData(Canal.Vizinho, 3)]
    [InlineData(Canal.Log, 2)]
    public void Canais_abertos_nao_aceitam_nenhum_rotulo_e_cada_bloqueio_vira_evento(Canal canal, int exposicao)
    {
        var (saida, fila) = NovaSaida();

        foreach (var tipo in Enum.GetValues<TipoDeDado>())
        {
            var veredito = Assert.IsType<Veredito.Bloqueada>(saida.Liberar(TextoRotulado.Do("valor secreto", tipo), canal));
            Assert.Equal(new HashSet<TipoDeDado> { tipo }, veredito.TiposDeDado);
            Assert.DoesNotContain("secreto", veredito.TextoMascarado);
        }

        Assert.Equal(exposicao, PoliticaDeCanal.Exposicao(canal));
        var eventos = fila.Drenar();
        Assert.Equal(Enum.GetValues<TipoDeDado>().Length, eventos.Count);
        Assert.All(eventos, e => Assert.Equal(canal, e.Canal));
    }

    [Fact]
    public void App_autenticado_aceita_nome_endereco_e_telefone_e_recusa_saude()
    {
        var (saida, _) = NovaSaida();

        var nome = saida.Liberar(TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome), Canal.AppAutenticado);
        var saude = saida.Liberar(TextoRotulado.Do("diurético", TipoDeDado.Saude), Canal.AppAutenticado);

        Assert.Equal("Aparecida Souza", Assert.IsType<Veredito.Liberada>(nome).Texto);
        Assert.IsType<Veredito.Bloqueada>(saude);
        Assert.Equal(1, PoliticaDeCanal.Exposicao(Canal.AppAutenticado));
    }

    [Fact]
    public void Banco_aceita_todo_rotulo_e_texto_sem_rotulo_sai_por_qualquer_canal_sem_evento()
    {
        var (saida, fila) = NovaSaida();

        Assert.IsType<Veredito.Liberada>(saida.Liberar(TextoRotulado.Do("x", Enum.GetValues<TipoDeDado>()), Canal.Banco));
        foreach (var canal in Enum.GetValues<Canal>())
        {
            Assert.Equal("frase fixa", Assert.IsType<Veredito.Liberada>(saida.Liberar(TextoRotulado.Do("frase fixa"), canal)).Texto);
        }

        Assert.Equal(0, fila.Pendentes);
    }

    [Fact]
    public void Rotulo_atravessa_a_juncao_e_bloqueia_o_texto_composto()
    {
        var (saida, fila) = NovaSaida();
        var frase = TextoRotulado.Do("Bom dia, grupo! ").Juntar(TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome), "") +
            TextoRotulado.Do(" precisa de visita.");

        var veredito = Assert.IsType<Veredito.Bloqueada>(saida.Liberar(frase, Canal.GrupoDoBairro));

        Assert.Equal("Bom dia, grupo! [NOME] precisa de visita.", veredito.TextoMascarado);
        Assert.Equal(3, Assert.Single(fila.Drenar()).Severidade);
    }

    [Fact]
    public void Bloqueio_registra_evento_sem_o_valor()
    {
        var (saida, fila) = NovaSaida();
        var texto = new MotorDeParsing().Rotular("ele mora sozinho e toma diurético").Texto;

        var veredito = Assert.IsType<Veredito.Bloqueada>(saida.Liberar(texto, Canal.GrupoDoBairro));

        Assert.Equal("ele [MORA_SOZINHO] e toma [SAUDE]", veredito.TextoMascarado);
        var evento = Assert.Single(fila.Drenar());
        Assert.Equal(4, evento.Severidade);
        Assert.Equal(TipoDeEventoDeSeguranca.BloqueioDeSaida, evento.Tipo);
        Assert.Equal(Canal.GrupoDoBairro, evento.Canal);
        Assert.Equal([TipoDeDado.Saude, TipoDeDado.MoraSozinho], evento.TiposDeDado);
        Assert.Equal("Cadastro", evento.Modulo);
        Assert.Equal("/api/mensagens/verificar", evento.Rota);
        Assert.Equal(12, evento.Pontuacao);
        var gravado = JsonSerializer.Serialize(evento);
        Assert.DoesNotContain("sozinho", gravado);
        Assert.DoesNotContain("diur", gravado);
    }

    [Fact]
    public void Evento_recusa_lista_vazia_rota_com_identificador_e_anexa_a_mensagem_uma_vez()
    {
        Assert.Throws<ArgumentException>(() => EventoDeSeguranca.Registrar(
            TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.Log, [], "Cadastro", null, Fabrica.Agora));
        Assert.Throws<ArgumentException>(() => EventoDeSeguranca.Registrar(
            TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.Log, [(TipoDeDado.Nome, 3)], "Cadastro",
            "/api/idosos/6c1f0b7e-2d4a-4e8b-9f3c-1a5d7e9b0c2f/vizinho", Fabrica.Agora));

        var evento = EventoDeSeguranca.Registrar(
            TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.Log, [(TipoDeDado.Nome, 3), (TipoDeDado.Saude, 4)], "Cadastro",
            "/api/idosos/{id}/vizinho", Fabrica.Agora);

        Assert.Equal(4, evento.Severidade);
        Assert.True(evento.AnexarMensagem(new MensagemAoCidadao("primeira", OrigemDaMensagem.Llm)));
        Assert.False(evento.AnexarMensagem(new MensagemAoCidadao("segunda", OrigemDaMensagem.FraseFixa)));
        Assert.Equal("primeira", evento.Mensagem!.Texto);
    }

    [Fact]
    public void Explicacao_do_bloqueio_e_a_frase_do_exemplo()
    {
        Assert.Equal(
            "Essa mensagem diz que a pessoa mora sozinha e fala da saúde dela. No grupo do bairro, qualquer um lê.",
            FrasesFixas.Explicacao([TipoDeDado.Saude, TipoDeDado.MoraSozinho], Canal.GrupoDoBairro));
    }
}
