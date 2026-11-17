"""Testes do simulador. Rodam sem broker e sem dependência: python3 -m unittest -v"""
from __future__ import annotations

import contextlib
import io
import json
import random
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

import conferir_fio
from modelo import (
	CAMPOS,
	TIPOS,
	Casa,
	Comportamento,
	NoSimulado,
	carregar_casas,
	comportamento_do_papel,
	topico_evento,
)

LIGADO = datetime(2026, 11, 18, 12, 0, 0, tzinfo=timezone.utc)
TOKEN = "ab" * 16


def rodar(comportamento: Comportamento, segundos: int, comandos: dict[int, str] | None = None, **kw):
	"""Anda o nó de segundo em segundo e devolve (instante de envio, pacote)."""
	no = NoSimulado(Casa(7, TOKEN, ""), comportamento, LIGADO, random.Random(1), **kw)
	saida = []
	for s in range(segundos + 1):
		agora = LIGADO + timedelta(seconds=s)
		if comandos and s in comandos:
			no.receber_comando(comandos[s], agora)
		saida += [(agora, p) for p in no.passo(agora)]
	return no, saida


def tipos(saida, depois_de: int = 0, ao_vivo: bool = True):
	limite = LIGADO + timedelta(seconds=depois_de)
	return [p.tipo for t, p in saida if t >= limite and (not ao_vivo or not p.bufferizado)]


class Contrato(unittest.TestCase):
	def test_corpo_tem_os_seis_campos_do_contrato(self):
		_, saida = rodar(Comportamento.NORMAL, 400)
		for _, pacote in saida:
			corpo = json.loads(pacote.corpo())
			self.assertEqual(list(corpo), list(CAMPOS))
			self.assertIn(corpo["tipo"], TIPOS)
			self.assertRegex(corpo["ts"], r"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$")
			self.assertLessEqual(len(pacote.corpo().encode()), 256)

	def test_seq_comeca_em_1_e_sobe_em_todo_pacote(self):
		_, saida = rodar(Comportamento.NORMAL, 600)
		self.assertEqual([p.seq for _, p in saida], list(range(1, len(saida) + 1)))

	def test_heartbeat_a_cada_20_s_e_telemetria_ao_ligar_e_a_cada_5_min(self):
		_, saida = rodar(Comportamento.NORMAL, 660)
		batidas = [t for t, p in saida if p.tipo == "HEARTBEAT"]
		self.assertTrue(all(b - a == timedelta(seconds=20) for a, b in zip(batidas, batidas[1:])))
		tele = [t - LIGADO for t, p in saida if p.tipo == "TELEMETRIA"]
		self.assertLess(tele[0], timedelta(seconds=6))
		self.assertEqual([b - a for a, b in zip(tele, tele[1:])], [timedelta(minutes=5)] * (len(tele) - 1))

	def test_topico_do_evento(self):
		self.assertEqual(topico_evento(TOKEN), f"corredor/v1/{TOKEN}/evento")


