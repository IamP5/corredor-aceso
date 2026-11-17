using System.Text.RegularExpressions;

namespace Corredor.Cadastro.Dominio;

public readonly record struct IdosoId(Guid Valor)
{
    public static IdosoId Novo() => new(Guid.NewGuid());

    public override string ToString() => Valor.ToString();
}

// Código da ACS que vem da autenticação, como acs-demo-01. Não existe tabela de ACS.
public readonly partial record struct AcsId(string Valor)
{
    public static bool TryParse(string? texto, out AcsId acs)
    {
        var valido = texto is not null && Formato().IsMatch(texto);
        acs = valido ? new AcsId(texto!) : default;
        return valido;
    }

    public override string ToString() => Valor;

    [GeneratedRegex("^[a-z0-9-]{3,40}$")]
    private static partial Regex Formato();
}

public sealed record Autorizacoes(bool AvisarVizinho, bool AvisarFamilia);

public enum DestinatarioDoAviso { Vizinho, Familia }

public enum Desfecho { EstavaBem, EstavaFora, NaoAbriu, Encaminhada }

public enum RespostaDoVizinho { EstaBem, PrecisaDeAjuda }

public enum VereditoDaMensagem { Liberada, Bloqueada }
