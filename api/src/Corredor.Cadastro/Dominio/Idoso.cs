using Corredor.Contratos;
using Corredor.Guarda;

namespace Corredor.Cadastro.Dominio;

public sealed record IdosoCadastrado(IdosoId Idoso);

public sealed record ConsentimentoRegistrado(IdosoId Idoso, CasaToken Token, MicroareaId Microarea, PerfilDaCasa Perfil, bool Novo);

public sealed record ConsentimentoRevogado(IdosoId Idoso, CasaToken Token);

public sealed record PerfilDaCasaAlterado(IdosoId Idoso, CasaToken? Token, PerfilDaCasa Perfil);

public sealed class Casa
{
    private Casa() { }

    internal Casa(TextoRotulado endereco) => Endereco = endereco;

    public TextoRotulado Endereco { get; private set; } = null!;
    public CasaToken? Token { get; private set; }

    internal void ReceberToken(CasaToken token) => Token = token;

    internal void ApagarToken() => Token = null;
}

public sealed class Consentimento
{
    private Consentimento() { }

    internal Consentimento(AcsId colhidoPor, string versaoDoTermo, Autorizacoes autorizacoes, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        ColhidoPor = colhidoPor;
        ColhidoEm = agora;
        VersaoDoTermo = versaoDoTermo;
        Autorizacoes = autorizacoes;
    }

    public Guid Id { get; private set; }
    public AcsId ColhidoPor { get; private set; }
    public DateTimeOffset ColhidoEm { get; private set; }
    public string VersaoDoTermo { get; private set; } = null!;
    public Autorizacoes Autorizacoes { get; private set; } = null!;
    public DateTimeOffset? RevogadoEm { get; private set; }

    internal void Revogar(DateTimeOffset agora) => RevogadoEm ??= agora;
}

public sealed class VizinhoDeConfianca
{
    private VizinhoDeConfianca() { }

    internal VizinhoDeConfianca(TextoRotulado nome, TextoRotulado telefone, DateTimeOffset aceitouEm)
    {
        Nome = nome;
        Telefone = telefone;
        AceitouEm = aceitouEm;
    }

    public TextoRotulado Nome { get; private set; } = null!;
    public TextoRotulado Telefone { get; private set; } = null!;
    public DateTimeOffset AceitouEm { get; private set; }
}

// Raiz do agregado. O idoso tem token se, e somente se, tem consentimento vigente (invariantes 1 e 8).
// O gatilho idoso_token_com_consentimento confere a mesma regra no COMMIT.
public sealed class Idoso
{
    private readonly List<Consentimento> _consentimentos = [];

    private Idoso() { }

    public IdosoId Id { get; private set; }
    public MicroareaId Microarea { get; private set; }
    public TextoRotulado Nome { get; private set; } = null!;
    public TextoRotulado? Condicoes { get; private set; }
    public TextoRotulado? Medicamentos { get; private set; }
    public PerfilDaCasa Perfil { get; private set; } = null!;
    public Casa Casa { get; private set; } = null!;
    public IReadOnlyCollection<Consentimento> Consentimentos => _consentimentos;
    public VizinhoDeConfianca? Vizinho { get; private set; }
    public uint Versao { get; private set; }

    public Consentimento? ConsentimentoVigente => _consentimentos.SingleOrDefault(c => c.RevogadoEm is null);

    public CasaToken? TokenAtivo => Casa.Token;

    public bool PodeAvisarVizinho => ConsentimentoVigente?.Autorizacoes.AvisarVizinho == true && Vizinho is not null;

    // O rótulo vem do campo, não do conteúdo (invariante 9).
    public static (Idoso Idoso, IdosoCadastrado Evento) Cadastrar(
        IdosoId id, MicroareaId microarea, string nome, string endereco, PerfilDaCasa perfil, string? condicoes, string? medicamentos)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw Erros.NomeObrigatorio();
        }

        if (string.IsNullOrWhiteSpace(endereco))
        {
            throw Erros.EnderecoObrigatorio();
        }

        var idoso = new Idoso
        {
            Id = id,
            Microarea = microarea,
            Nome = TextoRotulado.Do(nome, TipoDeDado.Nome),
            Casa = new Casa(TextoRotulado.Do(endereco, TipoDeDado.Endereco)),
            Perfil = perfil,
            Condicoes = Opcional(condicoes, TipoDeDado.Saude),
            Medicamentos = Opcional(medicamentos, TipoDeDado.Saude),
        };
        return (idoso, new IdosoCadastrado(id));
    }

    // Com consentimento vigente, devolve o mesmo token e o caso de uso repete a ativação no Motor (invariante 5).
    public ConsentimentoRegistrado RegistrarConsentimento(AcsId acs, string versaoDoTermo, Autorizacoes autorizacoes, DateTimeOffset agora)
    {
        var novo = ConsentimentoVigente is null;
        if (novo)
        {
            if (string.IsNullOrWhiteSpace(versaoDoTermo))
            {
                throw Erros.CampoInvalido("versaoDoTermo");
            }

            _consentimentos.Add(new Consentimento(acs, versaoDoTermo, autorizacoes, agora));
            Casa.ReceberToken(CasaToken.Novo());
        }

        return new ConsentimentoRegistrado(Id, Casa.Token!.Value, Microarea, Perfil, novo);
    }

    // Sem consentimento vigente, não faz nada e devolve null.
    public ConsentimentoRevogado? RevogarConsentimento(DateTimeOffset agora)
    {
        if (ConsentimentoVigente is not { } vigente)
        {
            return null;
        }

        var token = Casa.Token!.Value;
        vigente.Revogar(agora);
        Casa.ApagarToken();
        Vizinho = null;
        return new ConsentimentoRevogado(Id, token);
    }

    // Indicar de novo substitui o vizinho (invariante 6).
    public void IndicarVizinho(string nome, string telefone, DateTimeOffset aceitouEm)
    {
        if (ConsentimentoVigente?.Autorizacoes.AvisarVizinho != true)
        {
            throw Erros.AutorizacaoAusente();
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw Erros.NomeObrigatorio();
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw Erros.CampoInvalido("telefone");
        }

        Vizinho = new VizinhoDeConfianca(
            TextoRotulado.Do(nome, TipoDeDado.Nome), TextoRotulado.Do(telefone, TipoDeDado.Telefone), aceitouEm);
    }

    public PerfilDaCasaAlterado AlterarPerfil(PerfilDaCasa perfil)
    {
        Perfil = perfil;
        return new PerfilDaCasaAlterado(Id, Casa.Token, perfil);
    }

    // A família nunca aparece sem a autorização AvisarFamilia (invariante 7).
    public IReadOnlyList<DestinatarioDoAviso> DestinatariosDoAviso()
    {
        var destinatarios = new List<DestinatarioDoAviso>();
        if (PodeAvisarVizinho)
        {
            destinatarios.Add(DestinatarioDoAviso.Vizinho);
        }

        if (ConsentimentoVigente?.Autorizacoes.AvisarFamilia == true)
        {
            destinatarios.Add(DestinatarioDoAviso.Familia);
        }

        return destinatarios;
    }

    private static TextoRotulado? Opcional(string? texto, TipoDeDado rotulo) =>
        string.IsNullOrWhiteSpace(texto) ? null : TextoRotulado.Do(texto, rotulo);
}
