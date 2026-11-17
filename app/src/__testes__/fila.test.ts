import { ErroDeRede, ErroHttp, type ApiCorredor } from '../api/cliente';
import type { NovaVisita } from '../api/contratos';
import { criarApiMock } from '../api/mock/mock';
import { CASAS_DO_MOCK } from '../api/mock/casas';
import { contarPendentes, desfechosDaCasa, enviarFila, registrarDesfecho } from '../banco/fila';
import { abrirBancoDeTeste } from './bancoDeTeste';

const TOKEN = 'f'.repeat(32);
const id = (n: number) => `00000000-0000-4000-8000-${String(n).padStart(12, '0')}`;

/** API falsa: cada chamada consome a próxima resposta da lista. */
function apiRoteirizada(respostas: (Error | 'ok')[]) {
  const recebidas: NovaVisita[] = [];
  const api = {
    async registrarVisita(v: NovaVisita) {
      recebidas.push(v);
      const r = respostas.shift() ?? 'ok';
      if (r instanceof Error) throw r;
      return { id: v.id, registradaEm: '2026-11-18T11:30:00-03:00' };
    },
  } as unknown as ApiCorredor;
  return { api, recebidas };
}

async function bancoComDesfechos(n: number) {
  const banco = await abrirBancoDeTeste();
  for (let i = 1; i <= n; i++) {
    await registrarDesfecho(banco, {
      id: id(i),
      casaToken: TOKEN,
      desfecho: 'EstavaFora',
      observacao: 'vizinha disse que foi à feira',
      ocorridaEm: new Date(`2026-11-18T13:4${i}:00Z`),
    });
  }
  return banco;
}

describe('fila de desfechos', () => {
  it('grava o desfecho sem rede e envia o mesmo id quando a rede volta', async () => {
    const banco = await bancoComDesfechos(1);
    const { api, recebidas } = apiRoteirizada([new ErroDeRede('sem rede'), 'ok']);

    const semRede = await enviarFila(banco, api);
    expect(semRede.parouPor).toEqual({ tipo: 'tentarDeNovo', codigo: 'SEM_CONEXAO' });
    const [pendente] = await desfechosDaCasa(banco, TOKEN);
    expect(pendente).toMatchObject({ estado: 'pendente', tentativas: 1, ultimoErro: 'SEM_CONEXAO' });

    const comRede = await enviarFila(banco, api);
    expect(comRede).toEqual({ enviados: 1, recusados: 0, parouPor: null });
    expect(recebidas.map((v) => v.id)).toEqual([id(1), id(1)]);
    expect(recebidas[1].ocorridaEm).toBe('2026-11-18T10:41:00-03:00');
    const [enviado] = await desfechosDaCasa(banco, TOKEN);
    expect(enviado).toMatchObject({ estado: 'enviado', observacao: null, registradaEm: '2026-11-18T14:30:00.000Z' });
    expect(await contarPendentes(banco)).toBe(0);
  });

  it('envia da mais antiga para a mais nova, uma por vez', async () => {
    const banco = await bancoComDesfechos(3);
    const { api, recebidas } = apiRoteirizada([]);
    await enviarFila(banco, api);
    expect(recebidas.map((v) => v.id)).toEqual([id(1), id(2), id(3)]);
  });

  it('para no 503 e guarda as outras na fila', async () => {
    const banco = await bancoComDesfechos(2);
    const { api, recebidas } = apiRoteirizada([new ErroHttp(503, 'MOTOR_INDISPONIVEL')]);
    const r = await enviarFila(banco, api);
    expect(r.parouPor).toEqual({ tipo: 'tentarDeNovo', codigo: 'MOTOR_INDISPONIVEL' });
    expect(recebidas).toHaveLength(1);
    expect(await contarPendentes(banco)).toBe(2);
  });

  it('marca 422 como recusado, não tenta de novo e segue para a próxima', async () => {
    const banco = await bancoComDesfechos(2);
    const { api, recebidas } = apiRoteirizada([new ErroHttp(422, 'TOKEN_INATIVO'), 'ok']);
    expect(await enviarFila(banco, api)).toEqual({ enviados: 1, recusados: 1, parouPor: null });
    const estados = (await desfechosDaCasa(banco, TOKEN)).map((d) => [d.id, d.estado, d.ultimoErro]);
    expect(estados).toEqual([
      [id(2), 'enviado', null],
      [id(1), 'recusado', 'TOKEN_INATIVO'],
    ]);
    await enviarFila(banco, api);
    expect(recebidas).toHaveLength(2);
  });

  it('no 401 para a fila sem contar tentativa e pede a entrada de novo', async () => {
    const banco = await bancoComDesfechos(1);
    const { api } = apiRoteirizada([new ErroHttp(401, 'HTTP_401')]);
    expect((await enviarFila(banco, api)).parouPor).toEqual({ tipo: 'pedirEntrada' });
    expect((await desfechosDaCasa(banco, TOKEN))[0]).toMatchObject({ estado: 'pendente', tentativas: 0 });
  });

  it('duas chamadas ao mesmo tempo dividem um envio só', async () => {
    const banco = await bancoComDesfechos(1);
    const { api, recebidas } = apiRoteirizada([]);
    await Promise.all([enviarFila(banco, api), enviarFila(banco, api)]);
    expect(recebidas).toHaveLength(1);
  });
});

