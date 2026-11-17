import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { ItemDaListaDeNomes, Radar } from '../api/contratos';
import { ESQUEMA_V1 } from '../banco/esquema';
import { lerRadarNaTela, trocarAvisos, trocarListaDeNomes, trocarRadar } from '../banco/cache';
import { registrarDesfecho } from '../banco/fila';
import { abrirBancoDeTeste } from './bancoDeTeste';

const T = (c: string) => c.repeat(32);
const AGORA = new Date('2026-11-18T13:31:00Z');

const perfil = { contato: 'Sozinho' as const, eletrodependente: false };
const radar: Radar = {
  turnoAberto: true,
  agora: '2026-11-18T10:31:00-03:00',
  pendentes: [
    { casaToken: T('b'), situacao: 'Vermelho', motivo: 'SemMovimentoNaJanela', desde: '2026-11-18T10:00:00-03:00', atendida: false, perfil, ultimoPacoteEm: '2026-11-18T10:30:52-03:00' },
    { casaToken: T('a'), situacao: 'Cinza', motivo: 'SemHeartbeat', desde: '2026-11-18T09:10:00-03:00', atendida: false, perfil, ultimoPacoteEm: null },
  ],
  atendidas: [
    { casaToken: T('c'), situacao: 'Cinza', motivo: 'SemHeartbeat', desde: '2026-11-18T08:40:00-03:00', atendida: true, perfil, ultimoPacoteEm: '2026-11-18T08:40:00-03:00' },
  ],
  aguardando: [{ casaToken: T('d'), saidaAvisada: true }],
  verdes: 27,
};
const nomes: ItemDaListaDeNomes[] = [
  { casaToken: T('a'), nome: 'Geraldo Jequitibá', endereco: 'Rua do Sabiá, 30 - Vila Exemplo', vizinho: null },
  { casaToken: T('c'), nome: 'Efigênia Jatobá', endereco: 'Rua do Sabiá, 12 - Vila Exemplo', vizinho: { nome: 'Odete Embaúba', telefone: '(00) 90000-0001' } },
  { casaToken: T('d'), nome: 'Zélia Ipê', endereco: 'Rua do Bem-te-vi, 4 - Vila Exemplo', vizinho: null },
];

describe('radar na tela', () => {
  it('usa o mesmo esquema do modelo de dados', () => {
    const doc = readFileSync(join(__dirname, '../../../docs/arquitetura/referencia/dados/sqlite/app.sql'), 'utf8');
    const semComentario = (s: string) =>
      s.split('\n').filter((l) => !l.startsWith('--') && !l.startsWith('PRAGMA')).join('\n').trim();
    expect(ESQUEMA_V1.trim()).toBe(semComentario(doc));
  });

  it('mantém a ordem do servidor: pendentes, atendidas, depois aguardando', async () => {
    const banco = await abrirBancoDeTeste();
    await trocarListaDeNomes(banco, nomes, AGORA);
    await trocarRadar(banco, 3, radar, AGORA);
    const { cards, cabecalho } = await lerRadarNaTela(banco);
    expect(cards.map((c) => [c.lista, c.casaToken[0]])).toEqual([
      ['pendente', 'b'],
      ['pendente', 'a'],
      ['atendida', 'c'],
      ['aguardando', 'd'],
    ]);
    expect(cabecalho).toMatchObject({ microareaId: 3, turnoAberto: true, verdes: 27, agoraDoServidor: '2026-11-18T13:31:00.000Z' });
  });

  it('junta o nome ao token e mostra a casa que não está na lista de nomes', async () => {
    const banco = await abrirBancoDeTeste();
    await trocarListaDeNomes(banco, nomes, AGORA);
    await trocarRadar(banco, 3, radar, AGORA);
    const { cards } = await lerRadarNaTela(banco);
    const semNome = cards.find((c) => c.casaToken === T('b'));
    expect(semNome?.pessoa).toBeNull();
    expect(semNome?.situacao).toBe('Vermelho');
    expect(cards.find((c) => c.casaToken === T('c'))?.pessoa).toEqual({
      nome: 'Efigênia Jatobá',
      endereco: 'Rua do Sabiá, 12 - Vila Exemplo',
      vizinho: { nome: 'Odete Embaúba', telefone: '(00) 90000-0001' },
    });
  });

  it('grava as horas em UTC, então a hora com -03:00 casa com o aviso do mesmo episódio', async () => {
    const banco = await abrirBancoDeTeste();
    await trocarRadar(banco, 3, radar, AGORA);
    await trocarAvisos(
      banco,
      [{ id: 'x', casaToken: T('b'), motivo: 'SemMovimentoNaJanela', desde: '2026-11-18T13:00:00Z', registradoEm: '2026-11-18T10:00:12-03:00', resposta: null, respondidoEm: null }],
      AGORA,
    );
    const { cards } = await lerRadarNaTela(banco);
    const card = cards[0];
    expect(card.lista === 'pendente' && card.desde).toBe('2026-11-18T13:00:00.000Z');
    expect(card.lista === 'pendente' && card.aviso).toEqual({ em: '2026-11-18T13:00:12.000Z', resposta: null });
  });

  it('mostra só o desfecho mais recente de cada casa', async () => {
    const banco = await abrirBancoDeTeste();
    await trocarRadar(banco, 3, radar, AGORA);
    await registrarDesfecho(banco, { id: '00000000-0000-4000-8000-000000000001', casaToken: T('b'), desfecho: 'NaoAbriu', observacao: null, ocorridaEm: new Date('2026-11-18T13:10:00Z') });
    await registrarDesfecho(banco, { id: '00000000-0000-4000-8000-000000000002', casaToken: T('b'), desfecho: 'EstavaBem', observacao: null, ocorridaEm: new Date('2026-11-18T13:40:00Z') });
    const { cards } = await lerRadarNaTela(banco);
    expect(cards[0].ultimoDesfecho).toEqual({ desfecho: 'EstavaBem', em: '2026-11-18T13:40:00.000Z', estado: 'pendente' });
    expect(cards[0].situacao).toBe('Vermelho');
  });

  it('troca a lista de nomes inteira: a casa que revogou some do celular', async () => {
    const banco = await abrirBancoDeTeste();
    await trocarListaDeNomes(banco, nomes, AGORA);
    await trocarListaDeNomes(banco, nomes.slice(1), AGORA);
    await trocarRadar(banco, 3, radar, AGORA);
    const { cards } = await lerRadarNaTela(banco);
    expect(cards.find((c) => c.casaToken === T('a'))?.pessoa).toBeNull();
  });

  it('apaga desfechos enviados há mais de 24 horas e guarda os pendentes', async () => {
    const banco = await abrirBancoDeTeste();
    await registrarDesfecho(banco, { id: '00000000-0000-4000-8000-00000000000a', casaToken: T('a'), desfecho: 'EstavaBem', observacao: null, ocorridaEm: new Date('2026-11-17T10:00:00Z') });
    await registrarDesfecho(banco, { id: '00000000-0000-4000-8000-00000000000b', casaToken: T('a'), desfecho: 'EstavaFora', observacao: null, ocorridaEm: new Date('2026-11-17T10:05:00Z') });
    await banco.runAsync(`UPDATE desfecho SET estado = 'enviado', registrada_em = '2026-11-17T10:01:00.000Z' WHERE id LIKE '%a'`, []);
    await trocarRadar(banco, 3, radar, AGORA);
    const restantes = await banco.getAllAsync<{ id: string }>('SELECT id FROM desfecho', []);
    expect(restantes.map((r) => r.id)).toEqual(['00000000-0000-4000-8000-00000000000b']);
  });
});
