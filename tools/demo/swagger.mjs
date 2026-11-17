// Prints do Swagger da API que o tools/demo.sh subiu: a visão geral e uma rota aberta do Cadastro e do Motor.
// Uso: CORREDOR_API_URL=http://localhost:5080 node tools/demo/swagger.mjs [pasta de saída]
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { abrirNavegador } from './navegador.mjs';

const API = process.env.CORREDOR_API_URL ?? 'http://localhost:5080';
const SAIDA = process.argv[2] ?? 'docs/evidencias/api';
const LARGURA = 1280;
const espera = (ms) => new Promise((r) => setTimeout(r, ms));
mkdirSync(SAIDA, { recursive: true });

const pagina = await abrirNavegador(9334);
const tela = (altura) => pagina.cdp('Emulation.setDeviceMetricsOverride', { width: LARGURA, height: altura, deviceScaleFactor: 1, mobile: false });

// A visão geral vai até o começo da lista de schemas, que só repete os tipos.
async function fotoDaPagina(arquivo) {
  const altura = await pagina.avaliar('Math.ceil(document.querySelector(".models")?.getBoundingClientRect().top ?? document.documentElement.scrollHeight)');
  await tela(Math.min(altura, 8000));
  await espera(400);
  await pagina.foto(join(SAIDA, arquivo));
  await tela(900);
}

// Abre a rota e fotografa só o bloco dela.
async function fotoDaRota(id, arquivo) {
  const aberta = await pagina.avaliar(`(() => {
    const bloco = document.getElementById(${JSON.stringify(id)});
    if (!bloco) return false;
    (bloco.querySelector('.opblock-summary-control') ?? bloco.querySelector('.opblock-summary')).click();
    return true;
  })()`);
  if (!aberta) throw new Error(`rota não achada no Swagger: ${id}`);
  await espera(1500);
  const altura = await pagina.avaliar('document.documentElement.scrollHeight');
  await tela(Math.min(altura, 8000));
  await espera(400);
  const caixa = await pagina.avaliar(`(() => {
    const r = document.getElementById(${JSON.stringify(id)}).getBoundingClientRect();
    return { x: 0, y: r.top + window.scrollY - 10, width: ${LARGURA}, height: r.height + 20 };
  })()`);
  const { data } = await pagina.cdp('Page.captureScreenshot', { format: 'png', clip: { ...caixa, scale: 1 }, captureBeyondViewport: true });
  writeFileSync(join(SAIDA, arquivo), Buffer.from(data, 'base64'));
  await tela(900);
}

try {
  await tela(900);
  await pagina.ir(`${API}/swagger`);
  await pagina.esperarTexto('Corredor Aceso');
  await pagina.esperarTexto('Motor');
  const ids = await pagina.avaliar('[...document.querySelectorAll(".opblock")].map((b) => b.id)');
  console.log(`${ids.length} rotas no Swagger`);
  await fotoDaPagina('01-swagger-visao-geral.png');
  const cadastro = ids.find((id) => id.includes('Cadastro') && id.endsWith('-CadastrarIdoso')) ?? ids.find((id) => id.includes('Cadastro'));
  const motor = ids.find((id) => id.includes('Motor') && id.includes('get_api_radar')) ?? ids.find((id) => id.includes('Motor'));
  await fotoDaRota(cadastro, '02-swagger-cadastro.png');
  console.log(`rota do Cadastro: ${cadastro}`);
  await pagina.ir(`${API}/swagger`);
  await pagina.esperarTexto('Motor');
  await fotoDaRota(motor, '03-swagger-motor.png');
  console.log(`rota do Motor: ${motor}`);
} finally {
  pagina.fechar();
}
