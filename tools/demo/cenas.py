#!/usr/bin/env python3
"""Cenas da demo contra o sistema de verdade: API em Development, Mosquitto, simulador e job de ML.

Chamado por tools/demo.sh (bash tools/demo.sh cenas [nome]). Cada cena move o relógio de demo só para
frente, então a ordem importa: rode depois de `tools/demo.sh subir`, que recria o banco.

Cenas, na ordem de `todas`:
  dia-critico  o job de ML declara o dia; as casas esperam; o movimento do simulador tira os cards
  desfechos    "não abriu" mantém o vermelho; "estava fora" é ausência confirmada; vizinho responde
  movimento    o movimento chega do simulador e tira do radar as casas que estavam vermelhas
  mudo         o aparelho para; a casa fica Cinza; a visita atende só na janela atual; nunca Verde
  energia      queda de energia na casa eletrodependente; trava até a ACS ver a pessoa
  reenvio      o Wi-Fi cai; o buffer volta duas vezes; cada evento é gravado uma vez
  bloqueio     a mensagem para o grupo do bairro é bloqueada; o painel muda; o LLM explica

Saída: uma linha por fato observado. Tokens aparecem só com os 8 primeiros caracteres.
Ambiente: CORREDOR_API_URL, CORREDOR_TOKEN_ACS, CORREDOR_DEMO_ESTADO (pasta .demo).
"""
from __future__ import annotations

import datetime as dt
import json
import os
import signal
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
from pathlib import Path
from zoneinfo import ZoneInfo

RAIZ = Path(__file__).resolve().parents[2]
API = os.environ.get("CORREDOR_API_URL", "http://localhost:5080").rstrip("/")
TOKEN_ACS = os.environ.get("CORREDOR_TOKEN_ACS", "dev-acs-demo-01-so-no-notebook")
ESTADO = Path(os.environ.get("CORREDOR_DEMO_ESTADO", RAIZ / ".demo"))
FUSO = ZoneInfo("America/Sao_Paulo")
MICROAREA = 3

CASAS = {c["n"]: c for c in json.loads((RAIZ / "api/semente/tokens.local.json").read_text())}
N_DO_TOKEN = {c["casaToken"]: n for n, c in CASAS.items()}
falhas: list[str] = []


# ---------- saída ----------
def dizer(texto: str) -> None:
	agora = dt.datetime.now(FUSO).strftime("%H:%M:%S")
	print(f"[{agora}] {texto}", flush=True)


def conferir(condicao: bool, texto: str) -> bool:
	dizer(("OK    " if condicao else "FALHOU ") + texto)
	if not condicao:
		falhas.append(texto)
	return condicao


def casa(token: str) -> str:
	n = N_DO_TOKEN.get(token)
	return f"casa {n}" if n else f"casa {token[:8]}"


# ---------- HTTP ----------
def http(metodo: str, caminho: str, corpo: object | None = None, acs: bool = True) -> tuple[int, object]:
	cabecalhos = {"Accept": "application/json"}
	if corpo is not None:
		cabecalhos["Content-Type"] = "application/json"
	if acs:
		cabecalhos["Authorization"] = f"Bearer {TOKEN_ACS}"
	dados = None if corpo is None else json.dumps(corpo).encode()
	pedido = urllib.request.Request(API + caminho, data=dados, method=metodo, headers=cabecalhos)
	try:
		with urllib.request.urlopen(pedido, timeout=30) as r:
			texto = r.read().decode()
			return r.status, (json.loads(texto) if texto else None)
	except urllib.error.HTTPError as e:
		texto = e.read().decode()
		return e.code, (json.loads(texto) if texto.startswith("{") else texto)


def radar() -> dict:
	status, corpo = http("GET", f"/api/radar?microarea={MICROAREA}")
	assert status == 200, (status, corpo)
	return corpo


