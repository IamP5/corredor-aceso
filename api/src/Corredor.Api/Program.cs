using System.Text.Json.Serialization;
using Corredor.Api;
using Corredor.Cadastro;
using Corredor.Contratos;
using Corredor.Guarda;
using Corredor.Motor;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

ConfiguracaoDoLlm.Aplicar(builder.Configuration, builder.Environment);

builder.Services.AddOpenApi(openapi => openapi.AddDocumentTransformer<EsquemaBearerNoSwagger>());
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Antes da Guarda: os IExceptionHandler rodam na ordem em que foram registrados.
builder.Services.AddExceptionHandler<TratadorDeRegraDeDominio>();
builder.Services.AddSingleton(builder.Configuration.GetRequiredSection("Relogio").Get<ParametrosDoRelogio>()!);

builder.AddAutenticacaoDaAcs();
builder.AddGuarda().AddMotor().AddCadastro();

// O Motor registra o RelogioDeDemo em Development. Sem ele, valem o relógio do sistema e deslocamento zero.
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.TryAddSingleton<IDeslocamentoDoRelogio, SemDeslocamento>();

var app = builder.Build();

if (args is ["--semear", var arquivo])
{
    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Demo"))
    {
        app.Logger.LogError("--semear só roda nos ambientes Development e Demo");
        return 2;
    }
    await app.Services.SemearCadastroAsync(arquivo, app.Lifetime.ApplicationStopping);
    return 0;
}

app.UseExceptionHandler();
app.UseGuarda();
app.UseSwaggerUI(swagger => swagger.SwaggerEndpoint("/openapi/v1.json", "Corredor Aceso"));
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/healthz/ready").AllowAnonymous();

var api = app.MapGroup("/api").ExigirMicroareaDaAcs().ComGuarda();
api.MapGet("/versao", () => Versao.Ler(app.Configuration)).AllowAnonymous();
api.MapMotor();
api.MapCadastro();
api.MapGuarda();

app.Run();
return 0;
