using Corredor.Guarda;
using Corredor.Guarda.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corredor.Guarda.Testes;

public class GuardaModuloTestes
{
    [Fact]
    public void AddGuarda_registra_o_contexto_no_schema_guarda_com_o_historico_dentro_dele()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:Guarda"] = "Host=127.0.0.1;Database=sem_conexao";
        builder.AddGuarda();
        using var host = builder.Build();
        using var escopo = host.Services.CreateScope();
        var bd = escopo.ServiceProvider.GetRequiredService<GuardaDbContext>();

        var historico = bd.GetService<IHistoryRepository>().GetCreateIfNotExistsScript();

        Assert.Equal("guarda", bd.Model.GetDefaultSchema());
        Assert.Contains("guarda.\"__EFMigrationsHistory\"", historico);
        Assert.Contains("migration_id", historico);
    }
}
