using Corredor.Motor.Infra;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corredor.Motor;

public static class MotorModulo
{
    public static IHostApplicationBuilder AddMotor(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<MotorDbContext>(opcoes => opcoes
            .UseNpgsql(builder.Configuration.GetConnectionString("Motor"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MotorDbContext.Esquema))
            .UseSnakeCaseNamingConvention());
        // Toda verificação registrada entra no /healthz/ready do host.
        builder.Services.AddHealthChecks().AddDbContextCheck<MotorDbContext>("bd-motor");
        return builder;
    }

    // Recebe o grupo /api do host: "/radar" aqui vira /api/radar.
    public static IEndpointRouteBuilder MapMotor(this IEndpointRouteBuilder rotas) => rotas;
}
