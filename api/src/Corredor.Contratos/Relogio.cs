namespace Corredor.Contratos;

// Vem da seção Relogio do appsettings.json. O host registra uma instância só, lida pelo Motor e pelo Cadastro.
public sealed record ParametrosDoRelogio(TimeSpan ToleranciaDoRelogio);

// Em Development, o RelogioDeDemo do Motor implementa. Fora dele, o host registra uma implementação que devolve zero.
public interface IDeslocamentoDoRelogio
{
    TimeSpan Deslocamento { get; }
}
