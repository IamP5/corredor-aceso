using Corredor.Contratos;
using Corredor.Motor.Aplicacao;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;
using static Corredor.Motor.Testes.Cenario;

namespace Corredor.Motor.Testes.Banco;

// IMotorDeCasas, dia critico, toque, encerramento, radar, linha do tempo e expurgo, contra o Postgres.
[Collection(ColecaoDoBanco.Nome)]
public sealed class CasosDeUsoTestes(PostgresDoMotor postgres)
{
    private static readonly MicroareaId Microarea = new(3);
    private static readonly PerfilDaCasa Sozinha = new(PerfilDeContato.Sozinho, false);
    private static readonly PerfilDaCasa Eletro = new(PerfilDeContato.Sozinho, true);
    private static readonly PrevisaoDoDia Previsao = new(new Probabilidade(0.81m), "minima de 24 graus pela segunda noite", OrigemDoDiaCritico.Modelo);

    [Fact]
    public async Task AtivarCasaDuasVezes_SoAtualizaOPerfil()
    {
        await using var motor = await postgres.NovoMotorAsync(H(9));
        var token = CasaToken.Novo();

        await Ativar(motor, token, Sozinha);
        motor.Relogio.Agora = H(10);
        await Ativar(motor, token, Eletro);

        var casa = await motor.Ler(bd => bd.Casas.SingleAsync(c => c.Token == token));
        Assert.Equal(Eletro, casa.Perfil);
        Assert.Equal(H(9), casa.AtivadaEm);
    }

    [Fact]
    public async Task CasaDesativada_RecusaAtivacaoPerfilEConfirmacao()
    {
        await using var motor = await postgres.NovoMotorAsync(H(9));
        var token = CasaToken.Novo();
        await Ativar(motor, token, Sozinha);
        await motor.Executar<IMotorDeCasas>(m => m.DesativarCasaAsync(token, CancellationToken.None));

        var ativar = await Assert.ThrowsAsync<RegraDeDominioViolada>(() => Ativar(motor, token, Sozinha));
        var perfil = await Assert.ThrowsAsync<RegraDeDominioViolada>(() =>
            motor.Executar<IMotorDeCasas>(m => m.DefinirPerfilAsync(token, Eletro, CancellationToken.None)));
        var confirmar = await Assert.ThrowsAsync<RegraDeDominioViolada>(() => Confirmar(motor, token, Visita(H(9))));

        Assert.All([ativar, perfil, confirmar], e => Assert.Equal("CASA_DESATIVADA", e.Codigo));
    }

    [Fact]
    public async Task CasaDesconhecida_RecusaPerfilEConfirmacao()
    {
        await using var motor = await postgres.NovoMotorAsync(H(9));

        var erro = await Assert.ThrowsAsync<RegraDeDominioViolada>(() =>
            motor.Executar<IMotorDeCasas>(m => m.DefinirPerfilAsync(CasaToken.Novo(), Eletro, CancellationToken.None)));

        Assert.Equal("CASA_DESCONHECIDA", erro.Codigo);
        Assert.Equal(TipoDeViolacao.Desconhecido, erro.Tipo);
    }

    [Fact]
    public async Task ConfirmacaoRepetida_EIgnoradaEForaDoPrazo_ERecusada()
    {
        await using var motor = await postgres.NovoMotorAsync(H(11));
        var token = CasaToken.Novo();
        await Ativar(motor, token, Sozinha);
        var visita = Visita(H(10, 40));

        await Confirmar(motor, token, visita);
        await Confirmar(motor, token, visita with { Quando = H(10, 50) });
        var futuro = await Assert.ThrowsAsync<RegraDeDominioViolada>(() => Confirmar(motor, token, Visita(H(11, 3))));
        var vencida = await Assert.ThrowsAsync<RegraDeDominioViolada>(() => Confirmar(motor, token, Visita(H(11).AddHours(-25))));

        var confirmacoes = await motor.Ler(bd => bd.Casas.AsNoTracking().Where(c => c.Token == token).Select(c => c.Confirmacoes).SingleAsync());
        Assert.Equal(H(10, 40), Assert.Single(confirmacoes).Quando);
        Assert.Equal("CONFIRMACAO_NO_FUTURO", futuro.Codigo);
        Assert.Equal("CONFIRMACAO_VENCIDA", vencida.Codigo);
    }

