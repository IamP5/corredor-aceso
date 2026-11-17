using System.Reflection;
using Corredor.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// Uma string de conexao por modulo, como no host: ConnectionStrings__Motor, __Cadastro e __Guarda.
// Na VM cada uma usa o papel do modulo. No notebook as tres podem ser iguais.
public static class Conexao
{
	public static string De(string modulo) =>
		Environment.GetEnvironmentVariable("CORREDOR_PG_" + modulo.ToUpperInvariant())
		?? Environment.GetEnvironmentVariable("CORREDOR_PG")
		?? "Host=127.0.0.1;Port=55470;Username=postgres;Password=corredor;Database=mao";

	public static DbContextOptions<T> Opcoes<T>(string modulo) where T : DbContext =>
		new DbContextOptionsBuilder<T>()
			.UseNpgsql(De(modulo), o => o.MigrationsHistoryTable("__EFMigrationsHistory", modulo))
			.UseSnakeCaseNamingConvention()
			.Options;
}

// A migracao Gatilhos le o SQL de ddl/<schema>_gatilhos.sql, embutido no assembly. O arquivo e um so.
public static class Recurso
{
	public static string Ler(string nome)
	{
		using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream(nome)
			?? throw new InvalidOperationException($"recurso ausente: {nome}");
		using var leitor = new StreamReader(fluxo);
		return leitor.ReadToEnd();
	}
}

public sealed class FabricaDoMotor : IDesignTimeDbContextFactory<MotorDbContext>
{
	public MotorDbContext CreateDbContext(string[] args) => new(Conexao.Opcoes<MotorDbContext>("motor"));
}

public sealed class FabricaDoCadastro : IDesignTimeDbContextFactory<CadastroDbContext>
{
	public CadastroDbContext CreateDbContext(string[] args) => new(Conexao.Opcoes<CadastroDbContext>("cadastro"));
}

public sealed class FabricaDaGuarda : IDesignTimeDbContextFactory<GuardaDbContext>
{
	public GuardaDbContext CreateDbContext(string[] args) => new(Conexao.Opcoes<GuardaDbContext>("guarda"));
}
