#!/usr/bin/env python3
"""Baixa os dados brutos públicos para ml/dados/brutos/ e atualiza manifesto.csv.

Fontes:
  INMET, estação automática A701 (São Paulo, Mirante de Santana), um ZIP por ano em
  https://portal.inmet.gov.br/uploads/dadoshistoricos/AAAA.zip
  O ZIP do ano tem todas as estações do país (80 a 100 MB). O servidor aceita
  requisição por faixa de bytes, então o script lê só o índice do ZIP e o arquivo
  da A701 (cerca de 1,5 MB por ano).

  SIH/SUS, AIH Reduzida (RD) de São Paulo, um arquivo por competência em
  ftp://ftp.datasus.gov.br/dissemin/publicos/SIHSUS/200801_/Dados/RDSPaamm.dbc

Pode rodar de novo: o arquivo que já existe e bate com o sha256 do manifesto não é
baixado outra vez. O ano corrente do INMET e as três competências mais novas do SIH
são sempre baixados de novo, porque a fonte ainda acrescenta dados a eles.

Uso:
  python baixar_dados.py                    # INMET 2014 a hoje, SIH 1501 até a última competência
  python baixar_dados.py --so inmet
  python baixar_dados.py --sih-de 2401 --sih-ate 2412
"""
import argparse
import csv
import datetime as dt
import hashlib
import io
import pathlib
import re
import ssl
import sys
import time
import urllib.request
import zipfile
from concurrent.futures import ThreadPoolExecutor, as_completed

AQUI = pathlib.Path(__file__).resolve().parent
BRUTOS = AQUI / "brutos"
MANIFESTO = AQUI / "manifesto.csv"
CAMPOS = ["arquivo", "url", "bytes", "sha256", "baixado_em"]

INMET_URL = "https://portal.inmet.gov.br/uploads/dadoshistoricos/{ano}.zip"
SIH_DIR = "ftp://ftp.datasus.gov.br/dissemin/publicos/SIHSUS/200801_/Dados/"
# O portal do INMET derruba conexão sem User-Agent de navegador.
UA = {"User-Agent": "Mozilla/5.0 (X11; Linux x86_64) corredor-aceso-ml"}

try:  # o Python do python.org no macOS vem sem certificados raiz
    import certifi

    _TLS = ssl.create_default_context(cafile=certifi.where())
except ImportError:
    _TLS = ssl.create_default_context()
urllib.request.install_opener(urllib.request.build_opener(urllib.request.HTTPSHandler(context=_TLS)))


def sha256(caminho: pathlib.Path) -> str:
    h = hashlib.sha256()
    with caminho.open("rb") as f:
        for bloco in iter(lambda: f.read(1 << 20), b""):
            h.update(bloco)
    return h.hexdigest()


def ler_manifesto() -> dict:
    if not MANIFESTO.exists():
        return {}
    with MANIFESTO.open(encoding="utf-8") as f:
        return {linha["arquivo"]: linha for linha in csv.DictReader(f)}


