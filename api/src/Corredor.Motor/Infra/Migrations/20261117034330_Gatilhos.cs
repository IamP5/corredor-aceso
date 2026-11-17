using Corredor.Motor.Infra;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corredor.Motor.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Gatilhos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O SQL mora em Infra/Sql/motor_gatilhos.sql, embutido no assembly: EventoSensor nao aceita UPDATE.
            migrationBuilder.Sql(RecursoDoMotor.Ler("motor_gatilhos.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION motor.recusar_alteracao() CASCADE;");
        }
    }
}