def situacoes(r: dict) -> dict[int, tuple[str, str | None, bool]]:
	"""n -> (situação, motivo, atendida). Verde não tem card: é o que sobra."""
	mapa: dict[int, tuple[str, str | None, bool]] = {}
	for grupo in ("pendentes", "atendidas"):
		for c in r[grupo]:
			mapa[N_DO_TOKEN.get(c["casaToken"], 0)] = (c["situacao"], c["motivo"], c["atendida"])
	for c in r["aguardando"]:
		mapa[N_DO_TOKEN.get(c["casaToken"], 0)] = ("Aguardando", None, False)
	for n in CASAS:
		mapa.setdefault(n, ("Verde", None, False))
	return mapa


def resumo(r: dict) -> str:
	return (f"agora {r['agora'][11:19]}, turno {'aberto' if r['turnoAberto'] else 'fechado'}: "
	        f"{len(r['pendentes'])} pendentes, {len(r['atendidas'])} atendidas, "
	        f"{len(r['aguardando'])} aguardando, {r['verdes']} verdes")


def hoje() -> dt.date:
	return dt.datetime.fromisoformat(radar()["agora"]).date()


def relogio(hora: str, esperar_heartbeat: bool = True) -> None:
	"""Move o relógio do Motor. Depois de um salto, todo aparelho parece mudo até o próximo heartbeat
	(UltimoPacoteEm é a hora do servidor), então espera um ciclo de heartbeat, de 20 s, mais folga."""
	alvo = f"{hoje().isoformat()}T{hora}:00-03:00"
	status, corpo = http("PUT", "/api/demo/relogio", {"agora": alvo}, acs=False)
	conferir(status == 200, f"PUT /api/demo/relogio {hora} -> {status}, deslocamento {corpo.get('deslocamentoEmSegundos') if isinstance(corpo, dict) else corpo} s")
	if esperar_heartbeat:
		time.sleep(25)


def visita(n: int, desfecho: str, observacao: str | None = None) -> tuple[int, object]:
	# Como o app: o id nasce no cliente e ocorridaEm é a hora do celular. O servidor soma o deslocamento.
	corpo = {"id": str(uuid.uuid4()), "casaToken": CASAS[n]["casaToken"],
	         "ocorridaEm": dt.datetime.now(FUSO).isoformat(timespec="seconds"), "desfecho": desfecho, "observacao": observacao}
	status, resposta = http("POST", "/api/visitas", corpo)
	codigo = f" {resposta.get('codigo')}" if status >= 400 and isinstance(resposta, dict) else ""
	dizer(f"POST /api/visitas casa {n} {desfecho} -> {status}{codigo}")
	conferir(status in (200, 201), f"a API aceitou a visita da casa {n}")
	return status, resposta


def esperar(descricao: str, segundos: float, condicao, intervalo: float = 3):
	limite = time.monotonic() + segundos
	while True:
		valor = condicao()
		if valor:
			return valor
		if time.monotonic() > limite:
			dizer(f"tempo esgotado ({segundos:.0f} s) esperando {descricao}")
			return None
		time.sleep(intervalo)


# ---------- simulador ----------
_simulador: subprocess.Popen | None = None


def simulador(cenario: str, *extra: str) -> Path:
	"""Troca o simulador em execução. Só um por vez: o client id do MQTT é fixo."""
	global _simulador
	parar_simulador()
	log = ESTADO / f"simulador-{cenario}.log"
	_simulador = subprocess.Popen(
		[sys.executable, str(RAIZ / "iot/simulador/simulador.py"), "--host", "127.0.0.1", "--porta", "1883",
		 "--cenario", cenario, "--semente", "20261119", *extra],
		stdout=log.open("w"), stderr=subprocess.STDOUT)
	dizer(f"simulador: cenário {cenario} {' '.join(extra)} (log em .demo/{log.name})")
	time.sleep(2)
	return log


