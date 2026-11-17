using System.Data;
using Corredor.Cadastro.Dominio;
using Corredor.Contratos;
using Corredor.Guarda;
using Corredor.Infra;
using Corredor.Motor.Dominio;
using Microsoft.EntityFrameworkCore;

public static class Consultas
{
	private static int _falhas;

	public static async Task RodarAsync(Func<MotorDbContext> novoMotor, Func<CadastroDbContext> novoCadastro, Func<GuardaDbContext> novaGuarda)
	{
		var agora = new DateTimeOffset(2026, 11, 18, 13, 31, 0, TimeSpan.Zero);
		var microarea = new MicroareaId(3);
		var acs = new AcsId("acs-demo-01");

		Titulo("1. Agregado Idoso: consentimento, vizinho, revogacao, novo consentimento");
		IdosoId idosoId;
		CasaToken primeiroToken;
		await using (var db = novoCadastro())
		{
			var idoso = Idoso.Cadastrar(microarea, "Aparecida Jatoba", "Rua Ficticia das Acacias, 12", new PerfilDaCasa(PerfilDeContato.Sozinho, true), "usa concentrador de oxigenio", null);
			primeiroToken = idoso.RegistrarConsentimento(acs, "2026-11", new Autorizacoes(true, false), agora.ToOffset(TimeSpan.FromHours(-3)));
			idoso.IndicarVizinho("Rosa Ipe", "(00) 90000-0001", agora.AddDays(-2));
			db.Add(idoso);
			await db.SaveChangesAsync();
			idosoId = idoso.Id;
		}

		await using (var db = novoCadastro())
		{
			var idoso = await db.Idosos.SingleAsync(i => i.Casa.Token == primeiroToken);
			Conferir("idoso achado pelo token, com consentimento e vizinho", idoso.Consentimentos.Count == 1 && idoso.Vizinho is not null);
			Conferir("rotulo vem da coluna: Nome", idoso.Nome.Rotulos.SetEquals(new[] { TipoDeDado.Nome }));
			Conferir("rotulo vem da coluna: Saude", idoso.Condicoes!.Rotulos.SetEquals(new[] { TipoDeDado.Saude }) && idoso.Medicamentos is null);
			Conferir("ToString() do nome devolve a mascara", idoso.Nome.ToString() == "[NOME]");
			Conferir("colhido_em gravado em UTC a partir de -03:00", idoso.Consentimentos.Single().ColhidoEm == agora && idoso.Consentimentos.Single().ColhidoEm.Offset == TimeSpan.Zero);
			idoso.RevogarConsentimento(agora.AddMinutes(1));
			await db.SaveChangesAsync();
		}

		await using (var db = novoCadastro())
		{
			var idoso = await db.Idosos.SingleAsync(i => i.Id == idosoId);
			Conferir("revogar apaga o token, remove o vizinho e guarda o consentimento revogado",
				idoso.Casa.Token is null && idoso.Vizinho is null && idoso.Consentimentos.Single().RevogadoEm is not null);
			var novo = idoso.RegistrarConsentimento(acs, "2026-11", new Autorizacoes(false, false), agora.AddMinutes(2));
			await db.SaveChangesAsync();
			Conferir("consentimento novo gera token novo; indice parcial aceita 1 revogado + 1 vigente", novo != primeiroToken && idoso.Consentimentos.Count == 2);
		}

		Titulo("2. Concorrencia otimista com xmin (DiaCritico)");
		var hoje = new DateOnly(2026, 11, 18);
		await using (var db = novoMotor())
		{
			db.Add(DiaCritico.Declarar(hoje, new Probabilidade(0.81m), "minima de 24 C pela segunda noite", OrigemDoDiaCritico.Modelo, agora.AddHours(-5.5)));
			await db.SaveChangesAsync();
		}

		await using (var a = novoMotor())
		await using (var b = novoMotor())
		{
			var diaA = await a.DiasCriticos.SingleAsync(d => d.Data == hoje);
			var diaB = await b.DiasCriticos.SingleAsync(d => d.Data == hoje);
			diaA.Encerrar(new ResumoDoTurno(40, 34, 4, 1, 1), agora);
			await a.SaveChangesAsync();
			diaB.Encerrar(new ResumoDoTurno(40, 30, 0, 9, 1), agora.AddSeconds(1));
			var conflito = false;
			try { await b.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { conflito = true; }
			Conferir("segundo encerramento concorrente falha com DbUpdateConcurrencyException", conflito);
			b.ChangeTracker.Clear();
			var relido = await b.DiasCriticos.SingleAsync(d => d.Data == hoje);
			Conferir("repetir o caso de uso com leitura nova devolve o resumo do primeiro encerramento", relido.Encerrar(new ResumoDoTurno(0, 0, 0, 0, 0), agora).ComSinalDeVidaAte10h == 34);
		}

		Titulo("3. RegistrarPacote: UPDATE monotonico + INSERT ON CONFLICT DO NOTHING");
		var token = CasaToken.Novo();
		await using (var db = novoMotor())
		{
			db.Add(CasaMonitorada.Ativar(token, microarea, new PerfilDaCasa(PerfilDeContato.Sozinho, false), agora.AddDays(-3)));
			await db.SaveChangesAsync();
		}

		await using (var db = novoMotor())
		{
			var t2 = agora.AddSeconds(-20);
			var t1 = agora.AddSeconds(-40);
			Conferir("pacote de casa ativa avanca ultimo_pacote_em", await Acesso.RegistrarPacoteAsync(db, token, 7, TipoDePacote.Movimento, t2, t2, null, null, false) == (1, 1));
			Conferir("mesmo (token, seq, ts) de novo: casa ativa, zero linha nova", await Acesso.RegistrarPacoteAsync(db, token, 7, TipoDePacote.Movimento, t2, agora, null, null, false) == (1, 0));
			Conferir("seq 1 com ts novo depois do reinicio do no e aceito", await Acesso.RegistrarPacoteAsync(db, token, 1, TipoDePacote.Telemetria, agora, agora, new TempC(31.5m), new TensaoV(126.8m), false) == (1, 1));
			await Acesso.RegistrarPacoteAsync(db, token, 0, TipoDePacote.Heartbeat, t1, t1, null, null, false);
			var casa = await db.Casas.AsNoTracking().SingleAsync(c => c.Token == token);
			Conferir("pacote com hora de chegada menor nao faz ultimo_pacote_em voltar", casa.UltimoPacoteEm == agora);
			Conferir("token desconhecido e descartado sem gravar", await Acesso.RegistrarPacoteAsync(db, CasaToken.Novo(), 1, TipoDePacote.Movimento, agora, agora, null, null, false) == (0, 0));
			Conferir("um evento por chave", await db.Eventos.CountAsync(e => e.CasaToken == token) == 2);
		}

		Titulo("4. Confirmar e DesativarCasa pelo agregado");
		await using (var db = novoMotor())
		{
			var casa = await db.Casas.SingleAsync(c => c.Token == token);
			var confirmacao = new Confirmacao(Guid.NewGuid(), OrigemDaConfirmacao.Acs, ResultadoDaConfirmacao.PessoaVista, agora.ToOffset(TimeSpan.FromHours(-3)));
			casa.Confirmar(confirmacao);
			casa.Confirmar(confirmacao);
			await db.SaveChangesAsync();
		}

		await using (var db = novoMotor())
		{
			var casa = await db.Casas.SingleAsync(c => c.Token == token);
			Conferir("confirmacao repetida gravada uma vez e carregada com a casa", casa.Confirmacoes.Count == 1 && casa.Confirmacoes.Single().Quando == agora);
			db.ChangeTracker.Clear();
			await Acesso.DesativarCasaAsync(db, token, agora.AddMinutes(5));
		}

		await using (var db = novoMotor())
		{
			var casa = await db.Casas.AsNoTracking().SingleAsync(c => c.Token == token);
			Conferir("casa desativada fica, sem confirmacoes e sem eventos",
				casa.DesativadaEm is not null && casa.Confirmacoes.Count == 0 && !await db.Eventos.AnyAsync(e => e.CasaToken == token));
			Conferir("pacote de casa desativada e descartado", await Acesso.RegistrarPacoteAsync(db, token, 9, TipoDePacote.Movimento, agora, agora, null, null, false) == (0, 0));
		}

		Titulo("5. Colecao de TipoDeDado em text[] e AnexarMensagem");
		Guid eventoId;
		await using (var db = novaGuarda())
		{
			var evento = EventoDeSeguranca.Registrar(TipoDeEventoDeSeguranca.BloqueioDeSaida, Canal.GrupoDoBairro, new[] { TipoDeDado.MoraSozinho, TipoDeDado.Saude }, 4, "Cadastro", "/api/mensagens/verificar", agora);
			db.Add(evento);
			await db.SaveChangesAsync();
			eventoId = evento.Id;
		}

		await using (var db = novaGuarda())
		{
			var evento = await db.Eventos.SingleAsync(e => e.Id == eventoId);
			Conferir("tipos_de_dado volta como lista de enum", evento.TiposDeDado.SequenceEqual(new[] { TipoDeDado.MoraSozinho, TipoDeDado.Saude }) && evento.Mensagem is null);
			evento.AnexarMensagem(new MensagemAoCidadao("Uma mensagem com dados de saude foi barrada.", OrigemDaMensagem.FraseFixa));
			await db.SaveChangesAsync();
		}

		await using (var db = novoCadastro())
		{
			db.Add(MensagemSaida.Verificar(Canal.GrupoDoBairro, acs, "ele [MORA_SOZINHO] e toma [SAUDE]", new[] { TipoDeDado.MoraSozinho, TipoDeDado.Saude }, agora));
			db.Add(Visita.Registrar(Guid.NewGuid(), idosoId, acs, Desfecho.EstavaFora, TextoRotulado.Do("vizinha disse que foi a feira"), agora.AddMinutes(-10), agora));
			db.Add(AvisoAoVizinho.Registrar(primeiroToken, microarea, MotivoDoAlerta.SemMovimentoNaJanela, agora.AddHours(-0.5), agora));
			await db.SaveChangesAsync();
			var visita = await db.Visitas.AsNoTracking().SingleAsync(v => v.IdosoId == idosoId);
			Conferir("observacao volta com os rotulos de texto livre", visita.Observacao!.Rotulos.Count == 5);
		}

		Titulo("6. Corrida entre pacote e revogacao");
		var casaA = CasaToken.Novo();
		var casaB = CasaToken.Novo();
		await using (var db = novoMotor())
		{
			db.Add(CasaMonitorada.Ativar(casaA, microarea, new PerfilDaCasa(PerfilDeContato.Sozinho, false), agora.AddDays(-1)));
			db.Add(CasaMonitorada.Ativar(casaB, microarea, new PerfilDaCasa(PerfilDeContato.Sozinho, false), agora.AddDays(-1)));
			await db.SaveChangesAsync();
			await Acesso.RegistrarPacoteAsync(db, casaB, 1, TipoDePacote.Movimento, agora.AddMinutes(-9), agora.AddMinutes(-9), null, null, false);
		}

		await using (var a = novoMotor())
		await using (var b = novoMotor())
		{
			await using var pacoteEmAndamento = await a.Database.BeginTransactionAsync();
			await a.Casas.Where(c => c.Token == casaA && c.DesativadaEm == null)
				.ExecuteUpdateAsync(s => s.SetProperty(c => c.UltimoPacoteEm, (DateTimeOffset?)agora));
			var revogacao = Acesso.DesativarCasaAsync(b, casaA, agora.AddSeconds(1));
			await Task.Delay(500);
			Conferir("a revogacao espera o pacote que esta no meio da transacao", !revogacao.IsCompleted);
			await a.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO motor.evento_sensor (casa_token, ocorrido_em, seq, tipo, recebido_em, bufferizado) VALUES ({casaA.Valor}, {agora}, {1L}, {"Movimento"}, {agora}, {false})");
			await pacoteEmAndamento.CommitAsync();
			await revogacao;
			Conferir("o evento que entrou antes da revogacao e apagado por ela", !await b.Eventos.AnyAsync(e => e.CasaToken == casaA));
		}

		await using (var a = novoMotor())
		await using (var b = novoMotor())
		{
			await using var revogacaoEmAndamento = await b.Database.BeginTransactionAsync();
			await b.Casas.Where(c => c.Token == casaB)
				.ExecuteUpdateAsync(s => s.SetProperty(c => c.DesativadaEm, (DateTimeOffset?)agora));
			var pacote = Acesso.RegistrarPacoteAsync(a, casaB, 2, TipoDePacote.Movimento, agora, agora, null, null, false);
			await Task.Delay(500);
			Conferir("o pacote espera a revogacao que esta no meio da transacao", !pacote.IsCompleted);
			await b.Eventos.Where(e => e.CasaToken == casaB).ExecuteDeleteAsync();
			await revogacaoEmAndamento.CommitAsync();
			Conferir("o pacote que esperou e descartado e a casa fica sem evento", await pacote == (0, 0) && !await b.Eventos.AnyAsync(e => e.CasaToken == casaB));
		}

		Titulo("6b. Expurgo pelo Npgsql, com o texto de consultas/expurgo.sql");
		await using (var db = novoMotor())
		{
			var casaVelha = await Acesso.CasasAtivas(db, microarea).Where(c => !c.Perfil.Eletrodependente).Select(c => c.Token).FirstAsync();
			var haDoisDias = agora.AddDays(-2);
			await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO motor.evento_sensor (casa_token, ocorrido_em, seq, tipo, recebido_em, temp_c, tensao_v, bufferizado) VALUES ({casaVelha.Valor}, {haDoisDias}, {900001L}, {"Telemetria"}, {haDoisDias}, {30.0m}, {0.0m}, {false})");
			var texto = File.ReadAllText("../consultas/expurgo.sql")
				.Replace("BEGIN;", "").Replace("COMMIT;", "")
				.Replace(":corte", "{0}").Replace(":tensao_minima", "{1}");
			await Acesso.ExpurgarAsync(db, texto, agora.AddHours(-24), 100m);
			Conferir("o texto do expurgo roda pelo Npgsql e apaga a leitura velha de casa comum",
				!await db.Eventos.AnyAsync(e => e.CasaToken == casaVelha && e.Seq == 900001L) && await db.Eventos.AnyAsync(e => e.CasaToken == casaVelha));
		}

		Titulo("7. Armadilhas");
		await using (var db = novoMotor())
		{
			var recusou = false;
			try { await db.Database.ExecuteSqlInterpolatedAsync($"SELECT {agora.ToOffset(TimeSpan.FromHours(-3))}::timestamptz"); }
			catch (Exception e) when (e is ArgumentException or InvalidCastException or NotSupportedException) { recusou = true; }
			Conferir("SQL escrito a mao: o Npgsql recusa DateTimeOffset com -03:00", recusou);

			var comDecimos = new DateTimeOffset(agora.Ticks + 7, TimeSpan.Zero);
			var amanha = new DateOnly(2026, 11, 19);
			db.Add(DiaCritico.Declarar(amanha, new Probabilidade(0.5m), "teste de precisao", OrigemDoDiaCritico.Manual, comDecimos));
			await db.SaveChangesAsync();
			db.ChangeTracker.Clear();
			var relido = await db.DiasCriticos.SingleAsync(d => d.Data == amanha);
			Conferir("hora com decimos de microssegundo volta cortada no microssegundo, sem codigo de corte", relido.AbertoEm == agora);
		}

		await using (var db = novoCadastro())
		{
			var desdeComDecimos = new DateTimeOffset(agora.AddMinutes(-31).Ticks + 7, TimeSpan.Zero);
			var tokenDoEpisodio = CasaToken.Novo();
			db.Add(AvisoAoVizinho.Registrar(tokenDoEpisodio, microarea, MotivoDoAlerta.CalorInterno, desdeComDecimos, agora));
			await db.SaveChangesAsync();
			db.ChangeTracker.Clear();
			var achado = await db.Avisos.AnyAsync(a => a.CasaToken == tokenDoEpisodio && a.Motivo == MotivoDoAlerta.CalorInterno && a.Desde == desdeComDecimos);
			Conferir("chave do episodio com Desde de decimos de microssegundo acha a linha gravada: o Npgsql corta o parametro igual", achado);
		}

		Titulo("8. Consultas principais: SQL gerado e plano");
		var desde = agora.AddHours(-24);
		var tensaoMinima = new TensaoV(100m);
		await using var motor = novoMotor();
		await using var cadastro = novoCadastro();
		await using var guarda = novaGuarda();

		await MostrarAsync("radar: casas ativas da microarea, com as confirmacoes", Acesso.CasasAtivas(motor, microarea));

		var eventosDoRadar = Acesso.EventosDaAvaliacao(motor, microarea, desde, tensaoMinima);
		await MostrarAsync("radar e entrada da avaliacao: eventos de 24 h mais as leituras baixas guardadas", eventosDoRadar);
		var porCasa = (await eventosDoRadar.ToListAsync()).ToLookup(e => e.CasaToken);
		Console.WriteLine($"   {porCasa.Count} casas com evento, {porCasa.Sum(g => g.Count())} eventos carregados");
		Conferir("eventos agrupados por casa na memoria", porCasa.Count == 40);

		await MostrarAsync("dia critico de hoje", Acesso.DiaCriticoDe(motor, hoje));

		var umaCasa = await Acesso.CasasAtivas(motor, microarea).Select(c => c.Token).FirstAsync();
		await MostrarAsync("linha do tempo de uma casa", Acesso.LinhaDoTempo(motor, umaCasa, desde));

		var listaDeNomes = Acesso.ListaDeNomes(cadastro, microarea);
		var sqlDaLista = listaDeNomes.ToQueryString();
		Conferir("lista de nomes nao le condicoes nem medicamentos do banco", !sqlDaLista.Contains("condicoes") && !sqlDaLista.Contains("medicamentos"));
		var itens = await listaDeNomes.ToListAsync();
		Conferir("lista de nomes projeta TextoRotulado com rotulo e deixa o vizinho nulo quando nao existe",
			itens.Count >= 40 && itens.All(x => x.Nome.Rotulos.Contains(TipoDeDado.Nome)) && itens.Any(x => x.NomeDoVizinho is null) && itens.Any(x => x.TelefoneDoVizinho is not null && x.TelefoneDoVizinho.Rotulos.Contains(TipoDeDado.Telefone)));
		await MostrarAsync("lista de nomes da microarea", listaDeNomes);

		await MostrarAsync("avisos das ultimas 24 h da microarea", Acesso.AvisosDoDia(cadastro, microarea, desde));

		await MostrarAsync("idoso pelo token (POST /visitas e ReceptorDeContato)", Acesso.IdosoDoToken(cadastro, primeiroToken));

		await MostrarAsync("painel: os 50 eventos de seguranca mais novos das ultimas 24 h", Acesso.EventosDoPainel(guarda, desde));

		var contagem = Acesso.ContagemDoPainel(guarda, desde);
		await MostrarAsync("painel: contagem por tipo, canal e severidade para o farol e os pontos", contagem);
		var grupos = await contagem.ToListAsync();
		Conferir("contagem do painel volta agrupada", grupos.Count is > 0 and <= 90 && grupos.Sum(g => g.Quantidade) > 50);

		Console.WriteLine();
		Console.WriteLine(_falhas == 0 ? "TODAS AS CONFERENCIAS PASSARAM" : $"{_falhas} CONFERENCIA(S) FALHARAM");
		Environment.ExitCode = _falhas == 0 ? 0 : 1;
	}

	private static async Task MostrarAsync<T>(string titulo, IQueryable<T> consulta)
	{
		Console.WriteLine();
		Console.WriteLine($"-- {titulo}");
		Console.WriteLine(consulta.ToQueryString());
		await using var comando = consulta.CreateDbCommand();
		comando.CommandText = "EXPLAIN (COSTS OFF) " + comando.CommandText;
		if (comando.Connection!.State != ConnectionState.Open)
		{
			await comando.Connection.OpenAsync();
		}

		await using var leitor = await comando.ExecuteReaderAsync();
		while (await leitor.ReadAsync())
		{
			Console.WriteLine("   plano| " + leitor.GetString(0));
		}
	}

	private static void Titulo(string texto)
	{
		Console.WriteLine();
		Console.WriteLine("== " + texto);
	}

	private static void Conferir(string descricao, bool passou)
	{
		Console.WriteLine($"   [{(passou ? "ok" : "FALHOU")}] {descricao}");
		if (!passou)
		{
			_falhas++;
		}
	}
}
