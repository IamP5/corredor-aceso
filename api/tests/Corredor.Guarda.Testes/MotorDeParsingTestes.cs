namespace Corredor.Guarda.Testes;

public sealed class MotorDeParsingTestes
{
    private readonly MotorDeParsing _motor = new();

    [Fact]
    public void Cpf_do_professor_com_digito_invalido_e_mascarado_pelo_formato()
    {
        var analise = _motor.Analisar("123.456.789-00");

        var achado = Assert.Single(analise.Achados);
        Assert.Equal(TipoDeDado.Cpf, achado.Tipo);
        Assert.Equal("***.456.***-00", achado.ValorMascarado);
        Assert.Equal(4, achado.Severidade);
        Assert.Equal("***.456.***-00", analise.TextoDeEntrada);
    }

    [Theory]
    [InlineData("{formatado}", "{mascara}")]
    [InlineData("{digitos}", "{mascara}")]
    [InlineData("meu CPF é {formatado}.", "meu CPF é {mascara}.")]
    [InlineData("(cpf:{digitos})", "(cpf:{mascara})")]
    public void Cpf_com_digito_valido_sobe_a_severidade_para_5(string modelo, string modeloDaMascara)
    {
        static string Preencher(string m) => m
            .Replace("{formatado}", CpfDeTeste.Formatado).Replace("{digitos}", CpfDeTeste.Digitos).Replace("{mascara}", CpfDeTeste.Mascara);
        var (cpf, mascara) = (Preencher(modelo), Preencher(modeloDaMascara));

        var analise = _motor.Analisar(cpf);

        Assert.Equal(5, Assert.Single(analise.Achados).Severidade);
        Assert.Equal(mascara, analise.TextoDeEntrada);
    }

    [Theory]
    [InlineData("rg 12.345.678-9", "rg **.***.***-9", TipoDeDado.Rg, 4)]
    [InlineData("cns 898 0010 0000 0001", "cns *** **** **** 0001", TipoDeDado.Cns, 5)]
    [InlineData("senha=abc123segredo", "[CREDENCIAL]", TipoDeDado.Credencial, 5)]
    [InlineData("Authorization: Bearer abcdefghijklmnop", "Authorization: [CREDENCIAL]", TipoDeDado.Credencial, 5)]
    public void Rg_cns_e_credencial_sao_mascarados_no_texto_de_entrada(string texto, string esperado, TipoDeDado tipo, int severidade)
    {
        var analise = _motor.Analisar(texto);

        var achado = Assert.Single(analise.Achados);
        Assert.Equal(tipo, achado.Tipo);
        Assert.Equal(severidade, achado.Severidade);
        Assert.Equal(esperado, analise.TextoDeEntrada);
        Assert.True(analise.MascarouNaEntrada);
    }

    [Theory]
    [InlineData("ele mora sozinho e toma diurético", "ele [MORA_SOZINHO] e toma [SAUDE]", new[] { TipoDeDado.MoraSozinho, TipoDeDado.Saude })]
    [InlineData("CID I10 com laudo de diabetes", "[SAUDE] com [SAUDE] de [SAUDE]", new[] { TipoDeDado.Saude })]
    [InlineData("toma losartana e insulina", "toma [SAUDE] e [SAUDE]", new[] { TipoDeDado.Saude })]
    [InlineData("Dona Aparecida mora na Rua das Flores, 12", "[NOME] mora na [ENDERECO]", new[] { TipoDeDado.Nome, TipoDeDado.Endereco })]
    [InlineData("ligue (00) 98888-0000", "ligue [TELEFONE]", new[] { TipoDeDado.Telefone })]
    public void Os_outros_tipos_so_rotulam_e_nao_mudam_o_texto_de_entrada(string texto, string mascarado, TipoDeDado[] tipos)
    {
        var analise = _motor.Analisar(texto);

        Assert.Equal(mascarado, analise.Texto.ToString());
        Assert.Equal(texto, analise.TextoDeEntrada);
        Assert.Equal(tipos.ToHashSet(), analise.Texto.Rotulos.ToHashSet());
        Assert.False(analise.MascarouNaEntrada);
    }

    [Theory]
    [InlineData("vizinha disse que foi à feira")]
    [InlineData("2026-11-18T10:40:00-03:00")]
    [InlineData("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b")]
    [InlineData("0b2d4f6a-8c1e-4a3b-9d5f-7e1c3a5b9d0f")]
    [InlineData("Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.")]
    public void Texto_sem_dado_pessoal_nao_tem_achado(string texto)
    {
        var analise = _motor.Analisar(texto);

        Assert.Empty(analise.Achados);
        Assert.Empty(analise.Texto.Rotulos);
        Assert.Equal(texto, analise.Texto.ToString());
    }

