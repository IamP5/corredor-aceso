using Corredor.Cadastro.Dominio;
using Corredor.Cadastro.Infra;
using Corredor.Contratos;
using Corredor.Guarda;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corredor.Cadastro.Aplicacao;

// RegistrarResposta e a política P20.
public sealed class CasosDoAviso(CadastroDbContext db, IMotorDeCasas motor, TimeProvider relogio)
{
    // A mesma resposta repetida não muda nada. EstaBem confirma a casa no Motor com o Id do aviso, que ignora a repetição.
    public async Task<AvisoAoVizinho> RegistrarRespostaAsync(Guid id, RespostaDoVizinho resposta, CancellationToken ct)
    {
        var aviso = await Persistencia.ComUmaRepeticaoAsync(db, async () =>
        {
            var aviso = await db.Avisos.SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw Erros.AvisoDesconhecido();
            if (aviso.RegistrarResposta(resposta, relogio.GetUtcNow()) is not null)
            {
                await db.SaveChangesAsync(ct);
            }

            return aviso;
        });

        if (aviso is { Resposta: RespostaDoVizinho.EstaBem, RespondidoEm: { } respondidoEm })
        {
            var confirmacao = new Confirmacao(aviso.Id, OrigemDaConfirmacao.Vizinho, ResultadoDaConfirmacao.PessoaVista, respondidoEm);
            await ChamadaAoMotor.ExecutarAsync(() => motor.ConfirmarAsync(aviso.CasaToken, confirmacao, ct));
        }

        return aviso;
    }
}

// P19. O Motor declara a porta e chama a cada ciclo do VigiaDoRadar, para cada alerta pendente.
// O Cadastro descarta a repetição pela chave do episódio. O envio simulado é a própria linha do aviso
// e uma linha de log sem telefone, depois do texto fixo passar pela SaidaSegura no canal Vizinho.
// Scoped: o VigiaDoRadar abre um escopo novo para cada chamada.
public sealed class ReceptorDeContato(CadastroDbContext db, ISaidaSegura saida, TimeProvider relogio, ILogger<ReceptorDeContato> log)
    : IReceptorDeContato
{
    public async Task PedirContatoAsync(PedidoDeContato pedido, CancellationToken ct)
    {
        var episodioExiste = await db.Avisos.AnyAsync(
            a => a.CasaToken == pedido.Token && a.Motivo == pedido.Motivo && a.Desde == pedido.Desde, ct);
        if (episodioExiste || await db.IdosoDoTokenAsync(pedido.Token, ct) is not { } idoso)
        {
            return;
        }

        if (!idoso.DestinatariosDoAviso().Contains(DestinatarioDoAviso.Vizinho)
            || AvisoAoVizinho.Registrar(idoso, pedido, relogio.GetUtcNow()) is not { } aviso)
        {
            return;
        }

        if (saida.Liberar(TextoRotulado.Do(AvisoAoVizinho.Texto), Canal.Vizinho) is not Veredito.Liberada)
        {
            return;
        }

        db.Avisos.Add(aviso);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (Persistencia.ChaveRepetida(e))
        {
            return;
        }

        log.LogInformation(
            "aviso ao vizinho {Aviso} enviado pelo canal simulado: microarea {Microarea}, motivo {Motivo}",
            aviso.Id, aviso.Microarea.Valor, aviso.Motivo);
    }
}