class Cenarios(unittest.TestCase):
	def test_casa_silenciosa_continua_batendo_sem_sinal_de_vida(self):
		_, saida = rodar(Comportamento.SILENCIOSA, 600, comandos={300: "PEDIR_TOQUE"})
		depois = tipos(saida, depois_de=30)
		self.assertIn("HEARTBEAT", depois)
		self.assertFalse({"MOVIMENTO", "BOTAO_BEM", "BOTAO_SAIR"} & set(depois))

	def test_aparelho_mudo_para_de_mandar_qualquer_pacote(self):
		_, saida = rodar(Comportamento.MUDA, 300)
		self.assertTrue(tipos(saida)[:1])
		self.assertEqual(tipos(saida, depois_de=30), [])

	def test_queda_de_energia_manda_telemetria_na_hora_em_que_a_tensao_cruza_100_v(self):
		_, saida = rodar(Comportamento.QUEDA_DE_ENERGIA, 200, inicio=timedelta(seconds=40), duracao=timedelta(seconds=60))
		tele = [(t - LIGADO).seconds for t, p in saida if p.tipo == "TELEMETRIA"]
		tensoes = {(t - LIGADO).seconds: p.tensao_v for t, p in saida if p.tipo == "TELEMETRIA"}
		self.assertIn(40, tele)
		self.assertIn(100, tele)
		self.assertLess(tensoes[40], 100)
		self.assertGreaterEqual(tensoes[100], 100)

	def test_reenvio_mantem_seq_ts_e_tipo_e_marca_bufferizado(self):
		no, saida = rodar(Comportamento.REENVIO, 200, inicio=timedelta(seconds=30), duracao=timedelta(seconds=60))
		self.assertTrue(no.guardados)
		self.assertNotIn("HEARTBEAT", [p.tipo for p in no.guardados])
		self.assertEqual([t for t, p in saida if 30 <= (t - LIGADO).seconds < 90], [])
		reenviados = [(t, p) for t, p in saida if p.bufferizado]
		self.assertEqual(len(reenviados), 2 * len(no.guardados))
		chave = lambda p: (p.seq, p.ts, p.tipo, p.temp_c, p.tensao_v)
		self.assertEqual([chave(p) for _, p in reenviados], [chave(p) for p in no.guardados] * 2)
		self.assertTrue(all(t == LIGADO + timedelta(seconds=90) > p.ts for t, p in reenviados))

	def test_reenvio_sempre_tem_o_que_guardar(self):
		for semente in range(30):
			no = NoSimulado(Casa(7, TOKEN, ""), Comportamento.REENVIO, LIGADO, random.Random(semente), inicio=timedelta(seconds=20), duracao=timedelta(seconds=30))
			for s in range(80):
				no.passo(LIGADO + timedelta(seconds=s))
			self.assertTrue(no.guardados, f"semente {semente}")

	def test_pedido_de_toque_faz_a_casa_normal_apertar_o_botao(self):
		_, saida = rodar(Comportamento.NORMAL, 120, comandos={60: "PEDIR_TOQUE"})
		self.assertIn("BOTAO_BEM", tipos(saida, depois_de=60))

	def test_casa_do_toque_so_aperta_depois_do_pedido(self):
		_, saida = rodar(Comportamento.TOQUE, 400, comandos={300: "PEDIR_TOQUE"})
		self.assertEqual([t for t in tipos(saida) if t in ("MOVIMENTO", "BOTAO_BEM")], ["BOTAO_BEM"])
		self.assertIn("BOTAO_BEM", tipos(saida, depois_de=300))

	def test_saida_avisada_manda_um_botao_sair_e_nenhum_movimento(self):
		_, saida = rodar(Comportamento.SAIDA_AVISADA, 600)
		self.assertEqual([t for t in tipos(saida) if t in ("MOVIMENTO", "BOTAO_SAIR")], ["BOTAO_SAIR"])

	def test_calor_manda_34_5_graus(self):
		_, saida = rodar(Comportamento.CALOR, 10)
		self.assertTrue(all(p.temp_c == 34.5 for _, p in saida))


class Semente(unittest.TestCase):
	def escrever(self, itens) -> Path:
		arquivo = Path(tempfile.mkdtemp()) / "tokens.local.json"
		arquivo.write_text(json.dumps(itens), encoding="utf-8")
		return arquivo

	def test_carrega_39_casas_e_pula_a_casa_do_wokwi(self):
		itens = [{"n": n, "casaToken": f"{n:032x}", "papelNaDemo": "movimento normal"} for n in range(1, 41)]
		casas = carregar_casas(self.escrever({"casas": itens}))
		self.assertEqual([c.n for c in casas], list(range(2, 41)))

	def test_le_os_tokens_onde_a_carga_os_escreve(self):
		# Corredor.Api --semear escreve tokens.local.json ao lado da semente, api/semente/casas.json
		# (seção 8.3 de 04-modelo-de-dados.md). Com a semente em outra pasta, o simulador não achava os tokens.
		from simulador import TOKENS_PADRAO

		self.assertEqual(TOKENS_PADRAO.name, "tokens.local.json")
		self.assertTrue((TOKENS_PADRAO.parent / "casas.json").is_file(), TOKENS_PADRAO.parent)
		self.assertIn("tokens.local.json", (TOKENS_PADRAO.parent / ".gitignore").read_text(encoding="utf-8"))

	def test_recusa_token_fora_do_formato(self):
		with self.assertRaises(ValueError):
			carregar_casas(self.escrever([{"n": 2, "casaToken": "ABC", "papelNaDemo": ""}]))

	def test_papel_da_semente_vira_comportamento(self):
		self.assertIs(comportamento_do_papel("B: saída avisada às 07h30"), Comportamento.SAIDA_AVISADA)
		self.assertIs(comportamento_do_papel("C: silêncio às 10h, vizinho responde"), Comportamento.SILENCIOSA)
		self.assertIs(comportamento_do_papel("F: calor de 34,5 °C desde as 11h"), Comportamento.CALOR)
		self.assertIs(comportamento_do_papel("G: botão às 14h40"), Comportamento.TOQUE)
		self.assertIs(comportamento_do_papel("aparelho mudo desde as 09h10"), Comportamento.MUDA)
		self.assertIs(comportamento_do_papel("movimento normal"), Comportamento.NORMAL)


