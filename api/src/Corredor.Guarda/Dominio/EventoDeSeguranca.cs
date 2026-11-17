using System.Text.RegularExpressions;

namespace Corredor.Guarda;

/// <summary>
/// O registro de um mascaramento na entrada ou de uma saída bloqueada. Nunca guarda o valor
/// original nem um trecho do texto: só o tipo de dado, o canal e o módulo.
/// </summary>
public sealed partial class EventoDeSeguranca
{
    private EventoDeSeguranca() { }

    public Guid Id { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }
    public TipoDeEventoDeSeguranca Tipo { get; private set; }
    public List<TipoDeDado> TiposDeDado { get; private set; } = [];
    public Canal Canal { get; private set; }
    public string Modulo { get; private set; } = null!;

    /// <summary>O modelo da rota, como /api/idosos/{id}/vizinho. Nulo fora de uma requisição.</summary>
    public string? Rota { get; private set; }

    public short Severidade { get; private set; }
    public MensagemAoCidadao? Mensagem { get; private set; }

    public int Pontuacao => Severidade * PoliticaDeCanal.Exposicao(Canal);

    public static EventoDeSeguranca Registrar(
        TipoDeEventoDeSeguranca tipo,
        Canal canal,
        IEnumerable<(TipoDeDado Tipo, int Severidade)> achados,
        string modulo,
        string? rota,
        DateTimeOffset agora)
    {
        var lista = achados.ToList();
        if (lista.Count == 0)
        {
            throw new ArgumentException("evento de segurança sem tipo de dado", nameof(achados));
        }

        if (string.IsNullOrWhiteSpace(modulo) || modulo.Length > 40)
        {
            throw new ArgumentException("módulo vazio ou longo demais", nameof(modulo));
        }

        if (rota is not null && (rota.Length > 200 || RotaComIdentificador().IsMatch(rota)))
        {
            throw new ArgumentException("a rota precisa ser o modelo, sem identificador", nameof(rota));
        }

        var severidade = lista.Max(a => a.Severidade);
        if (severidade is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(achados), "severidade fora de 1 a 5");
        }

        return new EventoDeSeguranca
        {
            Id = Guid.NewGuid(),
            OcorridoEm = agora,
            Tipo = tipo,
            Canal = canal,
            TiposDeDado = lista.Select(a => a.Tipo).Distinct().Order().ToList(),
            Severidade = (short)severidade,
            Modulo = modulo,
            Rota = rota,
        };
    }

    /// <summary>A mensagem é anexada uma vez. A segunda chamada não muda nada.</summary>
    public bool AnexarMensagem(MensagemAoCidadao mensagem)
    {
        if (Mensagem is not null)
        {
            return false;
        }

        Mensagem = mensagem;
        return true;
    }

    // O mesmo padrão de ck_evento_de_seguranca_rota_sem_identificador.
    [GeneratedRegex("[?]|[0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-")]
    private static partial Regex RotaComIdentificador();
}

/// <summary>Uma linha da contagem das últimas 24 horas, agrupada por tipo, canal e severidade.</summary>
public sealed record ContagemDoPainel(TipoDeEventoDeSeguranca Tipo, Canal Canal, short Severidade, int Quantidade)
{
    public int Pontos => Quantidade * Severidade * PoliticaDeCanal.Exposicao(Canal);
}

/// <summary>A cor do painel Risco Cidadão. Função pura sobre os eventos das últimas 24 horas.</summary>
public static class Farol
{
    public static readonly TimeSpan Janela = TimeSpan.FromHours(24);

    /// <summary>
    /// Vermelho com uma saída bloqueada de severidade 4 ou 5. Amarelo com qualquer outro evento.
    /// Verde sem evento. O mascaramento na entrada vai até amarelo.
    /// </summary>
    public static CorDoFarol Cor(IEnumerable<ContagemDoPainel> ultimas24h)
    {
        var lista = ultimas24h.Where(c => c.Quantidade > 0).ToList();
        if (lista.Any(c => c.Tipo == TipoDeEventoDeSeguranca.BloqueioDeSaida && c.Severidade >= 4))
        {
            return CorDoFarol.Vermelho;
        }

        return lista.Count > 0 ? CorDoFarol.Amarelo : CorDoFarol.Verde;
    }

    public static int Pontos(IEnumerable<ContagemDoPainel> ultimas24h) => ultimas24h.Sum(c => c.Pontos);

    public static IEnumerable<ContagemDoPainel> Contar(IEnumerable<EventoDeSeguranca> eventos, DateTimeOffset agora) =>
        eventos.Where(e => e.OcorridoEm >= agora - Janela && e.OcorridoEm <= agora)
            .GroupBy(e => (e.Tipo, e.Canal, e.Severidade))
            .Select(g => new ContagemDoPainel(g.Key.Tipo, g.Key.Canal, g.Key.Severidade, g.Count()));
}
