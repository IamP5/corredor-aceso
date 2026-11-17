using Corredor.Contratos;

namespace Corredor.Cadastro.Dominio;

// Referência. Entra pela migração Inicial e não tem comando na entrega.
public sealed class Microarea
{
    private Microarea() { }

    public MicroareaId Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public AcsId AcsResponsavel { get; private set; }
}
