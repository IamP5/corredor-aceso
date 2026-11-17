using Corredor.Contratos;
using Corredor.Motor.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Corredor.Motor.Infra;

// Mapeamento conferido em docs/arquitetura/referencia/dados/ef/Contextos.cs: gera o mesmo catalogo que ddl/motor.sql.
public sealed class MotorDbContext(DbContextOptions<MotorDbContext> opcoes) : DbContext(opcoes)
{
    public const string Esquema = "motor";

    private const string FormatoDoToken = "~ '^[0-9a-f]{32}$'";
    private const string PerfisDeContato = "('Sozinho', 'Acamado', 'Coabitado')";

    public DbSet<CasaMonitorada> Casas => Set<CasaMonitorada>();
    public DbSet<EventoSensor> Eventos => Set<EventoSensor>();
    public DbSet<DiaCritico> DiasCriticos => Set<DiaCritico>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<CasaToken>().HaveConversion<CasaTokenConverter>();
        c.Properties<MicroareaId>().HaveConversion<MicroareaIdConverter>();
        c.Properties<DateTimeOffset>().HaveConversion<UtcConverter>();
        c.Properties<TempC>().HaveConversion<TempCConverter>().HavePrecision(4, 1);
        c.Properties<TensaoV>().HaveConversion<TensaoVConverter>().HavePrecision(5, 1);
        c.Properties<Enum>().HaveConversion<string>();
        c.Conventions.Add(_ => new ConvencaoDeClassificacao(Classificacao.Motor));
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Esquema);

        b.Entity<CasaMonitorada>(e =>
        {
            e.ToTable("casa_monitorada", t =>
            {
                t.HasCheckConstraint("ck_casa_monitorada_token", $"casa_token {FormatoDoToken}");
                t.HasCheckConstraint("ck_casa_monitorada_microarea", "microarea_id > 0");
                t.HasCheckConstraint("ck_casa_monitorada_perfil_contato", $"perfil_contato IN {PerfisDeContato}");
            });
            e.HasKey(c => c.Token).HasName("pk_casa_monitorada");
            e.Property(c => c.Token).HasColumnName("casa_token");
            e.Property(c => c.Microarea).HasColumnName("microarea_id");
            e.ComplexProperty(c => c.Perfil);
            e.Ignore(c => c.Ativa);
            e.HasIndex(c => c.Microarea)
                .HasDatabaseName("ix_casa_monitorada_microarea_ativa")
                .HasFilter("desativada_em IS NULL");

            e.OwnsMany(c => c.Confirmacoes, o =>
            {
                o.ToTable("confirmacao", t =>
                {
                    t.HasCheckConstraint("ck_confirmacao_origem", "origem IN ('Acs', 'Vizinho')");
                    t.HasCheckConstraint("ck_confirmacao_resultado", "resultado IN ('PessoaVista', 'AusenciaConfirmada')");
                    t.HasCheckConstraint("ck_confirmacao_vizinho_so_ve_pessoa", "origem = 'Acs' OR resultado = 'PessoaVista'");
                });
                o.WithOwner().HasForeignKey("CasaToken").HasConstraintName("fk_confirmacao_casa_monitorada");
                o.HasKey(x => x.Id).HasName("pk_confirmacao");
                o.Property(x => x.Id).ValueGeneratedNever();
                o.HasIndex("CasaToken", nameof(Confirmacao.Quando)).HasDatabaseName("ix_confirmacao_casa_quando");
            });
        });

        b.Entity<EventoSensor>(e =>
        {
            e.ToTable("evento_sensor", t =>
            {
                t.HasCheckConstraint("ck_evento_sensor_seq", "seq >= 0");
                t.HasCheckConstraint("ck_evento_sensor_tipo", "tipo IN ('Movimento', 'BotaoBem', 'BotaoSair', 'Telemetria')");
                t.HasCheckConstraint("ck_evento_sensor_temp_c", "temp_c BETWEEN -10 AND 60");
                t.HasCheckConstraint("ck_evento_sensor_tensao_v", "tensao_v BETWEEN 0 AND 260");
                t.HasCheckConstraint("ck_evento_sensor_leitura_so_na_telemetria", "tipo = 'Telemetria' OR (temp_c IS NULL AND tensao_v IS NULL)");
            });
            e.HasKey(x => new { x.CasaToken, x.OcorridoEm, x.Seq }).HasName("pk_evento_sensor");
            e.HasOne<CasaMonitorada>().WithMany()
                .HasForeignKey(x => x.CasaToken)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_evento_sensor_casa_monitorada");
        });

        b.Entity<DiaCritico>(e =>
        {
            e.ToTable("dia_critico", t =>
            {
                t.HasCheckConstraint("ck_dia_critico_probabilidade", "probabilidade BETWEEN 0 AND 1");
                t.HasCheckConstraint("ck_dia_critico_motivo", "btrim(motivo) <> ''");
                t.HasCheckConstraint("ck_dia_critico_origem", "origem IN ('Modelo', 'RegraBase', 'Manual')");
                t.HasCheckConstraint("ck_dia_critico_resumo_so_no_encerramento",
                    "num_nulls(encerrado_em, resumo_casas_ativas, resumo_com_sinal_de_vida_ate_10h, resumo_atendidas, resumo_vermelhas_pendentes, resumo_cinzas_pendentes) IN (0, 6)");
                t.HasCheckConstraint("ck_dia_critico_resumo_faixa",
                    "resumo_casas_ativas >= 0 AND resumo_com_sinal_de_vida_ate_10h BETWEEN 0 AND resumo_casas_ativas AND resumo_atendidas >= 0 AND resumo_vermelhas_pendentes >= 0 AND resumo_cinzas_pendentes >= 0 AND resumo_atendidas + resumo_vermelhas_pendentes + resumo_cinzas_pendentes <= resumo_casas_ativas");
            });
            e.HasKey(d => d.Data).HasName("pk_dia_critico");
            e.Property(d => d.Probabilidade)
                .HasConversion(new ValueConverter<Probabilidade, decimal>(p => p.Valor, v => new Probabilidade(v)))
                .HasPrecision(4, 3);
            e.Property(d => d.Motivo).HasMaxLength(PrevisaoDoDia.TamanhoMaximoDoMotivo);
            e.Property(d => d.Versao).IsRowVersion();
            e.OwnsOne(d => d.Resumo, r =>
                r.Property(x => x.ComSinalDeVidaAte10h).HasColumnName("resumo_com_sinal_de_vida_ate_10h"));
        });
    }
}

public sealed class CasaTokenConverter() : ValueConverter<CasaToken, string>(t => t.Valor, s => Ler(s))
{
    private static CasaToken Ler(string s) =>
        CasaToken.TryParse(s, out var token) ? token : throw new InvalidOperationException("casa_token fora do formato");
}

public sealed class MicroareaIdConverter() : ValueConverter<MicroareaId, int>(m => m.Valor, v => new MicroareaId(v));

// O Npgsql recusa gravar DateTimeOffset com deslocamento diferente de zero. A rota recebe -03:00.
public sealed class UtcConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);

public sealed class TempCConverter() : ValueConverter<TempC, decimal>(t => t.Valor, v => new TempC(v));

public sealed class TensaoVConverter() : ValueConverter<TensaoV, decimal>(t => t.Valor, v => new TensaoV(v));
