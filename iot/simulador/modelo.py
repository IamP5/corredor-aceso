"""Modelo puro do simulador: casas, pacotes e o comportamento de cada nó.

Não abre rede nem lê relógio. Quem chama passa `agora`, então os testes andam no tempo
sem esperar. O contrato é o da seção 6.2 de docs/arquitetura/03-modelo-de-dominio.md.
"""
from __future__ import annotations

import enum
import json
import random
import re
from dataclasses import dataclass, field, replace
from datetime import datetime, timedelta, timezone
from pathlib import Path

TIPOS = ("MOVIMENTO", "BOTAO_BEM", "BOTAO_SAIR", "TELEMETRIA", "HEARTBEAT")
COMANDOS = ("DIA_CRITICO", "PEDIR_TOQUE")
CAMPOS = ("seq", "ts", "tipo", "tempC", "tensaoV", "bufferizado")
TOKEN = re.compile(r"^[0-9a-f]{32}$")

INTERVALO_HEARTBEAT = timedelta(seconds=20)
INTERVALO_TELEMETRIA = timedelta(minutes=5)
TENSAO_MINIMA = 100.0
TENSAO_NORMAL = 127.0
TEMPERATURA_DE_CALOR = 34.5
# A casa 1 é o nó do Wokwi. O simulador faz as outras 39.
CASA_DO_WOKWI = 1


def topico_evento(token: str) -> str:
	return f"corredor/v1/{token}/evento"


def topico_comando(token: str) -> str:
	return f"corredor/v1/{token}/comando"


def formatar_ts(instante: datetime) -> str:
	return instante.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


@dataclass(frozen=True)
class Pacote:
	seq: int
	ts: datetime
	tipo: str
	temp_c: float
	tensao_v: float
	bufferizado: bool = False

	def corpo(self) -> str:
		return json.dumps(
			{
				"seq": self.seq,
				"ts": formatar_ts(self.ts),
				"tipo": self.tipo,
				"tempC": round(self.temp_c, 1),
				"tensaoV": round(self.tensao_v, 1),
				"bufferizado": self.bufferizado,
			},
			separators=(",", ":"),
		)


@dataclass(frozen=True)
class Casa:
	n: int
	token: str
	papel: str


def carregar_casas(caminho: Path) -> list[Casa]:
	"""Lê o tokens.local.json que `Corredor.Api --semear` escreve e devolve as 39 casas.

	Aceita uma lista de objetos ou um objeto com a chave `casas`. Cada item tem `n`,
	`casaToken` e `papelNaDemo`. Token fora do formato do CasaToken é recusado.
	"""
	dados = json.loads(caminho.read_text(encoding="utf-8"))
	itens = dados["casas"] if isinstance(dados, dict) else dados
	casas = []
	for item in itens:
		token = item["casaToken"]
		if not TOKEN.match(token):
			raise ValueError(f"casa {item['n']}: token fora do formato de 32 hexadecimais minúsculos")
		if item["n"] == CASA_DO_WOKWI:
			continue
		casas.append(Casa(item["n"], token, item.get("papelNaDemo") or ""))
	return sorted(casas, key=lambda c: c.n)


def casas_ficticias(sorteio: random.Random, quantidade: int = 39) -> list[Casa]:
	"""Tokens sorteados, para rodar sem a API. O Motor descarta token desconhecido."""
	return [Casa(n, f"{sorteio.getrandbits(128):032x}", "") for n in range(2, 2 + quantidade)]


class Comportamento(enum.Enum):
	NORMAL = "normal"
	SILENCIOSA = "silenciosa"  # aparelho ligado, pessoa não passa: heartbeat e telemetria, nada mais
	MUDA = "muda"  # aparelho parado: nada sai
	QUEDA_DE_ENERGIA = "queda-de-energia"  # tensão cai abaixo de 100 V e volta
	REENVIO = "reenvio"  # Wi-Fi cai, eventos ficam no buffer, voltam com a hora original
	SAIDA_AVISADA = "saida-avisada"  # duplo toque logo no início, depois a casa fica vazia
	CALOR = "calor"  # temperatura interna de 34,5 °C
	TOQUE = "toque"  # parada até o pedido de toque, então aperta o botão


# Papel da casa na semente (papelNaDemo) para o comportamento no cenário `demo`.
PAPEIS = (
	("B:", Comportamento.SAIDA_AVISADA),
	("C:", Comportamento.SILENCIOSA),
	("D:", Comportamento.SILENCIOSA),
	("E:", Comportamento.SILENCIOSA),
	("F:", Comportamento.CALOR),
	("G:", Comportamento.TOQUE),
	("aparelho mudo", Comportamento.MUDA),
)


def comportamento_do_papel(papel: str) -> Comportamento:
	for prefixo, comportamento in PAPEIS:
		if papel.startswith(prefixo):
			return comportamento
	return Comportamento.NORMAL


