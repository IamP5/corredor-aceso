// Gera as galerias HTML: uma página por tipo de diagrama do Pele, uma página dos rascunhos do projeto e o índice.
// As páginas guardam só o código Mermaid; o navegador desenha cada diagrama na hora com o pele de node_modules,
// então a pasta fica pequena. Abra galeria/index.html depois de npm install.
// Uso: node galeria/gerar.mjs <pasta com exemplos por tipo> [<resultados.json do render em Node>]
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const aqui = dirname(fileURLToPath(import.meta.url));
const [pastaExemplos, arquivoResultados] = process.argv.slice(2);
const resultados = arquivoResultados && existsSync(arquivoResultados) ? JSON.parse(readFileSync(arquivoResultados, 'utf8')) : [];
const medida = (tipo, arquivo) => resultados.find((r) => r.tipo === tipo && r.arquivo === arquivo);
const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const tema = (arquivo, classe) => readFileSync(join(aqui, '..', 'tema', arquivo), 'utf8').replaceAll('.pele ', `.${classe} .pele `).replaceAll('.pele{', `.${classe} .pele{`);

function pagina(titulo, raiz, itens, nota) {
  const cartoes = itens.map((it, i) => `<section class="cartao" id="d${i}">
<h2>${esc(it.nome)}${it.legenda ? ` <small>${esc(it.legenda)}</small>` : ''}</h2>
<div class="lado"><pre>${esc(it.fonte)}</pre><div class="desenho" data-i="${i}"></div></div></section>`).join('\n');
  return `<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>${esc(titulo)}</title>
<style>
@font-face{font-family:"Atkinson Hyperlegible";font-weight:400;src:url(${raiz}tema/fontes/atkinson-hyperlegible-latin-400-normal.woff2)}
@font-face{font-family:"Atkinson Hyperlegible";font-weight:700;src:url(${raiz}tema/fontes/atkinson-hyperlegible-latin-700-normal.woff2)}
@font-face{font-family:"Atkinson Hyperlegible Mono";src:url(${raiz}tema/fontes/atkinson-hyperlegible-mono-latin-400-normal.woff2)}
@font-face{font-family:Fraunces;font-weight:600;src:url(${raiz}tema/fontes/fraunces-latin-600-normal.woff2)}
body{margin:0;font:15px "Atkinson Hyperlegible",sans-serif;background:#efe9de;color:#0b1220}
header{position:sticky;top:0;z-index:2;background:#0b1220;color:#f3ecdf;padding:10px 20px;display:flex;gap:16px;align-items:center;flex-wrap:wrap}
header h1{font:600 20px Fraunces,serif;margin:0}header a{color:#ffb13b}
main{padding:16px 20px;display:grid;gap:16px}
.cartao{background:#fff;border:1px solid #d8cfbf;border-radius:8px;padding:10px 14px}
.cartao h2{font-size:15px;margin:0 0 8px}.cartao small{font-weight:400;color:#545d6e}
.lado{display:grid;grid-template-columns:minmax(260px,1fr) minmax(0,2.2fr);gap:14px;align-items:start}
pre{margin:0;font:12px/1.45 "Atkinson Hyperlegible Mono",monospace;background:#f6f2ea;padding:8px;border-radius:6px;overflow:auto;max-height:520px}
.desenho{overflow:auto;padding:12px;border-radius:6px}
.tema-claro .desenho{background:#fbf7ef}.tema-escuro .desenho{background:#0b1220}
.desenho .erro{color:#b3262d;white-space:pre-wrap;font:12px monospace}.medida{font-size:12px;color:#545d6e}
${tema('claro.css', 'tema-claro')}${tema('escuro.css', 'tema-escuro')}${tema('classes.css', 'tema-claro')}${tema('classes.css', 'tema-escuro')}
</style>
<body class="tema-claro">
<header><h1>${esc(titulo)}</h1><a href="${raiz}galeria/index.html">índice</a>
<label>tema <select id="tema"><option value="tema-claro">claro do projeto</option><option value="tema-escuro">escuro do projeto</option><option value="padrao">padrão do Pele</option></select></label>
<span>${esc(nota)}</span></header>
<main>${cartoes}</main>
<script src="${raiz}node_modules/pele/dist/pele.global.js"></script>
<script>
const fontes = ${JSON.stringify(itens.map((it) => it.fonte))};
const medidas = ${JSON.stringify(itens.map((it) => it.medida ?? ''))};
function desenhar() {
  const t = document.getElementById('tema').value;
  document.body.className = t === 'padrao' ? '' : t;
  document.querySelectorAll('.desenho').forEach((el) => {
    const i = +el.dataset.i;
    const op = { idPrefix: 'g' + i, responsive: false };
    if (t !== 'padrao') Object.assign(op, { fontFamily: '"Atkinson Hyperlegible", sans-serif', fontFamilyMono: '"Atkinson Hyperlegible Mono", monospace' });
    try {
      const r = pele.render(fontes[i], op);
      el.innerHTML = r.svg + '<div class="medida">navegador: ' + Math.round(r.width) + 'x' + Math.round(r.height) + (medidas[i] ? ' · ' + medidas[i] : '') + '</div>';
    } catch (e) { el.innerHTML = '<div class="erro"></div>'; el.firstChild.textContent = e.message; }
  });
}
document.getElementById('tema').onchange = desenhar;
Promise.all(['400 16px "Atkinson Hyperlegible"', '700 16px "Atkinson Hyperlegible"', '16px "Atkinson Hyperlegible Mono"', '600 16px Fraunces'].map((f) => document.fonts.load(f))).then(desenhar, desenhar);
</script></html>`;
}

