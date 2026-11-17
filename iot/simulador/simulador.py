#!/usr/bin/env python3
"""Simulador das 39 casas do Corredor Aceso.

Publica no Mosquitto local com o mesmo contrato do nó do Wokwi (seção 6.2 de
docs/arquitetura/03-modelo-de-dominio.md) e assina os comandos das suas casas.

Uso na VM (infra/systemd/corredor-simulador.service):
    simulador.py --host 127.0.0.1 --porta 1883

Cenários (--cenario):
    demo              padrão. Cada casa faz o papel que a semente dá a ela (papelNaDemo).
    manha             manhã normal: as 39 casas com movimento, heartbeat e telemetria.
    silencio          uma casa para de dar sinal de vida, com o aparelho ligado. Casa 5.
    mudo              um aparelho para de mandar qualquer pacote. Casa 10.
    queda-de-energia  a tensão de uma casa eletrodependente cai abaixo de 100 V e volta. Casa 2.
    reenvio           o Wi-Fi de uma casa cai, os eventos ficam no buffer e voltam com a hora
                      original e bufferizado: true, duas vezes. Casa 13.
"""
from __future__ import annotations

import argparse
import json
import queue
import random
import sys
import time
from datetime import datetime, timedelta, timezone
from pathlib import Path

from modelo import (
	COMANDOS,
	Comportamento,
	NoSimulado,
	carregar_casas,
	casas_ficticias,
	comportamento_do_papel,
	topico_evento,
)

PASTA = Path(__file__).resolve().parent
TOKENS_PADRAO = PASTA.parent.parent / "api" / "semente" / "tokens.local.json"
CENARIOS = {
	"demo": None,
	"manha": None,
	"silencio": (Comportamento.SILENCIOSA, 5),
	"mudo": (Comportamento.MUDA, 10),
	"queda-de-energia": (Comportamento.QUEDA_DE_ENERGIA, 2),
	"reenvio": (Comportamento.REENVIO, 13),
}


def argumentos(argv: list[str] | None = None) -> argparse.Namespace:
	p = argparse.ArgumentParser(description="Simulador das 39 casas do Corredor Aceso")
	p.add_argument("--host", default="127.0.0.1")
	p.add_argument("--porta", type=int, default=1883)
	p.add_argument("--cenario", choices=sorted(CENARIOS), default="demo")
	p.add_argument("--casa", type=int, help="casa (n da semente) que sofre o cenário")
	p.add_argument("--inicio", type=float, default=30, help="segundos até o cenário começar")
	p.add_argument("--janela", type=float, default=60, help="segundos de queda de energia ou de Wi-Fi cortado")
	p.add_argument("--duracao", type=float, default=0, help="segundos até sair. 0 roda para sempre")
	p.add_argument("--tokens", type=Path, default=TOKENS_PADRAO, help="tokens.local.json escrito por --semear")
	p.add_argument("--tokens-ficticios", action="store_true", help="sorteia 39 tokens, para rodar sem a API")
	p.add_argument("--semente", type=int, default=None, help="semente do sorteio, para repetir uma rodada")
	p.add_argument("--detalhado", action="store_true", help="mostra também os heartbeats")
	return p.parse_args(argv)


def montar_nos(args: argparse.Namespace, agora: datetime) -> list[NoSimulado]:
	sorteio = random.Random(args.semente)
	if args.tokens_ficticios:
		casas = casas_ficticias(sorteio)
	elif args.tokens.is_file():
		casas = carregar_casas(args.tokens)
	else:
		sys.exit(f"sem {args.tokens}. Rode Corredor.Api --semear, passe --tokens ou use --tokens-ficticios.")

	alvo = CENARIOS[args.cenario]
	n_alvo = args.casa if args.casa is not None else (alvo[1] if alvo else None)
	if alvo and n_alvo not in {c.n for c in casas}:
		sys.exit(f"a casa {n_alvo} não está entre as casas simuladas")

	nos = []
	for casa in casas:
		if args.cenario == "demo":
			comportamento = comportamento_do_papel(casa.papel)
		elif alvo and casa.n == n_alvo:
			comportamento = alvo[0]
		else:
			comportamento = Comportamento.NORMAL
		nos.append(
			NoSimulado(
				casa,
				comportamento,
				agora,
				random.Random(sorteio.random()),
				inicio=timedelta(seconds=args.inicio),
				duracao=timedelta(seconds=args.janela),
			)
		)
	return nos


def executar(args: argparse.Namespace) -> None:
	import paho.mqtt.client as mqtt

	comandos: queue.Queue[tuple[str, str]] = queue.Queue()

	def ao_conectar(cliente, _dados, _flags, codigo, _props):
		print(f"conectado a {args.host}:{args.porta} ({codigo})", flush=True)
		cliente.subscribe("corredor/v1/+/comando", qos=1)

	def ao_receber(_cliente, _dados, msg):
		partes = msg.topic.split("/")
		try:
			tipo = json.loads(msg.payload)["tipo"]
		except (ValueError, KeyError, TypeError):
			print(f"comando ignorado em {msg.topic}: corpo fora do contrato", flush=True)
			return
		if len(partes) == 4 and tipo in COMANDOS:
			comandos.put((partes[2], tipo))

	cliente = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id="corredor-simulador")
	cliente.on_connect = ao_conectar
	cliente.on_message = ao_receber
	cliente.reconnect_delay_set(1, 30)
	cliente.connect(args.host, args.porta, keepalive=30)
	cliente.loop_start()

	inicio = datetime.now(timezone.utc)
	nos = montar_nos(args, inicio)
	por_token = {no.casa.token: no for no in nos}
	alvos = [f"casa {no.casa.n}: {no.comportamento.value}" for no in nos if no.comportamento is not Comportamento.NORMAL]
	print(f"cenário {args.cenario}, {len(nos)} casas. {'; '.join(alvos) or 'todas normais'}", flush=True)
	# Linhas lidas por conferir_fio.py --log. O token só vai ao log local, nunca ao Git.
	cenario = {"cenario": args.cenario, "ligadoEm": inicio.isoformat(), "inicio": args.inicio, "janela": args.janela}
	print(f"CENARIO {json.dumps(cenario)}", flush=True)
	for no in nos:
		print(f"CASA {no.casa.n} {no.casa.token} {no.comportamento.value}", flush=True)

	try:
		while args.duracao <= 0 or (datetime.now(timezone.utc) - inicio).total_seconds() < args.duracao:
			agora = datetime.now(timezone.utc)
			while not comandos.empty():
				token, tipo = comandos.get()
				no = por_token.get(token)
				if no:
					print(f"{agora:%H:%M:%S} casa {no.casa.n} recebeu {tipo}", flush=True)
					no.receber_comando(tipo, agora)
			for no in nos:
				ja_guardados = len(no.guardados)
				for pacote in no.passo(agora):
					cliente.publish(topico_evento(no.casa.token), pacote.corpo(), qos=1)
					if args.detalhado or pacote.tipo != "HEARTBEAT":
						print(f"{agora:%H:%M:%S} casa {no.casa.n} {pacote.corpo()}", flush=True)
				for pacote in no.guardados[ja_guardados:]:
					# Linha lida por conferir_fio.py --log para provar que o reenvio guarda a chave.
					print(f"GUARDADO {no.casa.token} {pacote.corpo()}", flush=True)
			time.sleep(1)
	except KeyboardInterrupt:
		pass
	finally:
		cliente.loop_stop()
		cliente.disconnect()


if __name__ == "__main__":
	executar(argumentos())
