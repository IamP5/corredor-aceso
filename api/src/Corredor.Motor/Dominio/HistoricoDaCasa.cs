using Corredor.Contratos;

namespace Corredor.Motor.Dominio;

// O que a avaliacao le de um evento do sensor. Seq, RecebidoEm e Bufferizado nao entram na regra.
public sealed record LeituraDeEvento(CasaToken CasaToken, TipoDePacote Tipo, DateTimeOffset OcorridoEm, TempC? TempC, TensaoV? TensaoV);

// Eventos do sensor e confirmacoes de uma casa: as ultimas 24 horas mais as leituras de tensao baixa de qualquer idade.
// So conta o que aconteceu ate o instante de corte, entao a avaliacao nunca le o futuro.
public sealed class HistoricoDaCasa
{
    private readonly IReadOnlyList<LeituraDeEvento> _eventos;
    private readonly IReadOnlyList<Confirmacao> _confirmacoes;

    private HistoricoDaCasa(IReadOnlyList<LeituraDeEvento> eventos, IReadOnlyList<Confirmacao> confirmacoes)
    {
        _eventos = eventos;
        _confirmacoes = confirmacoes;
    }

    public static HistoricoDaCasa De(IEnumerable<LeituraDeEvento> eventos, IEnumerable<Confirmacao> confirmacoes) =>
        new(eventos.OrderBy(e => e.OcorridoEm).ToList(), confirmacoes.OrderBy(c => c.Quando).ToList());

    public IReadOnlyList<Confirmacao> Confirmacoes => _confirmacoes;

    public HistoricoDaCasa Ate(DateTimeOffset agora) =>
        new(_eventos.Where(e => e.OcorridoEm <= agora).ToList(), _confirmacoes.Where(c => c.Quando <= agora).ToList());

    // Secao 5.3: no Sozinho conta movimento, botao e PessoaVista. No Acamado e no Coabitado, so PessoaVista.
    public bool TemSinalDeVidaDesde(DateTimeOffset instante, PerfilDeContato perfil) =>
        _confirmacoes.Any(c => c.Resultado == ResultadoDaConfirmacao.PessoaVista && c.Quando >= instante)
        || (perfil == PerfilDeContato.Sozinho && _eventos.Any(e => e.OcorridoEm >= instante && ESinalDeVida(e.Tipo)));

    public bool TeveSaidaAvisadaDesde(DateTimeOffset instante) =>
        _eventos.Any(e => e.Tipo == TipoDePacote.BotaoSair && e.OcorridoEm >= instante);

    public TensaoV? UltimaTensao => _eventos.LastOrDefault(e => e.TensaoV is not null)?.TensaoV;

    public DateTimeOffset? InicioDaSequenciaBaixa(TensaoV minima) =>
        InicioDaSequenciaAtual(_eventos.Where(e => e.TensaoV is not null).ToList(), e => e.TensaoV < minima);

    public DateTimeOffset? InicioDaSequenciaQuente(TempC limite) =>
        InicioDaSequenciaAtual(_eventos.Where(e => e.TempC is not null).ToList(), e => e.TempC >= limite);

    // A trava da regra de energia: a primeira leitura baixa sem confirmacao da ACS com PessoaVista depois dela.
    public DateTimeOffset? PrimeiraLeituraBaixaSemConfirmacao(TensaoV minima)
    {
        var ultimaVistaPelaAcs = _confirmacoes
            .Where(c => c.Origem == OrigemDaConfirmacao.Acs && c.Resultado == ResultadoDaConfirmacao.PessoaVista)
            .Select(c => (DateTimeOffset?)c.Quando)
            .LastOrDefault();
        return _eventos
            .Where(e => e.TensaoV < minima && (ultimaVistaPelaAcs is null || e.OcorridoEm >= ultimaVistaPelaAcs))
            .Select(e => (DateTimeOffset?)e.OcorridoEm)
            .FirstOrDefault();
    }

    private static bool ESinalDeVida(TipoDePacote tipo) =>
        tipo is TipoDePacote.Movimento or TipoDePacote.BotaoBem or TipoDePacote.BotaoSair;

    // Inicio da sequencia atual em que a condicao vale, sem interrupcao ate a ultima leitura. Usada pelas regras 1 e 3.
    private static DateTimeOffset? InicioDaSequenciaAtual(IReadOnlyList<LeituraDeEvento> leituras, Func<LeituraDeEvento, bool> condicao)
    {
        DateTimeOffset? inicio = null;
        for (var i = leituras.Count - 1; i >= 0 && condicao(leituras[i]); i--)
        {
            inicio = leituras[i].OcorridoEm;
        }

        return inicio;
    }
}
