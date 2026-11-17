// Gera o site estático da arquitetura a partir dos Markdown, com os diagramas Pele desenhados na página.
// O Markdown continua sendo a fonte que a equipe edita. O site é só saída: apague e gere de novo.
//
// Uso: cd docs/arquitetura/diagramas && npm install && npm run site
//      Abra docs/arquitetura/site/index.html com dois cliques. Não precisa de servidor nem de rede.
//
// Fontes, nesta ordem: cada docs/arquitetura/*.md (01-visao-geral.md é a visão geral) e diagramas/GUIA.md.
// Para testar com outros arquivos:
//   SITE_FONTES=a.md:b.md SITE_SAIDA=/tmp/site node site.mjs
// SITE_FONTES lista os .md separados por ":" e SITE_SAIDA troca a pasta de saída.
//
// Diagramas no Markdown, duas formas:
//   1. Bloco cercado:
//        ```mermaid
//        flowchart LR
//          a --> b
//        ```
//   2. Referência a um arquivo .mmd, como imagem ou como link, sozinha no parágrafo:
//        ![Visão geral do Corredor Aceso](diagramas/01-visao-geral.mmd)
//        [Visão geral do Corredor Aceso](diagramas/01-visao-geral.mmd)
//      O caminho é relativo ao .md. O texto entre colchetes vira a legenda do diagrama.
// Os dois viram SVG dentro da página, na largura natural. Diagrama mais largo que a coluna de texto sai
// dela até a largura da janela. Mais largo que a janela, encolhe para caber e ganha zoom (pinça ou
// Ctrl/Cmd + roda, arrastar para mover, duplo clique, teclas + - 0, botões). Cada diagrama ganha também
// um SVG avulso em svg/, claro e escuro, com fontes e tema embutidos.
//
// Links relativos entre os .md viram links entre as páginas, com a âncora. Links para outros arquivos
// continuam apontando para o mesmo arquivo, com o caminho refeito a partir da pasta do site.
// Diagrama que não renderiza aparece na página com o erro e o código, e o build sai com código 1.
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { basename, delimiter, dirname, extname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Marked, Renderer } from 'marked';
import { CLASSES, TEMAS, renderizar, renderizarSemTema } from './tema/pele-tema.mjs';

const pasta = dirname(fileURLToPath(import.meta.url));
const arquitetura = resolve(pasta, '..');
const saida = resolve(process.env.SITE_SAIDA ?? join(arquitetura, 'site'));
const fontes = process.env.SITE_FONTES
  ? process.env.SITE_FONTES.split(delimiter).filter(Boolean).map((f) => resolve(f))
  : [
      ...readdirSync(arquitetura).filter((f) => f.endsWith('.md')).sort().map((f) => join(arquitetura, f)),
      join(pasta, 'GUIA.md'),
    ];

// Uma página por .md: o caminho de origem decide o nome do .html, e os links entre páginas usam esse mapa.
const paginas = fontes.map((arquivo) => ({ arquivo, nome: `${basename(arquivo, '.md').toLowerCase()}.html` }));
const paginaDe = new Map(paginas.map((p) => [p.arquivo, p]));
if (new Set(paginas.map((p) => p.nome)).size !== paginas.length) throw new Error('Dois .md geram a mesma página');
for (const p of paginas) {
  if (!existsSync(p.arquivo)) throw new Error(`Fonte não encontrada: ${p.arquivo}`);
  if (!relative(saida, p.arquivo).startsWith('..')) throw new Error(`A saída ${saida} contém a fonte ${p.arquivo}`);
}

