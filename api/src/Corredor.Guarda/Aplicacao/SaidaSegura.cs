namespace Corredor.Guarda;

/// <summary>O modelo da rota da requisição atual e o módulo dono dela. Nulos fora de uma requisição.</summary>
public interface IRotaAtual
{
    string? Modelo { get; }

    string? Modulo { get; }
}

/// <summary>
/// A SaidaSegura. Consulta a PoliticaDeCanal e devolve o Veredito. Todo bloqueio registra uma
/// SaidaBloqueada, sem o valor, com o modelo da rota atual.
/// </summary>
public sealed class SaidaSegura(FilaDeSeguranca fila, RegrasDePii regras, TimeProvider relogio, IRotaAtual rotaAtual) : ISaidaSegura
{
    public Veredito Liberar(TextoRotulado texto, Canal canal) => Liberar(texto, canal, rotaAtual.Modulo ?? "Api");

    public Veredito Liberar(TextoRotulado texto, Canal canal, string modulo)
    {
        var proibidos = PoliticaDeCanal.Proibidos(canal, texto.Rotulos);
        if (proibidos.Count == 0)
        {
            return new Veredito.Liberada(texto.Valor);
        }

        var evento = EventoDeSeguranca.Registrar(
            TipoDeEventoDeSeguranca.BloqueioDeSaida,
            canal,
            proibidos.Select(t => (t, regras.SeveridadeDe(t))),
            modulo,
            rotaAtual.Modelo,
            relogio.GetUtcNow());
        fila.Enfileirar(evento);
        return new Veredito.Bloqueada(proibidos, texto.ToString());
    }
}
