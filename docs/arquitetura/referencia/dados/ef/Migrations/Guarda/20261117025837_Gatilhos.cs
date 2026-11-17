using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verificacao.Migrations.Guarda
{
	/// <inheritdoc />
	public partial class Gatilhos : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// O SQL mora em ddl/guarda_gatilhos.sql, embutido no assembly. A migracao e o DDL de referencia leem o mesmo arquivo.
			migrationBuilder.Sql(Recurso.Ler("guarda_gatilhos.sql"));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("DROP FUNCTION guarda.proteger_evento() CASCADE;");
		}
	}
}
