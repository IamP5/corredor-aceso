namespace Corredor.Api;

// A Guarda lê a seção Llm: ApiKey, Modelo e UrlBase. Na VM, a chave vem de Llm__ApiKey no segredos.env.
// No notebook, vem dos nomes OPENROUTER_* do .env da raiz, que só é lido em Development.
// Ordem: Llm__* no ambiente, depois OPENROUTER_* no ambiente, depois o .env, depois o padrão.
internal static class ConfiguracaoDoLlm
{
    private static readonly (string Chave, string Variavel, string? Padrao)[] Mapa =
    [
        ("Llm:ApiKey", "OPENROUTER_API_KEY", null),
        ("Llm:Modelo", "OPENROUTER_MODEL", "openai/gpt-6-luna"),
        ("Llm:UrlBase", "OPENROUTER_BASE_URL", "https://openrouter.ai/api/v1"),
    ];

    public static void Aplicar(IConfigurationManager configuracao, IHostEnvironment ambiente)
    {
        var arquivo = ambiente.IsDevelopment() ? LerEnvDoRepositorio(ambiente.ContentRootPath) : [];
        var valores = new Dictionary<string, string?>();
        foreach (var (chave, variavel, padrao) in Mapa)
        {
            if (!string.IsNullOrEmpty(configuracao[chave])) continue;
            var valor = Environment.GetEnvironmentVariable(variavel) ?? arquivo.GetValueOrDefault(variavel) ?? padrao;
            if (!string.IsNullOrEmpty(valor)) valores[chave] = valor;
        }
        configuracao.AddInMemoryCollection(valores);
    }

    // A raiz do repositório é a pasta do global.json. O .env de lá nunca entra no Git.
    private static Dictionary<string, string> LerEnvDoRepositorio(string inicio)
    {
        for (var pasta = new DirectoryInfo(inicio); pasta is not null; pasta = pasta.Parent)
        {
            if (!File.Exists(Path.Combine(pasta.FullName, "global.json"))) continue;
            var env = Path.Combine(pasta.FullName, ".env");
            return File.Exists(env) ? Ler(env) : [];
        }
        return [];
    }

    private static Dictionary<string, string> Ler(string arquivo)
    {
        var valores = new Dictionary<string, string>();
        foreach (var linha in File.ReadLines(arquivo).Select(l => l.Trim()))
        {
            if (linha.Length == 0 || linha.StartsWith('#')) continue;
            var igual = linha.IndexOf('=');
            if (igual <= 0) continue;
            valores[linha[..igual].Trim()] = linha[(igual + 1)..].Trim().Trim('"', '\'');
        }
        return valores;
    }
}
