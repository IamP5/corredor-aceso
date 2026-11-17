using Corredor.Guarda.Infra;
using Corredor.Guarda.Rotas;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corredor.Guarda;

public static class GuardaModulo
{
    // O host chama antes de AddMotor e AddCadastro. O ProvedorDeLogSeguro envolve a fábrica de log de todos.
    // Um IExceptionHandler registrado aqui roda depois do tratador de RegraDeDominioViolada do host.
    public static IHostApplicationBuilder AddGuarda(this IHostApplicationBuilder builder)
    {
        var servicos = builder.Services;
        servicos.AddDbContext<GuardaDbContext>(opcoes => opcoes
            .UseNpgsql(builder.Configuration.GetConnectionString("Guarda"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", GuardaDbContext.Esquema))
            .UseSnakeCaseNamingConvention());
        // Toda verificação registrada entra no /healthz/ready do host.
        servicos.AddHealthChecks().AddDbContextCheck<GuardaDbContext>("bd-guarda");

        servicos.AddHttpContextAccessor();
        servicos.AddSingleton(RegrasDePii.Padrao);
        servicos.AddSingleton<MotorDeParsing>();
        servicos.AddSingleton<IMotorDeParsing>(sp => sp.GetRequiredService<MotorDeParsing>());
        servicos.AddSingleton<IRotaAtual, RotaAtual>();
        servicos.AddSingleton<FilaDeSeguranca>();
        servicos.AddSingleton<SaidaSegura>();
        servicos.AddSingleton<ISaidaSegura>(sp => sp.GetRequiredService<SaidaSegura>());
        servicos.AddScoped<IArmazemDeEventos, ArmazemPostgres>();
        servicos.AddScoped<Painel>();
        servicos.AddHostedService<ProcessadorDeEventos>();

        servicos.Configure<OpcoesDoLlm>(builder.Configuration.GetSection("Llm"));
        // O Tradutor tem limite próprio de 5 segundos. O do HttpClient fica acima para não competir.
        servicos.AddHttpClient<Tradutor>(http => http.Timeout = TimeSpan.FromSeconds(30));

        servicos.AddExceptionHandler<TratadorDeErros>();
        servicos.AddScoped<FiltroDeResposta>();
        EnvolverOLog(servicos);
        return builder;
    }

    // InterceptadorDeEntrada: roda em toda requisição, depois do UseExceptionHandler do host e antes de qualquer log do corpo.
    public static IApplicationBuilder UseGuarda(this IApplicationBuilder app) =>
        app.UseMiddleware<InterceptadorDeEntrada>();

    // O FiltroDeResposta vale para as rotas dos três módulos, porque recebe o grupo /api inteiro.
    public static RouteGroupBuilder ComGuarda(this RouteGroupBuilder api) =>
        api.AddEndpointFilter<FiltroDeResposta>();

    // Recebe o grupo /api do host: "/painel" aqui vira /api/painel.
    public static IEndpointRouteBuilder MapGuarda(this IEndpointRouteBuilder rotas)
    {
        RotasDaGuarda.MapPainel(rotas);
        if (rotas.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            RotasDaGuarda.MapDemonstracao(rotas);
        }

        return rotas;
    }

    // Troca a ILoggerFactory do host pelo ProvedorDeLogSeguro em volta dela. Vale para todo
    // provedor, inclusive os registrados depois de AddGuarda, e verifica cada linha uma vez só.
    private static void EnvolverOLog(IServiceCollection servicos)
    {
        var original = servicos.Last(d => d.ServiceType == typeof(ILoggerFactory) && !d.IsKeyedService);
        servicos.Remove(original);
        servicos.Add(ServiceDescriptor.Describe(
            typeof(ILoggerFactory),
            sp => new ProvedorDeLogSeguro(
                (ILoggerFactory)(original.ImplementationInstance
                    ?? original.ImplementationFactory?.Invoke(sp)
                    ?? ActivatorUtilities.CreateInstance(sp, original.ImplementationType!)),
                sp.GetRequiredService<SaidaSegura>,
                sp.GetRequiredService<MotorDeParsing>),
            original.Lifetime));
    }
}
