using Corredor.Contratos;
using Corredor.Motor.Dominio;
using static Corredor.Motor.Testes.Cenario;
using static Corredor.Motor.Testes.CasosDoAvaliadorTestes;

namespace Corredor.Motor.Testes;

// Secoes 5.6, 5.7 e 5.8 do modelo de dominio, linha por linha.
public sealed class TabelasVerdadeTestes
{
    // 5.6, linha 1: R1 decide com turno aberto ou fechado, mesmo com R2, R3 e R4 tambem valendo.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Situacao_Linha1_EnergiaVenceTodasAsOutras(bool turnoAberto)
    {
        var casa = Eletrodependente().TelemetriaDe(H(8), H(10, 30), tempC: 36m, tensaoV: 0m).Mudo(H(9));
        var agora = turnoAberto ? H(10, 30) : H(18, 30);

        Esperar(casa.Em(agora), SituacaoDaCasa.Vermelho, MotivoDoAlerta.EnergiaEletrodependente, H(8), atendida: false);
    }

    // 5.6, linha 2: sem R1, o aparelho mudo fica cinza, com turno aberto ou fechado, mesmo com calor e sem movimento.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Situacao_Linha2_SemHeartbeatVemAntesDoCalorEDoSilencio(bool turnoAberto)
    {
        var casa = Sozinho().TelemetriaDe(H(6), H(9), tempC: 36m).Mudo(H(9));
        var agora = turnoAberto ? H(10, 30) : H(18, 30);

        Esperar(casa.Em(agora), SituacaoDaCasa.Cinza, MotivoDoAlerta.SemHeartbeat, H(9), atendida: false);
    }

    // 5.6, linha 3: com turno aberto, o calor vem antes do silencio na janela.
    [Fact]
    public void Situacao_Linha3_CalorVenceASemMovimento() =>
        Esperar(Sozinho().TelemetriaDe(H(6), H(10, 30), tempC: 34m).Em(H(10, 30)),
            SituacaoDaCasa.Vermelho, MotivoDoAlerta.CalorInterno, H(8), atendida: false);

    // 5.6, linha 4: sem R1, R2 e R3, a janela vencida sem sinal de vida da vermelho. Nunca ha sinal na janela atual.
    [Fact]
    public void Situacao_Linha4_SemMovimentoNaJanela()
    {
        var avaliacao = Sozinho().Movimento(H(9, 59)).Em(H(15));

        Esperar(avaliacao, SituacaoDaCasa.Vermelho, MotivoDoAlerta.SemMovimentoNaJanela, H(15), atendida: false);
    }

    // 5.6, linha 5: o unico caminho para Verde.
    [Fact]
    public void Situacao_Linha5_Verde() =>
        Assert.Equal(SituacaoDaCasa.Verde, Sozinho().Movimento(H(11)).Em(H(11, 1)).Situacao);

    // 5.6, linha 6: turno aberto, nenhuma regra, sem sinal na janela atual.
    [Theory]
    [InlineData(PerfilDeContato.Sozinho)]
    [InlineData(PerfilDeContato.Acamado)]
    [InlineData(PerfilDeContato.Coabitado)]
    public void Situacao_Linha6_Aguardando(PerfilDeContato contato)
    {
        var (casa, agora) = contato switch
        {
            PerfilDeContato.Sozinho => (Sozinho().Movimento(H(9)), H(11)),
            PerfilDeContato.Acamado => (Acamado().Visita(H(11), Desfecho.EstavaBem), H(15, 1)),
            _ => (Coabitado().Movimento(H(9)), H(11)),
        };

        Assert.Equal(SituacaoDaCasa.Aguardando, casa.Em(agora).Situacao);
    }

    // 5.6, linha 7: fora do turno, R3 e R4 nao se aplicam e nao existe Verde.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Situacao_Linha7_ForaDoTurnoSoAguarda(bool diaComum)
    {
        var casa = Sozinho().TelemetriaDe(H(13), H(18, 30), tempC: 40m).Movimento(H(18, 29));
        casa = diaComum ? casa.DiaComum() : casa;

        var avaliacao = casa.Em(diaComum ? H(16) : H(18, 30));

        Assert.Equal(SituacaoDaCasa.Aguardando, avaliacao.Situacao);
        Assert.Null(avaliacao.Alerta);
    }

    public static TheoryData<string, OrigemDaConfirmacao, ResultadoDaConfirmacao, bool> LinhasDoAtendimento => new()
    {
        { "EnergiaEletrodependente", OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.PessoaVista, true },
        { "EnergiaEletrodependente", OrigemDaConfirmacao.Vizinho, ResultadoDaConfirmacao.PessoaVista, false },
        { "EnergiaEletrodependente", OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.AusenciaConfirmada, false },
        { "SemHeartbeat", OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.PessoaVista, true },
        { "SemHeartbeat", OrigemDaConfirmacao.Vizinho, ResultadoDaConfirmacao.PessoaVista, true },
        { "SemHeartbeat", OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.AusenciaConfirmada, false },
        { "CalorInterno", OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.PessoaVista, true },
        { "CalorInterno", OrigemDaConfirmacao.Vizinho, ResultadoDaConfirmacao.PessoaVista, true },
        { "CalorInterno", OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.AusenciaConfirmada, false },
        { "SemMovimentoNaJanela", OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.AusenciaConfirmada, true },
    };

