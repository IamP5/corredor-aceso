using Microsoft.EntityFrameworkCore;

namespace Corredor.Guarda.Infra;

public sealed class GuardaDbContext(DbContextOptions<GuardaDbContext> opcoes) : DbContext(opcoes)
{
    public const string Esquema = "guarda";

    protected override void OnModelCreating(ModelBuilder modelo) => modelo.HasDefaultSchema(Esquema);
}