    [Fact]
    public async Task DesativarCasa_ApagaEventosEConfirmacoesERepetirDaOMesmoResultado()
    {
        await using var motor = await postgres.NovoMotorAsync(H(9));
        var token = CasaToken.Novo();
        await Ativar(motor, token, Sozinha);
        await Pacote(motor, token, 1, TipoDePacote.Movimento, H(8, 50));
        await Confirmar(motor, token, Visita(H(8, 55)));

        await motor.Executar<IMotorDeCasas>(m => m.DesativarCasaAsync(token, CancellationToken.None));
        await motor.Executar<IMotorDeCasas>(m => m.DesativarCasaAsync(token, CancellationToken.None));

        var casa = await motor.Ler(bd => bd.Casas.SingleAsync(c => c.Token == token));
        Assert.Equal(H(9), casa.DesativadaEm);
        Assert.Empty(casa.Confirmacoes);
        Assert.Equal(0, await motor.Ler(bd => bd.Eventos.CountAsync()));
        var linha = await Assert.ThrowsAsync<RegraDeDominioViolada>(() =>
            motor.Executar<ConsultarLinhaDoTempo, LinhaDoTempo>(c => c.ExecutarAsync(token, CancellationToken.None)));
        Assert.Equal("CASA_DESCONHECIDA", linha.Codigo);
    }

    [Fact]
    public async Task Caso29_DeclararDuasVezes_MantemUmTurnoComAberturaDas05h()
    {
        await using var motor = await postgres.NovoMotorAsync(H(5));
        await Ativar(motor, CasaToken.Novo(), Sozinha);

        await Declarar(motor);
        motor.Relogio.Agora = H(5, 1);
        var segunda = await Declarar(motor, new PrevisaoDoDia(new Probabilidade(0.9m), Previsao.Motivo, Previsao.Origem));

        Assert.Equal(H(5), segunda.AbertoEm);
        Assert.Equal(0.9m, segunda.Probabilidade.Valor);
        Assert.Equal(1, await motor.Ler(bd => bd.DiasCriticos.CountAsync()));
        Assert.Equal(2, motor.Publicador.Publicados.Count(p => p.Comando == ComandoDoNo.DiaCritico));
    }

    [Fact]
    public async Task DeclararOutraDataOuDepoisDoEncerramento_ERecusado()
    {
        await using var motor = await postgres.NovoMotorAsync(H(5));
        await Declarar(motor);
        motor.Relogio.Agora = H(17);
        await motor.Executar<EncerrarTurno, DiaCritico>(e => e.ExecutarAsync(Dia, CancellationToken.None));

        var encerrado = await Assert.ThrowsAsync<RegraDeDominioViolada>(() => Declarar(motor));
        var amanha = await Assert.ThrowsAsync<RegraDeDominioViolada>(() =>
            motor.Executar<DeclararDiaCritico, DiaCritico>(d => d.ExecutarAsync(Dia.AddDays(1), Previsao, CancellationToken.None)));

        Assert.Equal("TURNO_ENCERRADO", encerrado.Codigo);
        Assert.Equal(TipoDeViolacao.Conflito, encerrado.Tipo);
        Assert.Equal("DATA_NAO_E_HOJE", amanha.Codigo);
    }

