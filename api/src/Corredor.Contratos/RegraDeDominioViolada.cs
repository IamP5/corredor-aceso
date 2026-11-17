namespace Corredor.Contratos;

// Mora aqui porque o Motor lança pela IMotorDeCasas e o Cadastro precisa reconhecer,
// sem que um referencie o outro. O host converte em {"codigo", "mensagem"}.
// A mensagem é texto fixo, nunca dado pessoal.
public sealed class RegraDeDominioViolada(string codigo, string mensagem, TipoDeViolacao tipo = TipoDeViolacao.Invalida)
    : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
    public TipoDeViolacao Tipo { get; } = tipo;
}

// Cada valor vira um status HTTP no host: 422, 409, 404, 403 e 503.
public enum TipoDeViolacao { Invalida, Conflito, Desconhecido, Proibida, Indisponivel }
