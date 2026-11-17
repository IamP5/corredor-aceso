using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Corredor.Api;

// O JSON da Web API aceita número escrito como texto, e o .NET 10 publica todo parâmetro numérico como
// "integer ou string" com um pattern. O Swagger UI não valida esse tipo duplo e trata o campo como vazio,
// então o "Try it out" nunca envia. O parâmetro de rota e de query é lido como número, então o documento diz só número.
internal sealed class ParametroNumericoSemTexto : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema esquema, OpenApiSchemaTransformerContext contexto, CancellationToken ct)
    {
        if (contexto.ParameterDescription is not null
            && esquema.Type is { } tipo
            && tipo.HasFlag(JsonSchemaType.String)
            && (tipo.HasFlag(JsonSchemaType.Integer) || tipo.HasFlag(JsonSchemaType.Number)))
        {
            esquema.Type = tipo & ~JsonSchemaType.String;
            esquema.Pattern = null;
        }
        return Task.CompletedTask;
    }
}
