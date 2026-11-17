using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Corredor.Contratos
{
	public readonly record struct CasaToken
	{
		private static readonly Regex Formato = new("^[0-9a-f]{32}$", RegexOptions.Compiled);

		private CasaToken(string valor) => Valor = valor;

		public string Valor { get; }

		public static bool TryParse(string? texto, out CasaToken token)
		{
			var valido = texto is not null && Formato.IsMatch(texto);
			token = valido ? new CasaToken(texto!) : default;
			return valido;
		}

		public static CasaToken Novo() =>
			new(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant());

		public override string ToString() => Valor;
	}

	public readonly record struct MicroareaId(int Valor);

	public enum PerfilDeContato { Sozinho, Acamado, Coabitado }

	public sealed record PerfilDaCasa(PerfilDeContato Contato, bool Eletrodependente);

	public enum OrigemDaConfirmacao { Acs, Vizinho }

	public enum ResultadoDaConfirmacao { PessoaVista, AusenciaConfirmada }

	public sealed record Confirmacao(
		Guid Id,
		OrigemDaConfirmacao Origem,
		ResultadoDaConfirmacao Resultado,
		DateTimeOffset Quando);

	public enum MotivoDoAlerta { EnergiaEletrodependente, SemHeartbeat, CalorInterno, SemMovimentoNaJanela }
}

namespace Corredor.Motor.Dominio
{
	using Corredor.Contratos;

	public enum TipoDePacote { Movimento, BotaoBem, BotaoSair, Telemetria, Heartbeat }

	public enum OrigemDoDiaCritico { Modelo, RegraBase, Manual }

	public readonly record struct Probabilidade(decimal Valor);

	// TempC e TensaoV sao objetos de valor. O operador de comparacao deixa o LINQ escrever
	// e.TensaoV < minima, e o EF Core traduz para tensao_v < @minima pelo conversor.
	public readonly record struct TempC(decimal Valor)
	{
		public static bool operator <(TempC a, TempC b) => a.Valor < b.Valor;
		public static bool operator >(TempC a, TempC b) => a.Valor > b.Valor;
		public static bool operator <=(TempC a, TempC b) => a.Valor <= b.Valor;
		public static bool operator >=(TempC a, TempC b) => a.Valor >= b.Valor;
	}

	public readonly record struct TensaoV(decimal Valor)
	{
		public static bool operator <(TensaoV a, TensaoV b) => a.Valor < b.Valor;
		public static bool operator >(TensaoV a, TensaoV b) => a.Valor > b.Valor;
		public static bool operator <=(TensaoV a, TensaoV b) => a.Valor <= b.Valor;
		public static bool operator >=(TensaoV a, TensaoV b) => a.Valor >= b.Valor;
	}

	public sealed record ResumoDoTurno(
		int CasasAtivas,
		int ComSinalDeVidaAte10h,
		int Atendidas,
		int VermelhasPendentes,
		int CinzasPendentes);

	public sealed class CasaMonitorada
	{
		private readonly List<Confirmacao> _confirmacoes = new();

		private CasaMonitorada() { }

		public CasaToken Token { get; private set; }
		public MicroareaId Microarea { get; private set; }
		public PerfilDaCasa Perfil { get; private set; } = null!;
		public DateTimeOffset AtivadaEm { get; private set; }
		public DateTimeOffset? DesativadaEm { get; private set; }
		public DateTimeOffset? UltimoPacoteEm { get; private set; }
		public IReadOnlyCollection<Confirmacao> Confirmacoes => _confirmacoes;

		public static CasaMonitorada Ativar(CasaToken token, MicroareaId microarea, PerfilDaCasa perfil, DateTimeOffset agora) =>
			new() { Token = token, Microarea = microarea, Perfil = perfil, AtivadaEm = agora };

		public void DefinirPerfil(PerfilDaCasa perfil) => Perfil = perfil;

		public void Confirmar(Confirmacao confirmacao)
		{
			if (_confirmacoes.All(c => c.Id != confirmacao.Id))
			{
				_confirmacoes.Add(confirmacao);
			}
		}