def parar_simulador() -> None:
	global _simulador
	if _simulador and _simulador.poll() is None:
		_simulador.send_signal(signal.SIGINT)
		_simulador.wait(10)
	_simulador = None


def linha_do_tempo(n: int) -> dict:
	status, corpo = http("GET", f"/api/casas/{CASAS[n]['casaToken']}/linha-do-tempo")
	assert status == 200, (status, corpo)
	return corpo


# ---------- cenas ----------
def cena_dia_critico() -> None:
	dizer("== Cena 1: dia crítico declarado pelo job de ML, as casas esperam, o movimento tira os cards")
	relogio("05:00")
	log = simulador("demo", "--inicio", "0")
	esperar("o heartbeat das casas simuladas", 60, lambda: len(radar()["aguardando"]) >= 35)
	r = radar()
	dizer("antes do dia crítico: " + resumo(r))
	conferir(not r["turnoAberto"] and r["verdes"] == 0, "sem turno não existe Verde")

	dizer("python3 ml/prever_hoje.py --dia-das-variaveis 2025-12-29")
	job = subprocess.run([sys.executable, str(RAIZ / "ml/prever_hoje.py"), "--dia-das-variaveis", "2025-12-29"],
	                     capture_output=True, text=True, env={**os.environ, "CORREDOR_API_URL": API})
	for linha in (job.stdout + job.stderr).strip().splitlines():
		dizer("  ml: " + linha.replace(API, "<api>"))
	conferir(job.returncode == 0, f"prever_hoje.py saiu com {job.returncode}")
	r = radar()
	dizer("logo depois: " + resumo(r))
	conferir(r["turnoAberto"], "o turno abriu")
	recebeu = esperar("o comando DIA_CRITICO chegar ao simulador", 20,
	                  lambda: "recebeu DIA_CRITICO" in log.read_text())
	conferir(bool(recebeu), "o Motor publicou DIA_CRITICO e o simulador recebeu")
	# A casa B avisa a saída com o duplo toque logo que o nó liga. Religar o simulador agora põe o toque
	# dentro do turno, como o 07h30 do roteiro. Antes da abertura, a saída avisada não vale.
	simulador("demo", "--inicio", "0")
	acamados = {n for n, s in situacoes(r).items() if s[1] == "SemMovimentoNaJanela"}
	conferir({2, 11, 21, 31} <= acamados, f"os acamados entram vermelhos na abertura: {sorted(acamados)}")

	time.sleep(90)
	r = radar()
	dizer("90 s depois, com movimento chegando: " + resumo(r))
	conferir(r["verdes"] > 0, "casas com movimento depois da abertura ficaram Verde")

	relogio("10:00")
	r = radar()
	s = situacoes(r)
	dizer("às 10h: " + resumo(r))
	vermelhas = sorted(n for n, (sit, mot, _) in s.items() if mot == "SemMovimentoNaJanela" and n not in {2, 11, 21, 31})
	dizer(f"vermelhas por SemMovimentoNaJanela, fora os acamados: {vermelhas}")
	conferir({5, 6, 7} <= set(vermelhas), "C, D e E, em silêncio desde cedo, estão vermelhas")
	b = next((c for c in r["aguardando"] if c["casaToken"] == CASAS[4]["casaToken"]), None)
	conferir(b is not None and b["saidaAvisada"], f"a casa 4 (B) avisou a saída e fica fora da regra 4: aguardando, saidaAvisada {b and b['saidaAvisada']}")
	dizer("radar: " + resumo(radar()))