const escapar = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const textoPuro = (html) => html.replace(/<[^>]*>/g, '').replace(/&quot;/g, '"').replace(/&#39;/g, "'")
  .replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');
// Mesmo slug do GitHub, para as âncoras dos .md continuarem valendo no site.
const slug = (texto) => texto.toLowerCase().trim().replace(/[^\p{L}\p{M}\p{N}\s_-]/gu, '').replace(/\s/g, '-');
const ehRelativo = (href) => href && !/^([a-z][a-z\d+.-]*:|#|\/)/i.test(href);
const ehMmd = (href) => ehRelativo(href) && /\.mmd$/i.test(href.split(/[?#]/)[0]);

const falhas = [];
// Caminho curto no log quando a fonte está perto; absoluto quando está longe (testes fora do projeto).
const mostrar = (p) => (relative(arquitetura, p).startsWith('..' + sep + '..' + sep) ? p : relative(arquitetura, p));

function gerarPagina(pagina) {
  const dirMd = dirname(pagina.arquivo);
  const base = basename(pagina.nome, '.html').replace(/[^\w-]/g, '_');
  const usados = new Map();
  const sumario = [];
  let titulo = '';
  let diagramas = 0;

  const ancora = (texto) => {
    let id = slug(texto) || 'secao';
    const n = usados.get(id) ?? 0;
    usados.set(id, n + 1);
    return n ? `${id}-${n}` : id;
  };

  const reescrever = (href) => {
    if (!ehRelativo(href)) return href;
    const [caminho, hash] = href.split('#');
    const alvo = resolve(dirMd, decodeURI(caminho));
    const destino = paginaDe.get(alvo);
    if (destino) return destino.nome + (hash ? `#${hash}` : '');
    return encodeURI(relative(saida, alvo).split(sep).join('/')) + (hash ? `#${hash}` : '');
  };

  const diagrama = (fonte, legenda, origem) => {
    diagramas += 1;
    const id = `${base}-${diagramas}`;
    try {
      const { svg, width, height } = renderizarSemTema(fonte, { idPrefix: id });
      for (const tema of ['claro', 'escuro']) {
        writeFileSync(join(saida, 'svg', `${id}${tema === 'escuro' ? '.escuro' : ''}.svg`),
          renderizar(fonte, tema, { idPrefix: `${id}-${tema}` }).svg);
      }
      const w = Math.ceil(width);
      const h = Math.ceil(height);
      return `<figure class="diagrama" id="diagrama-${diagramas}" style="--w:${w}px;--h:${h}px;--r:${(h / w).toFixed(4)}">`
        + `<div class="diagrama-quadro">${svg}</div>`
        + `<figcaption>${legenda ? `<span>${escapar(legenda)}</span>` : ''}`
        + `<span class="diagrama-links">${w}×${h} px · <a href="svg/${id}.svg">SVG claro</a> · <a href="svg/${id}.escuro.svg">SVG escuro</a></span>`
        + `<span class="diagrama-dica">Ctrl ou ⌘ + roda, pinça, duplo clique ou + e − para ampliar; arraste para mover.</span>`
        + `</figcaption></figure>\n`;
    } catch (erro) {
      const onde = erro.line ? ` (linha ${erro.line})` : '';
      falhas.push(`${mostrar(pagina.arquivo)}, diagrama ${diagramas}${origem ? ` (${origem})` : ''}${onde}: ${erro.message}`);
      return `<figure class="diagrama-erro" role="alert"><figcaption>Erro ao renderizar o diagrama ${diagramas}`
        + `${origem ? ` de <code>${escapar(origem)}</code>` : ''}${onde}: ${escapar(erro.message)}</figcaption>`
        + `<pre><code>${escapar(fonte)}</code></pre></figure>\n`;
    }
  };

  const diagramaMmd = (href, legenda) => {
    const arquivo = resolve(dirMd, decodeURI(href.split(/[?#]/)[0]));
    let fonte;
    try {
      fonte = readFileSync(arquivo, 'utf8');
    } catch {
      fonte = '';
    }
    const origem = relative(dirMd, arquivo);
    if (!fonte) {
      diagramas += 1;
      falhas.push(`${mostrar(pagina.arquivo)}: arquivo ${origem} não encontrado ou vazio`);
      return `<figure class="diagrama-erro" role="alert"><figcaption>Arquivo <code>${escapar(origem)}</code> não encontrado ou vazio.</figcaption></figure>\n`;
    }
    return diagrama(fonte, legenda, origem);
  };

  const mmdSozinho = (tokens) => {
    const uteis = tokens.filter((t) => !(t.type === 'text' && !t.text.trim()) && t.type !== 'br');
    return uteis.length === 1 && (uteis[0].type === 'image' || uteis[0].type === 'link') && ehMmd(uteis[0].href) ? uteis[0] : null;
  };

  const marked = new Marked({ gfm: true });
  marked.use({
    renderer: {
      code(token) {
        if ((token.lang ?? '').split(/\s/)[0] === 'mermaid') return diagrama(token.text, '', '');
        return false;
      },
      paragraph(token) {
        const ref = mmdSozinho(token.tokens);
        if (ref) return diagramaMmd(ref.href, ref.type === 'image' ? ref.text : textoPuro(this.parser.parseInline(ref.tokens)));
        return false;
      },
      image(token) {
        if (ehMmd(token.href)) return diagramaMmd(token.href, token.text);
        return Renderer.prototype.image.call(this, { ...token, href: reescrever(token.href) });
      },
      link(token) {
        if (ehMmd(token.href)) return diagramaMmd(token.href, textoPuro(this.parser.parseInline(token.tokens)));
        return Renderer.prototype.link.call(this, { ...token, href: reescrever(token.href) });
      },
      heading(token) {
        const html = this.parser.parseInline(token.tokens);
        const texto = textoPuro(html);
        if (token.depth === 1) {
          if (!titulo) titulo = texto;
          return `<h1>${html}</h1>\n`;
        }
        const id = ancora(texto);
        if (token.depth <= 3) sumario.push({ nivel: token.depth, id, html: html.replace(/<\/?a\b[^>]*>/g, '') });
        return `<h${token.depth} id="${id}">${html} <a class="ancora" href="#${id}" aria-label="Link para esta seção">#</a></h${token.depth}>\n`;
      },
      table(token) {
        return `<div class="tabela" role="region" aria-label="Tabela" tabindex="0">${Renderer.prototype.table.call(this, token)}</div>\n`;
      },
    },
  });

  const md = readFileSync(pagina.arquivo, 'utf8');
  const tokens = marked.lexer(md);
  const corpo = marked.parser(tokens);
  const primeiro = tokens.find((t) => t.type === 'paragraph');
  pagina.titulo = titulo || basename(pagina.arquivo, '.md');
  pagina.resumo = primeiro ? textoPuro(marked.parseInline(primeiro.text)) : '';

  // Sumário de h2 com h3 dentro.
  let toc = '';
  if (sumario.length) {
    let aberto = false;
    toc = '<nav class="sumario" aria-label="Sumário"><details open><summary>Sumário</summary><ol>';
    for (const item of sumario) {
      const li = `<li><a href="#${item.id}">${item.html}</a>`;
      if (item.nivel === 2) {
        toc += `${aberto ? '</ol></li>' : ''}${li}<ol>`;
        aberto = true;
      } else {
        toc += `${li}</li>`;
      }
    }
    toc += `${aberto ? '</ol></li>' : ''}</ol></details></nav>`;
    toc = toc.replace(/<ol><\/ol>/g, '');
  }
  const comSumario = corpo.replace(/(<h1>[\s\S]*?<\/h1>\n)/, `$1${toc}`);
  const html = comSumario === corpo ? toc + corpo : comSumario;
  pagina.html = html;
  pagina.comDiagramas = diagramas > 0;
}

function documento(titulo, corpo, comDiagramas, atual) {
  const nav = paginas.map((p) => `<a href="${p.nome}"${p.nome === atual ? ' aria-current="page"' : ''}>${escapar(p.titulo.replace(/ do Corredor Aceso$/, ''))}</a>`).join('');
  return `<!doctype html>
<html lang="pt-BR" data-tema="claro">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${escapar(titulo)} · Corredor Aceso</title>
<script>(function(){var t;try{t=localStorage.getItem('tema')}catch(e){}if(t!=='claro'&&t!=='escuro')t=matchMedia('(prefers-color-scheme: dark)').matches?'escuro':'claro';document.documentElement.setAttribute('data-tema',t)})()</script>
<link rel="stylesheet" href="site.css">
</head>
<body>
<header class="topo">
<a class="marca" href="index.html">Corredor Aceso · Arquitetura</a>
<nav class="paginas" aria-label="Páginas">${nav}</nav>
<button type="button" class="tema-botao">Tema</button>
</header>
<main>
${corpo}
</main>
${comDiagramas ? '<script src="js/pele.global.js"></script>\n' : ''}<script src="js/site.js"></script>
</body>
</html>
`;
}

const tema = (nome, seletor) => TEMAS[nome].replace(/\.pele\s*\{/, `${seletor} {`);

const CSS = `@font-face{font-family:"Atkinson Hyperlegible";font-weight:400;font-display:swap;src:url(fontes/atkinson-hyperlegible-latin-400-normal.woff2) format("woff2")}
@font-face{font-family:"Atkinson Hyperlegible";font-weight:700;font-display:swap;src:url(fontes/atkinson-hyperlegible-latin-700-normal.woff2) format("woff2")}
@font-face{font-family:"Atkinson Hyperlegible Mono";font-weight:400;font-display:swap;src:url(fontes/atkinson-hyperlegible-mono-latin-400-normal.woff2) format("woff2")}
@font-face{font-family:Fraunces;font-weight:600;font-display:swap;src:url(fontes/fraunces-latin-600-normal.woff2) format("woff2")}

/* Página: papel quente e tinta da noite no claro; noite e papel no escuro, com o âmbar como única luz. */
[data-tema=claro]{--fundo:#fbf7ef;--texto:#0b1220;--suave:#545d6e;--borda:#ddd2bd;--superficie:#f3ecdf;--zebra:#f6f0e4;--link:#9a5800;--marca:#9a5800;color-scheme:light}
[data-tema=escuro]{--fundo:#0b1220;--texto:#f3ecdf;--suave:#a3acbd;--borda:#2a3652;--superficie:#131c30;--zebra:#0f182a;--link:#ffb13b;--marca:#ffb13b;color-scheme:dark}

/* Diagramas: o mesmo SVG segue o tema da página, sem desenhar de novo. */
${tema('claro', '[data-tema=claro] .pele')}
${tema('escuro', '[data-tema=escuro] .pele')}
${CLASSES}

*,*::before,*::after{box-sizing:border-box}
html{--vw:100vw;--margem:1.25rem;background:var(--fundo);color:var(--texto);font:400 1.0625rem/1.65 "Atkinson Hyperlegible",system-ui,sans-serif;-webkit-text-size-adjust:100%}
body{margin:0;overflow-x:clip}
main{max-width:48rem;margin:0 auto;padding:1.5rem var(--margem) 5rem}
h1,h2,h3,h4{font-family:Fraunces,Georgia,serif;font-weight:600;line-height:1.2;scroll-margin-top:4.5rem}
h1{font-size:2.3rem;margin:1rem 0 1.25rem}
h2{font-size:1.65rem;margin:3rem 0 1rem;padding-top:.5rem;border-top:1px solid var(--borda)}
h3{font-size:1.3rem;margin:2.25rem 0 .75rem}
h4{font-size:1.1rem;margin:1.75rem 0 .5rem}
.ancora{color:var(--suave);text-decoration:none;font-size:.8em;opacity:0}
:is(h2,h3,h4):hover .ancora,.ancora:focus{opacity:1}
a{color:var(--link);text-underline-offset:.15em}
strong{font-weight:700}
code,pre,kbd{font-family:"Atkinson Hyperlegible Mono",ui-monospace,monospace;font-size:.92em}
:not(pre)>code{background:var(--superficie);border:1px solid var(--borda);border-radius:4px;padding:.05em .3em;overflow-wrap:break-word}
pre{background:var(--superficie);border:1px solid var(--borda);border-radius:6px;padding:.9rem 1rem;overflow-x:auto;line-height:1.5;tab-size:2}
blockquote{margin:1.25rem 0;padding:.25rem 1rem;border-left:3px solid var(--marca);color:var(--suave)}
hr{border:0;border-top:1px solid var(--borda);margin:2.5rem 0}
img{max-width:100%}
li+li{margin-top:.25rem}

.topo{position:sticky;top:0;z-index:10;display:flex;align-items:center;gap:1rem;padding:.6rem var(--margem);background:var(--fundo);border-bottom:1px solid var(--borda)}
.marca{font-family:Fraunces,Georgia,serif;font-weight:600;color:var(--texto);text-decoration:none;white-space:nowrap}
.paginas{display:flex;gap:1rem;overflow-x:auto;flex:1;font-size:.95rem;scrollbar-width:none}
.paginas a{color:var(--suave);text-decoration:none;white-space:nowrap}
.paginas a[aria-current]{color:var(--texto);box-shadow:inset 0 -2px var(--marca)}
.tema-botao{font:inherit;font-size:.9rem;color:var(--texto);background:var(--superficie);border:1px solid var(--borda);border-radius:999px;padding:.25rem .8rem;cursor:pointer;white-space:nowrap}
.tema-botao:hover{border-color:var(--marca)}
:focus-visible{outline:2px solid var(--marca);outline-offset:2px}

.sumario{margin:1.5rem 0 2.5rem;padding:.75rem 1.25rem;background:var(--superficie);border:1px solid var(--borda);border-radius:8px;font-size:.95rem}
.sumario summary{font-family:Fraunces,Georgia,serif;font-weight:600;font-size:1.1rem;cursor:pointer}
.sumario ol{margin:.5rem 0;padding-left:1.25rem}
.sumario ol ol{margin:.25rem 0;list-style:none;padding-left:1rem;font-size:.92em}
.sumario a{color:var(--texto);text-decoration:none}
.sumario a:hover{color:var(--link);text-decoration:underline}

.tabela{overflow-x:auto;margin:1.25rem 0;border:1px solid var(--borda);border-radius:6px}
table{border-collapse:collapse;width:100%;font-size:.95rem;line-height:1.45}
th,td{padding:.5rem .75rem;text-align:left;vertical-align:top;border-bottom:1px solid var(--borda)}
th{background:var(--superficie);font-weight:700;border-bottom:2px solid var(--marca);white-space:nowrap}
tbody tr:nth-child(even){background:var(--zebra)}
tbody tr:last-child td{border-bottom:0}

/* Diagrama: largura natural; sai da coluna até a largura da janela; mais largo que isso, encolhe e ganha zoom. */
.diagrama{--caixa:min(var(--w),calc(var(--vw) - 2 * var(--margem)));width:var(--caixa);max-width:none;margin:2rem 0 2rem calc(50% - var(--caixa) / 2)}
/* Contorno com outline, não border: a borda roubaria 2 px e encolheria o diagrama que cabe na largura natural. */
.diagrama-quadro{position:relative;display:grid;place-items:center;min-height:min(var(--h),70vh,max(var(--caixa) * var(--r) * 1.5,6.5rem));overflow:hidden;background:var(--pele-bg);outline:1px solid var(--borda);border-radius:8px}
.diagrama-quadro>svg{display:block}
.diagrama figcaption{display:flex;flex-wrap:wrap;gap:.25rem 1rem;justify-content:space-between;margin-top:.4rem;font-size:.85rem;color:var(--suave)}
.diagrama figcaption>span:first-child{color:var(--texto)}
.diagrama-dica{display:none;flex-basis:100%}
.diagrama.com-zoom .diagrama-dica{display:block}
/* Com zoom, o quadro tem a altura natural do desenho (até 70% da janela): no tamanho natural o desenho
   cabe inteiro na altura e só se move para os lados. Nunca fica menor que os três botões. */
.diagrama.com-zoom .diagrama-quadro{min-height:max(min(var(--h),70vh),6.5rem)}
.diagrama-erro{margin:2rem 0;padding:1rem;border:2px solid #b3262d;border-radius:8px;background:color-mix(in srgb,#b3262d 10%,var(--fundo))}
.diagrama-erro figcaption{font-weight:700;margin-bottom:.5rem;white-space:pre-wrap}

.indice{list-style:none;padding:0;display:grid;gap:1rem}
.indice li{margin:0;padding:1rem 1.25rem;background:var(--superficie);border:1px solid var(--borda);border-radius:8px}
.indice a{font-family:Fraunces,Georgia,serif;font-weight:600;font-size:1.25rem}
.indice p{margin:.4rem 0 0;color:var(--suave)}

@media (max-width:40rem){.marca{display:none}h1{font-size:1.9rem}}

@media print{
  ${tema('claro', ':root[data-tema] .pele')}
  :root[data-tema]{--fundo:#fff;--texto:#0b1220;--suave:#545d6e;--borda:#ccc;--superficie:#f6f6f6;--zebra:#f6f6f6;--link:#0b1220;--marca:#9a5800;color-scheme:light}
  html{font-size:10.5pt}
  .topo,.sumario,.ancora,.pele-zoom,.diagrama-dica,.diagrama-links{display:none!important}
  main{max-width:none;padding:0}
  h2{break-before:auto;break-after:avoid}
  h3,h4{break-after:avoid}
  .diagrama{width:100%;margin:1rem 0;break-inside:avoid}
  .diagrama-quadro{min-height:0;outline:0;background:none}
  .diagrama-quadro>svg{width:100%!important;max-width:min(100%,var(--w))!important;height:auto!important}
  .tabela{overflow:visible;border:0}
  table{font-size:9pt}
  th{white-space:normal}
  tr{break-inside:avoid}
  pre{white-space:pre-wrap;overflow-wrap:anywhere;overflow:visible}
  a{text-decoration:none}
}
`;

const JS = `(function () {
  var raiz = document.documentElement;
  var botao = document.querySelector('.tema-botao');
  function rotulo() {
    var escuro = raiz.getAttribute('data-tema') === 'escuro';
    botao.textContent = escuro ? 'Tema claro' : 'Tema escuro';
    botao.setAttribute('aria-label', escuro ? 'Mudar para o tema claro' : 'Mudar para o tema escuro');
  }
  function aplicar(tema, guardar) {
    raiz.setAttribute('data-tema', tema);
    if (guardar) { try { localStorage.setItem('tema', tema); } catch (e) {} }
    rotulo();
  }
  botao.addEventListener('click', function () {
    aplicar(raiz.getAttribute('data-tema') === 'escuro' ? 'claro' : 'escuro', true);
  });
  var preferencia = matchMedia('(prefers-color-scheme: dark)');
  preferencia.addEventListener('change', function (e) {
    var guardado = null;
    try { guardado = localStorage.getItem('tema'); } catch (err) {}
    if (!guardado) aplicar(e.matches ? 'escuro' : 'claro', false);
  });
  rotulo();

  // Largura útil sem a barra de rolagem, para o diagrama sair da coluna sem passar da janela.
  function largura() { raiz.style.setProperty('--vw', raiz.clientWidth + 'px'); }
  largura();
  addEventListener('resize', largura);

  if (!window.pele) return;
  var zooms = [];
  function marcar() {
    zooms.forEach(function (z) { z.figura.classList.toggle('com-zoom', z.zoom.fit < 0.999); });
  }
  document.querySelectorAll('.diagrama-quadro').forEach(function (quadro) {
    var zoom = pele.enableZoom(quadro, { maxScale: 3, labels: { zoomIn: 'Ampliar', zoomOut: 'Reduzir', reset: 'Ver o diagrama inteiro' } });
    zooms.push({ zoom: zoom, figura: quadro.parentNode });
  });
  marcar();
  addEventListener('resize', function () { requestAnimationFrame(marcar); });
  addEventListener('beforeprint', function () { zooms.forEach(function (z) { z.zoom.reset(); }); });
})();
`;

rmSync(saida, { recursive: true, force: true });
for (const sub of ['svg', 'js', 'fontes']) mkdirSync(join(saida, sub), { recursive: true });
for (const f of readdirSync(join(pasta, 'tema/fontes'))) copyFileSync(join(pasta, 'tema/fontes', f), join(saida, 'fontes', f));
copyFileSync(join(pasta, 'node_modules/pele/dist/pele.global.js'), join(saida, 'js/pele.global.js'));
copyFileSync(join(pasta, 'node_modules/pele/LICENSE'), join(saida, 'js/LICENSE-pele.txt'));
writeFileSync(join(saida, 'js/site.js'), JS);
writeFileSync(join(saida, 'site.css'), CSS);

// Primeiro todas as páginas, para a navegação de cada uma já ter o título das outras.
for (const pagina of paginas) gerarPagina(pagina);
for (const p of paginas) writeFileSync(join(saida, p.nome), documento(p.titulo, p.html, p.comDiagramas, p.nome));

const indice = `<h1>Arquitetura do Corredor Aceso</h1>
<p>Documentos de arquitetura gerados dos arquivos Markdown em <code>docs/</code>. Para mudar o texto, edite o Markdown e rode <code>npm run site</code> em <code>docs/arquitetura/diagramas</code>.</p>
<ul class="indice">
${paginas.map((p) => `<li><a href="${p.nome}">${escapar(p.titulo)}</a><p>${escapar(p.resumo)}</p></li>`).join('\n')}
</ul>`;
writeFileSync(join(saida, 'index.html'), documento('Arquitetura', indice, false, 'index.html'));

for (const p of paginas) console.log(`ok    ${p.nome.padEnd(28)} ${mostrar(p.arquivo)}`);
for (const f of falhas) console.error(`FALHA ${f}`);
console.log(`${paginas.length} páginas em ${mostrar(saida)}${falhas.length ? `, ${falhas.length} diagrama(s) com erro` : ''}`);
process.exit(falhas.length ? 1 : 0);