		public void Desativar(DateTimeOffset agora)
		{
			DesativadaEm ??= agora;
			_confirmacoes.Clear();
		}
	}

	public sealed class EventoSensor
	{
		private EventoSensor() { }

		public CasaToken CasaToken { get; private set; }
		public long Seq { get; private set; }
		public TipoDePacote Tipo { get; private set; }
		public DateTimeOffset OcorridoEm { get; private set; }
		public DateTimeOffset RecebidoEm { get; private set; }
		public TempC? TempC { get; private set; }
		public TensaoV? TensaoV { get; private set; }
		public bool Bufferizado { get; private set; }
	}

	public sealed class DiaCritico
	{
		private DiaCritico() { }

		public DateOnly Data { get; private set; }
		public Probabilidade Probabilidade { get; private set; }
		public string Motivo { get; private set; } = null!;
		public OrigemDoDiaCritico Origem { get; private set; }
		public DateTimeOffset AbertoEm { get; private set; }
		public DateTimeOffset? ToquePedidoEm { get; private set; }
		public DateTimeOffset? EncerradoEm { get; private set; }
		public ResumoDoTurno? Resumo { get; private set; }
		public uint Versao { get; private set; }

		public static DiaCritico Declarar(DateOnly data, Probabilidade probabilidade, string motivo, OrigemDoDiaCritico origem, DateTimeOffset agora) =>
			new() { Data = data, Probabilidade = probabilidade, Motivo = motivo, Origem = origem, AbertoEm = agora };

		public void PedirToque(DateTimeOffset agora) => ToquePedidoEm ??= agora;

		public ResumoDoTurno Encerrar(ResumoDoTurno resumo, DateTimeOffset agora)
		{
			if (EncerradoEm is null)
			{
				EncerradoEm = agora;
				Resumo = resumo;
			}

			return Resumo!;
		}
	}
}

namespace Corredor.Guarda
{
	public enum TipoDeDado { Cpf, Rg, Cns, Saude, MoraSozinho, Endereco, Telefone, Nome, Credencial }

	public enum Canal { Log, RespostaSemAutenticacao, MensagemDeErro, Mqtt, GrupoDoBairro, PromptDoLlm, Vizinho, AppAutenticado, Banco }

	public enum TipoDeEventoDeSeguranca { MascaramentoNaEntrada, BloqueioDeSaida }

	public enum OrigemDaMensagem { Llm, FraseFixa }

	public sealed record MensagemAoCidadao(string Texto, OrigemDaMensagem Origem);

	public sealed class TextoRotulado
	{
		private TextoRotulado(string valor, IEnumerable<TipoDeDado> rotulos)
		{
			Valor = valor;
			Rotulos = rotulos.ToHashSet();
		}

		internal string Valor { get; }
		public IReadOnlySet<TipoDeDado> Rotulos { get; }

		public static TextoRotulado Do(string valor, params TipoDeDado[] rotulos) => new(valor, rotulos);

		internal static TextoRotulado DoBanco(string valor, TipoDeDado[] rotulosDaColuna) => new(valor, rotulosDaColuna);

		public override string ToString() =>
			Rotulos.Count == 0 ? Valor : string.Join(' ', Rotulos.Order().Select(r => $"[{r.ToString().ToUpperInvariant()}]"));
	}

	public sealed class EventoDeSeguranca
	{
		private EventoDeSeguranca() { }

		public Guid Id { get; private set; }
		public DateTimeOffset OcorridoEm { get; private set; }
		public TipoDeEventoDeSeguranca Tipo { get; private set; }
		public List<TipoDeDado> TiposDeDado { get; private set; } = new();
		public Canal Canal { get; private set; }
		public string Modulo { get; private set; } = null!;
		public string? Rota { get; private set; }
		public short Severidade { get; private set; }
		public MensagemAoCidadao? Mensagem { get; private set; }

		public static EventoDeSeguranca Registrar(TipoDeEventoDeSeguranca tipo, Canal canal, IEnumerable<TipoDeDado> tipos, short severidade, string modulo, string? rota, DateTimeOffset agora) =>
			new()
			{
				Id = Guid.NewGuid(), OcorridoEm = agora, Tipo = tipo, Canal = canal,
				TiposDeDado = tipos.Distinct().ToList(), Severidade = severidade, Modulo = modulo, Rota = rota,
			};

