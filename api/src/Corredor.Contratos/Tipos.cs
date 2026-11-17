namespace Corredor.Contratos;

public enum PerfilDeContato { Sozinho, Acamado, Coabitado }

public sealed record PerfilDaCasa(PerfilDeContato Contato, bool Eletrodependente);

public enum OrigemDaConfirmacao { Acs, Vizinho }

public enum ResultadoDaConfirmacao { PessoaVista, AusenciaConfirmada }

public sealed record Confirmacao(
    Guid Id,
    OrigemDaConfirmacao Origem,
    ResultadoDaConfirmacao Resultado,
    DateTimeOffset Quando);

public enum MotivoDoAlerta { EnergiaEletrodependente, SemHeartbeat, CalorInterno, SemMovimentoNaJanela }

public sealed record PedidoDeContato(CasaToken Token, MotivoDoAlerta Motivo, DateTimeOffset Desde);