    [Fact]
    public async Task Caso30_PedidoDeToqueSaiUmaVezSoParaASozinhoSemSinalDesdeAs10h()
    {
        await using var motor = await postgres.NovoMotorAsync(H(5));
        var semSinal = CasaToken.Novo();
        var comSinal = CasaToken.Novo();
        var coabitada = CasaToken.Novo();
        await Ativar(motor, semSinal, Sozinha);
        await Ativar(motor, comSinal, Sozinha);
        await Ativar(motor, coabitada, new PerfilDaCasa(PerfilDeContato.Coabitado, false));
        await Declarar(motor);
        motor.Relogio.Agora = H(14, 30);
        await Pacote(motor, comSinal, 1, TipoDePacote.Movimento, H(11));

        await RodarVigia(motor);
        motor.Relogio.Agora = H(14, 31);
        await RodarVigia(motor);

        var toques = motor.Publicador.Publicados.Where(p => p.Comando == ComandoDoNo.PedirToque).ToList();
        Assert.Equal(semSinal, Assert.Single(toques).Token);
        Assert.Equal(H(14, 30), await motor.Ler(bd => bd.DiasCriticos.Select(d => d.ToquePedidoEm).SingleAsync()));
    }

    [Fact]
    public async Task Vigia_PedeContatoDoVermelhoESoDoCinzaComTurnoAberto()
    {
        await using var motor = await postgres.NovoMotorAsync(H(5));
        var silenciosa = CasaToken.Novo();
        var muda = CasaToken.Novo();
        await Ativar(motor, silenciosa, Sozinha);
        await Ativar(motor, muda, Sozinha);
        await Declarar(motor);
        motor.Relogio.Agora = H(10, 1);
        await Pacote(motor, silenciosa, 1, TipoDePacote.Heartbeat, H(10, 1));

        await RodarVigia(motor);

        Assert.Contains(new PedidoDeContato(silenciosa, MotivoDoAlerta.SemMovimentoNaJanela, H(10)), motor.Receptor.Pedidos);
        Assert.Contains(motor.Receptor.Pedidos, p => p.Token == muda && p.Motivo == MotivoDoAlerta.SemHeartbeat);

        motor.Receptor.Pedidos.Clear();
        motor.Relogio.Agora = H(18, 30);
        await RodarVigia(motor);
        Assert.DoesNotContain(motor.Receptor.Pedidos, p => p.Token == muda);
    }

    [Fact]
    public async Task EncerramentoDas18h_GravaOResumoUmaVez()
    {
        await using var motor = await postgres.NovoMotorAsync(H(5));
        var comSinal = CasaToken.Novo();
        var semSinal = CasaToken.Novo();
        await Ativar(motor, comSinal, Sozinha);
        await Ativar(motor, semSinal, Sozinha);
        await Declarar(motor);
        motor.Relogio.Agora = H(9);
        await Pacote(motor, comSinal, 1, TipoDePacote.Movimento, H(9));

        motor.Relogio.Agora = H(18, 0, 5);
        await RodarVigia(motor);
        motor.Relogio.Agora = H(18, 30);
        var deNovo = await motor.Executar<EncerrarTurno, DiaCritico>(e => e.ExecutarAsync(Dia, CancellationToken.None));

        Assert.Equal(H(18, 0, 5), deNovo.EncerradoEm);
        Assert.Equal(new ResumoDoTurno(CasasAtivas: 2, ComSinalDeVidaAte10h: 1, Atendidas: 0, VermelhasPendentes: 0, CinzasPendentes: 2), deNovo.Resumo);
    }

    [Fact]
    public async Task EncerrarDiaQueNaoExiste_E404()
    {
        await using var motor = await postgres.NovoMotorAsync(H(9));

        var erro = await Assert.ThrowsAsync<RegraDeDominioViolada>(() =>
            motor.Executar<EncerrarTurno, DiaCritico>(e => e.ExecutarAsync(Dia, CancellationToken.None)));

        Assert.Equal("DIA_CRITICO_DESCONHECIDO", erro.Codigo);
    }

