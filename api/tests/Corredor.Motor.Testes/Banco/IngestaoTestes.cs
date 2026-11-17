using Corredor.Contratos;
using Corredor.Motor.Aplicacao;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;
using static Corredor.Motor.Testes.Cenario;

namespace Corredor.Motor.Testes.Banco;

// Casos 26, 27 e 28 e as invariantes da ingestao, contra o Postgres e o SQL que o consumidor MQTT chama.
[Collection(ColecaoDoBanco.Nome)]
public sealed class IngestaoTestes(PostgresDoMotor postgres)
{
    private static readonly PerfilDaCasa Sozinha = new(PerfilDeContato.Sozinho, false);

    [Fact]
    public async Task Caso26_MesmoPacoteRecebidoDuasVezes_GravaUmEvento()
    {
        await using var motor = await MotorComCasa(out var token);
        var pacote = Pacote(token, 1842, TipoDePacote.Movimento, H(8, 12, 40));

        var primeiro = await Registrar(motor, pacote);
        var segundo = await Registrar(motor, pacote);

        Assert.Equal(ResultadoDoPacote.Gravado, primeiro);
        Assert.Equal(ResultadoDoPacote.Repetido, segundo);
        Assert.Equal(1, await motor.Ler(bd => bd.Eventos.CountAsync(e => e.CasaToken == token)));
    }

    [Fact]
    public async Task Caso27_NoReiniciaEMandaSeq1ComTsNovo_EventoAceito()
    {
        await using var motor = await MotorComCasa(out var token);

        await Registrar(motor, Pacote(token, 1, TipoDePacote.Movimento, H(8)));
        var depoisDoReinicio = await Registrar(motor, Pacote(token, 1, TipoDePacote.Movimento, H(9)));

        Assert.Equal(ResultadoDoPacote.Gravado, depoisDoReinicio);
        Assert.Equal(2, await motor.Ler(bd => bd.Eventos.CountAsync(e => e.CasaToken == token)));
    }

    [Fact]
    public async Task Caso28_TsTresMinutosAFrente_RecusaENaoMexeNoUltimoPacote()
    {
        await using var motor = await MotorComCasa(out var token);
        await Registrar(motor, Pacote(token, 1, TipoDePacote.Heartbeat, H(9)));

        var adiantado = await Registrar(motor, Pacote(token, 2, TipoDePacote.Movimento, H(9, 3)));

        Assert.Equal(ResultadoDoPacote.ForaDoPrazo, adiantado);
        Assert.Equal(H(9), await UltimoPacote(motor, token));
        Assert.Equal(0, await motor.Ler(bd => bd.Eventos.CountAsync(e => e.CasaToken == token)));
    }

    [Fact]
    public async Task PacoteComMaisDe24Horas_ERecusado()
    {
        await using var motor = await MotorComCasa(out var token);

        var velho = await Registrar(motor, Pacote(token, 1, TipoDePacote.Movimento, H(9).AddHours(-25)));

        Assert.Equal(ResultadoDoPacote.ForaDoPrazo, velho);
        Assert.Null(await UltimoPacote(motor, token));
    }

    [Fact]
    public async Task Heartbeat_SoAvancaOUltimoPacoteComAHoraDoServidor()
    {
        await using var motor = await MotorComCasa(out var token);
        motor.Relogio.Agora = H(9, 0, 30);

        var resultado = await Registrar(motor, Pacote(token, 7, TipoDePacote.Heartbeat, H(9)));

        Assert.Equal(ResultadoDoPacote.SoUltimoPacote, resultado);
        Assert.Equal(H(9, 0, 30), await UltimoPacote(motor, token));
        Assert.Equal(0, await motor.Ler(bd => bd.Eventos.CountAsync(e => e.CasaToken == token)));
    }

