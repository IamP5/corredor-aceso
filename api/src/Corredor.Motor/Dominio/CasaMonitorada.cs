using Corredor.Contratos;

namespace Corredor.Motor.Dominio;

// A casa vista pelo Motor: token, microarea, perfil e ultimo pacote. Nunca nome, endereco ou texto livre.
public sealed class CasaMonitorada
{
    private readonly List<Confirmacao> _confirmacoes = [];

    private CasaMonitorada() { }

    public CasaToken Token { get; private set; }
    public MicroareaId Microarea { get; private set; }
    public PerfilDaCasa Perfil { get; private set; } = null!;
    public DateTimeOffset AtivadaEm { get; private set; }
    public DateTimeOffset? DesativadaEm { get; private set; }
    public DateTimeOffset? UltimoPacoteEm { get; private set; }
    public IReadOnlyCollection<Confirmacao> Confirmacoes => _confirmacoes;

    public bool Ativa => DesativadaEm is null;

    public static CasaMonitorada Ativar(CasaToken token, MicroareaId microarea, PerfilDaCasa perfil, DateTimeOffset agora) =>
        new() { Token = token, Microarea = microarea, Perfil = perfil, AtivadaEm = agora };

    // Tambem e o caminho de ativar de novo uma casa ativa: so o perfil muda. A desativacao e definitiva.
    public void DefinirPerfil(PerfilDaCasa perfil)
    {
        ExigirAtiva();
        Perfil = perfil;
    }

    // Invariante 2: UltimoPacoteEm so anda para frente e recebe a hora do servidor.
    public void RegistrarPacote(DateTimeOffset recebidoEm)
    {
        ExigirAtiva();
        if (UltimoPacoteEm is null || recebidoEm > UltimoPacoteEm)
        {
            UltimoPacoteEm = recebidoEm;
        }
    }

    // Devolve falso quando o Id ja existe: a repeticao e ignorada (invariante 4).
    public bool Confirmar(Confirmacao confirmacao, DateTimeOffset agora, TimeSpan toleranciaDoRelogio, TimeSpan retencao)
    {
        ExigirAtiva();
        if (_confirmacoes.Any(c => c.Id == confirmacao.Id))
        {
            return false;
        }

        if (confirmacao.Origem == OrigemDaConfirmacao.Vizinho && confirmacao.Resultado == ResultadoDaConfirmacao.AusenciaConfirmada)
        {
            throw Violacoes.CampoInvalido("O vizinho so confirma que viu a pessoa.");
        }

        if (confirmacao.Quando > agora + toleranciaDoRelogio)
        {
            throw Violacoes.ConfirmacaoNoFuturo();
        }

        if (confirmacao.Quando < agora - retencao)
        {
            throw Violacoes.ConfirmacaoVencida();
        }

        _confirmacoes.Add(confirmacao);
        return true;
    }

    // Repetir nao muda nada. O caso de uso apaga os eventos da casa na mesma transacao.
    public void Desativar(DateTimeOffset agora)
    {
        DesativadaEm ??= agora;
        _confirmacoes.Clear();
    }

    private void ExigirAtiva()
    {
        if (!Ativa)
        {
            throw Violacoes.CasaDesativada();
        }
    }
}