describe('mock da API', () => {
  const agora = () => new Date('2026-11-18T13:31:00Z');

  it('repete o mesmo registro para o mesmo id', async () => {
    const api = criarApiMock({ agoraReal: agora });
    const visita = { id: id(9), casaToken: CASAS_DO_MOCK[6].casaToken, ocorridaEm: '2026-11-18T10:30:00-03:00', desfecho: 'NaoAbriu' as const, observacao: null };
    const primeira = await api.registrarVisita(visita);
    expect(await api.registrarVisita(visita)).toEqual(primeira);
  });

  it('EstavaBem tira o alerta de silêncio e a casa conta como verde', async () => {
    const api = criarApiMock({ agoraReal: agora });
    const antes = await api.radar(3);
    const casaE = antes.pendentes.find((c) => c.motivo === 'SemMovimentoNaJanela');
    expect(casaE).toBeDefined();
    await api.registrarVisita({ id: id(1), casaToken: casaE!.casaToken, ocorridaEm: '2026-11-18T10:32:00-03:00', desfecho: 'EstavaBem', observacao: null });
    const depois = await api.radar(3);
    expect(depois.pendentes.map((c) => c.casaToken)).not.toContain(casaE!.casaToken);
    expect(depois.verdes).toBe(antes.verdes + 1);
  });

  it('EstavaFora atende o silêncio, mas não atende a falta de energia', async () => {
    const api = criarApiMock({ agoraReal: agora });
    const antes = await api.radar(3);
    const energia = antes.pendentes.find((c) => c.motivo === 'EnergiaEletrodependente')!;
    const silencio = antes.pendentes.find((c) => c.motivo === 'SemMovimentoNaJanela')!;
    for (const [n, c] of [energia, silencio].entries()) {
      await api.registrarVisita({ id: id(n + 1), casaToken: c.casaToken, ocorridaEm: '2026-11-18T10:32:00-03:00', desfecho: 'EstavaFora', observacao: null });
    }
    const depois = await api.radar(3);
    expect(depois.pendentes.map((c) => c.casaToken)).toContain(energia.casaToken);
    expect(depois.atendidas.find((c) => c.casaToken === silencio.casaToken)).toMatchObject({ atendida: true, situacao: 'Vermelho' });
  });

  it('falha como rede caída quando está desconectado', async () => {
    const api = criarApiMock({ agoraReal: agora, conectado: () => false });
    await expect(api.radar(3)).rejects.toBeInstanceOf(ErroDeRede);
  });

  it('bloqueia mensagem com dado de saúde para o grupo do bairro', async () => {
    const api = criarApiMock({ agoraReal: agora });
    const r = await api.verificarMensagem('GrupoDoBairro', 'ele mora sozinho e toma diurético');
    expect(r).toMatchObject({
      veredito: 'Bloqueada',
      textoMascarado: 'ele [MORA_SOZINHO] e toma [SAUDE]',
      tiposDeDado: ['MoraSozinho', 'Saude'],
      explicacao: 'Essa mensagem diz que a pessoa mora sozinha e fala da saúde dela. No grupo do bairro, qualquer um lê.',
    });
  });
});
