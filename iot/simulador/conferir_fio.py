#!/usr/bin/env python3
"""Confere mensagens MQTT capturadas no fio contra o contrato da seção 6.2.

Captura no formato `mosquitto_sub -v -F '%U %t %p' -t 'corredor/v1/#'`: hora de chegada em
segundos Unix, tópico e corpo, uma mensagem por linha.

Sem --log, confere cada mensagem sozinha: tópico, campos, tipos, `ts`, tamanho, e que a mesma
chave (casaToken, seq, ts) nunca chega com corpos diferentes.

Com --log (a saída do simulador.py), confere também o comportamento de cada casa no cenário:
cadência do heartbeat, silêncio, aparelho mudo, queda de energia e reenvio com a chave original.

Uso: conferir_fio.py captura.txt [--log simulador.log]
Sai com 1 se achar qualquer erro.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path

from modelo import CAMPOS, COMANDOS, TIPOS, INTERVALO_HEARTBEAT, TENSAO_MINIMA

EVENTO = re.compile(r"^corredor/v1/([0-9a-f]{32})/evento$")
COMANDO = re.compile(r"^corredor/v1/([0-9a-f]{32})/comando$")
TS = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$")
TOLERANCIA_DO_RELOGIO = timedelta(minutes=2)
RETENCAO = timedelta(hours=24)
FOLGA = timedelta(seconds=2)


@dataclass(frozen=True)
class Evento:
	chegada: datetime
	token: str
	seq: int
	ts: datetime
	tipo: str
	temp_c: float | None
	tensao_v: float | None
	bufferizado: bool
	corpo: str


def numero(valor) -> bool:
	return valor is None or (isinstance(valor, (int, float)) and not isinstance(valor, bool))


def ler_captura(linhas: list[str], erros: list[str]) -> tuple[list[Evento], list[tuple[str, str]]]:
	eventos, comandos = [], []
	for i, linha in enumerate(linhas, start=1):
		if not linha.strip():
			continue
		try:
			hora, topico, corpo = linha.rstrip("\n").split(" ", 2)
			chegada = datetime.fromtimestamp(float(hora), timezone.utc)
		except ValueError:
			erros.append(f"linha {i}: fora do formato '%U %t %p'")
			continue
		if m := COMANDO.match(topico):
			try:
				dados = json.loads(corpo)
			except ValueError:
				dados = None
			if not isinstance(dados, dict) or set(dados) != {"tipo"} or dados["tipo"] not in COMANDOS:
				erros.append(f"linha {i}: comando fora do contrato: {corpo}")
			else:
				comandos.append((m.group(1), dados["tipo"]))
			continue
		m = EVENTO.match(topico)
		if not m:
			erros.append(f"linha {i}: tópico fora do contrato: {topico}")
			continue
		if len(corpo.encode()) > 256:
			erros.append(f"linha {i}: corpo com mais de 256 bytes")
		try:
			d = json.loads(corpo)
		except ValueError:
			erros.append(f"linha {i}: corpo não é JSON")
			continue
		if not isinstance(d, dict) or set(d) != set(CAMPOS):
			erros.append(f"linha {i}: campos {sorted(d) if isinstance(d, dict) else d} diferentes de {sorted(CAMPOS)}")
			continue
		problemas = []
		if not isinstance(d["seq"], int) or isinstance(d["seq"], bool) or d["seq"] < 1:
			problemas.append("seq não é inteiro positivo")
		if not isinstance(d["ts"], str) or not TS.match(d["ts"]):
			problemas.append("ts fora do formato UTC com Z")
		if d["tipo"] not in TIPOS:
			problemas.append(f"tipo desconhecido {d['tipo']!r}")
		if not numero(d["tempC"]) or not numero(d["tensaoV"]):
			problemas.append("tempC ou tensaoV não é número")
		if not isinstance(d["bufferizado"], bool):
			problemas.append("bufferizado não é booleano")
		if problemas:
			erros.append(f"linha {i}: {'; '.join(problemas)}")
			continue
		ts = datetime.strptime(d["ts"], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
		if ts > chegada + TOLERANCIA_DO_RELOGIO or ts < chegada - RETENCAO:
			erros.append(f"linha {i}: ts {d['ts']} fora da janela que o Motor aceita")
		eventos.append(Evento(chegada, m.group(1), d["seq"], ts, d["tipo"], d["tempC"], d["tensaoV"], d["bufferizado"], corpo))
	return eventos, comandos


def conferir_chaves(eventos: list[Evento], erros: list[str]) -> None:
	corpos: dict[tuple, set[str]] = defaultdict(set)
	for e in eventos:
		corpos[(e.token, e.seq, e.ts)].add(e.corpo)
	for (token, seq, ts), vistos in corpos.items():
		if len(vistos) > 1:
			erros.append(f"chave ({token[:6]}…, {seq}, {ts:%H:%M:%S}) chegou com corpos diferentes")
	for e in eventos:
		if e.bufferizado and e.ts > e.chegada - timedelta(seconds=1):
			erros.append(f"{e.token[:6]}… seq {e.seq}: bufferizado com ts igual à chegada, não ficou guardado")


def ler_log(linhas: list[str]) -> tuple[dict, dict[str, tuple[int, str]], list[tuple[str, dict]]]:
	cenario, casas, guardados = {}, {}, []
	for linha in linhas:
		partes = linha.split()
		if linha.startswith("CENARIO "):
			cenario = json.loads(linha[len("CENARIO "):])
		elif linha.startswith("CASA ") and len(partes) == 4:
			casas[partes[2]] = (int(partes[1]), partes[3])
		elif linha.startswith("GUARDADO "):
			guardados.append((partes[1], json.loads(partes[2])))
	return cenario, casas, guardados


def conferir_comportamento(eventos: list[Evento], log: list[str], erros: list[str], notas: list[str]) -> None:
	cenario, casas, guardados = ler_log(log)
	if not cenario:
		erros.append("o log não tem a linha CENARIO")
		return
	ligado = datetime.fromisoformat(cenario["ligadoEm"])
	# O ts tem precisão de segundo e é truncado, então a janela também é.
	comeco = (ligado + timedelta(seconds=cenario["inicio"])).replace(microsecond=0)
	fim = comeco + timedelta(seconds=cenario["janela"])
	fim_da_captura = max(e.chegada for e in eventos)
	por_casa: dict[str, list[Evento]] = defaultdict(list)
	for e in eventos:
		por_casa[e.token].append(e)
	desconhecidos = set(por_casa) - set(casas)
	if desconhecidos:
		erros.append(f"{len(desconhecidos)} token(s) no fio que o simulador não declarou")

	for token, (n, comportamento) in sorted(casas.items(), key=lambda kv: kv[1][0]):
		lista = por_casa.get(token, [])
		ao_vivo = [e for e in lista if not e.bufferizado]
		depois = [e for e in ao_vivo if e.chegada >= comeco + FOLGA]
		if comportamento == "muda":
			if depois:
				erros.append(f"casa {n} (muda): {len(depois)} pacote(s) depois do início")
			else:
				notas.append(f"casa {n} muda: {len([e for e in lista if e.chegada < comeco])} pacotes antes, 0 depois de {comeco:%H:%M:%S}")
			continue
		if comportamento == "silenciosa":
			sinais = [e for e in depois if e.tipo in ("MOVIMENTO", "BOTAO_BEM", "BOTAO_SAIR")]
			batidas = [e for e in depois if e.tipo == "HEARTBEAT"]
			if sinais:
				erros.append(f"casa {n} (silenciosa): {len(sinais)} sinal(is) de vida depois do início")
			if not batidas and fim_da_captura - comeco > INTERVALO_HEARTBEAT + FOLGA:
				erros.append(f"casa {n} (silenciosa): o aparelho também parou de bater")
			notas.append(f"casa {n} silenciosa: {len(batidas)} heartbeats e 0 sinais de vida depois de {comeco:%H:%M:%S}")
		if comportamento == "queda-de-energia":
			tele = [e for e in ao_vivo if e.tipo == "TELEMETRIA"]
			baixa = [e for e in tele if e.tensao_v is not None and e.tensao_v < TENSAO_MINIMA and comeco <= e.ts < fim]
			volta = [e for e in tele if e.tensao_v is not None and e.tensao_v >= TENSAO_MINIMA and e.ts >= fim]
			if not baixa:
				erros.append(f"casa {n} (queda): nenhuma TELEMETRIA abaixo de {TENSAO_MINIMA:.0f} V na janela")
			elif baixa[0].ts - comeco > FOLGA:
				erros.append(f"casa {n} (queda): a TELEMETRIA da queda saiu {baixa[0].ts - comeco} depois do cruzamento")
			if fim_da_captura > fim + FOLGA and not volta:
				erros.append(f"casa {n} (queda): nenhuma TELEMETRIA na volta da tensão")
			if baixa:
				notas.append(
					f"casa {n} queda: {baixa[0].tensao_v} V às {baixa[0].ts:%H:%M:%S}"
					+ (f", {volta[0].tensao_v} V às {volta[0].ts:%H:%M:%S}" if volta else "")
				)
		if comportamento == "reenvio":
			if not [g for g in guardados if g[0] == token]:
				erros.append(f"casa {n} (reenvio): nada foi guardado, o reenvio não foi provado")
			fora = [e for e in ao_vivo if comeco + FOLGA <= e.chegada < fim]
			if fora:
				erros.append(f"casa {n} (reenvio): {len(fora)} pacote(s) ao vivo com o Wi-Fi cortado")
		if comportamento in ("normal", "calor", "reenvio", "queda-de-energia", "silenciosa"):
			batidas = sorted(e.ts for e in ao_vivo if e.tipo == "HEARTBEAT")
			trechos = [batidas]
			if comportamento == "reenvio":
				# Com o Wi-Fi cortado não há heartbeat. Cada lado do corte é conferido sozinho.
				trechos = [[t for t in batidas if t < comeco], [t for t in batidas if t >= fim]]
			lacunas = [b - a for trecho in trechos for a, b in zip(trecho, trecho[1:])]
			grandes = [g for g in lacunas if g > INTERVALO_HEARTBEAT + FOLGA]
			if grandes:
				erros.append(f"casa {n}: heartbeat com lacuna de {max(grandes)}")

	reenviados = [e for e in eventos if e.bufferizado]
	for token, d in guardados:
		ts = datetime.strptime(d["ts"], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
		iguais = [e for e in eventos if e.token == token and e.seq == d["seq"] and e.ts == ts]
		if not iguais:
			erros.append(f"guardado seq {d['seq']} das {d['ts']} não apareceu no fio")
		elif any(not e.bufferizado for e in iguais):
			erros.append(f"guardado seq {d['seq']} saiu ao vivo também")
		elif any(e.tipo != d["tipo"] for e in iguais):
			erros.append(f"guardado seq {d['seq']} voltou com outro tipo")
	if guardados:
		atraso = min(e.chegada - e.ts for e in reenviados)
		notas.append(
			f"reenvio: {len(guardados)} eventos guardados, {len(reenviados)} chegadas com bufferizado: true, "
			f"mesmo seq, ts e tipo do momento em que foram guardados, o mais novo com {atraso.total_seconds():.0f} s de atraso"
		)


def main(argv: list[str] | None = None) -> int:
	p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
	p.add_argument("captura", type=Path)
	p.add_argument("--log", type=Path, help="saída do simulador.py da mesma rodada")
	args = p.parse_args(argv)
	erros: list[str] = []
	notas: list[str] = []
	eventos, comandos = ler_captura(args.captura.read_text(encoding="utf-8").splitlines(), erros)
	conferir_chaves(eventos, erros)
	if args.log and eventos:
		conferir_comportamento(eventos, args.log.read_text(encoding="utf-8").splitlines(), erros, notas)

	tipos = Counter(e.tipo for e in eventos)
	print(f"{len(eventos)} eventos de {len({e.token for e in eventos})} casas, {len(comandos)} comandos")
	print("por tipo: " + ", ".join(f"{t} {tipos[t]}" for t in TIPOS if tipos[t]))
	print(f"bufferizado: true em {sum(e.bufferizado for e in eventos)}, chaves distintas {len({(e.token, e.seq, e.ts) for e in eventos})}")
	for tipo in sorted(set(t for _, t in comandos)):
		print(f"comando {tipo}: {sum(1 for _, t in comandos if t == tipo)}")
	for nota in notas:
		print(f"  {nota}")
	if erros:
		print(f"FALHOU: {len(erros)} erro(s)")
		for erro in erros[:50]:
			print(f"  {erro}")
		return 1
	print("PASSOU: todas as mensagens seguem o contrato")
	return 0


if __name__ == "__main__":
	sys.exit(main())
