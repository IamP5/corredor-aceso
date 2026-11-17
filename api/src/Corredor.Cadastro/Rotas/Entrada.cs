using System.Security.Claims;
using Corredor.Cadastro.Dominio;
using Corredor.Contratos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Corredor.Cadastro.Rotas;

// A fronteira HTTP do Cadastro: converte texto em tipo do domínio e confere os limites da seção 7.2
// de 04-modelo-de-dados.md. Dentro dos casos de uso, os tipos já valem.
internal static class Entrada
{
    private static readonly TimeSpan FusoDeSaoPaulo = TimeSpan.FromHours(-3);

    // As horas saem com o fuso de São Paulo, que não tem horário de verão desde 2019.
    public static DateTimeOffset Hora(DateTimeOffset utc) => utc.ToOffset(FusoDeSaoPaulo);

    public static DateTimeOffset? Hora(DateTimeOffset? utc) => utc is { } h ? Hora(h) : null;

    public static string Texto(string? valor, string campo, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > maximo)
        {
            throw Erros.CampoInvalido(campo);
        }

        return valor;
    }

    public static string? Opcional(string? valor, string campo, int maximo) =>
        valor is { Length: var n } && n > maximo ? throw Erros.CampoInvalido(campo) : valor;

    public static T Enum<T>(string? valor, string campo) where T : struct, System.Enum =>
        valor is not null && !int.TryParse(valor, out _) && System.Enum.TryParse<T>(valor, ignoreCase: false, out var resultado)
        && System.Enum.IsDefined(resultado)
            ? resultado
            : throw Erros.CampoInvalido(campo);

    public static PerfilDaCasa Perfil(PerfilDaCasaHttp? perfil, string campo) =>
        perfil is null
            ? throw Erros.CampoInvalido(campo)
            : new PerfilDaCasa(Enum<PerfilDeContato>(perfil.Contato, $"{campo}.contato"), perfil.Eletrodependente);

    public static MicroareaId Microarea(int? valor, string campo) =>
        valor is > 0 ? new MicroareaId(valor.Value) : throw Erros.CampoInvalido(campo);

    public static CasaToken Token(string? valor, string campo) =>
        CasaToken.TryParse(valor, out var token) ? token : throw Erros.CampoInvalido(campo);

    // A ACS vem da autenticação do host, pelo claim "acs". Nunca do corpo.
    public static AcsId? Acs(HttpContext http) =>
        AcsId.TryParse(http.User.FindFirstValue("acs"), out var acs) ? acs : null;

    // A rota que grava quem fez exige a ACS autenticada. Sem ela, 401.
    public static RouteHandlerBuilder ExigeAcs(this RouteHandlerBuilder rota) =>
        rota.AddEndpointFilter(async (contexto, proximo) =>
                Acs(contexto.HttpContext) is null ? Results.Unauthorized() : await proximo(contexto))
            .Produces(StatusCodes.Status401Unauthorized);
}

public sealed record PerfilDaCasaHttp(string? Contato, bool Eletrodependente);
