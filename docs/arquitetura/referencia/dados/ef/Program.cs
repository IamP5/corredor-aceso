using Microsoft.EntityFrameworkCore;
using Corredor.Infra;

Func<MotorDbContext> novoMotor = () => new MotorDbContext(Conexao.Opcoes<MotorDbContext>("motor"));
Func<CadastroDbContext> novoCadastro = () => new CadastroDbContext(Conexao.Opcoes<CadastroDbContext>("cadastro"));
Func<GuardaDbContext> novaGuarda = () => new GuardaDbContext(Conexao.Opcoes<GuardaDbContext>("guarda"));

switch (args.FirstOrDefault())
{
	case "gerar":
		Directory.CreateDirectory("gerado");
		File.WriteAllText("gerado/motor.sql", novoMotor().Database.GenerateCreateScript());
		File.WriteAllText("gerado/cadastro.sql", novoCadastro().Database.GenerateCreateScript());
		File.WriteAllText("gerado/guarda.sql", novaGuarda().Database.GenerateCreateScript());
		Console.WriteLine("scripts em gerado/");
		break;
	case "consultar":
		await Consultas.RodarAsync(novoMotor, novoCadastro, novaGuarda);
		break;
	default:
		Console.WriteLine("uso: gerar | consultar");
		break;
}
