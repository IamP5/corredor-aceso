using Corredor.Cadastro.Dominio;
using Corredor.Contratos;
using Corredor.Guarda;
using Corredor.Infra;
using Corredor.Motor.Dominio;
using Microsoft.EntityFrameworkCore;

public sealed record LeituraDeEvento(CasaToken CasaToken, TipoDePacote Tipo, DateTimeOffset OcorridoEm, TempC? TempC, TensaoV? TensaoV);

public sealed record LinhaDaListaDeNomes(CasaToken? Token, TextoRotulado Nome, TextoRotulado Endereco, TextoRotulado? NomeDoVizinho, TextoRotulado? TelefoneDoVizinho);

public sealed record ContagemDoPainel(TipoDeEventoDeSeguranca Tipo, Canal Canal, short Severidade, int Quantidade);

public static class Acesso
{
	public static async Task<(int CasaAtiva, int EventoNovo)> RegistrarPacoteAsync(
		MotorDbContext db, CasaToken token, long seq, TipoDePacote tipo, DateTimeOffset ocorridoEm, DateTimeOffset recebidoEm,
		TempC? tempC, TensaoV? tensaoV, bool bufferizado, CancellationToken ct = default)
	{
		await using var tx = await db.Database.BeginTransactionAsync(ct);
		var casaAtiva = await db.Casas
			.Where(c => c.Token == token && c.DesativadaEm == null)
			.ExecuteUpdateAsync(s => s.SetProperty(
				c => c.UltimoPacoteEm,
				c => c.UltimoPacoteEm == null || c.UltimoPacoteEm < recebidoEm ? recebidoEm : c.UltimoPacoteEm), ct);
		var eventoNovo = 0;
		if (casaAtiva == 1 && tipo != TipoDePacote.Heartbeat)
		{
			eventoNovo = await db.Database.ExecuteSqlInterpolatedAsync($"""
				INSERT INTO motor.evento_sensor (casa_token, ocorrido_em, seq, tipo, recebido_em, temp_c, tensao_v, bufferizado)
				VALUES ({token.Valor}, {ocorridoEm.ToUniversalTime()}, {seq}, {tipo.ToString()}, {recebidoEm.ToUniversalTime()}, {tempC?.Valor}, {tensaoV?.Valor}, {bufferizado})
				ON CONFLICT (casa_token, ocorrido_em, seq) DO NOTHING
				""", ct);
		}

		await tx.CommitAsync(ct);
		return (casaAtiva, eventoNovo);
	}

	public static async Task DesativarCasaAsync(MotorDbContext db, CasaToken token, DateTimeOffset agora, CancellationToken ct = default)
	{
		await using var tx = await db.Database.BeginTransactionAsync(ct);
		var casa = await db.Casas.SingleAsync(c => c.Token == token, ct);
		casa.Desativar(agora);
		await db.SaveChangesAsync(ct);
		await db.Eventos.Where(e => e.CasaToken == token).ExecuteDeleteAsync(ct);
		await tx.CommitAsync(ct);
	}

	public static async Task ExpurgarAsync(MotorDbContext db, string sqlDoExpurgo, DateTimeOffset corte, decimal tensaoMinima, CancellationToken ct = default)
	{
		await using var tx = await db.Database.BeginTransactionAsync(ct);
		await db.Database.ExecuteSqlRawAsync(sqlDoExpurgo, new object[] { corte, tensaoMinima }, ct);
		await tx.CommitAsync(ct);
	}

	public static IQueryable<CasaMonitorada> CasasAtivas(MotorDbContext db, MicroareaId microarea) =>
		db.Casas.AsNoTracking()
			.Where(c => c.Microarea == microarea && c.DesativadaEm == null);

	public static IQueryable<LeituraDeEvento> EventosDaAvaliacao(MotorDbContext db, MicroareaId microarea, DateTimeOffset desde, TensaoV tensaoMinima) =>
		db.Eventos.AsNoTracking()
			.Where(e => db.Casas.Any(c => c.Token == e.CasaToken && c.Microarea == microarea && c.DesativadaEm == null))
			.Where(e => e.OcorridoEm >= desde || e.TensaoV < tensaoMinima)
			.Select(e => new LeituraDeEvento(e.CasaToken, e.Tipo, e.OcorridoEm, e.TempC, e.TensaoV));

	public static IQueryable<DiaCritico> DiaCriticoDe(MotorDbContext db, DateOnly hoje) =>
		db.DiasCriticos.AsNoTracking()
			.Where(d => d.Data == hoje);

	public static IQueryable<EventoSensor> LinhaDoTempo(MotorDbContext db, CasaToken token, DateTimeOffset desde) =>
		db.Eventos.AsNoTracking()
			.Where(e => e.CasaToken == token && e.OcorridoEm >= desde)
			.OrderBy(e => e.OcorridoEm).ThenBy(e => e.Seq);

	public static IQueryable<LinhaDaListaDeNomes> ListaDeNomes(CadastroDbContext db, MicroareaId microarea) =>
		db.Idosos.AsNoTracking()
			.Where(i => i.Microarea == microarea && i.Casa.Token != null)
			.Select(i => new LinhaDaListaDeNomes(i.Casa.Token, i.Nome, i.Casa.Endereco, i.Vizinho!.Nome, i.Vizinho!.Telefone));

	public static IQueryable<Idoso> IdosoDoToken(CadastroDbContext db, CasaToken token) =>
		db.Idosos
			.Where(i => i.Casa.Token == token);

	public static IQueryable<AvisoAoVizinho> AvisosDoDia(CadastroDbContext db, MicroareaId microarea, DateTimeOffset desde) =>
		db.Avisos.AsNoTracking()
			.Where(a => a.Microarea == microarea && a.RegistradoEm >= desde)
			.OrderBy(a => a.RegistradoEm);

	public static IQueryable<EventoDeSeguranca> EventosDoPainel(GuardaDbContext db, DateTimeOffset desde) =>
		db.Eventos.AsNoTracking()
			.Where(e => e.OcorridoEm >= desde)
			.OrderByDescending(e => e.OcorridoEm)
			.Take(50);

	public static IQueryable<ContagemDoPainel> ContagemDoPainel(GuardaDbContext db, DateTimeOffset desde) =>
		db.Eventos
			.Where(e => e.OcorridoEm >= desde)
			.GroupBy(e => new { e.Tipo, e.Canal, e.Severidade })
			.Select(g => new ContagemDoPainel(g.Key.Tipo, g.Key.Canal, g.Key.Severidade, g.Count()));
}
