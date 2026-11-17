using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verificacao.Migrations.Cadastro
{
	/// <inheritdoc />
	public partial class Gatilhos : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// O SQL mora em ddl/cadastro_gatilhos.sql, embutido no assembly. A migracao e o DDL de referencia leem o mesmo arquivo.
			migrationBuilder.Sql(Recurso.Ler("cadastro_gatilhos.sql"));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("DROP FUNCTION cadastro.recusar_alteracao() CASCADE; DROP FUNCTION cadastro.conferir_token_com_consentimento() CASCADE;");
		}
	}
}
