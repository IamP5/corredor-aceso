using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Corredor.Cadastro.Infra;

// O dotnet ef usa esta fábrica no lugar do host. Gerar migração e script não depende de outro módulo estar registrado
// nem de banco: as opções são as mesmas do AddCadastro, e o script não conecta.
internal sealed class FabricaDoCadastro : IDesignTimeDbContextFactory<CadastroDbContext>
{
    public CadastroDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CadastroDbContext>()
            .UseNpgsql(
                Environment.GetEnvironmentVariable("ConnectionStrings__Cadastro") ?? "Host=127.0.0.1;Database=sem_conexao",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CadastroDbContext.Esquema))
            .UseSnakeCaseNamingConvention()
            .Options);
}
