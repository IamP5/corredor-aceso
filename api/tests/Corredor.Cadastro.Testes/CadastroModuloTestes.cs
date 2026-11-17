using Corredor.Cadastro;
using Corredor.Cadastro.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corredor.Cadastro.Testes;

public class CadastroModuloTestes
{
    [Fact]
    public void AddCadastro_registra_o_contexto_no_schema_cadastro_com_o_historico_dentro_dele()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:Cadastro"] = "Host=127.0.0.1;Database=sem_conexao";
        builder.AddCadastro();
        using var host = builder.Build();
        using var escopo = host.Services.CreateScope();
        var bd = escopo.ServiceProvider.GetRequiredService<CadastroDbContext>();

        var historico = bd.GetService<IHistoryRepository>().GetCreateIfNotExistsScript();

        Assert.Equal("cadastro", bd.Model.GetDefaultSchema());
        Assert.Contains("cadastro.\"__EFMigrationsHistory\"", historico);
        Assert.Contains("migration_id", historico);
    }
}
