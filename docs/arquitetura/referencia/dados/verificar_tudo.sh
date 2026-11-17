#!/usr/bin/env bash
# Roda todas as conferencias deste modelo de dados. Precisa do conteiner corredor-dados (veja LEIA-ME.md).
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p saida
./comparar_ef.sh
./conferir_migracoes.sh
./testar_sql.sh
docker exec -i corredor-dados psql -U postgres -d migrado -v ON_ERROR_STOP=1 -q -At < volume.sql > saida/volume.txt
./testar_papeis.sh > saida/papeis.txt || { cat saida/papeis.txt; exit 1; }
echo "$(grep -c '^ok' saida/papeis.txt) conferencias de papeis passaram"
URL="Host=127.0.0.1;Port=55470;Password=corredor;Database=migrado"
(cd ef && CORREDOR_PG_MOTOR="$URL;Username=corredor_motor" CORREDOR_PG_CADASTRO="$URL;Username=corredor_cadastro" \
	CORREDOR_PG_GUARDA="$URL;Username=corredor_guarda" dotnet run --no-build -- consultar > ../saida/consultas.txt) \
	|| { grep -n "FALHOU" saida/consultas.txt; exit 1; }
echo "$(grep -c '\[ok\]' saida/consultas.txt) conferencias do EF Core passaram, conectadas como corredor_motor, corredor_cadastro e corredor_guarda"
./testar_sqlite.sh > saida/sqlite.txt || { cat saida/sqlite.txt; exit 1; }
echo "$(grep -c '^ok' saida/sqlite.txt) conferencias do SQLite passaram"
python3 semente/gerar_semente.py --verificar
python3 semente/verificar_semente.py
python3 semente/verificar_dados_pessoais.py . semente/permitidos.txt
plantado=$(mktemp -d)
# O dado plantado e montado em pedacos, para a varredura nao achar este proprio arquivo.
printf 'cpf %s%s tel (%s) 98888-0000 e-mail ana@%s\n' 123456 78909 11 gmail.com > "$plantado/falso.md"
achou=$(python3 semente/verificar_dados_pessoais.py "$plantado" semente/permitidos.txt | grep -c ': ' || true)
rm -rf "$plantado"
[ "$achou" = 3 ] || { echo "FALHOU: a varredura achou $achou dos 3 dados plantados"; exit 1; }
echo "varredura: falha com CPF valido sem pontuacao, telefone com DDD 11 e e-mail real plantados"
python3 montar.py --conferir
