using Microsoft.EntityFrameworkCore;

namespace Corredor.Guarda.Testes;

public class TextoRotuladoTestes
{
    [Fact]
    public void ToString_de_texto_com_rotulo_devolve_marcadores_e_nunca_o_valor()
    {
        var nome = TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome);
        var observacao = TextoRotulado.Do("toma diurético", TipoDeDado.Saude, TipoDeDado.MoraSozinho);

        Assert.Equal("[NOME]", nome.ToString());
        Assert.Equal("[SAUDE] [MORA_SOZINHO]", observacao.ToString());
        Assert.DoesNotContain("Aparecida", $"{nome}");
    }

    [Fact]
    public void ToString_de_texto_sem_rotulo_devolve_o_texto()
    {
        Assert.Equal("bom dia", TextoRotulado.Do("bom dia").ToString());
    }

    [Fact]
    public void Juntar_une_os_rotulos_e_mascara_so_a_parte_rotulada()
    {
        var frase = TextoRotulado.Do("Visita a ").Juntar(TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome), "");
        var soma = TextoRotulado.Do("Rua das Flores, 12", TipoDeDado.Endereco) + TextoRotulado.Do("(00) 98888-0000", TipoDeDado.Telefone);

        Assert.Equal(new HashSet<TipoDeDado> { TipoDeDado.Nome }, frase.Rotulos);
        Assert.Equal("Visita a [NOME]", frase.ToString());
        Assert.Equal(new HashSet<TipoDeDado> { TipoDeDado.Endereco, TipoDeDado.Telefone }, soma.Rotulos);
        Assert.Equal("Visita a Aparecida Souza", ((Veredito.Liberada)Fabrica.Saida().Liberar(frase, Canal.Banco)).Texto);
    }

    [Fact]
    public void Conversor_grava_o_texto_e_le_com_os_rotulos_da_coluna()
    {
        var conversor = new ConversorDeTextoRotulado(TipoDeDado.Saude, TipoDeDado.Nome);

        var gravado = conversor.ConvertToProvider(TextoRotulado.Do("usa oxigênio", TipoDeDado.Saude));
        var lido = (TextoRotulado)conversor.ConvertFromProvider("usa oxigênio")!;

        Assert.Equal("usa oxigênio", gravado);
        Assert.Equal(new HashSet<TipoDeDado> { TipoDeDado.Saude, TipoDeDado.Nome }, lido.Rotulos);
        Assert.Equal("usa oxigênio", ((Veredito.Liberada)Fabrica.Saida().Liberar(lido, Canal.Banco)).Texto);
    }

    [Fact]
    public void Conversor_mapeia_propriedade_obrigatoria_e_anulavel()
    {
        using var bd = new BdDeTeste();

        var entidade = bd.Model.FindEntityType(typeof(Ficha))!;

        Assert.IsType<ConversorDeTextoRotulado>(entidade.FindProperty(nameof(Ficha.Nome))!.GetValueConverter());
        Assert.IsType<ConversorDeTextoRotulado>(entidade.FindProperty(nameof(Ficha.Condicoes))!.GetValueConverter());
    }

    private sealed class Ficha
    {
        public int Id { get; set; }
        public TextoRotulado Nome { get; set; } = null!;
        public TextoRotulado? Condicoes { get; set; }
    }

    private sealed class BdDeTeste : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder opcoes) => opcoes.UseNpgsql("Host=127.0.0.1");

        protected override void OnModelCreating(ModelBuilder modelo) => modelo.Entity<Ficha>(e =>
        {
            e.Property(f => f.Nome).HasConversion(ConversorDeTextoRotulado.Rotulado(TipoDeDado.Nome));
            e.Property(f => f.Condicoes).HasConversion(ConversorDeTextoRotulado.Rotulado(TipoDeDado.Saude));
        });
    }
}

public class SaidaSeguraTestes
{
    private readonly SaidaSegura _saida = Fabrica.Saida();

    [Theory]
    [InlineData(Canal.GrupoDoBairro)]
    [InlineData(Canal.PromptDoLlm)]
    [InlineData(Canal.Mqtt)]
    [InlineData(Canal.MensagemDeErro)]
    [InlineData(Canal.RespostaSemAutenticacao)]
    [InlineData(Canal.Vizinho)]
    [InlineData(Canal.Log)]
    public void Canal_aberto_bloqueia_qualquer_rotulo(Canal canal)
    {
        var veredito = _saida.Liberar(TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome), canal);

        var bloqueada = Assert.IsType<Veredito.Bloqueada>(veredito);
        Assert.Equal(new HashSet<TipoDeDado> { TipoDeDado.Nome }, bloqueada.TiposDeDado);
        Assert.Equal("[NOME]", bloqueada.TextoMascarado);
    }

    [Fact]
    public void App_autenticado_aceita_nome_endereco_e_telefone_e_recusa_saude()
    {
        var lista = TextoRotulado.Do("Aparecida Souza", TipoDeDado.Nome) + TextoRotulado.Do(", Rua das Flores", TipoDeDado.Endereco);
        var saude = TextoRotulado.Do("diurético", TipoDeDado.Saude, TipoDeDado.Nome);

        Assert.Equal("Aparecida Souza, Rua das Flores", Assert.IsType<Veredito.Liberada>(_saida.Liberar(lista, Canal.AppAutenticado)).Texto);
        Assert.Equal(new HashSet<TipoDeDado> { TipoDeDado.Saude },
            Assert.IsType<Veredito.Bloqueada>(_saida.Liberar(saude, Canal.AppAutenticado)).TiposDeDado);
    }

    [Fact]
    public void Texto_sem_rotulo_sai_em_qualquer_canal()
    {
        Assert.IsType<Veredito.Liberada>(_saida.Liberar(TextoRotulado.Do("Preciso de ajuda para uma visita."), Canal.GrupoDoBairro));
    }

    [Fact]
    public void Motor_de_parsing_faz_o_texto_livre_ser_bloqueado_no_grupo_do_bairro()
    {
        var analisado = new MotorDeParsing().Rotular("ele mora sozinho e toma diurético", TipoDeDado.Saude);

        Assert.Contains(TipoDeDado.MoraSozinho, analisado.Texto.Rotulos);
        Assert.IsType<Veredito.Bloqueada>(_saida.Liberar(analisado.Texto, Canal.GrupoDoBairro));
    }
}
