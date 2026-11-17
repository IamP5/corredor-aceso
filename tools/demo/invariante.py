#!/usr/bin/env python3
"""Invariante do falso verde no sistema integrado, não só no teste de unidade.

Sorteia sequências de pacotes MQTT, visitas e respostas de vizinho por HTTP e saltos do relógio de demo,
e lê o radar depois de cada passo. Para cada casa `Verde`, confere com um modelo próprio, feito só com o
que este script mandou:

  - existe turno aberto (declarado, não encerrado, antes das 18h);
  - nenhum alerta: chegou pacote no último minuto (SilencioMaximo de demo), a casa eletrodependente não tem
    tensão baixa sem visita da ACS depois, e não há calor de 34 °C ou mais há 2 min (DuracaoDoCalor de demo);
  - existe sinal de vida aceito pelo perfil desde o início da janela atual (cortes 00h, abertura, 10h e 15h).

Casos a menos de 3 s de um limite são contados como indeterminados e não entram na conferência.
Uso: CORREDOR_API_URL=... CORREDOR_TOKEN_ACS=... invariante.py [passos] [semente]. Rode depois de
`tools/demo.sh subir`, sem o simulador ligado. Sai com 1 se achar uma casa verde sem prova.
"""
from __future__ import annotations

import datetime as dt
import json
import os
import random
import sys
import time
import uuid
from dataclasses import dataclass, field
from pathlib import Path
from zoneinfo import ZoneInfo

sys.path.insert(0, str(Path(__file__).resolve().parent))
from cenas import CASAS, FUSO, http, radar  # noqa: E402

import paho.mqtt.client as mqtt  # noqa: E402

SEMENTE = json.loads((Path(__file__).resolve().parents[2] / "api/semente/casas.json").read_text())
PERFIL = {c["n"]: (c["perfil"]["contato"], c["perfil"]["eletrodependente"]) for c in SEMENTE["casas"]}
SILENCIO = dt.timedelta(minutes=1)
CALOR = dt.timedelta(minutes=2)
FOLGA = dt.timedelta(seconds=3)
UTC = dt.timezone.utc


@dataclass
class Casa:
	n: int
	token: str
	contato: str
	eletro: bool
	seq: int = 0
	muda: bool = False
	ultimo_pacote: dt.datetime | None = None
	sinais: list[dt.datetime] = field(default_factory=list)  # sinal de vida aceito pelo perfil, hora de demo
	vistas_acs: list[dt.datetime] = field(default_factory=list)  # PessoaVista da ACS
	# (hora do fato, seq, valor). Duas leituras no mesmo segundo se ordenam pelo seq, como no nó.
	tensoes: list[tuple[dt.datetime, int, float]] = field(default_factory=list)
	temperaturas: list[tuple[dt.datetime, int, float]] = field(default_factory=list)


