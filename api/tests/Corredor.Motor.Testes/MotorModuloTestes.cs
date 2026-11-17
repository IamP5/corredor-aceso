using Corredor.Motor;
using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corredor.Motor.Testes;

public class MotorModuloTestes
{
    [Fact]
    public void AddMotor_registra_o_contexto_no_schema_motor_com_o_historico_dentro_dele()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:Motor"] = "Host=127.0.0.1;Database=sem_conexao";
        builder.AddMotor();
        using var host = builder.Build();
        using var escopo = host.Services.CreateScope();
        var bd = escopo.ServiceProvider.GetRequiredService<MotorDbContext>();

        var historico = bd.GetService<IHistoryRepository>().GetCreateIfNotExistsScript();

        Assert.Equal("motor", bd.Model.GetDefaultSchema());
        Assert.Contains("motor.\"__EFMigrationsHistory\"", historico);
        Assert.Contains("migration_id", historico);
    }
}
