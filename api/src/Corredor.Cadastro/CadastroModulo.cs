using Corredor.Cadastro.Infra;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corredor.Cadastro;

public static class CadastroModulo
{
    public static IHostApplicationBuilder AddCadastro(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<CadastroDbContext>(opcoes => opcoes
            .UseNpgsql(builder.Configuration.GetConnectionString("Cadastro"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CadastroDbContext.Esquema))
            .UseSnakeCaseNamingConvention());
        // Toda verificação registrada entra no /healthz/ready do host.
        builder.Services.AddHealthChecks().AddDbContextCheck<CadastroDbContext>("bd-cadastro");
        return builder;
    }

    // Recebe o grupo /api do host: "/idosos" aqui vira /api/idosos.
    public static IEndpointRouteBuilder MapCadastro(this IEndpointRouteBuilder rotas) => rotas;

    // O host chama com `Corredor.Api --semear <arquivo>`, só em Development e Demo, e encerra depois.
    public static Task SemearCadastroAsync(this IServiceProvider servicos, string arquivo, CancellationToken ct) =>
        throw new NotImplementedException("--semear ainda não existe no Cadastro");
}
