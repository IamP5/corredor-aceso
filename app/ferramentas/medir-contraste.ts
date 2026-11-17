// Mede o contraste de cada par de cores do app. Rode da pasta app/: npm run contraste
import { PARES_DE_CONTRASTE, razaoDeContraste } from '../src/ui/tema';

let falhou = false;
for (const p of PARES_DE_CONTRASTE) {
  const r = razaoDeContraste(p.texto, p.fundo);
  if (r < 4.5) falhou = true;
  console.log(`${r >= 4.5 ? 'ok  ' : 'FALHA'} ${r.toFixed(2).padStart(5)}:1  ${p.texto} sobre ${p.fundo}  ${p.nome}`);
}
process.exit(falhou ? 1 : 0);
