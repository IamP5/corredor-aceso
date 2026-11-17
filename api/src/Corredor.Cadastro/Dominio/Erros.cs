using Corredor.Contratos;

namespace Corredor.Cadastro.Dominio;

// Cada código da tabela de rotas do Cadastro. A mensagem é fixa e nunca repete o valor que chegou.
public static class Erros
{
    public static RegraDeDominioViolada NomeObrigatorio() =>
        new("NOME_OBRIGATORIO", "O nome é obrigatório.");

    public static RegraDeDominioViolada EnderecoObrigatorio() =>
        new("ENDERECO_OBRIGATORIO", "O endereço é obrigatório.");

    public static RegraDeDominioViolada AutorizacaoAusente() =>
        new("AUTORIZACAO_AUSENTE", "O idoso não autorizou avisar o vizinho.");

    public static RegraDeDominioViolada TokenInativo() =>
        new("TOKEN_INATIVO", "Este token não pertence a uma casa ativa.");

    public static RegraDeDominioViolada VisitaNoFuturo() =>
        new("VISITA_NO_FUTURO", "A hora da visita está no futuro.");

    public static RegraDeDominioViolada CampoInvalido(string campo) =>
        new("CAMPO_INVALIDO", $"O campo {campo} está fora do formato.");

    public static RegraDeDominioViolada AvisoJaRespondido() =>
        new("AVISO_JA_RESPONDIDO", "Este aviso já tem outra resposta.", TipoDeViolacao.Conflito);

    public static RegraDeDominioViolada IdosoDesconhecido() =>
        new("IDOSO_DESCONHECIDO", "Idoso não encontrado.", TipoDeViolacao.Desconhecido);

    public static RegraDeDominioViolada AvisoDesconhecido() =>
        new("AVISO_DESCONHECIDO", "Aviso não encontrado.", TipoDeViolacao.Desconhecido);

    public static RegraDeDominioViolada MotorIndisponivel() =>
        new("MOTOR_INDISPONIVEL", "O Motor não respondeu. O registro está gravado. Repita o pedido.", TipoDeViolacao.Indisponivel);
}
