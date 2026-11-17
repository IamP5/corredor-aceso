using Corredor.Guarda.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corredor.Guarda;

public static class GuardaModulo
{
    // O host chama antes de AddMotor e AddCadastro, para o ProvedorDeLogSeguro envolver o log de todos.
    // Um IExceptionHandler registrado aqui roda depois do tratador de RegraDeDominioViolada do host.
    public static IHostApplicationBuilder AddGuarda(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<GuardaDbContext>(opcoes => opcoes
            .UseNpgsql(builder.Configuration.GetConnectionString("Guarda"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", GuardaDbContext.Esquema))
            .UseSnakeCaseNamingConvention());
        // Toda verificação registrada entra no /healthz/ready do host.
        builder.Services.AddHealthChecks().AddDbContextCheck<GuardaDbContext>("bd-guarda");
        builder.Services.AddSingleton<ISaidaSegura, SaidaSegura>();
        builder.Services.AddSingleton<IMotorDeParsing, MotorDeParsingProvisorio>();
        return builder;
    }

    // Middlewares da Guarda, como o InterceptadorDeEntrada. Rodam em toda requisição, depois do UseExceptionHandler do host.
    public static IApplicationBuilder UseGuarda(this IApplicationBuilder app) => app;

    // Filtros de endpoint, como o FiltroDeResposta, valem para as rotas de todos os módulos.
    // Por isso a Guarda recebe o grupo /api inteiro, e não só as próprias rotas.
    public static RouteGroupBuilder ComGuarda(this RouteGroupBuilder api) => api;

    // Recebe o grupo /api do host: "/painel" aqui vira /api/painel.
    public static IEndpointRouteBuilder MapGuarda(this IEndpointRouteBuilder rotas) => rotas;
}