    [Fact]
    public async Task Radar_OrdenaVermelhoAntesDeCinzaDepoisRegraDepoisDesdeMaisAntigo()
    {
        await using var motor = await postgres.NovoMotorAsync(H(5));
        var cinza = CasaToken.Novo();
        var energia = CasaToken.Novo();
        var silencio = CasaToken.Novo();
        var verde = CasaToken.Novo();
        var atendida = CasaToken.Novo();
        var outraMicroarea = CasaToken.Novo();
        await Ativar(motor, cinza, Sozinha);
        await Ativar(motor, energia, Eletro);
        await Ativar(motor, silencio, Sozinha);
        await Ativar(motor, verde, Sozinha);
        await Ativar(motor, atendida, Sozinha);
        await motor.Executar<IMotorDeCasas>(m => m.AtivarCasaAsync(outraMicroarea, new MicroareaId(4), Sozinha, CancellationToken.None));
        await Declarar(motor);
        motor.Relogio.Agora = H(9, 50);
        await Pacote(motor, cinza, 1, TipoDePacote.Heartbeat, H(9, 50));
        motor.Relogio.Agora = H(10, 31);
        foreach (var token in new[] { energia, silencio, verde, atendida })
        {
            await Pacote(motor, token, 1, TipoDePacote.Heartbeat, H(10, 31));
        }

        await Pacote(motor, energia, 2, TipoDePacote.Telemetria, H(10, 20), tensaoV: 0m);
        await Pacote(motor, verde, 2, TipoDePacote.Movimento, H(10, 30));
        await Confirmar(motor, atendida, new Confirmacao(Guid.NewGuid(), OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.AusenciaConfirmada, H(10, 25)));

        var radar = await motor.Executar<ConsultarRadar, Radar>(r => r.ExecutarAsync(Microarea, CancellationToken.None));

        Assert.True(radar.TurnoAberto);
        Assert.Equal([energia, silencio, cinza], radar.Pendentes.Select(c => c.CasaToken));
        Assert.Equal([MotivoDoAlerta.EnergiaEletrodependente, MotivoDoAlerta.SemMovimentoNaJanela, MotivoDoAlerta.SemHeartbeat], radar.Pendentes.Select(c => c.Motivo));
        Assert.Equal(atendida, Assert.Single(radar.Atendidas).CasaToken);
        Assert.Equal(1, radar.Verdes);
        Assert.Empty(radar.Aguardando);
    }

    [Fact]
    public async Task LinhaDoTempo_TrazEventosEConfirmacoesDas24HorasEmOrdem()
    {
        await using var motor = await postgres.NovoMotorAsync(H(11));
        var token = CasaToken.Novo();
        await Ativar(motor, token, Sozinha);
        await Pacote(motor, token, 2, TipoDePacote.Movimento, H(10, 20));
        await Pacote(motor, token, 1, TipoDePacote.Movimento, H(9), bufferizado: true);
        await Pacote(motor, token, 3, TipoDePacote.Heartbeat, H(11));
        await Confirmar(motor, token, Visita(H(10, 40)));

        var linha = await motor.Executar<ConsultarLinhaDoTempo, LinhaDoTempo>(c => c.ExecutarAsync(token, CancellationToken.None));

        Assert.Equal([1L, 2L], linha.Eventos.Select(e => e.Seq));
        Assert.True(linha.Eventos[0].Bufferizado);
        Assert.Equal(H(11), linha.UltimoPacoteEm);
        Assert.Equal(H(10, 40), Assert.Single(linha.Confirmacoes).Quando);
    }

