using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corredor.Guarda.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "guarda");

            migrationBuilder.CreateTable(
                name: "evento_de_seguranca",
                schema: "guarda",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=nenhum"),
                    ocorrido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=nenhum"),
                    tipo = table.Column<string>(type: "text", nullable: false, comment: "classe=nenhum"),
                    tipos_de_dado = table.Column<string[]>(type: "text[]", nullable: false, comment: "classe=nenhum"),
                    canal = table.Column<string>(type: "text", nullable: false, comment: "classe=nenhum"),
                    modulo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "classe=nenhum"),
                    rota = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "classe=nenhum"),
                    severidade = table.Column<short>(type: "smallint", nullable: false, comment: "classe=nenhum"),
                    mensagem_texto = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true, comment: "classe=nenhum"),
                    mensagem_origem = table.Column<string>(type: "text", nullable: true, comment: "classe=nenhum")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evento_de_seguranca", x => x.id);
                    table.CheckConstraint("ck_evento_de_seguranca_canal", "canal IN ('Log', 'RespostaSemAutenticacao', 'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')");
                    table.CheckConstraint("ck_evento_de_seguranca_mensagem_inteira", "(mensagem_texto IS NULL) = (mensagem_origem IS NULL)");
                    table.CheckConstraint("ck_evento_de_seguranca_mensagem_origem", "mensagem_origem IN ('Llm', 'FraseFixa')");
                    table.CheckConstraint("ck_evento_de_seguranca_modulo", "btrim(modulo) <> ''");
                    table.CheckConstraint("ck_evento_de_seguranca_rota_sem_identificador", "rota !~ '[?]|[0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-'");
                    table.CheckConstraint("ck_evento_de_seguranca_severidade", "severidade BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_evento_de_seguranca_tipo", "tipo IN ('MascaramentoNaEntrada', 'BloqueioDeSaida')");
                    table.CheckConstraint("ck_evento_de_seguranca_tipos_de_dado", "cardinality(tipos_de_dado) > 0 AND tipos_de_dado <@ ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial']");
                });

            migrationBuilder.CreateIndex(
                name: "ix_evento_de_seguranca_ocorrido_em",
                schema: "guarda",
                table: "evento_de_seguranca",
                column: "ocorrido_em",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evento_de_seguranca",
                schema: "guarda");
        }
    }
}
