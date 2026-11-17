using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verificacao.Migrations.Motor
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "motor");

            migrationBuilder.CreateTable(
                name: "casa_monitorada",
                schema: "motor",
                columns: table => new
                {
                    casa_token = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado"),
                    microarea_id = table.Column<int>(type: "integer", nullable: false, comment: "classe=pseudonimizado"),
                    ativada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pseudonimizado"),
                    desativada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "classe=pseudonimizado"),
                    ultimo_pacote_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "classe=pseudonimizado"),
                    perfil_contato = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado+sensivel"),
                    perfil_eletrodependente = table.Column<bool>(type: "boolean", nullable: false, comment: "classe=pseudonimizado+sensivel")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_casa_monitorada", x => x.casa_token);
                    table.CheckConstraint("ck_casa_monitorada_microarea", "microarea_id > 0");
                    table.CheckConstraint("ck_casa_monitorada_perfil_contato", "perfil_contato IN ('Sozinho', 'Acamado', 'Coabitado')");
                    table.CheckConstraint("ck_casa_monitorada_token", "casa_token ~ '^[0-9a-f]{32}$'");
                });

            migrationBuilder.CreateTable(
                name: "dia_critico",
                schema: "motor",
                columns: table => new
                {
                    data = table.Column<DateOnly>(type: "date", nullable: false, comment: "classe=nenhum"),
                    probabilidade = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false, comment: "classe=nenhum"),
                    motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "classe=nenhum"),
                    origem = table.Column<string>(type: "text", nullable: false, comment: "classe=nenhum"),
                    aberto_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=nenhum"),
                    toque_pedido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "classe=nenhum"),
                    encerrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "classe=nenhum"),
                    resumo_casas_ativas = table.Column<int>(type: "integer", nullable: true, comment: "classe=nenhum"),
                    resumo_com_sinal_de_vida_ate_10h = table.Column<int>(type: "integer", nullable: true, comment: "classe=nenhum"),
                    resumo_atendidas = table.Column<int>(type: "integer", nullable: true, comment: "classe=nenhum"),
                    resumo_vermelhas_pendentes = table.Column<int>(type: "integer", nullable: true, comment: "classe=nenhum"),
                    resumo_cinzas_pendentes = table.Column<int>(type: "integer", nullable: true, comment: "classe=nenhum"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dia_critico", x => x.data);
                    table.CheckConstraint("ck_dia_critico_motivo", "btrim(motivo) <> ''");
                    table.CheckConstraint("ck_dia_critico_origem", "origem IN ('Modelo', 'RegraBase', 'Manual')");
                    table.CheckConstraint("ck_dia_critico_probabilidade", "probabilidade BETWEEN 0 AND 1");
                    table.CheckConstraint("ck_dia_critico_resumo_faixa", "resumo_casas_ativas >= 0 AND resumo_com_sinal_de_vida_ate_10h BETWEEN 0 AND resumo_casas_ativas AND resumo_atendidas >= 0 AND resumo_vermelhas_pendentes >= 0 AND resumo_cinzas_pendentes >= 0 AND resumo_atendidas + resumo_vermelhas_pendentes + resumo_cinzas_pendentes <= resumo_casas_ativas");
                    table.CheckConstraint("ck_dia_critico_resumo_so_no_encerramento", "num_nulls(encerrado_em, resumo_casas_ativas, resumo_com_sinal_de_vida_ate_10h, resumo_atendidas, resumo_vermelhas_pendentes, resumo_cinzas_pendentes) IN (0, 6)");
                });

            migrationBuilder.CreateTable(
                name: "confirmacao",
                schema: "motor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=pseudonimizado"),
                    origem = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado"),
                    resultado = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado"),
                    quando = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pseudonimizado"),
                    casa_token = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_confirmacao", x => x.id);
                    table.CheckConstraint("ck_confirmacao_origem", "origem IN ('Acs', 'Vizinho')");
                    table.CheckConstraint("ck_confirmacao_resultado", "resultado IN ('PessoaVista', 'AusenciaConfirmada')");
                    table.CheckConstraint("ck_confirmacao_vizinho_so_ve_pessoa", "origem = 'Acs' OR resultado = 'PessoaVista'");
                    table.ForeignKey(
                        name: "fk_confirmacao_casa_monitorada",
                        column: x => x.casa_token,
                        principalSchema: "motor",
                        principalTable: "casa_monitorada",
                        principalColumn: "casa_token",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evento_sensor",
                schema: "motor",
                columns: table => new
                {
                    casa_token = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado"),
                    seq = table.Column<long>(type: "bigint", nullable: false, comment: "classe=pseudonimizado"),
                    ocorrido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pseudonimizado"),
                    tipo = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado"),
                    recebido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pseudonimizado"),
                    temp_c = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true, comment: "classe=pseudonimizado"),
                    tensao_v = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true, comment: "classe=pseudonimizado"),
                    bufferizado = table.Column<bool>(type: "boolean", nullable: false, comment: "classe=pseudonimizado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evento_sensor", x => new { x.casa_token, x.ocorrido_em, x.seq });
                    table.CheckConstraint("ck_evento_sensor_leitura_so_na_telemetria", "tipo = 'Telemetria' OR (temp_c IS NULL AND tensao_v IS NULL)");
                    table.CheckConstraint("ck_evento_sensor_seq", "seq >= 0");
                    table.CheckConstraint("ck_evento_sensor_temp_c", "temp_c BETWEEN -10 AND 60");
                    table.CheckConstraint("ck_evento_sensor_tensao_v", "tensao_v BETWEEN 0 AND 260");
                    table.CheckConstraint("ck_evento_sensor_tipo", "tipo IN ('Movimento', 'BotaoBem', 'BotaoSair', 'Telemetria')");
                    table.ForeignKey(
                        name: "fk_evento_sensor_casa_monitorada",
                        column: x => x.casa_token,
                        principalSchema: "motor",
                        principalTable: "casa_monitorada",
                        principalColumn: "casa_token",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_casa_monitorada_microarea_ativa",
                schema: "motor",
                table: "casa_monitorada",
                column: "microarea_id",
                filter: "desativada_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_confirmacao_casa_quando",
                schema: "motor",
                table: "confirmacao",
                columns: new[] { "casa_token", "quando" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "confirmacao",
                schema: "motor");

            migrationBuilder.DropTable(
                name: "dia_critico",
                schema: "motor");

            migrationBuilder.DropTable(
                name: "evento_sensor",
                schema: "motor");

            migrationBuilder.DropTable(
                name: "casa_monitorada",
                schema: "motor");
        }
    }
}
