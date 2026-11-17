// Renderiza Mermaid com o Pele e aplica o tema do Corredor Aceso.
// renderizar(): SVG autossuficiente, com fontes em base64, variáveis do tema e fundo pintado.
//   Serve para o README, porque o GitHub mostra o SVG como <img>, sem CSS nem fontes de fora.
// renderizarSemTema(): SVG puro, sem <style>. Serve para o site, que põe o tema e as fontes na página
//   e troca claro por escuro só com CSS.
// Os dois saem na largura natural do desenho. Nenhum dos dois passa maxWidth ao Pele.
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { render } from 'pele';

const pasta = dirname(fileURLToPath(import.meta.url));
const ler = (arquivo) => readFileSync(join(pasta, arquivo), 'utf8');

export const TEMAS = {
  claro: ler('claro.css'),
  escuro: ler('escuro.css'),
};
export const CLASSES = ler('classes.css');

const metricas = JSON.parse(ler('metricas.json'));
// Mede o texto com as larguras reais da Atkinson Hyperlegible. estilo: 1 negrito, 2 itálico, 4 mono.
const medidor = {
  width(texto, tamanho, estilo) {
    const tabela = estilo & 4 ? metricas.mono : estilo & 1 ? metricas.negrito : metricas.regular;
    let soma = 0;
    for (const c of texto) soma += tabela.larguras[c] ?? tabela.media;
    return soma * tamanho;
  },
};

const base64 = (arquivo) => readFileSync(join(pasta, 'fontes', arquivo)).toString('base64');
const FONTES = {
  regular: `@font-face{font-family:"Atkinson Hyperlegible";font-weight:400;src:url(data:font/woff2;base64,${base64('atkinson-hyperlegible-latin-400-normal.woff2')}) format("woff2")}`,
  negrito: `@font-face{font-family:"Atkinson Hyperlegible";font-weight:700;src:url(data:font/woff2;base64,${base64('atkinson-hyperlegible-latin-700-normal.woff2')}) format("woff2")}`,
  mono: `@font-face{font-family:"Atkinson Hyperlegible Mono";font-weight:400;src:url(data:font/woff2;base64,${base64('atkinson-hyperlegible-mono-latin-400-normal.woff2')}) format("woff2")}`,
  titulo: `@font-face{font-family:Fraunces;font-weight:600;src:url(data:font/woff2;base64,${base64('fraunces-latin-600-normal.woff2')}) format("woff2")}`,
};

export const OPCOES = {
  fontFamily: '"Atkinson Hyperlegible", sans-serif',
  fontFamilyMono: '"Atkinson Hyperlegible Mono", monospace',
  measurer: medidor,
};

// O Pele escreve em inglês os nomes das raias do eventmodeling. A troca mantém a borda esquerda
// do texto onde o Pele a pôs (o texto é centrado no x), medindo o nome novo com a mesma tabela.
const RAIAS = [
  ['UI/Automation', 'Tela e automação'],
  ['Command/Read Model', 'Comando e leitura'],
  ['Events', 'Eventos'],
  ['UI/A: ', 'Tela: '],
  ['C/RM: ', 'Leitura: '],
  ['Stream: ', 'Eventos: '],
];
const traduzirRaia = (s) => {
  for (const [en, pt] of RAIAS) if (s === en || (en.endsWith(' ') && s.startsWith(en))) return pt + s.slice(en.length);
  return s;
};
function traduzirRaias(svg) {
  if (!svg.includes('pele-eventmodeling')) return svg;
  return svg.replace(
    /(<g class="pele-cluster pele-em-lane" data-id=")([^"]*)("[^>]*>[\s\S]*?font-size="([\d.]+)"[\s\S]*?<tspan x=")([\d.]+)("[^>]*>)([^<]*)(<\/tspan>)/g,
    (_, a, id, b, tamanho, x, c, texto, d) => {
      const novo = traduzirRaia(texto);
      const deslocamento = (medidor.width(novo, +tamanho, 1) - medidor.width(texto, +tamanho, 1)) / 2;
      return `${a}${traduzirRaia(id)}${b}${(+x + deslocamento).toFixed(2)}${c}${novo}${d}`;
    });
}

// Só embute as fontes que o SVG usa, para não pesar.
function fontesUsadas(svg) {
  const usadas = [FONTES.regular];
  if (/bold|weight/.test(svg.replace(/--pele-[\w-]*weight/g, ''))) usadas.push(FONTES.negrito);
  if (/(font-family[:=]"?var\(--_fm\))|pele-member|pele-er-|pele-tree-/.test(svg)) usadas.push(FONTES.mono);
  if (svg.includes('pele-title')) usadas.push(FONTES.titulo);
  return usadas.join('');
}

export function renderizarSemTema(fonte, opcoes = {}) {
  const resultado = render(fonte, { ...OPCOES, ...opcoes });
  return { ...resultado, svg: traduzirRaias(resultado.svg) };
}

export function renderizar(fonte, tema, opcoes = {}) {
  const resultado = renderizarSemTema(fonte, opcoes);
  const [x, y, w, h] = resultado.svg.match(/viewBox="([^"]+)"/)[1].split(/\s+/).map(Number);
  const id = opcoes.idPrefix ?? 'ca';
  const brilho = tema === 'escuro'
    ? `<defs><radialGradient id="${id}-brilho" cx="50%" cy="0%" r="75%"><stop offset="0" stop-color="var(--ca-brilho)" stop-opacity="var(--ca-brilho-opacidade)"/><stop offset="1" stop-color="var(--ca-brilho)" stop-opacity="0"/></radialGradient></defs>`
      + `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="var(--pele-bg)"/><rect x="${x}" y="${y}" width="${w}" height="${h}" fill="url(#${id}-brilho)"/>`
    : `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="var(--pele-bg)"/>`;
  const estilo = `<style>${fontesUsadas(resultado.svg)}${TEMAS[tema]}${CLASSES}</style>`;
  const svg = resultado.svg.replace(/^(<svg[^>]*>)/, `$1${estilo}${brilho}`);
  return { ...resultado, svg };
}
