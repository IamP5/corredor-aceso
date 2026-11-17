using Microsoft.EntityFrameworkCore;

namespace Corredor.Cadastro.Infra;

public sealed class CadastroDbContext(DbContextOptions<CadastroDbContext> opcoes) : DbContext(opcoes)
{
    public const string Esquema = "cadastro";

    protected override void OnModelCreating(ModelBuilder modelo) => modelo.HasDefaultSchema(Esquema);
}
