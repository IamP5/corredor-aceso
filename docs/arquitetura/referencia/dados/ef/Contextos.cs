using Corredor.Cadastro.Dominio;
using Corredor.Contratos;
using Corredor.Guarda;
using Corredor.Motor.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using static Corredor.Infra.Sql;

namespace Corredor.Infra;

public sealed class CasaTokenConverter() : ValueConverter<CasaToken, string>(t => t.Valor, s => Ler(s))
{
	private static CasaToken Ler(string s) =>
		CasaToken.TryParse(s, out var token) ? token : throw new InvalidOperationException("casa_token fora do formato");
}

public sealed class MicroareaIdConverter() : ValueConverter<MicroareaId, int>(m => m.Valor, v => new MicroareaId(v));

public sealed class IdosoIdConverter() : ValueConverter<IdosoId, Guid>(i => i.Valor, v => new IdosoId(v));

public sealed class AcsIdConverter() : ValueConverter<AcsId, string>(a => a.Valor, v => new AcsId(v));

// O Npgsql recusa gravar DateTimeOffset com deslocamento diferente de zero. A rota recebe -03:00.
public sealed class UtcConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);

public sealed class TempCConverter() : ValueConverter<TempC, decimal>(t => t.Valor, v => new TempC(v));

public sealed class TensaoVConverter() : ValueConverter<TensaoV, decimal>(t => t.Valor, v => new TensaoV(v));

public sealed class ConversorDeTextoRotulado(params TipoDeDado[] rotulosDaColuna)
	: ValueConverter<TextoRotulado, string>(t => t.Valor, s => TextoRotulado.DoBanco(s, rotulosDaColuna));

public static class Sql
{
	public static ValueConverter Rotulado(params TipoDeDado[] rotulosDaColuna) => new ConversorDeTextoRotulado(rotulosDaColuna);

	public const string Token = "~ '^[0-9a-f]{32}$'";
	public const string Acs = "~ '^[a-z0-9-]{3,40}$'";
	public const string Cpf = "!~ '[0-9]{3}\\.[0-9]{3}\\.[0-9]{3}-[0-9]{2}'";
	public const string PerfisDeContato = "('Sozinho', 'Acamado', 'Coabitado')";
	public const string Canais = "('Log', 'RespostaSemAutenticacao', 'MensagemDeErro', 'Mqtt', 'GrupoDoBairro', 'PromptDoLlm', 'Vizinho', 'AppAutenticado', 'Banco')";
	public const string TiposDeDado = "ARRAY['Cpf', 'Rg', 'Cns', 'Saude', 'MoraSozinho', 'Endereco', 'Telefone', 'Nome', 'Credencial']";
}

public sealed class MotorDbContext(DbContextOptions<MotorDbContext> options) : DbContext(options)
{
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
		b.HasDefaultSchema("motor");

		b.Entity<CasaMonitorada>(e =>
		{
			e.ToTable("casa_monitorada", t =>
			{
				t.HasCheckConstraint("ck_casa_monitorada_token", $"casa_token {Sql.Token}");
				t.HasCheckConstraint("ck_casa_monitorada_microarea", "microarea_id > 0");
				t.HasCheckConstraint("ck_casa_monitorada_perfil_contato", $"perfil_contato IN {Sql.PerfisDeContato}");
			});
			e.HasKey(c => c.Token).HasName("pk_casa_monitorada");
			e.Property(c => c.Token).HasColumnName("casa_token");
			e.Property(c => c.Microarea).HasColumnName("microarea_id");
			e.ComplexProperty(c => c.Perfil);
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
			e.Property(d => d.Motivo).HasMaxLength(200);
			e.Property(d => d.Versao).IsRowVersion();
			e.OwnsOne(d => d.Resumo, r =>
				r.Property(x => x.ComSinalDeVidaAte10h).HasColumnName("resumo_com_sinal_de_vida_ate_10h"));
		});
	}
}

public sealed class CadastroDbContext(DbContextOptions<CadastroDbContext> options) : DbContext(options)
{
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
		b.HasDefaultSchema("cadastro");

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

public sealed class GuardaDbContext(DbContextOptions<GuardaDbContext> options) : DbContext(options)
{
	public DbSet<EventoDeSeguranca> Eventos => Set<EventoDeSeguranca>();

	protected override void ConfigureConventions(ModelConfigurationBuilder c)
	{
		c.Properties<DateTimeOffset>().HaveConversion<UtcConverter>();
		c.Properties<Enum>().HaveConversion<string>();
		c.Conventions.Add(_ => new ConvencaoDeClassificacao(Classificacao.Guarda));
	}

	protected override void OnModelCreating(ModelBuilder b)
	{
		b.HasDefaultSchema("guarda");

		b.Entity<EventoDeSeguranca>(e =>
		{
			e.ToTable("evento_de_seguranca", t =>
			{
				t.HasCheckConstraint("ck_evento_de_seguranca_tipo", "tipo IN ('MascaramentoNaEntrada', 'BloqueioDeSaida')");
				t.HasCheckConstraint("ck_evento_de_seguranca_tipos_de_dado", $"cardinality(tipos_de_dado) > 0 AND tipos_de_dado <@ {Sql.TiposDeDado}");
				t.HasCheckConstraint("ck_evento_de_seguranca_canal", $"canal IN {Sql.Canais}");
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
			e.HasIndex(x => x.OcorridoEm).IsDescending().HasDatabaseName("ix_evento_de_seguranca_ocorrido_em");
			e.OwnsOne(x => x.Mensagem, m => m.Property(x => x.Texto).HasMaxLength(400));
		});
	}
}
