using Corredor.Contratos;
using Corredor.Motor.Dominio;
using static Corredor.Motor.Testes.Cenario;

namespace Corredor.Motor.Testes;

// Casos 1 a 25 da secao 7 do modelo de dominio. Todos em 18/11, turno das 05h as 18h, salvo "dia comum".
public sealed class CasosDoAvaliadorTestes
{
    [Fact]
    public void Caso01_SozinhoSemSinalDeVidaComAparelhoLigado_AguardaENuncaFicaVerde()
    {
        var avaliacao = Sozinho().Em(H(9));

        Assert.Equal(SituacaoDaCasa.Aguardando, avaliacao.Situacao);
        Assert.Null(avaliacao.Alerta);
    }

    [Fact]
    public void Caso02_SozinhoComMovimentoAs07h_FicaVerdeAs09h() =>
        Assert.Equal(SituacaoDaCasa.Verde, Sozinho().Movimento(H(7)).Em(H(9)).Situacao);

    [Fact]
    public void Caso03_SozinhoSemSinalDeVida_FicaVermelhoPendenteDesdeAs10h() =>
        Esperar(Sozinho().Em(H(10)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(10), atendida: false);

    [Fact]
    public void Caso04_SozinhoComUmMovimentoAs10h30_FicaVerdeAs10h31() =>
        Assert.Equal(SituacaoDaCasa.Verde, Sozinho().Movimento(H(10, 30)).Em(H(10, 31)).Situacao);

    [Fact]
    public void Caso05_MovimentoAntesDaAberturaNaoVale() =>
        Esperar(Sozinho().Movimento(H(3, 12)).Em(H(10)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(10), atendida: false);

    [Fact]
    public void Caso06_JanelaDaTardePedeSinalNovoEAindaNaoVenceu() =>
        Assert.Equal(SituacaoDaCasa.Aguardando, Sozinho().Movimento(H(7)).Em(H(12, 30)).Situacao);

    [Fact]
    public void Caso07_SemSinalNaTarde_FicaVermelhoDesdeAs15h() =>
        Esperar(Sozinho().Movimento(H(7)).Em(H(15, 30)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(15), atendida: false);

    [Fact]
    public void Caso08_VisitaEstavaBemAs10h12_FicaVerdeAs10h15() =>
        Assert.Equal(SituacaoDaCasa.Verde, Sozinho().Visita(H(10, 12), Desfecho.EstavaBem).Em(H(10, 15)).Situacao);

    [Fact]
    public void Caso09_VisitaDas09hValeSoNaJanelaDaManha()
    {
        var casa = Sozinho().Visita(H(9), Desfecho.EstavaBem);

        Assert.Equal(SituacaoDaCasa.Verde, casa.Em(H(9, 30)).Situacao);
        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(H(10)).Situacao);
        Esperar(casa.Em(H(15)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(15), atendida: false);
    }

    [Fact]
    public void Caso10_AparelhoMudoDesdeAs09h10_FicaCinzaPendenteENaoVerde() =>
        Esperar(Caso10().Em(H(10)), SituacaoDaCasa.Cinza, MotivoDoAlerta.SemHeartbeat, H(9, 10), atendida: false);

    [Fact]
    public void Caso11_VisitaAtendeOCinzaSoNaJanelaAtual()
    {
        var casa = Caso10().Visita(H(10, 30), Desfecho.EstavaBem);

        Esperar(casa.Em(H(10, 31)), SituacaoDaCasa.Cinza, MotivoDoAlerta.SemHeartbeat, H(9, 10), atendida: true);
        Esperar(casa.Em(H(15)), SituacaoDaCasa.Cinza, MotivoDoAlerta.SemHeartbeat, H(9, 10), atendida: false);
    }

    [Fact]
    public void Caso12_VisitaAntesDeOAparelhoCalarNaoAtende() =>
        Esperar(Caso10().Visita(H(9, 5), Desfecho.EstavaBem).Em(H(10)), SituacaoDaCasa.Cinza, MotivoDoAlerta.SemHeartbeat, H(9, 10), atendida: false);

    [Fact]
    public void Caso13_MovimentoBufferizadoContaPeloOcorridoEm()
    {
        var voltaComMovimentoDas10h20 = Sozinho().Movimento(H(9)).Movimento(H(10, 20)).Mudo(H(11));
        var voltaComMovimentoDas09h50 = Sozinho().Movimento(H(9)).Movimento(H(9, 50)).Mudo(H(11));

        Assert.Equal(SituacaoDaCasa.Verde, voltaComMovimentoDas10h20.Em(H(11)).Situacao);
        Assert.Equal(SituacaoDaCasa.Aguardando, voltaComMovimentoDas09h50.Em(H(11)).Situacao);
    }

    [Fact]
    public void Caso14_AparelhoMudoVoltaSemMovimento_FicaVermelhoDesdeAs10h() =>
        Esperar(Sozinho().Mudo(H(11)).Em(H(11)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(10), atendida: false);

    [Fact]
    public void Caso15_CasaQueNuncaMandouPacote_FicaCinzaDesdeAAtivacao() =>
        Esperar(Sozinho().NuncaMandouPacote().Em(H(9)), SituacaoDaCasa.Cinza, MotivoDoAlerta.SemHeartbeat, AtivadaEm, atendida: false);

    [Fact]
    public void Caso16_AcamadoSoSaiDoVermelhoComPessoaVistaEVoltaNaJanelaSeguinte()
    {
        var casa = Acamado().Movimento(H(5)).Visita(H(9), Desfecho.EstavaBem);

        Esperar(Acamado().Movimento(H(5)).Em(H(5, 1)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(5), atendida: false);
        Assert.Equal(SituacaoDaCasa.Verde, casa.Em(H(9, 1)).Situacao);
        Esperar(casa.Em(H(10)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(10), atendida: false);
    }

    [Fact]
    public void Caso17_SaidaAvisadaSuspendeARegraDoSilencioNoTurno()
    {
        var casa = Sozinho().SaidaAvisada(H(8, 40));

        Assert.Equal(SituacaoDaCasa.Verde, casa.Em(H(9)).Situacao);
        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(H(10)).Situacao);
        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(H(16)).Situacao);
        for (var agora = H(8, 40); agora < H(18); agora = agora.AddMinutes(10))
        {
            Assert.NotEqual(SituacaoDaCasa.Vermelho, casa.Em(agora).Situacao);
        }
    }

    [Fact]
    public void Caso18_CalorInternoDesdeAs13hAtendidoPelaVisitaSoNaJanelaAtual()
    {
        var casa = Sozinho().TelemetriaDe(H(11), H(15, 10), tempC: 35m).Movimento(H(13)).Visita(H(13, 30), Desfecho.EstavaBem);

        Esperar(casa.Em(H(13, 5)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.CalorInterno, H(13), atendida: false);
        Esperar(casa.Em(H(13, 31)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.CalorInterno, H(13), atendida: true);
        Esperar(casa.Em(H(15)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.CalorInterno, H(13), atendida: false);
    }

    [Fact]
    public void Caso19_EnergiaComTravaSoSaiComAcsVendoAPessoaDepoisDaUltimaLeituraBaixa()
    {
        var casa = Eletrodependente().DiaComum()
            .TelemetriaDe(H(14, 20), H(15, 5), tensaoV: 0m)
            .Telemetria(H(15, 10), tensaoV: 127m)
            .VizinhoRespondeEstaBem(H(14, 30))
            .Visita(H(14, 40), Desfecho.EstavaBem)
            .Visita(H(15, 30), Desfecho.EstavaBem);

        Esperar(casa.Em(H(14, 21)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.EnergiaEletrodependente, H(14, 20), atendida: false);
        Esperar(casa.Em(H(14, 31)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.EnergiaEletrodependente, H(14, 20), atendida: false);
        Esperar(casa.Em(H(14, 41)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.EnergiaEletrodependente, H(14, 20), atendida: true);
        Esperar(casa.Em(H(15)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.EnergiaEletrodependente, H(14, 20), atendida: false);
        Esperar(casa.Em(H(15, 11)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.EnergiaEletrodependente, H(14, 40), atendida: false);
        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(H(15, 31)).Situacao);
    }

    [Fact]
    public void Caso20_EletrodependenteFicaVermelhoCom61SegundosSemPacote()
    {
        var casa = Eletrodependente().DiaComum().Mudo(H(14));

        Esperar(casa.Em(H(14, 1, 1)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.EnergiaEletrodependente, H(14), atendida: false);
        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(H(14, 0, 59)).Situacao);
    }

    [Fact]
    public void Caso21_VisitaNaoAbriuNaoChamaOMotorEACasaContinuaPendente() =>
        Esperar(Sozinho().Visita(H(10, 40), Desfecho.NaoAbriu).Em(H(10, 41)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(10), atendida: false);

    [Fact]
    public void Caso22_EstavaForaAtendeOSilencioSoNaJanelaAtualENuncaFicaVerde()
    {
        var casa = Sozinho().Visita(H(10, 40), Desfecho.EstavaFora);

        Esperar(casa.Em(H(10, 41)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(10), atendida: true);
        Esperar(casa.Em(H(15)), SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(15), atendida: false);
        for (var agora = H(10, 40); agora < H(18); agora = agora.AddMinutes(10))
        {
            Assert.NotEqual(SituacaoDaCasa.Verde, casa.Em(agora).Situacao);
        }
    }

    [Fact]
    public void Caso23_VizinhoEstaBemAtendeOCinza() =>
        Esperar(Caso10().VizinhoRespondeEstaBem(H(10, 30)).Em(H(10, 31)), SituacaoDaCasa.Cinza, MotivoDoAlerta.SemHeartbeat, H(9, 10), atendida: true);

    [Fact]
    public void Caso24_CoabitadoNaoContaEventoDoSensor()
    {
        var casa = Coabitado().Movimento(H(7)).BotaoBem(H(8));

        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(H(9)).Situacao);
        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(H(10)).Situacao);
    }

    [Fact]
    public void Caso25_CoabitadoFicaVerdeComVisitaEstavaBem() =>
        Assert.Equal(SituacaoDaCasa.Verde, Coabitado().Visita(H(10, 30), Desfecho.EstavaBem).Em(H(10, 31)).Situacao);

    private static Cenario Caso10() => Sozinho().Movimento(H(9)).Mudo(H(9, 10));

    internal static void Esperar(Avaliacao avaliacao, SituacaoDaCasa situacao, MotivoDoAlerta motivo, DateTimeOffset desde, bool atendida)
    {
        Assert.Equal(situacao, avaliacao.Situacao);
        Assert.NotNull(avaliacao.Alerta);
        Assert.Equal(motivo, avaliacao.Alerta.Motivo);
        Assert.Equal(desde, avaliacao.Alerta.Desde);
        Assert.Equal(atendida, avaliacao.Atendida);
        Assert.Equal(!atendida, avaliacao.Pendente);
    }
}
