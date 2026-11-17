using Corredor.Cadastro.Aplicacao;
using Corredor.Cadastro.Infra;
using Corredor.Cadastro.Rotas;
using Corredor.Contratos;
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

        builder.Services.AddScoped<CasosDoIdoso>();
        builder.Services.AddScoped<CasosDaVisita>();
        builder.Services.AddScoped<CasosDoAviso>();
        builder.Services.AddScoped<Leituras>();
        // P19. AddScoped, não TryAdd: o Motor registra antes um SemReceptorDeContato com TryAddScoped, e este vence.
        builder.Services.AddScoped<IReceptorDeContato, ReceptorDeContato>();
        // P22: reenvia as casas ativas ao Motor ao iniciar.
        builder.Services.AddHostedService<ReconciliacaoComMotor>();
        return builder;
    }

    // Recebe o grupo /api do host: "/idosos" aqui vira /api/idosos.
    public static IEndpointRouteBuilder MapCadastro(this IEndpointRouteBuilder rotas) => RotasDoCadastro.Mapear(rotas);

    // O host chama com `Corredor.Api --semear <arquivo>`, só em Development e Demo, e encerra depois.
    public static Task SemearCadastroAsync(this IServiceProvider servicos, string arquivo, CancellationToken ct) =>
        Semeadura.SemearAsync(servicos, arquivo, ct);
}
