using Corredor.Cadastro.Testes.Apoio;
using Npgsql;

namespace Corredor.Cadastro.Testes;

// O que o banco garante mesmo com erro no código, tentado como o papel da API (corredor_cadastro).
public sealed class BancoTestes(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    [Theory]
    [InlineData("UPDATE cadastro.visita SET desfecho = 'EstavaBem'")]
    [InlineData("DELETE FROM cadastro.visita")]
    [InlineData("UPDATE cadastro.mensagem_saida SET veredito = 'Liberada'")]
    [InlineData("DELETE FROM cadastro.mensagem_saida")]
    public async Task Visita_e_mensagem_de_saida_so_aceitam_INSERT(string sql)
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var (_, token) = await api.CasaAtivaComVizinhoAsync();
        await api.PostAsync("/api/visitas", Cenario.Visita(Guid.NewGuid(), token, "NaoAbriu"));
        await api.PostAsync("/api/mensagens/verificar", new { canal = "GrupoDoBairro", texto = "reunião no posto às 15h" });

        var erro = await Assert.ThrowsAsync<PostgresException>(() => banco.ExecutarComoApiAsync(sql));

        Assert.Equal(PostgresErrorCodes.RestrictViolation, erro.SqlState);
    }

    [Fact]
    public async Task Token_sem_consentimento_vigente_e_recusado_no_COMMIT()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync();

        var erro = await Assert.ThrowsAsync<PostgresException>(() => banco.ExecutarComoApiAsync(
            $"UPDATE cadastro.idoso SET casa_token = '{Contratos.CasaToken.Novo()}' WHERE id = '{idoso}'"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, erro.SqlState);
    }

    [Fact]
    public async Task Revogacao_que_deixa_o_token_e_recusada_no_COMMIT()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync();
        await api.ConsentirAsync(idoso);

        var erro = await Assert.ThrowsAsync<PostgresException>(() => banco.ExecutarComoApiAsync(
            $"UPDATE cadastro.consentimento SET revogado_em = now() WHERE idoso_id = '{idoso}'"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, erro.SqlState);
    }

    [Fact]
    public async Task CPF_pontuado_em_claro_e_recusado_pelo_banco()
    {
        await using var api = await ApiDeTeste.IniciarAsync(banco);
        var idoso = await api.CadastrarAsync();

        var erro = await Assert.ThrowsAsync<PostgresException>(() => banco.ExecutarComoApiAsync(
            $"UPDATE cadastro.idoso SET condicoes = 'doc 123.456.789-00' WHERE id = '{idoso}'"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, erro.SqlState);
    }

    [Fact]
    public async Task Papel_da_API_nao_muda_tabela()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => banco.ExecutarComoApiAsync(
            "ALTER TABLE cadastro.visita ADD COLUMN extra text"));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, erro.SqlState);
    }

    [Fact]
    public async Task Toda_coluna_do_schema_cadastro_tem_a_classificacao_no_comentario()
    {
        var semClasse = await banco.ConsultarComoApiAsync<long>("""
            SELECT count(*) FROM information_schema.columns c
            WHERE c.table_schema = 'cadastro' AND c.table_name <> '__EFMigrationsHistory'
              AND coalesce(col_description(format('%I.%I', c.table_schema, c.table_name)::regclass, c.ordinal_position), '')
                  !~ '^classe=(pessoal|sensivel|pseudonimizado|pseudonimizado\+sensivel|nenhum)(;|$)'
            """);
        var total = await banco.ConsultarComoApiAsync<long>(
            "SELECT count(*) FROM information_schema.columns WHERE table_schema = 'cadastro' AND table_name <> '__EFMigrationsHistory'");

        Assert.Equal(0, semClasse);
        Assert.Equal(46, total);
    }

    [Fact]
    public async Task A_microarea_3_e_a_ACS_de_demo_entram_pela_migracao()
    {
        var acs = await banco.ConsultarComoApiAsync<string>("SELECT acs_responsavel FROM cadastro.microarea WHERE id = 3");

        Assert.Equal("acs-demo-01", acs);
    }
}