		public void AnexarMensagem(MensagemAoCidadao mensagem) => Mensagem ??= mensagem;
	}
}

namespace Corredor.Cadastro.Dominio
{
	using Corredor.Contratos;
	using Corredor.Guarda;

	public readonly record struct IdosoId(Guid Valor);

	public readonly record struct AcsId(string Valor);

	public enum Desfecho { EstavaBem, EstavaFora, NaoAbriu, Encaminhada }

	public enum RespostaDoVizinho { EstaBem, PrecisaDeAjuda }

	public enum VereditoDaMensagem { Liberada, Bloqueada }

	public sealed record Autorizacoes(bool AvisarVizinho, bool AvisarFamilia);

	public sealed class Microarea
	{
		private Microarea() { }

		public MicroareaId Id { get; private set; }
		public string Nome { get; private set; } = null!;
		public AcsId AcsResponsavel { get; private set; }
	}

	public sealed class Casa
	{
		private Casa() { }

		public Casa(TextoRotulado endereco) => Endereco = endereco;

		public TextoRotulado Endereco { get; private set; } = null!;
		public CasaToken? Token { get; private set; }

		internal void ReceberToken(CasaToken token) => Token = token;

		internal void ApagarToken() => Token = null;
	}

	public sealed class Consentimento
	{
		private Consentimento() { }

		internal Consentimento(AcsId colhidoPor, string versaoDoTermo, Autorizacoes autorizacoes, DateTimeOffset agora)
		{
			Id = Guid.NewGuid();
			ColhidoPor = colhidoPor;
			ColhidoEm = agora;
			VersaoDoTermo = versaoDoTermo;
			Autorizacoes = autorizacoes;
		}

		public Guid Id { get; private set; }
		public AcsId ColhidoPor { get; private set; }
		public DateTimeOffset ColhidoEm { get; private set; }
		public string VersaoDoTermo { get; private set; } = null!;
		public Autorizacoes Autorizacoes { get; private set; } = null!;
		public DateTimeOffset? RevogadoEm { get; private set; }

		internal void Revogar(DateTimeOffset agora) => RevogadoEm ??= agora;
	}

	public sealed class VizinhoDeConfianca
	{
		private VizinhoDeConfianca() { }

		public VizinhoDeConfianca(TextoRotulado nome, TextoRotulado telefone, DateTimeOffset aceitouEm)
		{
			Nome = nome;
			Telefone = telefone;
			AceitouEm = aceitouEm;
		}

		public TextoRotulado Nome { get; private set; } = null!;
		public TextoRotulado Telefone { get; private set; } = null!;
		public DateTimeOffset AceitouEm { get; private set; }
	}

	public sealed class Idoso
	{
		private readonly List<Consentimento> _consentimentos = new();

		private Idoso() { }

		public IdosoId Id { get; private set; }
		public MicroareaId Microarea { get; private set; }
		public TextoRotulado Nome { get; private set; } = null!;
		public TextoRotulado? Condicoes { get; private set; }
		public TextoRotulado? Medicamentos { get; private set; }
		public PerfilDaCasa Perfil { get; private set; } = null!;
		public Casa Casa { get; private set; } = null!;
		public IReadOnlyCollection<Consentimento> Consentimentos => _consentimentos;
		public VizinhoDeConfianca? Vizinho { get; private set; }
		public uint Versao { get; private set; }

		public Consentimento? ConsentimentoVigente => _consentimentos.SingleOrDefault(c => c.RevogadoEm is null);

		public static Idoso Cadastrar(MicroareaId microarea, string nome, string endereco, PerfilDaCasa perfil, string? condicoes, string? medicamentos) =>
			new()
			{
				Id = new IdosoId(Guid.NewGuid()),
				Microarea = microarea,
				Nome = TextoRotulado.Do(nome, TipoDeDado.Nome),
				Casa = new Casa(TextoRotulado.Do(endereco, TipoDeDado.Endereco)),
				Perfil = perfil,
				Condicoes = condicoes is null ? null : TextoRotulado.Do(condicoes, TipoDeDado.Saude),
				Medicamentos = medicamentos is null ? null : TextoRotulado.Do(medicamentos, TipoDeDado.Saude),
			};