    [Fact]
    public async Task Caso31_ExpurgoGuardaALeituraBaixaSemConfirmacaoEACasaContinuaVermelha()
    {
        await using var motor = await postgres.NovoMotorAsync(H(2, 40));
        var eletro = CasaToken.Novo();
        var comum = CasaToken.Novo();
        await Ativar(motor, eletro, Eletro);
        await Ativar(motor, comum, Sozinha);
        await Pacote(motor, eletro, 1, TipoDePacote.Telemetria, H(2, 40), tensaoV: 0m);
        await Pacote(motor, comum, 1, TipoDePacote.Telemetria, H(2, 40), tensaoV: 0m);
        await Pacote(motor, comum, 2, TipoDePacote.Movimento, H(2, 41));
        await Confirmar(motor, comum, Visita(H(2, 42)));

        motor.Relogio.Agora = H(2, 40).AddHours(25);
        await Pacote(motor, eletro, 2, TipoDePacote.Telemetria, motor.Relogio.Agora.AddMinutes(-1), tensaoV: 127m);
        await motor.Executar<ExpurgarDados>(e => e.ExecutarAsync(CancellationToken.None));
        await motor.Executar<ExpurgarDados>(e => e.ExecutarAsync(CancellationToken.None));

        var restantes = await motor.Ler(bd => bd.Eventos.OrderBy(e => e.OcorridoEm).Select(e => new { e.CasaToken, e.Seq }).ToListAsync());
        Assert.Equal([new { CasaToken = eletro, Seq = 1L }, new { CasaToken = eletro, Seq = 2L }], restantes);
        Assert.Empty(await motor.Ler(bd => bd.Casas.AsNoTracking().Where(c => c.Token == comum).Select(c => c.Confirmacoes).SingleAsync()));

        var radar = await motor.Executar<ConsultarRadar, Radar>(r => r.ExecutarAsync(Microarea, CancellationToken.None));
        var card = Assert.Single(radar.Pendentes, c => c.CasaToken == eletro);
        Assert.Equal(SituacaoDaCasa.Vermelho, card.Situacao);
        Assert.Equal(H(2, 40), card.Desde);
    }

    [Fact]
    public async Task Expurgo_SoltaALeituraBaixaDepoisQueAAcsVeAPessoa()
    {
        await using var motor = await postgres.NovoMotorAsync(H(2, 40));
        var eletro = CasaToken.Novo();
        await Ativar(motor, eletro, Eletro);
        await Pacote(motor, eletro, 1, TipoDePacote.Telemetria, H(2, 40), tensaoV: 0m);
        motor.Relogio.Agora = H(2, 40).AddHours(23);
        await Confirmar(motor, eletro, Visita(motor.Relogio.Agora));

        motor.Relogio.Agora = H(2, 40).AddHours(25);
        await motor.Executar<ExpurgarDados>(e => e.ExecutarAsync(CancellationToken.None));

        Assert.Equal(0, await motor.Ler(bd => bd.Eventos.CountAsync()));
        Assert.Single(await motor.Ler(bd => bd.Casas.AsNoTracking().Where(c => c.Token == eletro).Select(c => c.Confirmacoes).SingleAsync()));
    }

    private static Confirmacao Visita(DateTimeOffset quando) =>
        new(Guid.NewGuid(), OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.PessoaVista, quando);

    private static Task Ativar(MotorDeTeste motor, CasaToken token, PerfilDaCasa perfil) =>
        motor.Executar<IMotorDeCasas>(m => m.AtivarCasaAsync(token, Microarea, perfil, CancellationToken.None));

    private static Task Confirmar(MotorDeTeste motor, CasaToken token, Confirmacao confirmacao) =>
        motor.Executar<IMotorDeCasas>(m => m.ConfirmarAsync(token, confirmacao, CancellationToken.None));

    private static Task<DiaCritico> Declarar(MotorDeTeste motor, PrevisaoDoDia? previsao = null) =>
        motor.Executar<DeclararDiaCritico, DiaCritico>(d => d.ExecutarAsync(Dia, previsao ?? Previsao, CancellationToken.None));

    private static Task RodarVigia(MotorDeTeste motor) =>
        motor.Executar<CicloDoVigia>(c => c.RodarAsync(CancellationToken.None));

    private static async Task Pacote(MotorDeTeste motor, CasaToken token, long seq, TipoDePacote tipo, DateTimeOffset ts, decimal? tensaoV = null, bool bufferizado = false)
    {
        var resultado = await motor.Executar<RegistrarPacote, ResultadoDoPacote>(r =>
            r.ExecutarAsync(new PacoteDoNo(token, seq, tipo, ts, null, tensaoV, bufferizado), CancellationToken.None));
        Assert.True(resultado is ResultadoDoPacote.Gravado or ResultadoDoPacote.SoUltimoPacote, $"pacote {seq}: {resultado}");
    }
}
