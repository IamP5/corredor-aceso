using Corredor.Contratos;
using Corredor.Motor.Dominio;

namespace Corredor.Motor.Testes;

// Teste de propriedade: gera historias aleatorias e confere que nenhum caminho da Verde sem prova.
// Verde exige turno aberto, nenhum alerta, aparelho falando e sinal de vida aceito pelo perfil na janela atual.
// A janela e o sinal de vida sao recalculados aqui, direto da secao 5, sem usar o calendario do dominio.
public sealed class InvarianteDoVerdeTestes
{
    private const int Historias = 20_000;
    private static readonly DateOnly Dia = new(2026, 11, 18);
    private static readonly ParametrosDoMotor Parametros = ParametrosDoMotor.Producao;

    [Fact]
    public void VerdeSoComTurnoAbertoSemAlertaESinalDeVidaNaJanelaAtual()
    {
        var verdes = 0;
        for (var semente = 0; semente < Historias; semente++)
        {
            var h = Historia.Gerar(new Random(semente));
            var avaliacao = AvaliadorDeCasas.Padrao.Avaliar(h.Casa, h.Historico, h.Turno, h.Agora, Parametros);

            if (avaliacao.Alerta is { } alerta)
            {
                Assert.True(avaliacao.Situacao == alerta.Situacao, $"semente {semente}: situacao difere do alerta");
                Assert.True(avaliacao.Situacao is SituacaoDaCasa.Vermelho or SituacaoDaCasa.Cinza, $"semente {semente}");
                continue;
            }

            Assert.False(avaliacao.Atendida, $"semente {semente}: atendida sem alerta");
            if (avaliacao.Situacao != SituacaoDaCasa.Verde)
            {
                continue;
            }

            verdes++;
            Assert.True(h.TurnoAberto, $"semente {semente}: verde fora do turno");
            Assert.True(h.AparelhoFalando, $"semente {semente}: verde com aparelho mudo");
            Assert.True(h.SinalDeVidaDesde(h.InicioDaJanelaAtual), $"semente {semente}: verde sem sinal de vida na janela atual");
        }

        // A propriedade so prova algo se a geracao produz verdes de verdade.
        Assert.True(verdes > Historias / 50, $"so {verdes} verdes em {Historias} historias");
    }

    private sealed record Historia(
        CasaMonitorada Casa,
        HistoricoDaCasa Historico,
        TurnoRadar? Turno,
        DateTimeOffset Agora,
        IReadOnlyList<LeituraDeEvento> Eventos,
        IReadOnlyList<Confirmacao> Confirmacoes)
    {
        private static readonly TipoDePacote[] Tipos = [TipoDePacote.Movimento, TipoDePacote.BotaoBem, TipoDePacote.BotaoSair, TipoDePacote.Telemetria];

        public bool TurnoAberto =>
            Turno is { EncerradoEm: null } t && Agora >= t.AbertoEm && Agora < HorarioDeSaoPaulo.Em(Dia, Parametros.FimDoTurno);

        public bool AparelhoFalando => Casa.UltimoPacoteEm is { } u && Agora - u <= Parametros.SilencioMaximo;

        public DateTimeOffset InicioDaJanelaAtual =>
            new[] { Hora(0, 0), Hora(10, 0), Hora(15, 0), Turno?.AbertoEm ?? Hora(0, 0) }.Where(c => c <= Agora).Max();

        public bool SinalDeVidaDesde(DateTimeOffset inicio) =>
            Confirmacoes.Any(c => c.Resultado == ResultadoDaConfirmacao.PessoaVista && c.Quando >= inicio && c.Quando <= Agora)
            || (Casa.Perfil.Contato == PerfilDeContato.Sozinho
                && Eventos.Any(e => e.Tipo != TipoDePacote.Telemetria && e.OcorridoEm >= inicio && e.OcorridoEm <= Agora));

        public static Historia Gerar(Random r)
        {
            var token = CasaToken.Novo();
            var perfil = new PerfilDaCasa((PerfilDeContato)r.Next(3), r.Next(4) == 0);
            var casa = CasaMonitorada.Ativar(token, new MicroareaId(3), perfil, Hora(0, 0).AddHours(-r.Next(1, 48)));
            var agora = Hora(r.Next(0, 24), r.Next(0, 60));
            if (r.Next(10) > 0)
            {
                casa.RegistrarPacote(agora.AddSeconds(-r.Next(0, r.Next(4) > 0 ? 120 : 7200)));
            }

            TurnoRadar? turno = null;
            if (r.Next(4) > 0)
            {
                var previsao = new PrevisaoDoDia(new Probabilidade(0.8m), "previsao gerada", OrigemDoDiaCritico.Modelo);
                var dia = DiaCritico.Declarar(Dia, previsao, Hora(r.Next(0, 17), r.Next(0, 60)));
                if (r.Next(6) == 0)
                {
                    dia.Encerrar(new ResumoDoTurno(1, 0, 0, 0, 0), Hora(r.Next(12, 18), 0));
                }

                turno = dia.Turno(Parametros);
            }

            var eventos = Enumerable.Range(0, r.Next(0, 12)).Select(_ =>
            {
                var tipo = Tipos[r.Next(Tipos.Length)];
                // Metade dos eventos cai nas tres horas antes de agora, para a geracao tambem produzir verdes.
                var quando = r.Next(2) == 0 ? agora.AddMinutes(-r.Next(0, 180)) : Hora(0, 0).AddMinutes(r.Next(-600, 24 * 60));
                return tipo == TipoDePacote.Telemetria
                    ? new LeituraDeEvento(token, tipo, quando, TempC.Ler(r.Next(25, 40)), TensaoV.Ler(r.Next(3) == 0 ? 0 : 127))
                    : new LeituraDeEvento(token, tipo, quando, null, null);
            }).ToList();

            var confirmacoes = Enumerable.Range(0, r.Next(0, 4)).Select(_ =>
            {
                var origem = (OrigemDaConfirmacao)r.Next(2);
                var resultado = origem == OrigemDaConfirmacao.Vizinho ? ResultadoDaConfirmacao.PessoaVista : (ResultadoDaConfirmacao)r.Next(2);
                return new Confirmacao(Guid.NewGuid(), origem, resultado, Hora(0, 0).AddMinutes(r.Next(0, 24 * 60)));
            }).ToList();

            return new Historia(casa, HistoricoDaCasa.De(eventos, confirmacoes), turno, agora, eventos, confirmacoes);
        }

        private static DateTimeOffset Hora(int hora, int minuto) => HorarioDeSaoPaulo.Em(Dia, new TimeOnly(hora, minuto));
    }
}