    [Fact]
    public async Task UltimoPacote_SoAndaParaFrente()
    {
        await using var motor = await MotorComCasa(out var token);
        motor.Relogio.Agora = H(10);
        await Registrar(motor, Pacote(token, 1, TipoDePacote.Heartbeat, H(10)));

        motor.Relogio.Agora = H(9, 59);
        await Registrar(motor, Pacote(token, 2, TipoDePacote.Heartbeat, H(9, 59)));

        Assert.Equal(H(10), await UltimoPacote(motor, token));
    }

    [Fact]
    public async Task LeituraForaDaFaixaFicaAusenteELeituraForaDaTelemetriaNaoEGravada()
    {
        await using var motor = await MotorComCasa(out var token);

        await Registrar(motor, new PacoteDoNo(token, 1, TipoDePacote.Telemetria, H(8), 75m, 127.04m, false));
        await Registrar(motor, new PacoteDoNo(token, 2, TipoDePacote.Movimento, H(8, 1), 30m, 127m, true));

        var eventos = await motor.Ler(bd => bd.Eventos.Where(e => e.CasaToken == token).OrderBy(e => e.Seq).ToListAsync());
        Assert.Null(eventos[0].TempC);
        Assert.Equal(new TensaoV(127.0m), eventos[0].TensaoV);
        Assert.Null(eventos[1].TempC);
        Assert.Null(eventos[1].TensaoV);
        Assert.True(eventos[1].Bufferizado);
    }

    [Fact]
    public async Task TokenDesconhecidoOuCasaDesativada_NaoGravaNada()
    {
        await using var motor = await MotorComCasa(out var token);
        await motor.Executar<IMotorDeCasas>(m => m.DesativarCasaAsync(token, CancellationToken.None));
        var desconhecido = CasaToken.Novo();

        var desativada = await Registrar(motor, Pacote(token, 1, TipoDePacote.Movimento, H(9)));
        var semCasa = await Registrar(motor, Pacote(desconhecido, 1, TipoDePacote.Movimento, H(9)));

        Assert.Equal(ResultadoDoPacote.CasaInativa, desativada);
        Assert.Equal(ResultadoDoPacote.CasaInativa, semCasa);
        Assert.Null(await UltimoPacote(motor, token));
        Assert.Equal(0, await motor.Ler(bd => bd.Eventos.CountAsync()));
    }

    [Fact]
    public async Task EventoSensorNaoAceitaUpdate()
    {
        await using var motor = await MotorComCasa(out var token);
        await Registrar(motor, Pacote(token, 1, TipoDePacote.Movimento, H(9)));

        var erro = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => motor.Ler(bd =>
            bd.Database.ExecuteSqlRawAsync("UPDATE motor.evento_sensor SET bufferizado = true")));

        Assert.Equal("23001", erro.SqlState);
    }

    private Task<MotorDeTeste> MotorComCasa(out CasaToken token)
    {
        var novo = CasaToken.Novo();
        token = novo;
        return Criar();

        async Task<MotorDeTeste> Criar()
        {
            var motor = await postgres.NovoMotorAsync(H(9));
            await motor.Executar<IMotorDeCasas>(m => m.AtivarCasaAsync(novo, new MicroareaId(3), Sozinha, CancellationToken.None));
            return motor;
        }
    }

    private static PacoteDoNo Pacote(CasaToken token, long seq, TipoDePacote tipo, DateTimeOffset ts) =>
        new(token, seq, tipo, ts, null, null, false);

    private static Task<ResultadoDoPacote> Registrar(MotorDeTeste motor, PacoteDoNo pacote) =>
        motor.Executar<RegistrarPacote, ResultadoDoPacote>(r => r.ExecutarAsync(pacote, CancellationToken.None));

    private static Task<DateTimeOffset?> UltimoPacote(MotorDeTeste motor, CasaToken token) =>
        motor.Ler(bd => bd.Casas.Where(c => c.Token == token).Select(c => c.UltimoPacoteEm).SingleAsync());
}
