using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corredor.Cadastro.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Gatilhos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O SQL mora em Infra/Sql/cadastro_gatilhos.sql, embutido no assembly: a cópia do DDL de referência.
            migrationBuilder.Sql(Recurso.Ler("cadastro_gatilhos.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION cadastro.recusar_alteracao() CASCADE; DROP FUNCTION cadastro.conferir_token_com_consentimento() CASCADE;");
        }
    }
}
