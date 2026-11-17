using System.Reflection;

namespace Corredor.Api;

public sealed record RespostaDaVersao(string Versao, string Commit, string Ambiente);

internal static class Versao
{
    // O empacotar.sh passa -p:Version e -p:SourceRevisionId. O SDK junta os dois como "0.3.0+<sha>".
    // O deploy.sh compara "versao" com a tag sem o "v", então o commit sai em campo próprio.
    private static readonly string[] Partes = typeof(Versao).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion.Split('+', 2);

    public static RespostaDaVersao Ler(IConfiguration configuracao) => new(
        Partes[0],
        Partes.Length > 1 ? Partes[1] : "desconhecido",
        configuracao["Corredor:Ambiente"] ?? "desconhecido");
}