    [Fact]
    public void Mascarar_de_novo_nao_muda_nada()
    {
        var primeira = _motor.Analisar("documento 123.456.789-00, senha=abc123segredo").TextoDeEntrada;

        var segunda = _motor.Analisar(primeira);

        Assert.DoesNotContain(segunda.Achados, a => a.Tipo is TipoDeDado.Cpf);
        Assert.Equal(primeira, segunda.TextoDeEntrada);
    }

    [Fact]
    public void Json_mascara_cpf_rg_cns_e_credencial_em_qualquer_campo_e_deixa_nome_e_endereco_inteiros()
    {
        const string json = """
            {"documento":"123.456.789-00","nome":"Aparecida Souza","endereco":"Rua das Flores, 12",
             "senha":"segredo-123","apiKey":"k-1","vizinho":{"rg":"12.345.678-9","telefone":"(00) 98888-0000"},
             "lista":["cns 898 0010 0000 0001"],"casaToken":"3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b"}
            """;

        var analise = _motor.AnalisarJson(json);
        var saida = System.Text.Json.Nodes.JsonNode.Parse(analise.JsonDeEntrada)!;

        Assert.Equal("***.456.***-00", (string?)saida["documento"]);
        Assert.Equal("Aparecida Souza", (string?)saida["nome"]);
        Assert.Equal("Rua das Flores, 12", (string?)saida["endereco"]);
        Assert.Equal("[CREDENCIAL]", (string?)saida["senha"]);
        Assert.Equal("[CREDENCIAL]", (string?)saida["apiKey"]);
        Assert.Equal("**.***.***-9", (string?)saida["vizinho"]!["rg"]);
        Assert.Equal("(00) 98888-0000", (string?)saida["vizinho"]!["telefone"]);
        Assert.Equal("cns *** **** **** 0001", (string?)saida["lista"]![0]);
        Assert.Equal("3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", (string?)saida["casaToken"]);
        Assert.Contains(analise.Achados, a => a is { Tipo: TipoDeDado.Telefone, Campo: "vizinho.telefone" });
        Assert.DoesNotContain(analise.Achados, a => a.Campo == "casaToken");
    }

    [Fact]
    public void Json_sem_mascara_volta_identico()
    {
        const string json = """{ "casaToken": "3f9a1c0e5b7d4a2e8c6f1b0d9e7a5c3b", "desfecho": "EstavaFora" }""";

        Assert.Same(json, _motor.AnalisarJson(json).JsonDeEntrada);
    }

    [Fact]
    public void Achado_nunca_carrega_o_valor_em_claro()
    {
        var analise = _motor.AnalisarJson($$"""{"x":"{{CpfDeTeste.Formatado}} Dona Aparecida toma furosemida","senha":"segredo-123"}""");

        var tudo = string.Join('|', analise.Achados.Select(a => a.ToString()));
        Assert.DoesNotContain(CpfDeTeste.Formatado, tudo);
        Assert.DoesNotContain("Aparecida", tudo);
        Assert.DoesNotContain("furosemida", tudo);
        Assert.DoesNotContain("segredo-123", tudo);
    }

    [Fact]
    public void Rotular_une_o_rotulo_do_campo_aos_rotulos_do_conteudo()
    {
        IMotorDeParsing motor = _motor;

        var texto = motor.Rotular("ela mora sozinha", TipoDeDado.Saude).Texto;

        Assert.Equal(new HashSet<TipoDeDado> { TipoDeDado.Saude, TipoDeDado.MoraSozinho }, texto.Rotulos.ToHashSet());
        Assert.Equal("[SAUDE] [MORA_SOZINHO]", texto.ToString());
    }

    [Fact]
    public void Regras_vem_de_pii_json_com_padrao_mascara_e_severidade()
    {
        var regras = RegrasDePii.Padrao;

        Assert.Equal(Enum.GetValues<TipoDeDado>().ToHashSet(), regras.Regras.Select(r => r.Tipo).ToHashSet());
        Assert.All(regras.Regras, r => Assert.InRange(r.Severidade, 1, 5));
        Assert.Contains("senha", regras.ChavesDeCredencial);
        Assert.DoesNotContain("casaToken", regras.ChavesDeCredencial);
        Assert.Equal(4, regras.SeveridadeDe(TipoDeDado.Saude));
        Assert.Equal(3, regras.SeveridadeDe(TipoDeDado.Nome));
        Assert.Equal(5, regras.SeveridadeDe(TipoDeDado.Credencial));
    }
}
