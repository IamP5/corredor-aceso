#!/usr/bin/env bash
# Monta o pacote de uma release: API publicada, scripts de migração, a página do
# painel (se existir), VERSION e COMMIT. O ci.yml, o release.yml e a pessoa no
# notebook usam este mesmo script, então o pacote testado à mão tem o mesmo formato
# do pacote publicado. tools/conferir-pacote.sh confere o resultado.
# Uso: tools/empacotar.sh v0.3.0 [linux-x64] [linux-arm64]
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TAG="${1:?informe a tag, por exemplo v0.3.0}"
shift
[[ $TAG =~ ^v[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?$ ]] || {
  echo "tag inválida: $TAG (use vX.Y.Z ou vX.Y.Z-rc.1)" >&2
  exit 2
}
RIDS=("$@")
if ((${#RIDS[@]} == 0)); then RIDS=(linux-x64 linux-arm64); fi
COMMIT="$(git -C "$RAIZ" rev-parse HEAD 2>/dev/null || echo desconhecido)"
DIST="$RAIZ/dist"

rm -rf "$DIST"
bash "$RAIZ/tools/migracoes.sh" "$DIST/migracoes"
for rid in "${RIDS[@]}"; do
  pasta="$DIST/$rid"
  dotnet publish "$RAIZ/api/src/Corredor.Api" --configuration Release --runtime "$rid" --self-contained true \
    -p:Version="${TAG#v}" -p:SourceRevisionId="$COMMIT" --output "$pasta/api" --nologo --verbosity quiet
  cp -R "$DIST/migracoes" "$pasta/migracoes"
  if [[ -d $RAIZ/api/src/Corredor.Guarda/painel ]]; then cp -R "$RAIZ/api/src/Corredor.Guarda/painel" "$pasta/painel"; fi
  echo "$TAG" >"$pasta/VERSION"
  echo "$COMMIT" >"$pasta/COMMIT"
  COPYFILE_DISABLE=1 tar -czf "$DIST/corredor-$rid.tar.gz" -C "$pasta" .
  echo "pacote $DIST/corredor-$rid.tar.gz"
done
cd "$DIST"
if command -v sha256sum >/dev/null 2>&1; then
  sha256sum corredor-*.tar.gz >SHA256SUMS
else
  shasum -a 256 corredor-*.tar.gz >SHA256SUMS
fi
