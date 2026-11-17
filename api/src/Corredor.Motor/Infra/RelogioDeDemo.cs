using Corredor.Contratos;

namespace Corredor.Motor.Infra;

// Em Development, o relogio do Motor anda com um deslocamento definido por PUT /api/demo/relogio.
// O deslocamento fica em memoria: depois de reiniciar a API, repita o PUT.
public sealed class RelogioDeDemo(TimeProvider sistema) : TimeProvider, IDeslocamentoDoRelogio
{
    private long _deslocamentoEmTicks;

    public TimeSpan Deslocamento => TimeSpan.FromTicks(Interlocked.Read(ref _deslocamentoEmTicks));

    public override DateTimeOffset GetUtcNow() => sistema.GetUtcNow() + Deslocamento;

    public override TimeZoneInfo LocalTimeZone => sistema.LocalTimeZone;

    public TimeSpan MoverPara(DateTimeOffset agoraDesejado)
    {
        var deslocamento = agoraDesejado - sistema.GetUtcNow();
        Interlocked.Exchange(ref _deslocamentoEmTicks, deslocamento.Ticks);
        return deslocamento;
    }
}
