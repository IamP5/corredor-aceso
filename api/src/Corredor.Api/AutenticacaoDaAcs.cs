using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Corredor.Contratos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Corredor.Api;

// Uma ACS da configuração: Autenticacao:Acs:<n>:Id, :Microarea e :Token. Não existe tabela de ACS.
public sealed record AcsCadastrada(string Id, int Microarea, string Token);

// O modelo de domínio não define a autenticação. O modelo de dados só exige um AcsId no formato
// ^[a-z0-9-]{3,40}$. Um token fixo por ACS, lido da configuração, cumpre isso sem banco.
internal static partial class AutenticacaoDaAcs
{
    public const string Esquema = "Bearer";
    public const string ClaimAcs = "acs";
    public const string ClaimMicroarea = "microarea";

    [GeneratedRegex("^[a-z0-9-]{3,40}$")]
    private static partial Regex FormatoDoAcsId();

    // Toda rota exige a ACS, menos as que pedem AllowAnonymous. Rota nova nasce fechada.
    public static IHostApplicationBuilder AddAutenticacaoDaAcs(this IHostApplicationBuilder builder)
    {
        var acs = builder.Configuration.GetSection("Autenticacao:Acs").Get<AcsCadastrada[]>() ?? [];
        foreach (var a in acs)
        {
            if (!FormatoDoAcsId().IsMatch(a.Id) || a.Microarea <= 0 || a.Token.Length < 16)
                throw new InvalidOperationException($"Autenticacao:Acs inválida para '{a.Id}': id fora do formato, microárea ou token com menos de 16 caracteres");
        }
        builder.Services.AddSingleton<IReadOnlyList<AcsCadastrada>>(acs);
        builder.Services.AddAuthentication(Esquema).AddScheme<AuthenticationSchemeOptions, Tratador>(Esquema, null);
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(Esquema).RequireAuthenticatedUser().Build());
        return builder;
    }

    // "O host confere se a microárea da rota é a da ACS autenticada", seção 6.3 de 03-modelo-de-dominio.md.
    public static RouteGroupBuilder ExigirMicroareaDaAcs(this RouteGroupBuilder api) =>
        api.AddEndpointFilter((contexto, proximo) =>
        {
            var http = contexto.HttpContext;
            var daRota = http.Request.Query["microarea"].ToString();
            var daAcs = http.User.FindFirstValue(ClaimMicroarea);
            if (daRota.Length > 0 && daAcs is not null && daRota != daAcs)
                throw new RegraDeDominioViolada("MICROAREA_DE_OUTRA_ACS", "Esta microárea não é a sua.", TipoDeViolacao.Proibida);
            return proximo(contexto);
        });

    private sealed class Tratador(
        IOptionsMonitor<AuthenticationSchemeOptions> opcoes, ILoggerFactory log, UrlEncoder codificador, IReadOnlyList<AcsCadastrada> acs)
        : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, log, codificador)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var cabecalho = Request.Headers.Authorization.ToString();
            if (!cabecalho.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Task.FromResult(AuthenticateResult.NoResult());

            var recebido = Encoding.UTF8.GetBytes(cabecalho["Bearer ".Length..].Trim());
            var dona = acs.FirstOrDefault(a => CryptographicOperations.FixedTimeEquals(recebido, Encoding.UTF8.GetBytes(a.Token)));
            if (dona is null) return Task.FromResult(AuthenticateResult.Fail("token desconhecido"));

            var identidade = new ClaimsIdentity(
                [new Claim(ClaimAcs, dona.Id), new Claim(ClaimMicroarea, dona.Microarea.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                Esquema, ClaimAcs, null);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
        }
    }
}
