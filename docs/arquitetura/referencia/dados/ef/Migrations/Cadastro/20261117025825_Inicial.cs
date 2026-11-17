using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verificacao.Migrations.Cadastro
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cadastro");

            migrationBuilder.CreateTable(
                name: "mensagem_saida",
                schema: "cadastro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=nenhum"),
                    canal = table.Column<string>(type: "text", nullable: false, comment: "classe=nenhum"),
                    acs_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "classe=pessoal"),
                    texto_mascarado = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "classe=pessoal; mascarado"),
                    veredito = table.Column<string>(type: "text", nullable: false, comment: "classe=nenhum"),
                    tipos_de_dado = table.Column<string[]>(type: "text[]", nullable: false, comment: "classe=nenhum"),
                    verificada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=nenhum")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensagem_saida", x => x.id);
                    table.CheckConstraint("ck_mensagem_saida_acs_id", "acs_id ~ '^[a-z0-9-]{3,40}$'");
                    table.CheckConstraint("ck_mensagem_saida_bloqueada_tem_tipo", "veredito = 'Liberada' OR cardinality(tipos_de_dado) > 0");
                    table.CheckConstraint("ck_mensagem_saida_canal", "canal IN ('Log', 'RespostaSemAutenticacao', 'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')");
                    table.CheckConstraint("ck_mensagem_saida_sem_cpf_em_claro", "texto_mascarado !~ '[0-9]{3}\\.[0-9]{3}\\.[0-9]{3}-[0-9]{2}'");
                    table.CheckConstraint("ck_mensagem_saida_tipos_de_dado", "tipos_de_dado <@ ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial']");
                    table.CheckConstraint("ck_mensagem_saida_veredito", "veredito IN ('Liberada', 'Bloqueada')");
                });

            migrationBuilder.CreateTable(
                name: "microarea",
                schema: "cadastro",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "classe=nenhum"),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "classe=nenhum"),
                    acs_responsavel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "classe=pessoal")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_microarea", x => x.id);
                    table.CheckConstraint("ck_microarea_acs_responsavel", "acs_responsavel ~ '^[a-z0-9-]{3,40}$'");
                    table.CheckConstraint("ck_microarea_id", "id > 0");
                    table.CheckConstraint("ck_microarea_nome", "btrim(nome) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "aviso_ao_vizinho",
                schema: "cadastro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=pseudonimizado"),
                    casa_token = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado"),
                    microarea_id = table.Column<int>(type: "integer", nullable: false, comment: "classe=nenhum"),
                    motivo = table.Column<string>(type: "text", nullable: false, comment: "classe=pseudonimizado+sensivel"),
                    desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pseudonimizado"),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pseudonimizado"),
                    resposta = table.Column<string>(type: "text", nullable: true, comment: "classe=pseudonimizado"),
                    respondido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "classe=pseudonimizado"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aviso_ao_vizinho", x => x.id);
                    table.CheckConstraint("ck_aviso_ao_vizinho_casa_token", "casa_token ~ '^[0-9a-f]{32}$'");
                    table.CheckConstraint("ck_aviso_ao_vizinho_motivo", "motivo IN ('EnergiaEletrodependente', 'SemHeartbeat', 'CalorInterno', 'SemMovimentoNaJanela')");
                    table.CheckConstraint("ck_aviso_ao_vizinho_resposta", "resposta IN ('EstaBem', 'PrecisaDeAjuda')");
                    table.CheckConstraint("ck_aviso_ao_vizinho_resposta_com_hora", "(resposta IS NULL) = (respondido_em IS NULL)");
                    table.ForeignKey(
                        name: "fk_aviso_ao_vizinho_microarea",
                        column: x => x.microarea_id,
                        principalSchema: "cadastro",
                        principalTable: "microarea",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "idoso",
                schema: "cadastro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=pessoal"),
                    microarea_id = table.Column<int>(type: "integer", nullable: false, comment: "classe=nenhum"),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "classe=pessoal; rotulo=Nome"),
                    condicoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "classe=sensivel; rotulo=Saude"),
                    medicamentos = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "classe=sensivel; rotulo=Saude"),
                    casa_endereco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "classe=pessoal; rotulo=Endereco"),
                    casa_token = table.Column<string>(type: "text", nullable: true, comment: "classe=pessoal"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    perfil_contato = table.Column<string>(type: "text", nullable: false, comment: "classe=sensivel"),
                    perfil_eletrodependente = table.Column<bool>(type: "boolean", nullable: false, comment: "classe=sensivel")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idoso", x => x.id);
                    table.CheckConstraint("ck_idoso_casa_endereco", "btrim(casa_endereco) <> ''");
                    table.CheckConstraint("ck_idoso_casa_token", "casa_token ~ '^[0-9a-f]{32}$'");
                    table.CheckConstraint("ck_idoso_nome", "btrim(nome) <> ''");
                    table.CheckConstraint("ck_idoso_perfil_contato", "perfil_contato IN ('Sozinho', 'Acamado', 'Coabitado')");
                    table.CheckConstraint("ck_idoso_sem_cpf_em_claro", "concat_ws(' ', nome, casa_endereco, condicoes, medicamentos) !~ '[0-9]{3}\\.[0-9]{3}\\.[0-9]{3}-[0-9]{2}'");
                    table.ForeignKey(
                        name: "fk_idoso_microarea",
                        column: x => x.microarea_id,
                        principalSchema: "cadastro",
                        principalTable: "microarea",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consentimento",
                schema: "cadastro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=pessoal"),
                    colhido_por = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "classe=pessoal"),
                    colhido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pessoal"),
                    versao_do_termo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "classe=pessoal"),
                    autorizacoes_avisar_vizinho = table.Column<bool>(type: "boolean", nullable: false, comment: "classe=pessoal"),
                    autorizacoes_avisar_familia = table.Column<bool>(type: "boolean", nullable: false, comment: "classe=pessoal"),
                    revogado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "classe=pessoal"),
                    idoso_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=pessoal")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consentimento", x => x.id);
                    table.CheckConstraint("ck_consentimento_colhido_por", "colhido_por ~ '^[a-z0-9-]{3,40}$'");
                    table.CheckConstraint("ck_consentimento_versao_do_termo", "btrim(versao_do_termo) <> ''");
                    table.ForeignKey(
                        name: "fk_consentimento_idoso",
                        column: x => x.idoso_id,
                        principalSchema: "cadastro",
                        principalTable: "idoso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visita",
                schema: "cadastro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=sensivel"),
                    idoso_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=sensivel"),
                    acs_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "classe=pessoal"),
                    ocorrida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=sensivel"),
                    registrada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=sensivel"),
                    desfecho = table.Column<string>(type: "text", nullable: false, comment: "classe=sensivel"),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "classe=sensivel; rotulo=Saude+MoraSozinho+Nome+Endereco+Telefone")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visita", x => x.id);
                    table.CheckConstraint("ck_visita_acs_id", "acs_id ~ '^[a-z0-9-]{3,40}$'");
                    table.CheckConstraint("ck_visita_desfecho", "desfecho IN ('EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada')");
                    table.CheckConstraint("ck_visita_sem_cpf_em_claro", "observacao !~ '[0-9]{3}\\.[0-9]{3}\\.[0-9]{3}-[0-9]{2}'");
                    table.ForeignKey(
                        name: "fk_visita_idoso",
                        column: x => x.idoso_id,
                        principalSchema: "cadastro",
                        principalTable: "idoso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vizinho_de_confianca",
                schema: "cadastro",
                columns: table => new
                {
                    idoso_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "classe=pessoal"),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "classe=pessoal; rotulo=Nome"),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "classe=pessoal; rotulo=Telefone"),
                    aceitou_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "classe=pessoal")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vizinho_de_confianca", x => x.idoso_id);
                    table.CheckConstraint("ck_vizinho_de_confianca_nome", "btrim(nome) <> ''");
                    table.CheckConstraint("ck_vizinho_de_confianca_sem_cpf_em_claro", "concat_ws(' ', nome, telefone) !~ '[0-9]{3}\\.[0-9]{3}\\.[0-9]{3}-[0-9]{2}'");
                    table.CheckConstraint("ck_vizinho_de_confianca_telefone", "btrim(telefone) <> ''");
                    table.ForeignKey(
                        name: "fk_vizinho_de_confianca_idoso",
                        column: x => x.idoso_id,
                        principalSchema: "cadastro",
                        principalTable: "idoso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "cadastro",
                table: "microarea",
                columns: new[] { "id", "acs_responsavel", "nome" },
                values: new object[] { 3, "acs-demo-01", "Microárea 3 - Vila Exemplo" });

            migrationBuilder.CreateIndex(
                name: "ix_aviso_ao_vizinho_microarea_registrado",
                schema: "cadastro",
                table: "aviso_ao_vizinho",
                columns: new[] { "microarea_id", "registrado_em" });

            migrationBuilder.CreateIndex(
                name: "ux_aviso_ao_vizinho_episodio",
                schema: "cadastro",
                table: "aviso_ao_vizinho",
                columns: new[] { "casa_token", "motivo", "desde" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_consentimento_vigente",
                schema: "cadastro",
                table: "consentimento",
                column: "idoso_id",
                unique: true,
                filter: "revogado_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_idoso_microarea_com_token",
                schema: "cadastro",
                table: "idoso",
                column: "microarea_id",
                filter: "casa_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_idoso_casa_token",
                schema: "cadastro",
                table: "idoso",
                column: "casa_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_visita_idoso_ocorrida",
                schema: "cadastro",
                table: "visita",
                columns: new[] { "idoso_id", "ocorrida_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aviso_ao_vizinho",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "consentimento",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "mensagem_saida",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "visita",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "vizinho_de_confianca",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "idoso",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "microarea",
                schema: "cadastro");
        }
    }
}