def gravar_manifesto(linhas: dict) -> None:
    with MANIFESTO.open("w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=CAMPOS, lineterminator="\n")
        w.writeheader()
        for chave in sorted(linhas):
            w.writerow(linhas[chave])


def ja_tem(rel: str, manifesto: dict) -> bool:
    caminho = AQUI / rel
    return caminho.exists() and rel in manifesto and manifesto[rel]["sha256"] == sha256(caminho)


def registrar(rel: str, url: str) -> dict:
    caminho = AQUI / rel
    return {
        "arquivo": rel,
        "url": url,
        "bytes": caminho.stat().st_size,
        "sha256": sha256(caminho),
        "baixado_em": dt.date.today().isoformat(),
    }


class ArquivoRemoto(io.RawIOBase):
    """Arquivo HTTP lido por faixa de bytes, para o zipfile abrir sem baixar o ZIP inteiro."""

    def __init__(self, url: str):
        self.url, self.pos = url, 0
        req = urllib.request.Request(url, method="HEAD", headers=UA)
        with urllib.request.urlopen(req, timeout=60) as r:
            if r.headers.get("Accept-Ranges") != "bytes":
                raise OSError(f"{url} não aceita faixa de bytes")
            self.tamanho = int(r.headers["Content-Length"])

    def seekable(self):
        return True

    def readable(self):
        return True

    def tell(self):
        return self.pos

    def seek(self, deslocamento, origem=io.SEEK_SET):
        base = {io.SEEK_SET: 0, io.SEEK_CUR: self.pos, io.SEEK_END: self.tamanho}[origem]
        self.pos = base + deslocamento
        return self.pos

    def read(self, n=-1):
        if n is None or n < 0:
            n = self.tamanho - self.pos
        if n == 0 or self.pos >= self.tamanho:
            return b""
        fim = min(self.pos + n, self.tamanho) - 1
        req = urllib.request.Request(self.url, headers={**UA, "Range": f"bytes={self.pos}-{fim}"})
        for tentativa in range(5):
            try:
                with urllib.request.urlopen(req, timeout=120) as r:
                    dados = r.read()
                break
            except OSError:
                if tentativa == 4:
                    raise
                time.sleep(2 ** tentativa)
        self.pos += len(dados)
        return dados

    def readinto(self, b):
        dados = self.read(len(b))
        b[: len(dados)] = dados
        return len(dados)


def baixar_inmet(ano: int, manifesto: dict, forcar: bool) -> dict | None:
    rel = f"brutos/inmet/A701_{ano}.csv"
    if not forcar and ja_tem(rel, manifesto):
        print(f"inmet {ano}: já baixado")
        return None
    url = INMET_URL.format(ano=ano)
    with zipfile.ZipFile(io.BufferedReader(ArquivoRemoto(url), buffer_size=1 << 20)) as z:
        membros = [m for m in z.namelist() if "_A701_" in m.upper()]
        if len(membros) != 1:
            raise RuntimeError(f"{url}: esperava um arquivo da A701, achei {membros}")
        destino = AQUI / rel
        destino.parent.mkdir(parents=True, exist_ok=True)
        destino.write_bytes(z.read(membros[0]))
    print(f"inmet {ano}: {membros[0]}")
    return registrar(rel, f"{url}#{membros[0]}")


def listar_sih() -> list[str]:
    with urllib.request.urlopen(SIH_DIR, timeout=120) as r:
        texto = r.read().decode("latin1")
    return sorted(set(re.findall(r"RDSP\d{4}\.dbc", texto, flags=re.I)))


def baixar_sih(nome: str, manifesto: dict, forcar: bool) -> dict | None:
    rel = f"brutos/sih/{nome}"
    if not forcar and ja_tem(rel, manifesto):
        return None
    destino = AQUI / rel
    destino.parent.mkdir(parents=True, exist_ok=True)
    temporario = destino.with_suffix(".parcial")
    for tentativa in range(5):
        try:
            with urllib.request.urlopen(SIH_DIR + nome, timeout=300) as r, temporario.open("wb") as f:
                while bloco := r.read(1 << 20):
                    f.write(bloco)
            break
        except OSError:
            if tentativa == 4:
                raise
            time.sleep(5 * (tentativa + 1))
    temporario.replace(destino)
    print(f"sih {nome}: {destino.stat().st_size / 1e6:.1f} MB")
    return registrar(rel, SIH_DIR + nome)


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--so", choices=["inmet", "sih"])
    p.add_argument("--inmet-de", type=int, default=2014)
    p.add_argument("--inmet-ate", type=int, default=dt.date.today().year)
    p.add_argument("--sih-de", default="1501", help="competência aamm")
    p.add_argument("--sih-ate", default="9999", help="competência aamm")
    p.add_argument("--paralelo", type=int, default=4)
    a = p.parse_args()

    manifesto = ler_manifesto()
    hoje = dt.date.today()

    if a.so in (None, "inmet"):
        for ano in range(a.inmet_de, a.inmet_ate + 1):
            linha = baixar_inmet(ano, manifesto, forcar=ano == hoje.year)
            if linha:
                manifesto[linha["arquivo"]] = linha
                gravar_manifesto(manifesto)

    if a.so in (None, "sih"):
        nomes = [n for n in listar_sih() if a.sih_de <= n[4:8] <= a.sih_ate]
        recentes = set(nomes[-3:])
        print(f"sih: {len(nomes)} competências, de {nomes[0]} a {nomes[-1]}")
        with ThreadPoolExecutor(a.paralelo) as ex:
            futuros = {ex.submit(baixar_sih, n, manifesto, n in recentes): n for n in nomes}
            for f in as_completed(futuros):
                linha = f.result()
                if linha:
                    manifesto[linha["arquivo"]] = linha
                    gravar_manifesto(manifesto)

    gravar_manifesto(manifesto)
    print(f"manifesto: {len(manifesto)} arquivos")
    return 0


if __name__ == "__main__":
    sys.exit(main())