class Mundo:
	def __init__(self, sorteio: random.Random):
		self.s = sorteio
		self.casas = {n: Casa(n, c["casaToken"], *PERFIL[n]) for n, c in CASAS.items()}
		self.deslocamento = dt.timedelta(0)
		self.abertura: dt.datetime | None = None
		self.encerrado = False
		self.cliente = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id="corredor-invariante")
		self.cliente.connect("127.0.0.1", 1883)
		self.cliente.loop_start()
		self.conferidas = self.indeterminadas = 0
		self.violacoes: list[str] = []
		self.leituras = 0

	def agora_demo(self) -> dt.datetime:
		return dt.datetime.now(UTC) + self.deslocamento

	# ----- ações -----
	def pacote(self, c: Casa, tipo: str, temp: float = 28.0, tensao: float = 127.0, atraso: dt.timedelta = dt.timedelta(0)):
		c.seq += 1
		ts_real = (dt.datetime.now(UTC) - atraso).replace(microsecond=0)
		corpo = {"seq": c.seq, "ts": ts_real.strftime("%Y-%m-%dT%H:%M:%SZ"), "tipo": tipo, "tempC": temp,
		         "tensaoV": tensao, "bufferizado": atraso > dt.timedelta(0)}
		self.cliente.publish(f"corredor/v1/{c.token}/evento", json.dumps(corpo), qos=1).wait_for_publish(5)
		quando = ts_real + self.deslocamento
		c.ultimo_pacote = self.agora_demo()
		if tipo in ("MOVIMENTO", "BOTAO_BEM", "BOTAO_SAIR") and c.contato == "Sozinho":
			c.sinais.append(quando)
		if tipo == "TELEMETRIA":
			c.tensoes.append((quando, c.seq, tensao))
			c.temperaturas.append((quando, c.seq, temp))

	def visita(self, c: Casa, desfecho: str):
		atraso = dt.timedelta(seconds=self.s.choice([0, 0, 30, 300, 1200]))
		ocorrida = (dt.datetime.now(FUSO) - atraso).replace(microsecond=0)
		status, _ = http("POST", "/api/visitas", {"id": str(uuid.uuid4()), "casaToken": c.token,
		                                         "ocorridaEm": ocorrida.isoformat(), "desfecho": desfecho, "observacao": None})
		if status in (200, 201) and desfecho in ("EstavaBem", "Encaminhada"):
			quando = ocorrida.astimezone(UTC) + self.deslocamento
			c.sinais.append(quando)
			c.vistas_acs.append(quando)
		return status

	def vizinho_responde(self):
		status, avisos = http("GET", "/api/avisos?microarea=3")
		abertos = [a for a in avisos if a["resposta"] is None] if status == 200 else []
		if not abertos:
			return
		aviso = self.s.choice(abertos)
		status, corpo = http("POST", f"/api/avisos/{aviso['id']}/resposta", {"resposta": "EstaBem"}, acs=False)
		if status == 200:
			n = next(n for n, c in self.casas.items() if c.token == aviso["casaToken"])
			self.casas[n].sinais.append(dt.datetime.fromisoformat(corpo["respondidoEm"]).astimezone(UTC))

	def mover_relogio(self, minutos: float):
		time.sleep(1.5)  # deixa o Motor gravar o que já saiu com o deslocamento antigo
		alvo = self.agora_demo() + dt.timedelta(minutes=minutos)
		status, corpo = http("PUT", "/api/demo/relogio", {"agora": alvo.astimezone(FUSO).isoformat()}, acs=False)
		assert status == 200, corpo
		self.deslocamento = dt.timedelta(seconds=corpo["deslocamentoEmSegundos"])

	def declarar(self):
		data = self.agora_demo().astimezone(FUSO).date().isoformat()
		status, corpo = http("PUT", f"/api/dias-criticos/{data}",
		                     {"probabilidade": 0.5, "motivo": "invariante do falso verde", "origem": "MANUAL"}, acs=False)
		assert status == 200, corpo
		self.abertura = dt.datetime.fromisoformat(corpo["abertoEm"]).astimezone(UTC)

	def encerrar(self):
		data = self.agora_demo().astimezone(FUSO).date().isoformat()
		status, _ = http("POST", f"/api/dias-criticos/{data}/encerramento")
		self.encerrado = status == 200

	# ----- modelo -----
	def inicio_da_janela(self, agora: dt.datetime) -> dt.datetime:
		local = agora.astimezone(FUSO)
		meia_noite = local.replace(hour=0, minute=0, second=0, microsecond=0)
		cortes = [meia_noite, meia_noite.replace(hour=10), meia_noite.replace(hour=15)]
		if self.abertura:
			cortes.append(self.abertura.astimezone(FUSO))
		return max(c for c in cortes if c <= local).astimezone(UTC)

	def conferir_verde(self, c: Casa, agora: dt.datetime, turno_no_radar: bool) -> None:
		"""Devolve sem nada se a casa verde tem prova. Registra violação ou indeterminação."""
		perto = lambda a, b: abs((a - b).total_seconds()) < FOLGA.total_seconds()  # noqa: E731
		faltas, duvidas = [], []
		fim = agora.astimezone(FUSO).replace(hour=18, minute=0, second=0, microsecond=0)
		if not turno_no_radar or self.abertura is None or self.encerrado or agora >= fim:
			(duvidas if perto(agora, fim) else faltas).append("turno aberto")
		if c.ultimo_pacote is None or c.ultimo_pacote < agora - SILENCIO:
			(duvidas if c.ultimo_pacote and perto(c.ultimo_pacote, agora - SILENCIO) else faltas).append("pacote no último minuto")
		if c.eletro and c.tensoes:
			ultima = max(c.tensoes)[2]
			baixas = [q for q, _, v in c.tensoes if v < 100]
			if ultima < 100 or any(not any(v >= b for v in c.vistas_acs) for b in baixas):
				faltas.append("tensão boa, ou visita da ACS depois da tensão baixa")
		seq_quente = []
		for q, _, t in sorted(c.temperaturas):
			seq_quente = seq_quente + [q] if t >= 34 else []
		if seq_quente and agora - seq_quente[0] >= CALOR:
			(duvidas if perto(agora - seq_quente[0], CALOR) else faltas).append("sem calor de 34 °C há 2 min")
		inicio = self.inicio_da_janela(agora)
		validos = [q for q in c.sinais if inicio <= q <= agora]
		if not validos:
			quase = [q for q in c.sinais if perto(q, inicio) or perto(q, agora)]
			(duvidas if quase else faltas).append(f"sinal de vida desde {inicio.astimezone(FUSO):%H:%M:%S}")
		if faltas:
			self.violacoes.append(f"{agora.astimezone(FUSO):%H:%M:%S} casa {c.n} ({c.contato}{', eletro' if c.eletro else ''}) "
			                      f"Verde sem: {', '.join(faltas)}")
		elif duvidas:
			self.indeterminadas += 1
		else:
			self.conferidas += 1

	def ler_radar(self) -> dict:
		r = radar()
		self.leituras += 1
		agora = dt.datetime.fromisoformat(r["agora"]).astimezone(UTC)
		com_card = {x["casaToken"] for g in ("pendentes", "atendidas", "aguardando") for x in r[g]}
		verdes = [c for c in self.casas.values() if c.token not in com_card]
		if len(verdes) != r["verdes"]:
			self.violacoes.append(f"contagem de verdes {r['verdes']} diferente das {len(verdes)} casas sem card")
		for c in verdes:
			self.conferir_verde(c, agora, r["turnoAberto"])
		return r