		public CasaToken RegistrarConsentimento(AcsId acs, string versaoDoTermo, Autorizacoes autorizacoes, DateTimeOffset agora)
		{
			if (ConsentimentoVigente is null)
			{
				_consentimentos.Add(new Consentimento(acs, versaoDoTermo, autorizacoes, agora));
				Casa.ReceberToken(CasaToken.Novo());
			}

			return Casa.Token!.Value;
		}

		public void IndicarVizinho(string nome, string telefone, DateTimeOffset aceitouEm) =>
			Vizinho = new VizinhoDeConfianca(TextoRotulado.Do(nome, TipoDeDado.Nome), TextoRotulado.Do(telefone, TipoDeDado.Telefone), aceitouEm);

		public void RevogarConsentimento(DateTimeOffset agora)
		{
			ConsentimentoVigente?.Revogar(agora);
			Casa.ApagarToken();
			Vizinho = null;
		}
	}

	public sealed class Visita
	{
		private Visita() { }

		public Guid Id { get; private set; }
		public IdosoId IdosoId { get; private set; }
		public AcsId AcsId { get; private set; }
		public DateTimeOffset OcorridaEm { get; private set; }
		public DateTimeOffset RegistradaEm { get; private set; }
		public Desfecho Desfecho { get; private set; }
		public TextoRotulado? Observacao { get; private set; }

		public static Visita Registrar(Guid id, IdosoId idoso, AcsId acs, Desfecho desfecho, TextoRotulado? observacao, DateTimeOffset ocorridaEm, DateTimeOffset agora) =>
			new() { Id = id, IdosoId = idoso, AcsId = acs, Desfecho = desfecho, Observacao = observacao, OcorridaEm = ocorridaEm, RegistradaEm = agora };
	}

	public sealed class MensagemSaida
	{
		private MensagemSaida() { }

		public Guid Id { get; private set; }
		public Canal Canal { get; private set; }
		public AcsId AcsId { get; private set; }
		public string TextoMascarado { get; private set; } = null!;
		public VereditoDaMensagem Veredito { get; private set; }
		public List<TipoDeDado> TiposDeDado { get; private set; } = new();
		public DateTimeOffset VerificadaEm { get; private set; }

		public static MensagemSaida Verificar(Canal canal, AcsId acs, string textoMascarado, IEnumerable<TipoDeDado> tipos, DateTimeOffset agora)
		{
			var achados = tipos.Distinct().ToList();
			return new()
			{
				Id = Guid.NewGuid(), Canal = canal, AcsId = acs, TextoMascarado = textoMascarado, TiposDeDado = achados,
				Veredito = achados.Count == 0 ? VereditoDaMensagem.Liberada : VereditoDaMensagem.Bloqueada, VerificadaEm = agora,
			};
		}
	}

	public sealed class AvisoAoVizinho
	{
		private AvisoAoVizinho() { }

		public Guid Id { get; private set; }
		public CasaToken CasaToken { get; private set; }
		public MicroareaId Microarea { get; private set; }
		public MotivoDoAlerta Motivo { get; private set; }
		public DateTimeOffset Desde { get; private set; }
		public DateTimeOffset RegistradoEm { get; private set; }
		public RespostaDoVizinho? Resposta { get; private set; }
		public DateTimeOffset? RespondidoEm { get; private set; }
		public uint Versao { get; private set; }

		public static AvisoAoVizinho Registrar(CasaToken token, MicroareaId microarea, MotivoDoAlerta motivo, DateTimeOffset desde, DateTimeOffset agora) =>
			new() { Id = Guid.NewGuid(), CasaToken = token, Microarea = microarea, Motivo = motivo, Desde = desde, RegistradoEm = agora };

		public void RegistrarResposta(RespostaDoVizinho resposta, DateTimeOffset agora)
		{
			Resposta ??= resposta;
			RespondidoEm ??= agora;
		}
	}
}