const ligacoes = [];
if (pastaExemplos) {
  mkdirSync(join(aqui, 'exemplos'), { recursive: true });
  const indice = existsSync(join(pastaExemplos, 'INDICE.md')) ? readFileSync(join(pastaExemplos, 'INDICE.md'), 'utf8') : '';
  for (const tipo of readdirSync(pastaExemplos).filter((d) => !d.includes('.')).sort()) {
    const itens = readdirSync(join(pastaExemplos, tipo)).filter((f) => f.endsWith('.mmd')).sort().map((f) => {
      const m = medida(tipo, f);
      const legenda = indice.match(new RegExp(`\\| ${tipo}/${f.replace(/[.]/g, '\\.')} \\| ([^|]*) \\|`))?.[1]?.trim();
      return { nome: f, legenda: legenda && !legenda.startsWith('(sem') ? legenda : '', fonte: readFileSync(join(pastaExemplos, tipo, f), 'utf8'), medida: m ? (m.ok ? `Node, tema padrão: ${m.width}x${m.height}` : `Node: ERRO ${m.erro}`) : '' };
    });
    writeFileSync(join(aqui, 'exemplos', `${tipo}.html`), pagina(`Pele: ${tipo} (${itens.length})`, '../../', itens, 'exemplos de pele.run/examples'));
    ligacoes.push([`exemplos/${tipo}.html`, tipo, itens.length]);
  }
}

const pastaRascunhos = join(aqui, 'rascunhos');
const rascunhos = readdirSync(pastaRascunhos).filter((f) => f.endsWith('.mmd')).sort();
const porVista = rascunhos.map((f) => ({ nome: f.replace(/\.mmd$/, '').replace('--', ' · '), fonte: readFileSync(join(pastaRascunhos, f), 'utf8') }));
writeFileSync(join(aqui, 'rascunhos.html'), pagina(`Rascunhos do Corredor Aceso (${porVista.length})`, '../', porVista, 'cada vista em 2 a 4 tipos, na largura natural; a escolha está no GUIA.md'));

const existentes = readdirSync(join(aqui, 'exemplos')).filter((f) => f.endsWith('.html'));
const lista = (ligacoes.length ? ligacoes : existentes.map((f) => [`exemplos/${f}`, f.replace('.html', ''), ''])).map(([h, t, n]) => `<li><a href="${h}">${t}</a> ${n}</li>`).join('');
writeFileSync(join(aqui, 'index.html'), `<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>Galeria de diagramas</title>
<style>body{font:16px sans-serif;max-width:760px;margin:30px auto;padding:0 16px;color:#0b1220;background:#fbf7ef}ul{columns:3}a{color:#9a5800}</style>
<h1>Galeria de diagramas do Corredor Aceso</h1>
<p>Precisa de <code>npm install</code> na pasta <code>docs/arquitetura/diagramas</code>: as páginas desenham com <code>node_modules/pele</code>.</p>
<p><a href="rascunhos.html"><b>Rascunhos do projeto</b></a>: cada vista em 2 a 4 tipos, na largura natural, com o tema claro e o escuro.</p>
<h2>Exemplos do Pele por tipo</h2><ul>${lista}</ul></html>`);
console.log(`${ligacoes.length} páginas de tipo, ${porVista.length} rascunhos`);
