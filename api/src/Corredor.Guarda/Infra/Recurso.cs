using System.Reflection;

namespace Corredor.Guarda.Infra;

internal static class Recurso
{
    public static string Ler(string nome)
    {
        using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream(nome)
            ?? throw new InvalidOperationException($"recurso ausente: {nome}");
        using var leitor = new StreamReader(fluxo);
        return leitor.ReadToEnd();
    }
}
