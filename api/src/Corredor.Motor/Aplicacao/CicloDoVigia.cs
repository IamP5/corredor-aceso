using Corredor.Contratos;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;

namespace Corredor.Motor.Aplicacao;

// Uma rodada do VigiaDoRadar: encerra o turno as 18h, pede o toque as 14h30 e manda o pedido de contato de cada alerta pendente.
public sealed class CicloDoVigia(
    MotorDbContext bd,
    AvaliacaoDasCasas avaliacao,
    EncerrarTurno encerrarTurno,
    IPublicadorDeComandos publicador,
    IReceptorDeContato receptor,
    TimeProvider relogio,
    ParametrosDoMotor parametros)
{
    public async Task RodarAsync(CancellationToken ct)
    {
        var foto = await avaliacao.FotografarAsync(relogio.GetUtcNow(), microarea: null, ct);
        if (foto is { Dia: { EncerradoEm: null } dia, Turno: { } turno })
        {
            if (foto.Agora >= turno.FimPrevisto)
            {
                await encerrarTurno.ExecutarAsync(dia.Data, ct);
            }
            else if (dia.ToquePedidoEm is null && foto.Agora >= HorarioDeSaoPaulo.Em(dia.Data, parametros.HoraDoPedidoDeToque))
            {
                await PedirToqueAsync(foto, ct);
            }
        }

        // O Cadastro descarta a repeticao pela chave (casaToken, motivo, desde), entao o pedido sai a cada rodada.
        var pedidos = foto.Casas
            .Where(c => c.Avaliacao.Pendente
                && (c.Avaliacao.Situacao == SituacaoDaCasa.Vermelho || (c.Avaliacao.Situacao == SituacaoDaCasa.Cinza && foto.TurnoAberto)))
            .Select(c => new PedidoDeContato(c.Casa.Token, c.Avaliacao.Alerta!.Motivo, c.Avaliacao.Alerta.Desde));
        foreach (var pedido in pedidos)
        {
            await receptor.PedirContatoAsync(pedido, ct);
        }
    }

    // Invariante 6 de DiaCritico: o toque sai uma vez por dia, so para as casas Sozinho sem sinal de vida desde as 10h.
    private async Task PedirToqueAsync(FotoDasCasas foto, CancellationToken ct)
    {
        var dia = await bd.DiasCriticos.SingleAsync(d => d.Data == foto.Dia!.Data, ct);
        if (!dia.PedirToque(foto.Agora))
        {
            return;
        }

        try
        {
            await bd.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Outra rodada pediu o toque primeiro.
            return;
        }

        var dezHoras = HorarioDeSaoPaulo.Em(dia.Data, parametros.FimDaManha);
        var semSinal = foto.Casas.Where(c => c.Casa.Perfil.Contato == PerfilDeContato.Sozinho
            && !c.Historico.Ate(foto.Agora).TemSinalDeVidaDesde(dezHoras, PerfilDeContato.Sozinho));
        foreach (var casa in semSinal)
        {
            await publicador.PublicarAsync(casa.Casa.Token, ComandoDoNo.PedirToque, ct);
        }
    }
}

// Apaga eventos e confirmacoes com mais de 24 horas pelo SQL conferido de consultas/expurgo.sql.
// Guarda a leitura baixa de casa ativa e eletrodependente ate a ACS ver a pessoa depois dela.
public sealed class ExpurgarDados(MotorDbContext bd, TimeProvider relogio, ParametrosDoMotor parametros)
{
    private static readonly string Sql = RecursoDoMotor.Ler("expurgo.sql");

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var corte = (relogio.GetUtcNow() - parametros.Retencao).ToUniversalTime();
        await using var tx = await bd.Database.BeginTransactionAsync(ct);
        await bd.Database.ExecuteSqlRawAsync(Sql, [corte, parametros.TensaoMinima], ct);
        await tx.CommitAsync(ct);
    }
}
