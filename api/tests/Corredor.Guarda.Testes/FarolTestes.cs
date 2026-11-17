namespace Corredor.Guarda.Testes;

public sealed class FarolTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 18, 20, 0, 0, TimeSpan.Zero);

    private static EventoDeSeguranca Evento(TipoDeEventoDeSeguranca tipo, Canal canal, TipoDeDado dado, int severidade, TimeSpan atras) =>
        EventoDeSeguranca.Registrar(tipo, canal, [(dado, severidade)], "Cadastro", null, Agora - atras);

    [Fact]
    public void Sem_evento_e_verde()
    {
        Assert.Equal(CorDoFarol.Verde, Farol.Cor([]));
        Assert.Equal(0, Farol.Pontos([]));
    }

    [Fact]
    public void Mascaramento_na_entrada_vai_so_ate_amarelo_mesmo_com_severidade_5()
    {
        var contagem = Farol.Contar(
            [Evento(TipoDeEventoDeSeguranca.MascaramentoNaEntrada, Canal.Banco, TipoDeDado.Cpf, 5, TimeSpan.FromHours(1))],
            Agora);

        Assert.Equal(CorDoFarol.Amarelo, Farol.Cor(contagem));
    }

    [Fact]
    public void Bloqueio_de_severidade_3_e_amarelo()
    {
        var contagem = Farol.Contar(
            [Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.Log, TipoDeDado.Nome, 3, TimeSpan.FromMinutes(5))],
            Agora).ToList();

        Assert.Equal(CorDoFarol.Amarelo, Farol.Cor(contagem));
        Assert.Equal(6, Farol.Pontos(contagem));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void Bloqueio_de_severidade_4_ou_5_e_vermelho(int severidade)
    {
        var contagem = Farol.Contar(
            [Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.GrupoDoBairro, TipoDeDado.Saude, severidade, TimeSpan.FromMinutes(1))],
            Agora);

        Assert.Equal(CorDoFarol.Vermelho, Farol.Cor(contagem));
    }

    [Fact]
    public void Evento_com_mais_de_24_horas_nao_conta()
    {
        var contagem = Farol.Contar(
            [Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.GrupoDoBairro, TipoDeDado.Saude, 4, TimeSpan.FromHours(25))],
            Agora);

        Assert.Equal(CorDoFarol.Verde, Farol.Cor(contagem));
    }

    [Fact]
    public void Pontos_somam_severidade_vezes_exposicao()
    {
        var contagem = Farol.Contar(
        [
            Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.GrupoDoBairro, TipoDeDado.Saude, 4, TimeSpan.FromMinutes(1)),
            Evento(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.Log, TipoDeDado.Nome, 3, TimeSpan.FromMinutes(2)),
            Evento(TipoDeEventoDeSeguranca.MascaramentoNaEntrada, Canal.Banco, TipoDeDado.Cpf, 4, TimeSpan.FromMinutes(3)),
        ], Agora);

        Assert.Equal((4 * 3) + (3 * 2) + (4 * 1), Farol.Pontos(contagem));
    }
}
