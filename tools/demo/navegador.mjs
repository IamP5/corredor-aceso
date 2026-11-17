// Cliente mínimo do Chrome DevTools Protocol, sem dependência: Node 22+ tem WebSocket e fetch.
// Abre o Chrome sem janela num perfil próprio, numa tela de 390 por 844 pontos, como um celular.
import { spawn } from 'node:child_process';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const CHROME = process.env.CHROME ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const espera = (ms) => new Promise((r) => setTimeout(r, ms));

export async function abrirNavegador(porta = 9333) {
  const perfil = mkdtempSync(join(tmpdir(), 'corredor-chrome-'));
  const chrome = spawn(CHROME, [`--remote-debugging-port=${porta}`, `--user-data-dir=${perfil}`, '--headless=new',
    '--no-first-run', '--no-default-browser-check', 'about:blank'], { stdio: 'ignore' });
  let alvo;
  for (let i = 0; i < 50 && !alvo; i++) {
    await espera(200);
    alvo = await fetch(`http://127.0.0.1:${porta}/json/list`).then((r) => r.json()).then((l) => l.find((p) => p.type === 'page')).catch(() => null);
  }
  const ws = new WebSocket(alvo.webSocketDebuggerUrl);
  await new Promise((r) => ws.addEventListener('open', r, { once: true }));
  let id = 0;
  const pendentes = new Map();
  ws.addEventListener('message', (e) => {
    const m = JSON.parse(e.data);
    if (m.id && pendentes.has(m.id)) { pendentes.get(m.id)(m); pendentes.delete(m.id); }
  });
  const cdp = (method, params = {}) => new Promise((resolve, reject) => {
    const n = ++id;
    pendentes.set(n, (m) => (m.error ? reject(new Error(`${method}: ${m.error.message}`)) : resolve(m.result)));
    ws.send(JSON.stringify({ id: n, method, params }));
  });
  await cdp('Page.enable');
  await cdp('Runtime.enable');
  await cdp('Emulation.setDeviceMetricsOverride', { width: 390, height: 844, deviceScaleFactor: 2, mobile: true });

  const avaliar = async (expressao) => {
    const r = await cdp('Runtime.evaluate', { expression: expressao, awaitPromise: true, returnByValue: true });
    if (r.exceptionDetails) throw new Error(r.exceptionDetails.exception?.description ?? r.exceptionDetails.text);
    return r.result.value;
  };

  const pagina = {
    cdp,
    avaliar,
    async ir(url) { await cdp('Page.navigate', { url }); await espera(1500); },
    texto: () => avaliar('document.body.innerText'),
    async esperarTexto(trecho, ms = 20000) {
      const fim = Date.now() + ms;
      while (Date.now() < fim) {
        if ((await pagina.texto()).includes(trecho)) return true;
        await espera(300);
      }
      throw new Error(`não apareceu na tela: ${trecho}`);
    },
    // Toca no elemento cujo rótulo de acessibilidade ou texto começa com `rotulo`. O mais interno ganha.
    async tocar(rotulo) {
      const ok = await avaliar(`(() => {
        const alvo = ${JSON.stringify(rotulo)};
        const candidatos = [...document.querySelectorAll('[aria-label],[role=button],[role=radio],[role=link],a,button')]
          .filter((e) => (e.getAttribute('aria-label') ?? e.innerText ?? '').trim().startsWith(alvo) && e.offsetParent !== null);
        const el = candidatos.at(-1);
        if (!el) return false;
        el.scrollIntoView({ block: 'center' });
        el.click();
        return true;
      })()`);
      if (!ok) throw new Error(`nada para tocar: ${rotulo}`);
      await espera(700);
    },
    async digitar(rotulo, valor) {
      const ok = await avaliar(`(() => {
        const el = document.querySelector('[aria-label=${JSON.stringify(rotulo)}]');
        if (!el) return false;
        const proto = el.tagName === 'TEXTAREA' ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
        Object.getOwnPropertyDescriptor(proto, 'value').set.call(el, ${JSON.stringify(valor)});
        el.dispatchEvent(new Event('input', { bubbles: true }));
        return true;
      })()`);
      if (!ok) throw new Error(`campo não achado: ${rotulo}`);
      await espera(300);
    },
    async foto(arquivo) {
      const { data } = await cdp('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
      writeFileSync(arquivo, Buffer.from(data, 'base64'));
    },
    async fotoInteira(arquivo) {
      // No react-native-web a tela rola dentro de um ScrollView, não no documento. Mede o maior conteúdo rolável.
      const altura = await avaliar('Math.max(...[...document.querySelectorAll("*")].map((e) => e.scrollHeight + (e.getBoundingClientRect().top > 0 ? e.getBoundingClientRect().top : 0)))');
      await cdp('Emulation.setDeviceMetricsOverride', { width: 390, height: Math.min(Math.max(altura, 844), 4000), deviceScaleFactor: 2, mobile: true });
      await espera(400);
      await pagina.foto(arquivo);
      await cdp('Emulation.setDeviceMetricsOverride', { width: 390, height: 844, deviceScaleFactor: 2, mobile: true });
    },
    fechar() { ws.close(); chrome.kill(); },
  };
  return pagina;
}
