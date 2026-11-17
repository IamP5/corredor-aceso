using Microsoft.EntityFrameworkCore;

namespace Corredor.Motor.Infra;

public sealed class MotorDbContext(DbContextOptions<MotorDbContext> opcoes) : DbContext(opcoes)
{
    public const string Esquema = "motor";

    protected override void OnModelCreating(ModelBuilder modelo) => modelo.HasDefaultSchema(Esquema);
}