class ConferirFio(unittest.TestCase):
	def conferir(self, linhas: list[str], log: list[str] | None = None) -> int:
		pasta = Path(tempfile.mkdtemp())
		(pasta / "c.txt").write_text("\n".join(linhas), encoding="utf-8")
		argv = [str(pasta / "c.txt")]
		if log is not None:
			(pasta / "l.txt").write_text("\n".join(log), encoding="utf-8")
			argv += ["--log", str(pasta / "l.txt")]
		with contextlib.redirect_stdout(io.StringIO()):
			return conferir_fio.main(argv)

	def linha(self, segundos: float, corpo: str, topico: str = f"corredor/v1/{TOKEN}/evento") -> str:
		return f"{(LIGADO + timedelta(seconds=segundos)).timestamp()} {topico} {corpo}"

	def test_aceita_a_saida_do_reenvio(self):
		no, saida = rodar(Comportamento.REENVIO, 200)
		linhas = [self.linha((t - LIGADO).total_seconds(), p.corpo()) for t, p in saida]
		cenario = {"cenario": "reenvio", "ligadoEm": LIGADO.isoformat(), "inicio": 30, "janela": 60}
		log = [f"CENARIO {json.dumps(cenario)}", f"CASA 7 {TOKEN} reenvio"]
		log += [f"GUARDADO {TOKEN} {p.corpo()}" for p in no.guardados]
		self.assertEqual(self.conferir(linhas, log), 0)

	def test_recusa_reenvio_com_ts_trocado(self):
		no, saida = rodar(Comportamento.REENVIO, 200)
		linhas = []
		for t, p in saida:
			if p.bufferizado:
				p = p.__class__(p.seq, t, p.tipo, p.temp_c, p.tensao_v, True)
			linhas.append(self.linha((t - LIGADO).total_seconds(), p.corpo()))
		cenario = {"cenario": "reenvio", "ligadoEm": LIGADO.isoformat(), "inicio": 30, "janela": 60}
		log = [f"CENARIO {json.dumps(cenario)}", f"CASA 7 {TOKEN} reenvio"]
		log += [f"GUARDADO {TOKEN} {p.corpo()}" for p in no.guardados]
		self.assertEqual(self.conferir(linhas, log), 1)

	def test_recusa_campo_a_mais_tipo_errado_e_ts_no_futuro(self):
		ruins = [
			'{"seq":1,"ts":"2026-11-18T12:00:00Z","tipo":"MOVIMENTO","tempC":30,"tensaoV":127,"bufferizado":false,"nome":"x"}',
			'{"seq":1,"ts":"2026-11-18T12:00:00Z","tipo":"PING","tempC":30,"tensaoV":127,"bufferizado":false}',
			'{"seq":1,"ts":"2026-11-18T12:10:00Z","tipo":"MOVIMENTO","tempC":30,"tensaoV":127,"bufferizado":false}',
		]
		for corpo in ruins:
			self.assertEqual(self.conferir([self.linha(0, corpo)]), 1, corpo)

	def test_recusa_mesma_chave_com_corpos_diferentes(self):
		a = '{"seq":1,"ts":"2026-11-18T12:00:00Z","tipo":"MOVIMENTO","tempC":30,"tensaoV":127,"bufferizado":false}'
		b = a.replace("MOVIMENTO", "BOTAO_BEM")
		self.assertEqual(self.conferir([self.linha(0, a), self.linha(1, b)]), 1)

	def test_aceita_comando_do_contrato_e_recusa_outro(self):
		topico = f"corredor/v1/{TOKEN}/comando"
		self.assertEqual(self.conferir([self.linha(0, '{"tipo":"PEDIR_TOQUE"}', topico)]), 0)
		self.assertEqual(self.conferir([self.linha(0, '{"tipo":"DESLIGAR"}', topico)]), 1)

	def test_queda_com_ligado_em_fracao_de_segundo(self):
		ligado = LIGADO + timedelta(milliseconds=868)
		no = NoSimulado(Casa(2, TOKEN, ""), Comportamento.QUEDA_DE_ENERGIA, ligado, random.Random(1), inicio=timedelta(seconds=20), duracao=timedelta(seconds=30))
		linhas = []
		for s in range(80):
			agora = ligado + timedelta(seconds=s)
			linhas += [self.linha((agora - LIGADO).total_seconds(), p.corpo()) for p in no.passo(agora)]
		cenario = {"cenario": "queda-de-energia", "ligadoEm": ligado.isoformat(), "inicio": 20, "janela": 30}
		self.assertEqual(self.conferir(linhas, [f"CENARIO {json.dumps(cenario)}", f"CASA 2 {TOKEN} queda-de-energia"]), 0)

	def test_pega_aparelho_mudo_que_continua_falando(self):
		_, saida = rodar(Comportamento.NORMAL, 120)
		linhas = [self.linha((t - LIGADO).total_seconds(), p.corpo()) for t, p in saida]
		cenario = {"cenario": "mudo", "ligadoEm": LIGADO.isoformat(), "inicio": 30, "janela": 60}
		self.assertEqual(self.conferir(linhas, [f"CENARIO {json.dumps(cenario)}", f"CASA 7 {TOKEN} muda"]), 1)


if __name__ == "__main__":
	unittest.main()
