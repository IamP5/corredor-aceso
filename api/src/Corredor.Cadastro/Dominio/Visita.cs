using Corredor.Contratos;
using Corredor.Guarda;

namespace Corredor.Cadastro.Dominio;

// Raiz imutável. O Id nasce no celular e é a chave de idempotência. Uma correção é outra visita.
public sealed class Visita
{
    private Visita() { }

    public Guid Id { get; private set; }
    public IdosoId IdosoId { get; private set; }
    public AcsId AcsId { get; private set; }
    public DateTimeOffset OcorridaEm { get; private set; }
    public DateTimeOffset RegistradaEm { get; private set; }
    public Desfecho Desfecho { get; private set; }
    public TextoRotulado? Observacao { get; private set; }

    // ocorridaEm já chega com o deslocamento do relógio de demo somado (invariante 3).
    public static Visita Registrar(
        Guid id, Idoso idoso, AcsId acs, Desfecho desfecho, TextoRotulado? observacao,
        DateTimeOffset ocorridaEm, DateTimeOffset agora, TimeSpan toleranciaDoRelogio)
    {
        if (idoso.TokenAtivo is null)
        {
            throw Erros.TokenInativo();
        }

        if (ocorridaEm > agora + toleranciaDoRelogio)
        {
            throw Erros.VisitaNoFuturo();
        }

        return new Visita
        {
            Id = id,
            IdosoId = idoso.Id,
            AcsId = acs,
            Desfecho = desfecho,
            Observacao = observacao,
            OcorridaEm = ocorridaEm,
            RegistradaEm = agora,
        };
    }
}

// O único lugar que decide se um desfecho vira confirmação no Motor.
// "Não abriu" não manda nada. "Estava fora" é ausência confirmada, nunca sinal de vida.
public static class TradutorDeDesfecho
{
    private static readonly IReadOnlyDictionary<Desfecho, ResultadoDaConfirmacao?> Tabela =
        new Dictionary<Desfecho, ResultadoDaConfirmacao?>
        {
            [Desfecho.EstavaBem] = ResultadoDaConfirmacao.PessoaVista,
            [Desfecho.Encaminhada] = ResultadoDaConfirmacao.PessoaVista,
            [Desfecho.EstavaFora] = ResultadoDaConfirmacao.AusenciaConfirmada,
            [Desfecho.NaoAbriu] = null,
        };

    // O Id da confirmação é o Id da visita, e Quando é OcorridaEm.
    public static Confirmacao? Para(Visita visita) =>
        Tabela[visita.Desfecho] is { } resultado
            ? new Confirmacao(visita.Id, OrigemDaConfirmacao.Acs, resultado, visita.OcorridaEm)
            : null;
}