    // 5.7: a confirmacao chega as 11h, depois do Desde e dentro da janela atual. Cada regra aceita so a sua coluna.
    [Theory]
    [MemberData(nameof(LinhasDoAtendimento))]
    public void Atendimento_CadaRegraAceitaSoSuaColuna(string motivo, OrigemDaConfirmacao origem, ResultadoDaConfirmacao resultado, bool atendida)
    {
        var alvo = Enum.Parse<MotivoDoAlerta>(motivo);
        var casa = alvo switch
        {
            MotivoDoAlerta.EnergiaEletrodependente => Eletrodependente().TelemetriaDe(H(10, 30), H(11, 30), tensaoV: 0m),
            MotivoDoAlerta.SemHeartbeat => Sozinho().Mudo(H(10, 30)),
            MotivoDoAlerta.CalorInterno => Sozinho().TelemetriaDe(H(8), H(11, 30), tempC: 35m),
            _ => Sozinho(),
        };

        var avaliacao = casa.Confirmacao(origem, resultado, H(11)).Em(H(11, 1));

        Assert.Equal(alvo, avaliacao.Alerta?.Motivo);
        Assert.Equal(atendida, avaliacao.Atendida);
    }

    // 5.7, ultima linha: PessoaVista na casa em silencio e sinal de vida, entao a regra deixa de devolver alerta.
    [Theory]
    [InlineData(OrigemDaConfirmacao.Acs)]
    [InlineData(OrigemDaConfirmacao.Vizinho)]
    public void Atendimento_PessoaVistaDesfazOSilencio(OrigemDaConfirmacao origem)
    {
        var avaliacao = Sozinho().Confirmacao(origem, ResultadoDaConfirmacao.PessoaVista, H(11)).Em(H(11, 1));

        Assert.Null(avaliacao.Alerta);
        Assert.Equal(SituacaoDaCasa.Verde, avaliacao.Situacao);
    }

    // 5.7: confirmacao antes do Desde, ou antes do inicio da janela atual, deixa a casa pendente.
    [Fact]
    public void Atendimento_ConfirmacaoForaDoLimiteNaoConta()
    {
        var antesDoDesde = Sozinho().Mudo(H(10, 30)).Visita(H(10, 20), Desfecho.EstavaBem).Em(H(11, 1));
        var janelaAnterior = Sozinho().Mudo(H(9, 30)).Visita(H(9, 40), Desfecho.EstavaBem).Em(H(10, 1));

        Assert.True(antesDoDesde.Pendente);
        Assert.True(janelaAnterior.Pendente);
    }

    // 5.8, casa Sozinho com turno aberto as 05h.
    [Theory]
    [InlineData(8, 0, null, null, SituacaoDaCasa.Aguardando, null, null)]
    [InlineData(8, 0, 6, 12, SituacaoDaCasa.Verde, null, null)]
    [InlineData(10, 0, null, null, SituacaoDaCasa.Vermelho, 10, 0)]
    [InlineData(10, 1, 9, 50, SituacaoDaCasa.Aguardando, null, null)]
    [InlineData(10, 31, 10, 30, SituacaoDaCasa.Verde, null, null)]
    [InlineData(12, 0, 6, 12, SituacaoDaCasa.Aguardando, null, null)]
    [InlineData(15, 0, 6, 12, SituacaoDaCasa.Vermelho, 15, 0)]
    [InlineData(15, 1, 10, 30, SituacaoDaCasa.Aguardando, null, null)]
    [InlineData(15, 21, 15, 20, SituacaoDaCasa.Verde, null, null)]
    [InlineData(18, 30, 15, 20, SituacaoDaCasa.Aguardando, null, null)]
    public void Exemplos_Sozinho(int hora, int minuto, int? horaDoSinal, int? minutoDoSinal, SituacaoDaCasa esperada, int? horaDesde, int? minutoDesde)
    {
        var casa = Sozinho();
        if (horaDoSinal is { } h)
        {
            casa.Movimento(H(h, minutoDoSinal!.Value));
        }

        var avaliacao = casa.Em(H(hora, minuto));

        Assert.Equal(esperada, avaliacao.Situacao);
        Assert.Equal(horaDesde is { } hd ? H(hd, minutoDesde!.Value) : null, avaliacao.Alerta?.Desde);
    }

    // 5.8, casa Acamado com turno aberto as 05h.
    [Theory]
    [InlineData(5, 1, null, SituacaoDaCasa.Vermelho, 5)]
    [InlineData(9, 1, 9, SituacaoDaCasa.Verde, null)]
    [InlineData(10, 0, 9, SituacaoDaCasa.Vermelho, 10)]
    [InlineData(11, 1, 11, SituacaoDaCasa.Verde, null)]
    [InlineData(15, 1, 11, SituacaoDaCasa.Aguardando, null)]
    public void Exemplos_Acamado(int hora, int minuto, int? horaDaVisita, SituacaoDaCasa esperada, int? horaDesde)
    {
        var casa = Acamado();
        if (horaDaVisita is { } h)
        {
            casa.Visita(H(h), Desfecho.EstavaBem);
        }

        var avaliacao = casa.Em(H(hora, minuto));

        Assert.Equal(esperada, avaliacao.Situacao);
        Assert.Equal(horaDesde is { } hd ? H(hd) : null, avaliacao.Alerta?.Desde);
    }
}
