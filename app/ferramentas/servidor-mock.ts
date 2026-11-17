// Serve o mock do app por HTTP, nas rotas da seção 6.3. Serve para provar o cliente HTTP
// e o caminho sem rede antes de a API .NET existir: derrube este servidor e a API some.
// Rode da pasta app/: npm run api:mock -- 3001
// Depois: EXPO_PUBLIC_API_URL=http://localhost:3001 npx expo start
import { createServer } from 'node:http';
import { ErroDeRede, ErroHttp } from '../src/api/cliente';
import { criarApiMock } from '../src/api/mock/mock';

const porta = Number(process.argv[2] ?? 3001);
const api = criarApiMock();

async function lerCorpo(req: import('node:http').IncomingMessage): Promise<unknown> {
  let texto = '';
  for await (const pedaco of req) texto += pedaco;
  return texto ? JSON.parse(texto) : null;
}

createServer(async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  if (req.method === 'OPTIONS') return res.writeHead(204).end();

  const url = new URL(req.url ?? '/', `http://localhost:${porta}`);
  const microarea = Number(url.searchParams.get('microarea'));
  const rota = `${req.method} ${url.pathname}`;
  let m: RegExpMatchArray | null;
  try {
    let status = 200;
    let corpo: unknown;
    if (rota === 'GET /api/radar') corpo = await api.radar(microarea);
    else if (rota === 'GET /api/lista-de-nomes') corpo = await api.listaDeNomes(microarea);
    else if (rota === 'GET /api/avisos') corpo = await api.avisos(microarea);
    else if ((m = rota.match(/^GET \/api\/casas\/([0-9a-f]{32})\/linha-do-tempo$/))) corpo = await api.linhaDoTempo(m[1]);
    else if (rota === 'POST /api/visitas') {
      const visita = (await lerCorpo(req)) as Parameters<typeof api.registrarVisita>[0];
      corpo = await api.registrarVisita(visita);
      status = 201;
    } else if (rota === 'POST /api/mensagens/verificar') {
      const { canal, texto } = (await lerCorpo(req)) as { canal: 'GrupoDoBairro'; texto: string };
      corpo = await api.verificarMensagem(canal, texto);
    } else if ((m = rota.match(/^POST \/api\/dias-criticos\/(\d{4}-\d{2}-\d{2})\/encerramento$/))) {
      corpo = await api.encerrarTurno(m[1], microarea);
    } else throw new ErroHttp(404, 'ROTA_DESCONHECIDA');
    console.log(`${new Date().toISOString()} ${rota} ${status}`);
    res.writeHead(status, { 'Content-Type': 'application/json' }).end(JSON.stringify(corpo));
  } catch (e) {
    const status = e instanceof ErroHttp ? e.status : e instanceof ErroDeRede ? 503 : 500;
    const codigo = e instanceof ErroHttp ? e.codigo : 'ERRO_INTERNO';
    console.log(`${new Date().toISOString()} ${rota} ${status} ${codigo}`);
    res.writeHead(status, { 'Content-Type': 'application/json' }).end(JSON.stringify({ codigo, mensagem: codigo }));
  }
}).listen(porta, () => console.log(`mock da API em http://localhost:${porta}/api`));
