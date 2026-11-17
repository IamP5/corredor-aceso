namespace Corredor.Guarda;

public enum TipoDeEventoDeSeguranca { MascaramentoNaEntrada, BloqueioDeSaida }

public enum OrigemDaMensagem { Llm, FraseFixa }

public enum CorDoFarol { Verde, Amarelo, Vermelho }

public sealed record MensagemAoCidadao(string Texto, OrigemDaMensagem Origem);

public static class TiposDeDado
{
    /// <summary>Os tipos que o interceptador troca pela máscara no corpo da requisição.</summary>
    public static readonly IReadOnlySet<TipoDeDado> MascaradosNaEntrada =
        new HashSet<TipoDeDado> { TipoDeDado.Cpf, TipoDeDado.Rg, TipoDeDado.Cns, TipoDeDado.Credencial };
}
