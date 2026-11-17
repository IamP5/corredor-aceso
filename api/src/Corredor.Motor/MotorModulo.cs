using Corredor.Contratos;
using Corredor.Motor.Aplicacao;
using Corredor.Motor.Dominio;
using Corredor.Motor.Infra;
using Corredor.Motor.Rotas;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Corredor.Motor;

public static class MotorModulo
{
    public static IHostApplicationBuilder AddMotor(this IHostApplicationBuilder builder)
    {
        var servicos = builder.Services;
        servicos.AddDbContext<MotorDbContext>(opcoes => opcoes
            .UseNpgsql(builder.Configuration.GetConnectionString("Motor"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MotorDbContext.Esquema))
            .UseSnakeCaseNamingConvention());

        servicos.AddOptions<ParametrosDoMotor>()
            .Bind(builder.Configuration.GetSection(ParametrosDoMotor.Secao))
            .Validate(p => p.Coerente, "ParametrosDoMotor incoerente: SilencioMaximoEletrodependente precisa ser o dobro do heartbeat, e as horas em ordem")
            .ValidateOnStart();
        servicos.AddSingleton(sp => sp.GetRequiredService<IOptions<ParametrosDoMotor>>().Value);

        // Em Development, o RelogioDeDemo e o relogio do processo. Fora dele, o host registra TimeProvider.System e deslocamento zero.
        if (builder.Environment.IsDevelopment())
        {
            servicos.AddSingleton(sp => new RelogioDeDemo(TimeProvider.System));
            servicos.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<RelogioDeDemo>());
            servicos.AddSingleton<IDeslocamentoDoRelogio>(sp => sp.GetRequiredService<RelogioDeDemo>());
        }

        servicos.AddSingleton<RegraDeContato, EnergiaEletrodependente>();
        servicos.AddSingleton<RegraDeContato, SemHeartbeat>();
        servicos.AddSingleton<RegraDeContato, CalorInterno>();
        servicos.AddSingleton<RegraDeContato, SemMovimentoNaJanela>();
        servicos.AddSingleton<AvaliadorDeCasas>();

        servicos.AddScoped<IMotorDeCasas, MotorDeCasas>();
        servicos.AddScoped<RegistrarPacote>();
        servicos.AddScoped<AvaliacaoDasCasas>();
        servicos.AddScoped<ConsultarRadar>();
        servicos.AddScoped<ConsultarLinhaDoTempo>();
        servicos.AddScoped<DeclararDiaCritico>();
        servicos.AddScoped<EncerrarTurno>();
        servicos.AddScoped<CicloDoVigia>();
        servicos.TryAddScoped<IReceptorDeContato, SemReceptorDeContato>();
        servicos.AddScoped<ExpurgarDados>();

        servicos.AddSingleton(OpcoesDoMqtt.De(builder.Configuration));
        servicos.AddSingleton<ConexaoMqtt>();
        servicos.AddSingleton<IPublicadorDeComandos, PublicadorDeComandos>();
        servicos.AddHostedService<ConsumidorMqtt>();
        servicos.AddHostedService<VigiaDoRadar>();
        servicos.AddHostedService<Expurgo>();

        // Toda verificacao registrada entra no /healthz/ready do host.
        servicos.AddHealthChecks()
            .AddDbContextCheck<MotorDbContext>("bd-motor")
            .AddCheck<SaudeDoMqtt>("mqtt-motor");
        return builder;
    }

    // Recebe o grupo /api do host: "/radar" aqui vira /api/radar.
    public static IEndpointRouteBuilder MapMotor(this IEndpointRouteBuilder rotas) =>
        rotas.MapearRotas(rotas.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment());
}
