using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corredor.Guarda.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Gatilhos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O SQL mora em Infra/ddl/guarda_gatilhos.sql, embutido no assembly.
            migrationBuilder.Sql(Recurso.Ler("guarda_gatilhos.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION guarda.proteger_evento() CASCADE;");
        }
    }
}
