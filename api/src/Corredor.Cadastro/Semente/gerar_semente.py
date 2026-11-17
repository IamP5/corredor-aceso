#!/usr/bin/env python3
"""Gera as 40 casas simuladas da demo. Mesma semente, mesmo arquivo, byte a byte.

O arquivo nao tem token. O token nasce em RegistrarConsentimento quando `Corredor.Api --semear` carrega
a semente, entao cada banco tem os seus e nenhum token entra no Git. O `id` de cada idoso e fixo para a
carga ser repetivel: `--semear` pula o idoso que ja existe.

uso: gerar_semente.py            escreve casas.json ao lado deste arquivo
     gerar_semente.py --verificar  gera de novo e compara com o casas.json gravado
"""
import json
import random
import sys
import uuid
from pathlib import Path

SEMENTE = 20261119
ARQUIVO = Path(__file__).with_name("casas.json")
BAIRRO = "Vila Exemplo"

# Prenomes comuns entre pessoas de 60+ no Brasil. Um prenome sozinho nao identifica ninguem.
PRENOMES = [
	"Aparecida", "Benedita", "Conceição", "Dirce", "Efigênia", "Francisca", "Geralda", "Helena",
	"Iracema", "Josefa", "Lourdes", "Madalena", "Neusa", "Odete", "Palmira", "Quitéria",
	"Raimunda", "Severina", "Terezinha", "Valdete", "Zulmira", "Antônio", "Benedito", "Cícero",
	"Djalma", "Expedito", "Francisco", "Geraldo", "Hélio", "Ivo", "João", "Lázaro",
	"Manoel", "Nelson", "Orlando", "Pedro", "Raimundo", "Sebastião", "Valdemar", "Zacarias",
]
# Sobrenome de casa simulada e sempre nome de arvore que nao e sobrenome comum.
ARVORES = [
	"Jatobá", "Ipê", "Aroeira", "Jequitibá", "Paineira", "Sibipiruna", "Quaresmeira", "Manacá",
	"Embaúba", "Araucária", "Cambuci", "Guapuruvu", "Pitangueira", "Jabuticabeira", "Sapucaia",
	"Copaíba", "Jacarandá", "Tipuana", "Mulungu", "Grumixama",
]
RUAS = [
	"Rua do Sabiá", "Rua do Bem-te-vi", "Rua do Tico-tico", "Rua da Saíra",
	"Travessa do João-de-barro", "Travessa da Cambacica", "Viela do Sanhaço", "Viela da Corruíra",
]
CONDICOES = ["hipertensão", "diabetes", "insuficiência cardíaca", "sequela de AVC", "artrose", None, None]
MEDICAMENTOS = ["diurético", "anti-hipertensivo", "insulina", "anticoagulante", None, None]
# Papel de cada casa nos fluxos de 02-event-storming.md. O simulador le este campo. A API nao recebe.
PAPEIS = {
	1: "A: nó do Wokwi", 4: "B: saída avisada às 07h30", 5: "C: silêncio às 10h, vizinho responde",
	6: "D: silêncio às 10h, visita EstavaBem", 7: "E: silêncio às 10h, visita NaoAbriu",
	8: "F: calor de 34,5 °C desde as 11h", 9: "G: botão às 14h40", 10: "aparelho mudo desde as 09h10",
}


def perfil(n: int) -> dict:
	if n in (2, 11, 21, 31):
		contato = "Acamado"
	elif n in (3, 12, 22, 32, 36, 39):
		contato = "Coabitado"
	else:
		contato = "Sozinho"
	return {"contato": contato, "eletrodependente": n in (1, 2, 3)}


def gerar() -> list[dict]:
	sorteio = random.Random(SEMENTE)
	prenomes = PRENOMES[:]
	sorteio.shuffle(prenomes)
	casas = []
	for n in range(1, 41):
		p = perfil(n)
		avisar_vizinho = n % 4 != 0
		tem_vizinho = avisar_vizinho and n % 10 != 9
		casas.append({
			"n": n,
			"id": str(uuid.uuid5(uuid.NAMESPACE_URL, f"corredor-aceso/semente/casa-{n}")),
			"papelNaDemo": PAPEIS.get(n, "movimento normal"),
			"nome": f"{prenomes[n - 1]} {ARVORES[(n - 1) % len(ARVORES)]}",
			"endereco": f"{RUAS[(n - 1) % len(RUAS)]}, {10 + 2 * n} - {BAIRRO}",
			"perfil": p,
			"condicoes": "usa concentrador de oxigênio" if p["eletrodependente"] else sorteio.choice(CONDICOES),
			"medicamentos": sorteio.choice(MEDICAMENTOS),
			"autorizacoes": {"avisarVizinho": avisar_vizinho, "avisarFamilia": n % 5 == 0},
			"vizinho": {
				"nome": f"{sorteio.choice(PRENOMES)} {ARVORES[(n + 7) % len(ARVORES)]}",
				"telefone": f"(00) 90000-{n:04d}",
			} if tem_vizinho else None,
		})
	return casas


def texto() -> str:
	return json.dumps({"microareaId": 3, "versaoDoTermo": "2026-11", "casas": gerar()}, ensure_ascii=False, indent="\t") + "\n"


if __name__ == "__main__":
	if "--verificar" in sys.argv:
		if ARQUIVO.read_text(encoding="utf-8") != texto():
			sys.exit("casas.json não é o que o gerador produz. Alguém editou o arquivo à mão.")
		print("casas.json confere com o gerador")
	else:
		ARQUIVO.write_text(texto(), encoding="utf-8")
		print(f"{ARQUIVO.name}: {len(gerar())} casas")
