using Corredor.Cadastro.Dominio;
using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Corredor.Guarda;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corredor.Cadastro.Aplicacao;

public sealed record NovaVisita(Guid Id, CasaToken Token, DateTimeOffset OcorridaEm, Desfecho Desfecho, string? Observacao);

public sealed record VisitaGravada(Guid Id, DateTimeOffset RegistradaEm, bool Repeticao);

// RegistrarVisita e a política P11.
public sealed class CasosDaVisita(
    CadastroDbContext db,
    IMotorDeCasas motor,
    IMotorDeParsing parsing,
    TimeProvider relogio,
    IDeslocamentoDoRelogio deslocamento,
    ParametrosDoRelogio parametros,
    ILogger<CasosDaVisita> log)
{
    // O código com que o Motor recusa uma confirmação com mais de 24 horas.
    private const string ConfirmacaoVencida = "CONFIRMACAO_VENCIDA";

    // O id nasce no celular. Repetir devolve a visita que já existe e chama o Motor de novo, que ignora o Id repetido.
    public async Task<VisitaGravada> RegistrarAsync(NovaVisita nova, AcsId acs, CancellationToken ct)
    {
        var (visita, repeticao) = await GravarAsync(nova, acs, ct);

        // P11. A confirmação leva o Id da visita. O token é o da casa agora: depois da revogação não há o que confirmar.
        if (TradutorDeDesfecho.Para(visita) is { } confirmacao
            && await db.Idosos.Where(i => i.Id == visita.IdosoId).Select(i => i.Casa.Token).SingleAsync(ct) is { } token)
        {
            try
            {
                await ChamadaAoMotor.ExecutarAsync(() => motor.ConfirmarAsync(token, confirmacao, ct));
            }
            catch (RegraDeDominioViolada recusa) when (recusa.Codigo == ConfirmacaoVencida)
            {
                // Invariante 7: o Motor recusou a confirmação com mais de 24 horas. A visita fica e a resposta é 201.
                log.LogWarning("o Motor recusou a confirmação da visita {Visita} com {Codigo}", visita.Id, recusa.Codigo);
            }
        }

        return new VisitaGravada(visita.Id, visita.RegistradaEm, repeticao);
    }

    private async Task<(Visita Visita, bool Repeticao)> GravarAsync(NovaVisita nova, AcsId acs, CancellationToken ct)
    {
        if (await db.Visitas.AsNoTracking().SingleOrDefaultAsync(v => v.Id == nova.Id, ct) is { } existente)
        {
            return (existente, true);
        }

        var idoso = await db.IdosoDoTokenAsync(nova.Token, ct) ?? throw Erros.TokenInativo();
        // Invariante 5: o rótulo Saude vem do campo, mais os que o MotorDeParsing achar.
        var observacao = string.IsNullOrWhiteSpace(nova.Observacao)
            ? null
            : parsing.Rotular(nova.Observacao, TipoDeDado.Saude).Texto;
        // Invariante 3: a hora da visita anda com o relógio de demo, como o ts do pacote.
        var visita = Visita.Registrar(
            nova.Id, idoso, acs, nova.Desfecho, observacao,
            nova.OcorridaEm + deslocamento.Deslocamento, relogio.GetUtcNow(), parametros.ToleranciaDoRelogio);
        db.Visitas.Add(visita);
        try
        {
            await db.SaveChangesAsync(ct);
            return (visita, false);
        }
        catch (DbUpdateException e) when (Persistencia.ChaveRepetida(e))
        {
            db.ChangeTracker.Clear();
            return (await db.Visitas.AsNoTracking().SingleAsync(v => v.Id == nova.Id, ct), true);
        }
    }
}
