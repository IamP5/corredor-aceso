using Corredor.Cadastro.Dominio;
using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Microsoft.EntityFrameworkCore;

namespace Corredor.Cadastro.Aplicacao;

public sealed record NovoIdoso(
    MicroareaId Microarea, string Nome, string Endereco, PerfilDaCasa Perfil, string? Condicoes, string? Medicamentos, IdosoId? Id = null);

// Os comandos do agregado Idoso e as políticas P1, P4 e P21.
public sealed class CasosDoIdoso(CadastroDbContext db, IMotorDeCasas motor, TimeProvider relogio)
{
    // Sem chave de idempotência: repetir cria outro idoso. Só a carga informa o id, e o id que já existe é pulado.
    public async Task<IdosoId> CadastrarAsync(NovoIdoso novo, CancellationToken ct)
    {
        if (novo.Id is { } id && await db.Idosos.AnyAsync(i => i.Id == id, ct))
        {
            return id;
        }

        if (!await db.Microareas.AnyAsync(m => m.Id == novo.Microarea, ct))
        {
            throw Erros.CampoInvalido("microareaId");
        }

        var (idoso, _) = Idoso.Cadastrar(
            novo.Id ?? IdosoId.Novo(), novo.Microarea, novo.Nome, novo.Endereco, novo.Perfil, novo.Condicoes, novo.Medicamentos);
        db.Idosos.Add(idoso);
        await db.SaveChangesAsync(ct);
        return idoso.Id;
    }

    // P1: depois de gravar, ativa a casa no Motor. Com consentimento vigente, devolve o mesmo token e ativa de novo.
    public async Task<CasaToken> RegistrarConsentimentoAsync(
        IdosoId id, AcsId acs, string versaoDoTermo, Autorizacoes autorizacoes, CancellationToken ct)
    {
        var registrado = await Persistencia.ComUmaRepeticaoAsync(db, async () =>
        {
            var idoso = await db.IdosoAsync(id, ct);
            var evento = idoso.RegistrarConsentimento(acs, versaoDoTermo, autorizacoes, relogio.GetUtcNow());
            await db.SaveChangesAsync(ct);
            return evento;
        });

        await ChamadaAoMotor.ExecutarAsync(() =>
            motor.AtivarCasaAsync(registrado.Token, registrado.Microarea, registrado.Perfil, ct));
        return registrado.Token;
    }

    // P4: numa transação, revoga, apaga o token e o vizinho, e apaga os avisos daquele token. Depois desativa no Motor.
    // As visitas ficam.
    public async Task RevogarConsentimentoAsync(IdosoId id, CancellationToken ct)
    {
        var revogado = await Persistencia.ComUmaRepeticaoAsync(db, async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var idoso = await db.IdosoAsync(id, ct);
            var evento = idoso.RevogarConsentimento(relogio.GetUtcNow());
            if (evento is not null)
            {
                await db.SaveChangesAsync(ct);
                await db.Avisos.Where(a => a.CasaToken == evento.Token).ExecuteDeleteAsync(ct);
            }

            await tx.CommitAsync(ct);
            return evento;
        });

        if (revogado is not null)
        {
            await ChamadaAoMotor.ExecutarAsync(() => motor.DesativarCasaAsync(revogado.Token, ct));
        }
    }

    // Indicar de novo substitui o vizinho. A rota é do Swagger e só uma pessoa a usa (seção 2.6).
    public async Task IndicarVizinhoAsync(IdosoId id, string nome, string telefone, DateTimeOffset aceitouEm, CancellationToken ct)
    {
        var idoso = await db.IdosoAsync(id, ct);
        idoso.IndicarVizinho(nome, telefone, aceitouEm);
        await db.SaveChangesAsync(ct);
    }

    // P21: com token ativo, leva o perfil novo ao Motor. O último valor vale.
    public async Task AlterarPerfilAsync(IdosoId id, PerfilDaCasa perfil, CancellationToken ct)
    {
        var alterado = await Persistencia.ComUmaRepeticaoAsync(db, async () =>
        {
            var idoso = await db.IdosoAsync(id, ct);
            var evento = idoso.AlterarPerfil(perfil);
            await db.SaveChangesAsync(ct);
            return evento;
        });

        if (alterado.Token is { } token)
        {
            await ChamadaAoMotor.ExecutarAsync(() => motor.DefinirPerfilAsync(token, perfil, ct));
        }
    }
}
