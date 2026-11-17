using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corredor.Guarda;

/// <summary>
/// Lê a FilaDeSeguranca depois da resposta HTTP. Grava cada evento e passa para a tradução, que
/// roda num segundo laço para que uma chamada lenta ao LLM não atrase a gravação. Ao iniciar,
/// anexa a frase fixa a todo evento que ficou sem mensagem, porque a fila vive na memória.
/// </summary>
public sealed class ProcessadorDeEventos(
    FilaDeSeguranca fila,
    IServiceScopeFactory escopos,
    ILogger<ProcessadorDeEventos> log) : BackgroundService
{
    private readonly Channel<EventoDeSeguranca> _paraTraduzir = Channel.CreateUnbounded<EventoDeSeguranca>();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await AnexarFraseFixaAosOrfaosAsync(ct);
        await Task.WhenAll(GravarAsync(ct), TraduzirAsync(ct));
    }

    private async Task AnexarFraseFixaAosOrfaosAsync(CancellationToken ct)
    {
        try
        {
            using var escopo = escopos.CreateScope();
            var armazem = escopo.ServiceProvider.GetRequiredService<IArmazemDeEventos>();
            foreach (var evento in await armazem.SemMensagemAsync(ct))
            {
                var fixa = new MensagemAoCidadao(FrasesFixas.DoTipoDeEvento(evento.Tipo), OrigemDaMensagem.FraseFixa);
                await armazem.AnexarMensagemAsync(evento.Id, fixa, ct);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogWarning("Eventos sem mensagem não receberam a frase fixa: {Erro}", e.GetType().Name);
        }
    }

    private async Task GravarAsync(CancellationToken ct)
    {
        await foreach (var evento in fila.Leitor.ReadAllAsync(ct))
        {
            try
            {
                using var escopo = escopos.CreateScope();
                await escopo.ServiceProvider.GetRequiredService<IArmazemDeEventos>().GravarAsync(evento, ct);
                _paraTraduzir.Writer.TryWrite(evento);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError("Evento de segurança {Id} não foi gravado: {Erro}", evento.Id, e.GetType().Name);
            }
            finally
            {
                fila.Concluir();
            }
        }
    }

    private async Task TraduzirAsync(CancellationToken ct)
    {
        await foreach (var evento in _paraTraduzir.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var escopo = escopos.CreateScope();
                var tradutor = escopo.ServiceProvider.GetRequiredService<Tradutor>();
                var mensagem = await tradutor.TraduzirAsync(evento, ct);
                await escopo.ServiceProvider.GetRequiredService<IArmazemDeEventos>().AnexarMensagemAsync(evento.Id, mensagem, ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError("Evento de segurança {Id} ficou sem mensagem: {Erro}", evento.Id, e.GetType().Name);
            }
        }
    }
}
