// Mede o contraste WCAG 2.x dos pares de cor que o Pele desenha, nos dois temas.
// Texto precisa de 4,5:1; linhas e bordas, de 3:1. Sai com código 1 se algum par falhar.
// Uso: node tema/contraste.mjs
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const pasta = dirname(fileURLToPath(import.meta.url));

function variaveis(arquivo) {
  const css = readFileSync(join(pasta, arquivo), 'utf8');
  return Object.fromEntries([...css.matchAll(/--([\w-]+):\s*([^;]+);/g)].map(([, k, v]) => [k, v.trim()]));
}
const rgb = (hex) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16));
const hexDe = (c) => '#' + c.map((x) => Math.round(x).toString(16).padStart(2, '0')).join('');
const misturar = (cor, fundo, opacidade) => hexDe(rgb(cor).map((x, i) => x * opacidade + rgb(fundo)[i] * (1 - opacidade)));
function luminancia(hex) {
  const [r, g, b] = rgb(hex).map((x) => {
    const c = x / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}
const razao = (a, b) => {
  const [l1, l2] = [luminancia(a), luminancia(b)].sort((x, y) => y - x);
  return (l1 + 0.05) / (l2 + 0.05);
};

let falhas = 0;
const linhas = [];
for (const [nome, arquivo] of [['claro', 'claro.css'], ['escuro', 'escuro.css']]) {
  const v = variaveis(arquivo);
  const p = (k) => v[`pele-${k}`];
  const pares = [
    ['texto', 'fg sobre bg', p('fg'), p('bg')],
    ['texto', 'fg sobre surface (nó)', p('fg'), p('surface')],
    ['texto', 'fg sobre surface-alt (grupo)', p('fg'), p('surface-alt')],
    ['texto', 'muted sobre bg', p('muted'), p('bg')],
    ['texto', 'muted sobre surface', p('muted'), p('surface')],
    ['texto', 'muted sobre surface-alt', p('muted'), p('surface-alt')],
    ['texto', 'accent sobre bg', p('accent'), p('bg')],
    ['texto', 'critical sobre bg', p('critical'), p('bg')],
    ['texto', 'luz-texto sobre luz', v['ca-luz-texto'], v['ca-luz']],
    ['texto', 'fg sobre alerta', p('fg'), v['ca-alerta']],
    ['texto', 'fg sobre verde', p('fg'), v['ca-verde']],
    ['texto', 'cinza-texto sobre cinza', v['ca-cinza-texto'], v['ca-cinza']],
    ['linha', 'line sobre bg', p('line'), p('bg')],
    ['linha', 'line sobre surface-alt', p('line'), p('surface-alt')],
    ['linha', 'border sobre bg', p('border'), p('bg')],
  ];
  for (let i = 1; i <= 8; i++) {
    const s = p(`series-${i}`);
    pares.push(['texto', `bg sobre series-${i} (barra sólida)`, p('bg'), s]);
    pares.push(['texto', `fg sobre series-${i} a 22% (nó tingido)`, p('fg'), misturar(s, p('bg'), 0.22)]);
    pares.push(['linha', `series-${i} sobre bg (borda)`, s, p('bg')]);
  }
  if (v['ca-brilho']) {
    const pico = misturar(v['ca-brilho'], p('bg'), Number(v['ca-brilho-opacidade']));
    pares.push(['texto', `fg sobre o pico do brilho ${pico}`, p('fg'), pico]);
    pares.push(['texto', `muted sobre o pico do brilho ${pico}`, p('muted'), pico]);
  }
  for (const [tipo, par, a, b] of pares) {
    const r = razao(a, b);
    const minimo = tipo === 'texto' ? 4.5 : 3;
    const ok = r >= minimo;
    if (!ok) falhas += 1;
    linhas.push(`| ${nome} | ${par} | ${a} / ${b} | ${r.toFixed(2)}:1 | ${minimo}:1 | ${ok ? 'ok' : 'FALHA'} |`);
  }
}
console.log('| tema | par | cores | contraste | mínimo | resultado |\n|---|---|---|---|---|---|');
console.log(linhas.join('\n'));
console.log(falhas ? `\n${falhas} par(es) abaixo do mínimo` : '\nTodos os pares passam');
process.exit(falhas ? 1 : 0);