def cena_movimento() -> None:
	dizer("== O movimento chega e tira os cards: as casas vermelhas voltam a ter gente passando no corredor")
	r = radar()
	vermelhas = sorted(n for n, (_, mot, _a) in situacoes(r).items() if mot == "SemMovimentoNaJanela" and n not in {2, 11, 21, 31})
	dizer(f"vermelhas por SemMovimentoNaJanela, fora os acamados: {vermelhas}. " + resumo(r))
	conferir(bool(vermelhas), "há casa vermelha esperando movimento")
	# No cenário manha toda casa é normal, e cada nó manda o primeiro MOVIMENTO de 5 a 90 s depois de ligar.
	simulador("manha")
	inicio_da_janela = r["agora"][11:13] + ":00"

	def sairam():
		s2 = situacoes(radar())
		return all(s2[n][0] == "Verde" for n in vermelhas) and s2

	s2 = esperar("o movimento das casas vermelhas", 150, sairam, intervalo=5) or situacoes(radar())
	for n in vermelhas:
		movimentos = [e for e in linha_do_tempo(n)["eventos"] if e["tipo"] == "MOVIMENTO" and e["ocorridoEm"][11:16] >= inicio_da_janela]
		conferir(s2[n][0] == "Verde" and bool(movimentos),
		         f"casa {n}: saiu dos pendentes e está {s2[n][0]}, com {len(movimentos)} MOVIMENTO nesta janela")
	dizer("radar: " + resumo(radar()))


def cena_desfechos() -> None:
	dizer("== Desfechos da visita: não abriu, estava fora, estava bem e a resposta do vizinho")
	status, _ = visita(7, "NaoAbriu", "ninguém atendeu")
	s = situacoes(radar())
	conferir(status == 201 and s[7] == ("Vermelho", "SemMovimentoNaJanela", False),
	         f"casa 7 (E) depois de NaoAbriu: {s[7]}, continua pendente")
	status, _ = visita(7, "EstavaFora", "vizinha disse que foi à feira")
	s = situacoes(radar())
	conferir(status == 201 and s[7] == ("Vermelho", "SemMovimentoNaJanela", True),
	         f"casa 7 depois de EstavaFora: {s[7]}, atendida e nunca Verde")
	tl = linha_do_tempo(7)
	conferir([c["resultado"] for c in tl["confirmacoes"]] == ["AusenciaConfirmada"],
	         f"linha do tempo da casa 7: confirmações {[(c['origem'], c['resultado']) for c in tl['confirmacoes']]}")

	status, _ = visita(6, "EstavaBem")
	s = situacoes(radar())
	conferir(status == 201 and s[6][0] == "Verde", f"casa 6 (D) depois de EstavaBem: {s[6]}")

	aviso = esperar("o aviso ao vizinho da casa 5", 30, lambda: next(
		(a for a in http("GET", f"/api/avisos?microarea={MICROAREA}")[1] if a["casaToken"] == CASAS[5]["casaToken"]), None))
	if conferir(aviso is not None, "o VigiaDoRadar pediu contato e o Cadastro registrou o aviso ao vizinho da casa 5 (C)"):
		status, corpo = http("POST", f"/api/avisos/{aviso['id']}/resposta", {"resposta": "EstaBem"}, acs=False)
		s = situacoes(radar())
		conferir(status == 200 and s[5][0] == "Verde", f"vizinho respondeu EstaBem -> {status}; casa 5: {s[5]}")
		status, corpo = http("POST", f"/api/avisos/{aviso['id']}/resposta", {"resposta": "PrecisaDeAjuda"}, acs=False)
		conferir(status == 409, f"resposta diferente depois -> {status} {corpo.get('codigo') if isinstance(corpo, dict) else ''}")
	dizer("radar: " + resumo(radar()))


