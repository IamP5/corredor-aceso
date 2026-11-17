using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Corredor.Guarda.Rotas;

/// <summary>Lê o modelo da rota do endpoint da requisição. Nunca o caminho com valores.</summary>
public sealed class RotaAtual(IHttpContextAccessor acesso) : IRotaAtual
{
    public string? Modelo => ModeloDe(acesso.HttpContext);

    public string? Modulo => Modelo is { } m ? ModuloDaRota(m) : null;

    public static string? ModeloDe(HttpContext? contexto)
    {
        if (contexto?.GetEndpoint() is not RouteEndpoint endpoint)
        {
            return null;
        }

        var modelo = endpoint.RoutePattern.RawText;
        if (string.IsNullOrEmpty(modelo))
        {
            return null;
        }

        return modelo.StartsWith('/') ? modelo : "/" + modelo;
    }

    // O dono de cada prefixo de rota, pela tabela de rotas de 03-modelo-de-dominio.md 6.3.
    private static readonly (string Prefixo, string Modulo)[] Donos =
    [
        ("/api/dias-criticos", "Motor"),
        ("/api/radar", "Motor"),
        ("/api/casas", "Motor"),
        ("/api/demo", "Motor"),
        ("/api/idosos", "Cadastro"),
        ("/api/lista-de-nomes", "Cadastro"),
        ("/api/visitas", "Cadastro"),
        ("/api/mensagens", "Cadastro"),
        ("/api/avisos", "Cadastro"),
        ("/api/painel", "Guarda"),
        ("/api/guarda", "Guarda"),
    ];

    public static string ModuloDaRota(string modelo) =>
        Donos.FirstOrDefault(d => modelo.StartsWith(d.Prefixo, StringComparison.OrdinalIgnoreCase)).Modulo ?? "Api";
}