@dataclass
class NoSimulado:
	"""Um nó de corredor. Mesmas regras do firmware em iot/no/no.ino.

	- `seq` começa em 1 e sobe a cada pacote, de qualquer tipo.
	- HEARTBEAT a cada 20 s. TELEMETRIA ao ligar, a cada 5 min e quando a tensão cruza 100 V.
	- Sem rede, MOVIMENTO, BOTAO_* e TELEMETRIA vão para o buffer. HEARTBEAT não, porque
	  ele só diz que o aparelho está ligado agora.
	- Na volta, o buffer sai com o seq e o ts originais e `bufferizado: true`.

	`inicio` é quando o comportamento do cenário começa. Antes dele, a casa é normal.
	"""

	casa: Casa
	comportamento: Comportamento
	ligado_em: datetime
	sorteio: random.Random
	inicio: timedelta = timedelta(seconds=30)
	duracao: timedelta = timedelta(seconds=60)
	repeticoes_do_reenvio: int = 2
	seq: int = 0
	temp_c: float = 28.0
	tensao_v: float = TENSAO_NORMAL
	buffer: list[Pacote] = field(default_factory=list)
	guardados: list[Pacote] = field(default_factory=list)
	_proximo_heartbeat: datetime | None = None
	_proxima_telemetria: datetime | None = None
	_proximo_movimento: datetime | None = None
	_botao_em: datetime | None = None
	_saida_feita: bool = False

	def __post_init__(self) -> None:
		self._proximo_heartbeat = self.ligado_em + timedelta(seconds=self.sorteio.uniform(0, 20))
		self._proxima_telemetria = self.ligado_em + timedelta(seconds=self.sorteio.uniform(0, 5))
		self._proximo_movimento = self.ligado_em + timedelta(seconds=self.sorteio.uniform(5, 90))
		self.temp_c = round(self.sorteio.uniform(25.5, 30.5), 1)

	# Fases do cenário: antes do início, durante e depois.
	def _ativo(self, agora: datetime) -> bool:
		return agora >= self.ligado_em + self.inicio

	def _durante(self, agora: datetime) -> bool:
		comeco = self.ligado_em + self.inicio
		return comeco <= agora < comeco + self.duracao

	def _efeito(self, agora: datetime) -> Comportamento:
		if self.comportamento in (Comportamento.QUEDA_DE_ENERGIA, Comportamento.REENVIO):
			return self.comportamento if self._durante(agora) else Comportamento.NORMAL
		if self.comportamento in (Comportamento.SILENCIOSA, Comportamento.MUDA):
			return self.comportamento if self._ativo(agora) else Comportamento.NORMAL
		return self.comportamento

	def _pacote(self, tipo: str, agora: datetime) -> Pacote:
		self.seq += 1
		return Pacote(self.seq, agora, tipo, self.temp_c, self.tensao_v)

	def receber_comando(self, tipo: str, agora: datetime) -> None:
		if tipo != "PEDIR_TOQUE":
			return
		efeito = self._efeito(agora)
		if efeito in (Comportamento.NORMAL, Comportamento.CALOR, Comportamento.REENVIO, Comportamento.QUEDA_DE_ENERGIA):
			self._botao_em = agora + timedelta(seconds=self.sorteio.uniform(5, 30))
		elif efeito is Comportamento.TOQUE:
			self._botao_em = agora + timedelta(seconds=20)

	def passo(self, agora: datetime) -> list[Pacote]:
		"""Os pacotes que saem do nó até `agora`, na ordem de envio."""
		efeito = self._efeito(agora)
		if efeito is Comportamento.MUDA:
			return []
		offline = efeito is Comportamento.REENVIO
		fatos: list[str] = []

		if efeito is Comportamento.CALOR:
			self.temp_c = TEMPERATURA_DE_CALOR
		tensao = 0.0 if efeito is Comportamento.QUEDA_DE_ENERGIA else TENSAO_NORMAL
		cruzou = (tensao < TENSAO_MINIMA) != (self.tensao_v < TENSAO_MINIMA)
		self.tensao_v = tensao
		if cruzou or agora >= self._proxima_telemetria:
			fatos.append("TELEMETRIA")
			self._proxima_telemetria = agora + INTERVALO_TELEMETRIA

		if efeito is Comportamento.SAIDA_AVISADA and not self._saida_feita:
			fatos.append("BOTAO_SAIR")
			self._saida_feita = True
		if offline and self._proximo_movimento > agora + timedelta(seconds=10):
			# Com o Wi-Fi cortado, alguém passa logo, para o buffer ter o que mostrar.
			self._proximo_movimento = agora + timedelta(seconds=self.sorteio.uniform(2, 10))
		pessoa_passa = efeito in (Comportamento.NORMAL, Comportamento.CALOR, Comportamento.REENVIO, Comportamento.QUEDA_DE_ENERGIA)
		if agora >= self._proximo_movimento:
			if pessoa_passa:
				fatos.append("MOVIMENTO")
			media = 15 if offline else 240
			self._proximo_movimento = agora + timedelta(seconds=self.sorteio.expovariate(1 / media))
		if self._botao_em is not None and agora >= self._botao_em:
			fatos.append("BOTAO_BEM")
			self._botao_em = None

		saida: list[Pacote] = []
		for tipo in fatos:
			pacote = self._pacote(tipo, agora)
			if offline:
				self.buffer.append(pacote)
				self.guardados.append(pacote)
			else:
				saida.append(pacote)

		if agora >= self._proximo_heartbeat:
			self._proximo_heartbeat = agora + INTERVALO_HEARTBEAT
			if not offline:
				saida.append(self._pacote("HEARTBEAT", agora))

		if not offline and self.buffer:
			reenvio = [replace(p, bufferizado=True) for p in self.buffer]
			saida = reenvio * self.repeticoes_do_reenvio + saida
			self.buffer.clear()
		return saida