def cena_mudo() -> None:
	dizer("== Cena 2: o aparelho mudo fica cinza e nunca vira verde")
	n = 15
	simulador("manha")
	s = esperar(f"a casa {n} ficar Verde", 420, lambda: (x := situacoes(radar()))[n][0] == "Verde" and x, intervalo=10)
	conferir(bool(s), f"casa {n} (Sozinho, não eletrodependente) está Verde, com sinal de vida nesta janela")
	simulador("mudo", "--casa", str(n), "--inicio", "20")
	vistos: list[str] = []

	def cinza():
		sit = situacoes(radar())[n]
		vistos.append(sit[0])
		return sit[0] == "Cinza" and sit

	s = esperar(f"a casa {n} ficar Cinza", 150, cinza, intervalo=5)
	s = situacoes(radar())[n]
	conferir(s == ("Cinza", "SemHeartbeat", False), f"casa {n}: {s} depois de 20 s de pacotes e mais de 1 min sem nenhum")
	for _ in range(6):
		time.sleep(5)
		vistos.append(situacoes(radar())[n][0])
	conferir(vistos[-6:].count("Verde") == 0, f"30 s depois continua sem Verde: {vistos[-6:]}")

	visita(n, "EstavaBem", "visitei, está bem")
	s = situacoes(radar())[n]
	conferir(s == ("Cinza", "SemHeartbeat", True), f"depois da visita EstavaBem: {s}. Atendida, ainda Cinza")
	time.sleep(10)
	s = situacoes(radar())[n]
	conferir(s[0] == "Cinza", f"10 s depois, sem pacote novo: {s}")

	relogio("15:00")
	s = situacoes(radar())[n]
	conferir(s == ("Cinza", "SemHeartbeat", False), f"na janela seguinte (15h) a visita não vale mais: {s}")

	simulador("manha")
	s = esperar(f"o primeiro pacote novo da casa {n}", 60, lambda: (x := situacoes(radar())[n])[0] != "Cinza" and x)
	conferir(bool(s), f"com pacote novo a casa sai do Cinza: {s}")
	dizer("radar: " + resumo(radar()))


def cena_energia() -> None:
	dizer("== Queda de energia na casa eletrodependente (casa 2, acamada, concentrador)")
	n = 2
	simulador("queda-de-energia", "--casa", str(n), "--inicio", "15", "--janela", "45")
	s = esperar("a casa 2 ficar vermelha por energia", 60,
	            lambda: (x := situacoes(radar())[n])[1] == "EnergiaEletrodependente" and x)
	conferir(bool(s), f"durante a queda: casa 2 {situacoes(radar())[n]}")
	time.sleep(50)
	tl = linha_do_tempo(n)
	tensoes = [e["tensaoV"] for e in tl["eventos"] if e["tipo"] == "TELEMETRIA"][-3:]
	dizer(f"últimas tensões da casa 2 na linha do tempo: {tensoes}")
	s = situacoes(radar())[n]
	conferir(tensoes and tensoes[-1] >= 100 and s[1] == "EnergiaEletrodependente",
	         f"a luz voltou ({tensoes[-1] if tensoes else '?'} V) e a casa continua vermelha pela trava: {s}")
	aviso = esperar("o aviso ao vizinho da casa 2 por energia", 30, lambda: next(
		(a for a in http("GET", f"/api/avisos?microarea={MICROAREA}")[1]
		 if a["casaToken"] == CASAS[n]["casaToken"] and a["motivo"] == "EnergiaEletrodependente"), None))
	conferir(aviso is not None, "o vizinho da casa 2 recebeu um aviso por EnergiaEletrodependente")
	visita(n, "EstavaBem", "concentrador ligado de novo")
	s = situacoes(radar())[n]
	conferir(s[1] != "EnergiaEletrodependente", f"depois que a ACS viu a pessoa: {s}")
	dizer("radar: " + resumo(radar()))


