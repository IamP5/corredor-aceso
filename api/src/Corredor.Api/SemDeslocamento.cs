using Corredor.Contratos;

namespace Corredor.Api;

internal sealed class SemDeslocamento : IDeslocamentoDoRelogio
{
    public TimeSpan Deslocamento => TimeSpan.Zero;
}
