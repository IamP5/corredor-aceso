using Corredor.Contratos;

namespace Corredor.Motor.Dominio;

// Os erros de dominio do Motor. A mensagem e texto fixo, sem dado pessoal. O host escolhe o status pelo tipo.
public static class Violacoes
{
    public static RegraDeDominioViolada CasaDesativada() =>
        new("CASA_DESATIVADA", "A casa foi desativada e nao aceita mais nada.", TipoDeViolacao.Conflito);

    public static RegraDeDominioViolada CasaDesconhecida() =>
        new("CASA_DESCONHECIDA", "Nenhuma casa ativa com este token.", TipoDeViolacao.Desconhecido);

    public static RegraDeDominioViolada ConfirmacaoNoFuturo() =>
        new("CONFIRMACAO_NO_FUTURO", "A confirmacao tem hora no futuro.");

    // O Cadastro reconhece este codigo: mantem a visita e responde 201.
    public static RegraDeDominioViolada ConfirmacaoVencida() =>
        new("CONFIRMACAO_VENCIDA", "A confirmacao tem mais de 24 horas.");

    public static RegraDeDominioViolada DataNaoEHoje() =>
        new("DATA_NAO_E_HOJE", "O dia critico so pode ser declarado para hoje.");

    public static RegraDeDominioViolada ProbabilidadeForaDaFaixa() =>
        new("PROBABILIDADE_FORA_DA_FAIXA", "A probabilidade fica entre 0 e 1.");

    public static RegraDeDominioViolada TurnoEncerrado() =>
        new("TURNO_ENCERRADO", "O turno deste dia ja foi encerrado.", TipoDeViolacao.Conflito);

    public static RegraDeDominioViolada DiaCriticoDesconhecido() =>
        new("DIA_CRITICO_DESCONHECIDO", "Nao existe dia critico nesta data.", TipoDeViolacao.Desconhecido);

    public static RegraDeDominioViolada CampoInvalido(string mensagem) =>
        new("CAMPO_INVALIDO", mensagem);
}
