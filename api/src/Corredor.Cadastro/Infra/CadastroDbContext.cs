using Corredor.Cadastro.Dominio;
using Corredor.Contratos;
using Corredor.Guarda;
using Microsoft.EntityFrameworkCore;
using static Corredor.Guarda.ConversorDeTextoRotulado;

namespace Corredor.Cadastro.Infra;

// O conversor de TextoRotulado mora na Guarda, a única que lê o valor. Na leitura, o rótulo vem da coluna.
// O mapeamento conferido em docs/arquitetura/referencia/dados/ef/Contextos.cs, que gera o mesmo catálogo do DDL.
public sealed class CadastroDbContext(DbContextOptions<CadastroDbContext> opcoes) : DbContext(opcoes)
{
    public const string Esquema = "cadastro";

    public DbSet<Microarea> Microareas => Set<Microarea>();
    public DbSet<Idoso> Idosos => Set<Idoso>();
    public DbSet<Visita> Visitas => Set<Visita>();
    public DbSet<MensagemSaida> Mensagens => Set<MensagemSaida>();
    public DbSet<AvisoAoVizinho> Avisos => Set<AvisoAoVizinho>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<CasaToken>().HaveConversion<CasaTokenConverter>();
        c.Properties<MicroareaId>().HaveConversion<MicroareaIdConverter>();
        c.Properties<IdosoId>().HaveConversion<IdosoIdConverter>();
        c.Properties<AcsId>().HaveConversion<AcsIdConverter>().HaveMaxLength(40);
        c.Properties<DateTimeOffset>().HaveConversion<UtcConverter>();
        c.Properties<Enum>().HaveConversion<string>();
        c.Conventions.Add(_ => new ConvencaoDeClassificacao(Classificacao.Cadastro));
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Esquema);

        b.Entity<Microarea>(e =>
        {
            e.ToTable("microarea", t =>
            {
                t.HasCheckConstraint("ck_microarea_id", "id > 0");
                t.HasCheckConstraint("ck_microarea_nome", "btrim(nome) <> ''");
                t.HasCheckConstraint("ck_microarea_acs_responsavel", $"acs_responsavel {Sql.Acs}");
            });
            e.HasKey(m => m.Id).HasName("pk_microarea");
            e.Property(m => m.Id).ValueGeneratedNever();
            e.Property(m => m.Nome).HasMaxLength(80);
            e.HasData(new { Id = new MicroareaId(3), Nome = "Microárea 3 - Vila Exemplo", AcsResponsavel = new AcsId("acs-demo-01") });
        });

        b.Entity<Idoso>(e =>
        {
            e.ToTable("idoso", t =>
            {
                t.HasCheckConstraint("ck_idoso_nome", "btrim(nome) <> ''");
                t.HasCheckConstraint("ck_idoso_casa_endereco", "btrim(casa_endereco) <> ''");
                t.HasCheckConstraint("ck_idoso_perfil_contato", $"perfil_contato IN {Sql.PerfisDeContato}");
                t.HasCheckConstraint("ck_idoso_casa_token", $"casa_token {Sql.Token}");
                t.HasCheckConstraint("ck_idoso_sem_cpf_em_claro", $"concat_ws(' ', nome, casa_endereco, condicoes, medicamentos) {Sql.Cpf}");
            });
            e.HasKey(i => i.Id).HasName("pk_idoso");
            e.Property(i => i.Id).ValueGeneratedNever();
            e.Property(i => i.Microarea).HasColumnName("microarea_id");
            e.HasOne<Microarea>().WithMany()
                .HasForeignKey(i => i.Microarea)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_idoso_microarea");
            e.HasIndex(i => i.Microarea)
                .HasDatabaseName("ix_idoso_microarea_com_token")
                .HasFilter("casa_token IS NOT NULL");
            e.Property(i => i.Nome).HasConversion(Rotulado(TipoDeDado.Nome)).HasMaxLength(120);
            e.Property(i => i.Condicoes).HasConversion(Rotulado(TipoDeDado.Saude)).HasMaxLength(500);
            e.Property(i => i.Medicamentos).HasConversion(Rotulado(TipoDeDado.Saude)).HasMaxLength(500);
            e.ComplexProperty(i => i.Perfil);
            e.Property(i => i.Versao).IsRowVersion();

            e.OwnsOne(i => i.Casa, c =>
            {
                c.Property(x => x.Endereco).HasConversion(Rotulado(TipoDeDado.Endereco)).HasMaxLength(200);
                c.HasIndex(x => x.Token).IsUnique().HasDatabaseName("ux_idoso_casa_token");
            });
            e.Navigation(i => i.Casa).IsRequired();

            e.OwnsMany(i => i.Consentimentos, c =>
            {
                c.ToTable("consentimento", t =>
                {
                    t.HasCheckConstraint("ck_consentimento_colhido_por", $"colhido_por {Sql.Acs}");
                    t.HasCheckConstraint("ck_consentimento_versao_do_termo", "btrim(versao_do_termo) <> ''");
                });
                c.WithOwner().HasForeignKey("IdosoId").HasConstraintName("fk_consentimento_idoso");
                c.HasKey(x => x.Id).HasName("pk_consentimento");
                c.Property(x => x.Id).ValueGeneratedNever();
                c.Property(x => x.VersaoDoTermo).HasMaxLength(20);
                c.OwnsOne(x => x.Autorizacoes);
                c.Navigation(x => x.Autorizacoes).IsRequired();
                c.HasIndex("IdosoId").IsUnique()
                    .HasDatabaseName("ux_consentimento_vigente")
                    .HasFilter("revogado_em IS NULL");
            });

            e.OwnsOne(i => i.Vizinho, v =>
            {
                v.ToTable("vizinho_de_confianca", t =>
                {
                    t.HasCheckConstraint("ck_vizinho_de_confianca_nome", "btrim(nome) <> ''");
                    t.HasCheckConstraint("ck_vizinho_de_confianca_telefone", "btrim(telefone) <> ''");
                    t.HasCheckConstraint("ck_vizinho_de_confianca_sem_cpf_em_claro", $"concat_ws(' ', nome, telefone) {Sql.Cpf}");
                });
                v.WithOwner().HasForeignKey("IdosoId").HasConstraintName("fk_vizinho_de_confianca_idoso");
                v.HasKey("IdosoId").HasName("pk_vizinho_de_confianca");
                v.Property<IdosoId>("IdosoId").HasColumnName("idoso_id");
                v.Property(x => x.Nome).HasConversion(Rotulado(TipoDeDado.Nome)).HasMaxLength(120);
                v.Property(x => x.Telefone).HasConversion(Rotulado(TipoDeDado.Telefone)).HasMaxLength(20);
            });
        });

        b.Entity<Visita>(e =>
        {
            e.ToTable("visita", t =>
            {
                t.HasCheckConstraint("ck_visita_acs_id", $"acs_id {Sql.Acs}");
                t.HasCheckConstraint("ck_visita_desfecho", "desfecho IN ('EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada')");
                t.HasCheckConstraint("ck_visita_sem_cpf_em_claro", $"observacao {Sql.Cpf}");
            });
            e.HasKey(v => v.Id).HasName("pk_visita");
            e.Property(v => v.Id).ValueGeneratedNever();
            e.HasOne<Idoso>().WithMany()
                .HasForeignKey(v => v.IdosoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_visita_idoso");
            e.HasIndex(v => new { v.IdosoId, v.OcorridaEm }).HasDatabaseName("ix_visita_idoso_ocorrida");
            e.Property(v => v.Observacao)
                .HasConversion(Rotulado(TipoDeDado.Saude, TipoDeDado.MoraSozinho, TipoDeDado.Nome, TipoDeDado.Endereco, TipoDeDado.Telefone))
                .HasMaxLength(500);
        });

        b.Entity<MensagemSaida>(e =>
        {
            e.ToTable("mensagem_saida", t =>
            {
                t.HasCheckConstraint("ck_mensagem_saida_canal", $"canal IN {Sql.Canais}");
                t.HasCheckConstraint("ck_mensagem_saida_acs_id", $"acs_id {Sql.Acs}");
                t.HasCheckConstraint("ck_mensagem_saida_veredito", "veredito IN ('Liberada', 'Bloqueada')");
                t.HasCheckConstraint("ck_mensagem_saida_tipos_de_dado", $"tipos_de_dado <@ {Sql.TiposDeDado}");
                t.HasCheckConstraint("ck_mensagem_saida_bloqueada_tem_tipo", "veredito = 'Liberada' OR cardinality(tipos_de_dado) > 0");
                t.HasCheckConstraint("ck_mensagem_saida_sem_cpf_em_claro", $"texto_mascarado {Sql.Cpf}");
            });
            e.HasKey(m => m.Id).HasName("pk_mensagem_saida");
            e.Property(m => m.Id).ValueGeneratedNever();
            e.Property(m => m.TextoMascarado).HasMaxLength(1000);
            e.PrimitiveCollection(m => m.TiposDeDado).ElementType().HasConversion<string>();
        });

        b.Entity<AvisoAoVizinho>(e =>
        {
            e.ToTable("aviso_ao_vizinho", t =>
            {
                t.HasCheckConstraint("ck_aviso_ao_vizinho_casa_token", $"casa_token {Sql.Token}");
                t.HasCheckConstraint("ck_aviso_ao_vizinho_motivo", "motivo IN ('EnergiaEletrodependente', 'SemHeartbeat', 'CalorInterno', 'SemMovimentoNaJanela')");
                t.HasCheckConstraint("ck_aviso_ao_vizinho_resposta", "resposta IN ('EstaBem', 'PrecisaDeAjuda')");
                t.HasCheckConstraint("ck_aviso_ao_vizinho_resposta_com_hora", "(resposta IS NULL) = (respondido_em IS NULL)");
            });
            e.HasKey(a => a.Id).HasName("pk_aviso_ao_vizinho");
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.Microarea).HasColumnName("microarea_id");
            e.HasOne<Microarea>().WithMany()
                .HasForeignKey(a => a.Microarea)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_aviso_ao_vizinho_microarea");
            e.HasIndex(a => new { a.CasaToken, a.Motivo, a.Desde }).IsUnique().HasDatabaseName("ux_aviso_ao_vizinho_episodio");
            e.HasIndex(a => new { a.Microarea, a.RegistradoEm }).HasDatabaseName("ix_aviso_ao_vizinho_microarea_registrado");
            e.Property(a => a.Versao).IsRowVersion();
        });
    }
}
