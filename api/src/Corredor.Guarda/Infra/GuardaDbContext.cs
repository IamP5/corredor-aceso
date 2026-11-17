using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Corredor.Guarda.Infra;

public sealed class GuardaDbContext(DbContextOptions<GuardaDbContext> options) : DbContext(options)
{
    public const string Esquema = "guarda";

    public DbSet<EventoDeSeguranca> Eventos => Set<EventoDeSeguranca>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<DateTimeOffset>().HaveConversion<UtcConverter>();
        c.Properties<Enum>().HaveConversion<string>();
        c.Conventions.Add(_ => new ConvencaoDeClassificacao(Classificacao));
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Esquema);

        b.Entity<EventoDeSeguranca>(e =>
        {
            e.ToTable("evento_de_seguranca", t =>
            {
                t.HasCheckConstraint("ck_evento_de_seguranca_tipo", "tipo IN ('MascaramentoNaEntrada', 'BloqueioDeSaida')");
                t.HasCheckConstraint("ck_evento_de_seguranca_tipos_de_dado", $"cardinality(tipos_de_dado) > 0 AND tipos_de_dado <@ {TiposDeDadoSql}");
                t.HasCheckConstraint("ck_evento_de_seguranca_canal", $"canal IN {CanaisSql}");
                t.HasCheckConstraint("ck_evento_de_seguranca_modulo", "btrim(modulo) <> ''");
                t.HasCheckConstraint("ck_evento_de_seguranca_rota_sem_identificador", "rota !~ '[?]|[0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-'");
                t.HasCheckConstraint("ck_evento_de_seguranca_severidade", "severidade BETWEEN 1 AND 5");
                t.HasCheckConstraint("ck_evento_de_seguranca_mensagem_origem", "mensagem_origem IN ('Llm', 'FraseFixa')");
                t.HasCheckConstraint("ck_evento_de_seguranca_mensagem_inteira", "(mensagem_texto IS NULL) = (mensagem_origem IS NULL)");
            });
            e.HasKey(x => x.Id).HasName("pk_evento_de_seguranca");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.PrimitiveCollection(x => x.TiposDeDado).ElementType().HasConversion<string>();
            e.Property(x => x.Modulo).HasMaxLength(40);
            e.Property(x => x.Rota).HasMaxLength(200);
            e.Ignore(x => x.Pontuacao);
            e.HasIndex(x => x.OcorridoEm).IsDescending().HasDatabaseName("ix_evento_de_seguranca_ocorrido_em");
            e.OwnsOne(x => x.Mensagem, m => m.Property(x => x.Texto).HasMaxLength(Tradutor.TamanhoMaximo));
        });
    }

    private const string CanaisSql =
        "('Log', 'RespostaSemAutenticacao', 'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')";

    private const string TiposDeDadoSql =
        "ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial']";

    /// <summary>A classificação de cada coluna, gravada como comentário. Coluna nova sem entrada derruba o modelo.</summary>
    public static readonly IReadOnlyDictionary<string, string> Classificacao = new Dictionary<string, string>
    {
        ["evento_de_seguranca.id"] = "classe=nenhum",
        ["evento_de_seguranca.ocorrido_em"] = "classe=nenhum",
        ["evento_de_seguranca.tipo"] = "classe=nenhum",
        ["evento_de_seguranca.tipos_de_dado"] = "classe=nenhum",
        ["evento_de_seguranca.canal"] = "classe=nenhum",
        ["evento_de_seguranca.modulo"] = "classe=nenhum",
        ["evento_de_seguranca.rota"] = "classe=nenhum",
        ["evento_de_seguranca.severidade"] = "classe=nenhum",
        ["evento_de_seguranca.mensagem_texto"] = "classe=nenhum",
        ["evento_de_seguranca.mensagem_origem"] = "classe=nenhum",
    };
}

// O Npgsql recusa gravar DateTimeOffset com deslocamento diferente de zero.
internal sealed class UtcConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);

/// <summary>Grava a classificação de cada coluna no comentário dela.</summary>
public sealed class ConvencaoDeClassificacao(IReadOnlyDictionary<string, string> classes) : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entidade in modelBuilder.Metadata.GetEntityTypes())
        {
            var tabela = entidade.GetTableName();
            if (tabela is null)
            {
                continue;
            }

            var loja = StoreObjectIdentifier.Table(tabela, entidade.GetSchema());
            foreach (var propriedade in entidade.GetDeclaredProperties())
            {
                var coluna = propriedade.GetColumnName(loja);
                if (coluna is null or "xmin")
                {
                    continue;
                }

                if (!classes.TryGetValue($"{tabela}.{coluna}", out var classe))
                {
                    throw new InvalidOperationException($"coluna sem classificacao: {tabela}.{coluna}");
                }

                propriedade.Builder.HasComment(classe);
            }
        }
    }
}