def cena_reenvio() -> None:
	dizer("== Reenvio do buffer: o Wi-Fi da casa 13 cai e os eventos voltam duas vezes")
	n = 13
	antes = {(e["seq"], e["ocorridoEm"]) for e in linha_do_tempo(n)["eventos"]}
	log = simulador("reenvio", "--casa", str(n), "--inicio", "10", "--janela", "40")
	time.sleep(70)
	guardados = [json.loads(l.split(" ", 2)[2]) for l in log.read_text().splitlines() if l.startswith("GUARDADO")]
	reenvios = [l for l in log.read_text().splitlines() if f"casa {n} " in l and '"bufferizado":true' in l]
	dizer(f"o nó guardou {len(guardados)} eventos e publicou {len(reenvios)} pacotes com bufferizado: true")
	eventos = linha_do_tempo(n)["eventos"]
	desloc = dt.datetime.fromisoformat(radar()["agora"]) - dt.datetime.now(FUSO)
	for g in guardados:
		ts = dt.datetime.fromisoformat(g["ts"].replace("Z", "+00:00"))
		iguais = [e for e in eventos if e["seq"] == g["seq"] and abs(
			(dt.datetime.fromisoformat(e["ocorridoEm"]) - ts - desloc).total_seconds()) < 5]
		conferir(len(iguais) == 1 and iguais[0]["bufferizado"],
		         f"seq {g['seq']} {g['tipo']}: gravado {len(iguais)} vez, bufferizado {iguais[0]['bufferizado'] if iguais else '-'}")
	conferir(len(guardados) > 0 and len(reenvios) >= 2 * len(guardados), "cada evento guardado saiu duas vezes no fio")
	simulador("manha")


def cena_bloqueio() -> None:
	dizer("== Cena 3: a mensagem com dado pessoal é bloqueada e explicada")
	_, antes = http("GET", "/api/painel", acs=False)
	dizer(f"painel antes: cor {antes['cor']}, {antes['pontos24h']} pontos, {len(antes['eventos'])} eventos")
	status, corpo = http("POST", "/api/mensagens/verificar",
	                     {"canal": "GrupoDoBairro", "texto": "ele mora sozinho e toma diurético"})
	dizer(f"POST /api/mensagens/verificar -> {status} {json.dumps(corpo, ensure_ascii=False)}")
	conferir(status == 200 and corpo["veredito"] == "Bloqueada"
	         and corpo["textoMascarado"] == "ele [MORA_SOZINHO] e toma [SAUDE]"
	         and sorted(corpo["tiposDeDado"]) == ["MoraSozinho", "Saude"], "a Guarda bloqueou e mascarou")

	def traduzido():
		_, p = http("GET", "/api/painel", acs=False)
		novos = [e for e in p["eventos"] if e["canal"] == "GrupoDoBairro" and e["tipo"] == "BloqueioDeSaida"]
		return (p, novos[0]) if novos and novos[0]["origemDaMensagem"] == "Llm" else None

	achado = esperar("a tradução do LLM no painel", 40, traduzido, intervalo=2)
	_, depois = http("GET", "/api/painel", acs=False)
	evento = next((e for e in depois["eventos"] if e["canal"] == "GrupoDoBairro"), None)
	dizer(f"painel depois: cor {depois['cor']}, {depois['pontos24h']} pontos, {len(depois['eventos'])} eventos")
	if evento:
		dizer(f"evento: {json.dumps(evento, ensure_ascii=False)}")
	conferir(depois["cor"] == "Vermelho", "o farol do painel Risco Cidadão ficou vermelho")
	conferir(achado is not None, "a mensagem ao cidadão veio do LLM (origemDaMensagem = Llm), não da frase fixa")


CENAS = {
	"dia-critico": cena_dia_critico,
	"desfechos": cena_desfechos,
	"movimento": cena_movimento,
	"mudo": cena_mudo,
	"energia": cena_energia,
	"reenvio": cena_reenvio,
	"bloqueio": cena_bloqueio,
}


def main() -> int:
	escolhidas = sys.argv[1:] or list(CENAS)
	try:
		for nome in escolhidas:
			CENAS[nome]()
	finally:
		if os.environ.get("CORREDOR_DEMO_MANTER_SIMULADOR") != "1":
			parar_simulador()
	dizer(f"== {len(falhas)} conferência(s) falharam" if falhas else "== todas as conferências passaram")
	for f in falhas:
		dizer("  " + f)
	return 1 if falhas else 0


if __name__ == "__main__":
	sys.exit(main())