def main() -> int:
	passos = int(sys.argv[1]) if len(sys.argv) > 1 else 200
	semente = int(sys.argv[2]) if len(sys.argv) > 2 else 20261119
	s = random.Random(semente)
	m = Mundo(s)
	hoje = dt.datetime.fromisoformat(radar()["agora"]).date()
	status, corpo = http("PUT", "/api/demo/relogio", {"agora": f"{hoje}T04:30:00-03:00"}, acs=False)
	m.deslocamento = dt.timedelta(seconds=corpo["deslocamentoEmSegundos"])
	passo_da_declaracao = s.randint(2, 12)
	contagem = {"pacotes": 0, "visitas": 0, "saltos": 0}
	print(f"semente {semente}, {passos} passos, dia crítico no passo {passo_da_declaracao}", flush=True)
	inicio = time.monotonic()
	for passo in range(1, passos + 1):
		if passo == passo_da_declaracao:
			m.declarar()
		for c in m.casas.values():
			if s.random() < 0.03:
				c.muda = not c.muda
			if c.muda:
				continue
			if s.random() < 0.9:
				m.pacote(c, "HEARTBEAT"); contagem["pacotes"] += 1
			sorteio = s.random()
			if sorteio < 0.08:
				m.pacote(c, "MOVIMENTO"); contagem["pacotes"] += 1
			elif sorteio < 0.10:
				m.pacote(c, s.choice(["BOTAO_BEM", "BOTAO_SAIR"])); contagem["pacotes"] += 1
			elif sorteio < 0.17:
				tensao = 0.0 if c.eletro and s.random() < 0.3 else 127.0
				m.pacote(c, "TELEMETRIA", temp=s.choice([27.5, 31.0, 34.5, 35.5]), tensao=tensao); contagem["pacotes"] += 1
			elif sorteio < 0.20:
				# evento do buffer: hora antiga, bufferizado true, às vezes de antes da janela atual
				m.pacote(c, "MOVIMENTO", atraso=dt.timedelta(minutes=s.choice([1, 5, 20, 120]))); contagem["pacotes"] += 1
		for _ in range(s.choice([0, 0, 1, 2])):
			m.visita(s.choice(list(m.casas.values())), s.choice(["EstavaBem", "EstavaFora", "NaoAbriu", "Encaminhada"]))
			contagem["visitas"] += 1
		if s.random() < 0.1:
			m.vizinho_responde()
		time.sleep(0.4)
		m.ler_radar()
		if s.random() < 0.18:
			m.mover_relogio(s.choice([0.5, 1.5, 7, 25, 60, 90]))
			contagem["saltos"] += 1
			m.ler_radar()  # logo depois do salto, antes de qualquer heartbeat novo
		agora_local = m.agora_demo().astimezone(FUSO)
		if m.abertura and not m.encerrado and agora_local.hour >= 16 and s.random() < 0.05:
			m.encerrar()
		if agora_local.hour >= 19:
			break
	r = radar()
	print(f"fim: relógio em {r['agora'][11:19]}, {passo} passos em {time.monotonic() - inicio:.0f} s, "
	      f"{contagem['pacotes']} pacotes, {contagem['visitas']} visitas, {contagem['saltos']} saltos do relógio, "
	      f"{m.leituras} leituras do radar, turno {'encerrado pela ACS' if m.encerrado else 'até as 18h'}", flush=True)
	print(f"casas verdes conferidas com prova: {m.conferidas}; indeterminadas por folga de 3 s: {m.indeterminadas}; "
	      f"violações: {len(m.violacoes)}", flush=True)
	for v in m.violacoes[:30]:
		print("  VIOLAÇÃO " + v, flush=True)
	m.cliente.loop_stop()
	return 1 if m.violacoes or m.conferidas == 0 else 0


if __name__ == "__main__":
	sys.exit(main())
