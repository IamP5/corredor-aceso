using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Corredor.Api;

// Faz o Swagger mostrar o botão Authorize e mandar "Authorization: Bearer <token>".
internal sealed class EsquemaBearerNoSwagger : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument documento, OpenApiDocumentTransformerContext contexto, CancellationToken ct)
    {
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes[AutenticacaoDaAcs.Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Token da ACS, de Autenticacao:Acs na configuração.",
        };
        documento.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(AutenticacaoDaAcs.Esquema, documento)] = [] }];
        return Task.CompletedTask;
    }
}
