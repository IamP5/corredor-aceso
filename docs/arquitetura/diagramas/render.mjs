// Renderiza cada arquivo .mmd desta pasta em dois SVGs com o tema do Corredor Aceso, na largura natural:
//   nome.svg         tema claro (documentos, README, GitHub no modo claro)
//   nome.escuro.svg  tema escuro (pitch, GitHub no modo escuro)
// Uso: npm install && npm run render            (pasta atual)
//      node render.mjs modelos                  (outra pasta)
// Sai com código 1 se algum diagrama não renderizar.
import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { basename, dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { renderizar } from './tema/pele-tema.mjs';

const raiz = dirname(fileURLToPath(import.meta.url));
const pasta = resolve(raiz, process.argv[2] ?? '.');
const fontes = readdirSync(pasta).filter((nome) => nome.endsWith('.mmd')).sort();
let falhas = 0;

for (const nome of fontes) {
  const base = nome.replace(/\.mmd$/, '');
  const fonte = readFileSync(join(pasta, nome), 'utf8');
  try {
    const saidas = ['claro', 'escuro'].map((tema) => ({
      destino: tema === 'claro' ? `${base}.svg` : `${base}.escuro.svg`,
      ...renderizar(fonte, tema, { idPrefix: `${base}-${tema}`.replace(/[^\w-]/g, '_') }),
    }));
    for (const { destino, svg } of saidas) writeFileSync(join(pasta, destino), svg);
    const { width, height, svg } = saidas[0];
    console.log(`ok    ${base}.svg + .escuro.svg  ${Math.round(width)}x${Math.round(height)}  ${Math.round(svg.length / 1024)} KB`);
  } catch (erro) {
    falhas += 1;
    console.error(`FALHA ${basename(pasta)}/${nome}: ${erro.message}`);
  }
}

console.log(`${fontes.length - falhas} de ${fontes.length} diagramas renderizados`);
process.exit(falhas ? 1 : 0);
