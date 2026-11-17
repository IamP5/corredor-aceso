// Dirige o app (alvo web) contra a API real e salva um print de cada tela em docs/evidencias/integracao/.
// Pré-requisitos: `bash tools/demo.sh subir` (ou as cenas) com a API em CORREDOR_API_URL, e o app em
// http://localhost:8081 com EXPO_PUBLIC_API_URL apontando para a API (`bash tools/demo.sh app`).
// O caminho sem rede para a API com `bash tools/demo.sh parar-api`, registra a visita, sobe a API de novo
// e confere no Postgres que a visita foi gravada uma vez só.
import { execFileSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { abrirNavegador } from './navegador.mjs';

const RAIZ = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const SAIDA = join(RAIZ, 'docs', 'evidencias', 'integracao');
const API = process.env.CORREDOR_API_URL ?? 'http://localhost:5080';
const APP = process.env.CORREDOR_APP_URL ?? 'http://localhost:8081';
const TOKEN = process.env.CORREDOR_TOKEN_ACS ?? 'dev-acs-demo-01-so-no-notebook';
const espera = (ms) => new Promise((r) => setTimeout(r, ms));
const dizer = (t) => console.log(`[${new Date().toLocaleTimeString('pt-BR')}] ${t}`);
const OBSERVACAO_SEM_REDE = `registrada com a API fora, ${new Date().toISOString()}`;
mkdirSync(SAIDA, { recursive: true });

const api = (caminho, init = {}) =>
  fetch(API + caminho, { ...init, headers: { Authorization: `Bearer ${TOKEN}`, 'Content-Type': 'application/json', ...init.headers } })
    .then(async (r) => ({ status: r.status, corpo: await r.text().then((t) => (t ? JSON.parse(t) : null)) }));
const demo = (...args) => execFileSync('bash', [join(RAIZ, 'tools', 'demo.sh'), ...args], { stdio: 'inherit' });
const sql = (consulta) => execFileSync('docker', ['compose', '-f', join(RAIZ, 'compose.dev.yml'), 'exec', '-T', 'postgres',
  'psql', '-U', 'corredor', '-d', 'corredor_demo', '-At', '-c', consulta]).toString().trim();

const p = await abrirNavegador();
// Navega só por toques: recarregar a página no alvo web disputa o arquivo do SQLite com a aba anterior.
const inicio = async () => {
  for (let i = 0; i < 5 && !(await p.texto()).includes('Ver o radar'); i++) await p.tocar('Voltar').catch(() => {});
  await p.esperarTexto('Ver o radar');
};
const foto = async (nome, inteira = false) => {
  await (inteira ? p.fotoInteira(join(SAIDA, nome)) : p.foto(join(SAIDA, nome)));
  dizer(`print ${nome}`);
};
try {
  await p.ir(APP);
  await p.esperarTexto('Entrar', 120000);
  await p.digitar('Seu código de acesso', TOKEN);
  await foto('01-entrada.png');
  await p.tocar('Entrar');
  await p.esperarTexto('dia crítico');
  await espera(3000);
  await foto('02-dia-critico.png');

  await p.tocar('Ver o radar');
  await p.esperarTexto('Precisam de visita');
  await espera(1500);
  await foto('03-radar.png', true);
  const cards = await p.avaliar(`[...document.querySelectorAll('[role=button][aria-label]')].map((e) => e.getAttribute('aria-label')).filter((t) => /Urgente|sem sinal/.test(t))`);
  dizer(`cards pendentes na tela: ${cards.length}`);
  dizer(`primeiro card: ${cards[0]}`);

  // A casa do primeiro card, a linha do tempo e o desfecho da visita.
  await p.tocar(cards[0]);
  await p.esperarTexto('Registrar visita');
  await espera(2000);
  await foto('04-casa-e-linha-do-tempo.png', true);
  await p.tocar('Registrar visita');
  await p.esperarTexto('O que você encontrou');
  await p.tocar('Não abriu');
  await p.digitar('Observação, se quiser', 'bati duas vezes e ninguém atendeu');
  await foto('05-visita.png');
  await p.tocar('Salvar visita');
  await espera(4000);
  await foto('06-casa-depois-da-visita.png', true);

  // A mensagem para o grupo do bairro.
  await inicio();
  await p.tocar('Escrever para o grupo do bairro');
  await p.esperarTexto('Antes de mandar no grupo');
  await p.digitar('Sua mensagem', 'ele mora sozinho e toma diurético');
  await p.tocar('Verificar mensagem');
  await p.esperarTexto('versão');
  await espera(1000);
  await foto('07-mensagem-bloqueada.png', true);

  // Fila sem rede: a API para, a visita fica no celular, a API volta e a visita sobe uma vez.
  const antes = (await api(`/api/radar?microarea=3`)).corpo;
  const relogioAntes = new Date(antes.agora).getTime();
  const tempoAntes = Date.now();
  const alvo = antes.pendentes.find((c) => c.situacao === 'Vermelho' && !c.atendida) ?? antes.pendentes[0];
  const nomes = (await api('/api/lista-de-nomes?microarea=3')).corpo;
  const nomeDoAlvo = nomes.find((i) => i.casaToken === alvo.casaToken).nome;
  dizer(`casa do teste sem rede: ${nomeDoAlvo}`);
  dizer('parando a API');
  demo('parar-api');
  await inicio();
  await p.tocar('Ver o radar');
  await p.esperarTexto('Precisam de visita');
  const rotulo = await p.avaliar(`[...document.querySelectorAll('[role=button][aria-label]')].map((e) => e.getAttribute('aria-label')).find((t) => t.includes(${JSON.stringify(nomeDoAlvo)}))`);
  await p.tocar(rotulo);
  await p.tocar('Registrar visita');
  await p.esperarTexto('O que você encontrou');
  await p.tocar('Estava fora');
  await p.digitar('Observação, se quiser', OBSERVACAO_SEM_REDE);
  await p.tocar('Salvar visita');
  await espera(4000);
  await foto('08-fila-api-fora.png', true);
  const visitas = await p.avaliar(`(async () => {
    const t = document.body.innerText; return t;
  })()`);
  dizer(`tela com a API fora contém "Ainda não enviad": ${visitas.includes('inda não enviad')}`);
  dizer('subindo a API de novo');
  demo('api');
  // O relógio de demo mora na memória da API. Volta ao ponto em que estava, mais o tempo que passou.
  const agora = new Date(relogioAntes + (Date.now() - tempoAntes)).toISOString();
  const r = await fetch(`${API}/api/demo/relogio`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ agora }) });
  dizer(`relógio de demo restaurado -> ${r.status}`);
  // A fila tenta de novo a cada 15 s. Espera a visita chegar ao banco e a tela deixar de dizer "ainda não enviada".
  const contar = () => Number(sql(`SELECT count(*) FROM cadastro.visita WHERE observacao = '${OBSERVACAO_SEM_REDE}'`));
  const limite = Date.now() + 90000;
  while (contar() === 0 && Date.now() < limite) await espera(2000);
  dizer(`a visita feita sem API chegou ao banco: ${contar()} linha(s)`);
  await espera(3000);
  await p.esperarTexto('Registrar visita');
  await espera(1500);
  await foto('09-fila-enviada.png', true);
  dizer(`a tela da casa ainda diz "não enviada": ${(await p.texto()).includes('inda não enviad')}`);
  await espera(20000);
  dizer(`20 s depois, linhas no banco para a mesma visita: ${contar()}`);

  // Encerramento do turno.
  await inicio();
  await p.tocar('Encerrar o turno');
  await p.esperarTexto('Encerrar o turno de hoje?');
  await foto('10-encerrar.png');
  await p.tocar('Encerrar o turno');
  await p.esperarTexto('Turno encerrado');
  await espera(1000);
  await foto('11-turno-encerrado.png', true);
  dizer(`tela final: ${(await p.texto()).replace(/\s+/g, ' ').slice(0, 300)}`);
} finally {
  p.fechar();
}
