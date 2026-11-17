using Corredor.Contratos;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Corredor.Motor.Aplicacao;

public enum ComandoDoNo { DiaCritico, PedirToque }

// Publica em corredor/v1/{casaToken}/comando. A interface deixa os testes contarem o que o no recebeu.
public interface IPublicadorDeComandos
{
    Task PublicarAsync(CasaToken token, ComandoDoNo comando, CancellationToken ct);
}

public sealed class DeclararDiaCritico(MotorDbContext bd, TimeProvider relogio, IPublicadorDeComandos publicador)
{
    public async Task<DiaCritico> ExecutarAsync(DateOnly data, PrevisaoDoDia previsao, CancellationToken ct)
    {
        try
        {
            await DeclararAsync(data, previsao, ct);
        }
        catch (DbUpdateException erro) when (erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } || erro is DbUpdateConcurrencyException)
        {
            // Duas declaracoes da mesma data ao mesmo tempo: a segunda rele e so troca a previsao.
            bd.ChangeTracker.Clear();
            await DeclararAsync(data, previsao, ct);
        }

        var dia = await bd.DiasCriticos.AsNoTracking().SingleAsync(d => d.Data == data, ct);

        // Politica P8: o LED ambar acende em cada casa ativa. Repetir o comando nao muda o no.
        var tokens = await bd.Casas.AsNoTracking().Where(c => c.DesativadaEm == null).Select(c => c.Token).ToListAsync(ct);
        foreach (var token in tokens)
        {
            await publicador.PublicarAsync(token, ComandoDoNo.DiaCritico, ct);
        }

        return dia;
    }

    private async Task DeclararAsync(DateOnly data, PrevisaoDoDia previsao, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        if (await bd.DiasCriticos.SingleOrDefaultAsync(d => d.Data == data, ct) is { } existente)
        {
            existente.DeclararDeNovo(previsao, agora);
        }
        else
        {
            bd.DiasCriticos.Add(DiaCritico.Declarar(data, previsao, agora));
        }

        await bd.SaveChangesAsync(ct);
    }
}

public sealed class EncerrarTurno(MotorDbContext bd, AvaliacaoDasCasas avaliacao, TimeProvider relogio, ParametrosDoMotor parametros)
{
    public async Task<DiaCritico> ExecutarAsync(DateOnly data, CancellationToken ct)
    {
        try
        {
            return await EncerrarAsync(data, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A ACS e o VigiaDoRadar encerraram no mesmo segundo. O segundo rele e devolve o resumo do primeiro.
            bd.ChangeTracker.Clear();
            return await EncerrarAsync(data, ct);
        }
    }

    private async Task<DiaCritico> EncerrarAsync(DateOnly data, CancellationToken ct)
    {
        var dia = await bd.DiasCriticos.SingleOrDefaultAsync(d => d.Data == data, ct) ?? throw Violacoes.DiaCriticoDesconhecido();
        if (dia.EncerradoEm is not null)
        {
            return dia;
        }

        var agora = relogio.GetUtcNow();
        dia.Encerrar(await ResumirAsync(dia, agora, microarea: null, ct), agora);
        await bd.SaveChangesAsync(ct);
        return dia;
    }

    // O resumo gravado conta todas as microareas. A ACS recebe o da microarea dela, como no radar,
    // avaliado no mesmo instante do encerramento.
    public Task<ResumoDoTurno> ResumirMicroareaAsync(DiaCritico dia, MicroareaId microarea, CancellationToken ct) =>
        ResumirAsync(dia, dia.EncerradoEm ?? throw new InvalidOperationException("o turno ainda nao foi encerrado"), microarea, ct);

    // O resumo olha o turno ainda aberto. Depois do FimDoTurno, avalia no ultimo instante do turno.
    private async Task<ResumoDoTurno> ResumirAsync(DiaCritico dia, DateTimeOffset agora, MicroareaId? microarea, CancellationToken ct)
    {
        var turno = dia.Turno(parametros);
        var ultimoInstante = turno.FimPrevisto.AddTicks(-TimeSpan.TicksPerMillisecond);
        var instante = agora < ultimoInstante ? agora : ultimoInstante;
        var foto = await avaliacao.FotografarAsync(instante, turno, dia, microarea, ct);
        var dezHoras = HorarioDeSaoPaulo.Em(dia.Data, parametros.FimDaManha);
        return new ResumoDoTurno(
            CasasAtivas: foto.Casas.Count,
            ComSinalDeVidaAte10h: dia.AbertoEm >= dezHoras
                ? 0
                : foto.Casas.Count(c => c.Historico.Ate(dezHoras).TemSinalDeVidaDesde(dia.AbertoEm, c.Casa.Perfil.Contato)),
            Atendidas: foto.Casas.Count(c => c.Avaliacao.Atendida),
            VermelhasPendentes: foto.Casas.Count(c => c.Avaliacao is { Pendente: true, Situacao: SituacaoDaCasa.Vermelho }),
            CinzasPendentes: foto.Casas.Count(c => c.Avaliacao is { Pendente: true, Situacao: SituacaoDaCasa.Cinza }));
    }
}
