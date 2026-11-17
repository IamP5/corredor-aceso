// Cache da lista de nomes, do radar e dos avisos. Seção 6.2 de 04-modelo-de-dados.md.
// Cada recurso é trocado inteiro numa transação. Quem chama só passa resposta 200 já validada.
import type { Aviso, Desfecho, ItemDaListaDeNomes, MotivoDoAlerta, PerfilDaCasa, Radar } from '../api/contratos';
import { paraUtc } from '../dominio/hora';
import type { Banco } from './banco';

export type Recurso = 'lista_de_nomes' | 'radar' | 'avisos';
export type EstadoDoDesfecho = 'pendente' | 'enviado' | 'recusado';

const UM_DIA_MS = 24 * 60 * 60 * 1000;

async function marcarBaixado(banco: Banco, recurso: Recurso, baixadoEm: Date) {
  await banco.runAsync(
    `INSERT INTO sincronizacao (recurso, baixado_em) VALUES (?, ?)
     ON CONFLICT (recurso) DO UPDATE SET baixado_em = excluded.baixado_em`,
    [recurso, baixadoEm.toISOString()],
  );
}

const utcOuNulo = (iso: string | null) => (iso === null ? null : paraUtc(iso));
const bit = (b: boolean) => (b ? 1 : 0);

export async function trocarListaDeNomes(banco: Banco, itens: ItemDaListaDeNomes[], baixadoEm: Date) {
  await banco.withTransactionAsync(async () => {
    await banco.runAsync('DELETE FROM lista_de_nomes', []);
    for (const i of itens) {
      await banco.runAsync(
        `INSERT INTO lista_de_nomes (casa_token, nome, endereco, vizinho_nome, vizinho_telefone)
         VALUES (?, ?, ?, ?, ?)`,
        [i.casaToken, i.nome, i.endereco, i.vizinho?.nome ?? null, i.vizinho?.telefone ?? null],
      );
    }
    await marcarBaixado(banco, 'lista_de_nomes', baixadoEm);
  });
}

export async function trocarRadar(banco: Banco, microarea: number, radar: Radar, baixadoEm: Date) {
  await banco.withTransactionAsync(async () => {
    await banco.runAsync('DELETE FROM radar_card', []);
    await banco.runAsync(
      `INSERT INTO radar_cabecalho (id, microarea_id, turno_aberto, agora_do_servidor, verdes)
       VALUES (1, ?, ?, ?, ?)
       ON CONFLICT (id) DO UPDATE SET microarea_id = excluded.microarea_id, turno_aberto = excluded.turno_aberto,
         agora_do_servidor = excluded.agora_do_servidor, verdes = excluded.verdes`,
      [microarea, bit(radar.turnoAberto), paraUtc(radar.agora), radar.verdes],
    );
    const listas = [
      ['pendente', radar.pendentes],
      ['atendida', radar.atendidas],
    ] as const;
    for (const [lista, cards] of listas) {
      for (const [ordem, c] of cards.entries()) {
        await banco.runAsync(
          `INSERT INTO radar_card (casa_token, lista, ordem, situacao, motivo, desde, perfil_contato,
             eletrodependente, ultimo_pacote_em, saida_avisada)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NULL)`,
          [
            c.casaToken,
            lista,
            ordem,
            c.situacao,
            c.motivo,
            paraUtc(c.desde),
            c.perfil.contato,
            bit(c.perfil.eletrodependente),
            utcOuNulo(c.ultimoPacoteEm),
          ],
        );
      }
    }
    for (const [ordem, c] of radar.aguardando.entries()) {
      await banco.runAsync(
        `INSERT INTO radar_card (casa_token, lista, ordem, situacao, saida_avisada)
         VALUES (?, 'aguardando', ?, 'Aguardando', ?)`,
        [c.casaToken, ordem, bit(c.saidaAvisada)],
      );
    }
    await marcarBaixado(banco, 'radar', baixadoEm);
    // A cada sincronização do radar, os desfechos enviados há mais de 24 horas saem do celular.
    await banco.runAsync(`DELETE FROM desfecho WHERE estado = 'enviado' AND registrada_em < ?`, [
      new Date(baixadoEm.getTime() - UM_DIA_MS).toISOString(),
    ]);
  });
}

export async function trocarAvisos(banco: Banco, avisos: Aviso[], baixadoEm: Date) {
  await banco.withTransactionAsync(async () => {
    await banco.runAsync('DELETE FROM aviso', []);
    for (const a of avisos) {
      await banco.runAsync(
        `INSERT INTO aviso (id, casa_token, motivo, desde, registrado_em, resposta, respondido_em)
         VALUES (?, ?, ?, ?, ?, ?, ?)`,
        [a.id, a.casaToken, a.motivo, paraUtc(a.desde), paraUtc(a.registradoEm), a.resposta, utcOuNulo(a.respondidoEm)],
      );
    }
    await marcarBaixado(banco, 'avisos', baixadoEm);
  });
}

export async function lerSincronizacao(banco: Banco): Promise<Partial<Record<Recurso, string>>> {
  const linhas = await banco.getAllAsync<{ recurso: Recurso; baixado_em: string }>(
    'SELECT recurso, baixado_em FROM sincronizacao',
    [],
  );
  return Object.fromEntries(linhas.map((l) => [l.recurso, l.baixado_em]));
}

export interface Pessoa {
  nome: string;
  endereco: string;
  vizinho: { nome: string; telefone: string } | null;
}

export interface UltimoDesfecho {
  desfecho: Desfecho;
  em: string;
  estado: EstadoDoDesfecho;
}

interface BaseDoCard {
  casaToken: string;
  /** `null` quando o token não está na lista de nomes. O card aparece mesmo assim. */
  pessoa: Pessoa | null;
  ultimoDesfecho: UltimoDesfecho | null;
}

export type CardNaTela =
  | (BaseDoCard & {
      lista: 'pendente' | 'atendida';
      situacao: 'Vermelho' | 'Cinza';
      motivo: MotivoDoAlerta;
      desde: string;
      perfil: PerfilDaCasa;
      ultimoPacoteEm: string | null;
      aviso: { em: string; resposta: 'EstaBem' | 'PrecisaDeAjuda' | null } | null;
    })
  | (BaseDoCard & { lista: 'aguardando'; situacao: 'Aguardando'; saidaAvisada: boolean });

export interface CabecalhoDoRadar {
  microareaId: number;
  turnoAberto: boolean;
  agoraDoServidor: string;
  verdes: number;
  baixadoEm: string;
}

interface LinhaDoRadar {
  lista: CardNaTela['lista'];
  casa_token: string;
  situacao: CardNaTela['situacao'];
  motivo: MotivoDoAlerta | null;
  desde: string | null;
  perfil_contato: PerfilDaCasa['contato'] | null;
  eletrodependente: number | null;
  ultimo_pacote_em: string | null;
  saida_avisada: number | null;
  nome: string | null;
  endereco: string | null;
  vizinho_nome: string | null;
  vizinho_telefone: string | null;
  ultimo_desfecho: Desfecho | null;
  desfecho_em: string | null;
  estado_do_desfecho: EstadoDoDesfecho | null;
  vizinho_avisado_em: string | null;
  resposta_do_vizinho: 'EstaBem' | 'PrecisaDeAjuda' | null;
}

// Consulta de docs/arquitetura/referencia/dados/sqlite/radar_na_tela.sql.
// É o único lugar em que o token da casa encontra o nome da pessoa.
const RADAR_NA_TELA = `
SELECT c.lista, c.ordem, c.casa_token, c.situacao, c.motivo, c.desde,
	c.perfil_contato, c.eletrodependente, c.ultimo_pacote_em, c.saida_avisada,
	n.nome, n.endereco, n.vizinho_nome, n.vizinho_telefone,
	d.desfecho AS ultimo_desfecho, d.ocorrida_em AS desfecho_em, d.estado AS estado_do_desfecho,
	a.registrado_em AS vizinho_avisado_em, a.resposta AS resposta_do_vizinho
FROM radar_card c
LEFT JOIN lista_de_nomes n ON n.casa_token = c.casa_token
LEFT JOIN desfecho d ON d.id = (
	SELECT id FROM desfecho
	WHERE casa_token = c.casa_token
	ORDER BY ocorrida_em DESC LIMIT 1)
LEFT JOIN aviso a ON a.casa_token = c.casa_token AND a.motivo = c.motivo AND a.desde = c.desde
ORDER BY CASE c.lista WHEN 'pendente' THEN 0 WHEN 'atendida' THEN 1 ELSE 2 END, c.ordem`;

function paraCard(l: LinhaDoRadar): CardNaTela {
  const base: BaseDoCard = {
    casaToken: l.casa_token,
    pessoa:
      l.nome === null || l.endereco === null
        ? null
        : {
            nome: l.nome,
            endereco: l.endereco,
            vizinho:
              l.vizinho_nome === null || l.vizinho_telefone === null
                ? null
                : { nome: l.vizinho_nome, telefone: l.vizinho_telefone },
          },
    ultimoDesfecho:
      l.ultimo_desfecho === null || l.desfecho_em === null || l.estado_do_desfecho === null
        ? null
        : { desfecho: l.ultimo_desfecho, em: l.desfecho_em, estado: l.estado_do_desfecho },
  };
  if (l.lista === 'aguardando') {
    return { ...base, lista: 'aguardando', situacao: 'Aguardando', saidaAvisada: l.saida_avisada === 1 };
  }
  // O CHECK da tabela garante que um card com alerta tem motivo, desde e perfil.
  return {
    ...base,
    lista: l.lista,
    situacao: l.situacao as 'Vermelho' | 'Cinza',
    motivo: l.motivo as MotivoDoAlerta,
    desde: l.desde as string,
    perfil: { contato: l.perfil_contato as PerfilDaCasa['contato'], eletrodependente: l.eletrodependente === 1 },
    ultimoPacoteEm: l.ultimo_pacote_em,
    aviso: l.vizinho_avisado_em === null ? null : { em: l.vizinho_avisado_em, resposta: l.resposta_do_vizinho },
  };
}

export async function lerRadarNaTela(
  banco: Banco,
): Promise<{ cabecalho: CabecalhoDoRadar | null; cards: CardNaTela[] }> {
  const cab = await banco.getFirstAsync<{
    microarea_id: number;
    turno_aberto: number;
    agora_do_servidor: string;
    verdes: number;
    baixado_em: string | null;
  }>(
    `SELECT r.microarea_id, r.turno_aberto, r.agora_do_servidor, r.verdes, s.baixado_em
     FROM radar_cabecalho r LEFT JOIN sincronizacao s ON s.recurso = 'radar' WHERE r.id = 1`,
    [],
  );
  const linhas = await banco.getAllAsync<LinhaDoRadar>(RADAR_NA_TELA, []);
  return {
    cabecalho:
      cab === null
        ? null
        : {
            microareaId: cab.microarea_id,
            turnoAberto: cab.turno_aberto === 1,
            agoraDoServidor: cab.agora_do_servidor,
            verdes: cab.verdes,
            baixadoEm: cab.baixado_em ?? cab.agora_do_servidor,
          },
    cards: linhas.map(paraCard),
  };
}

export async function pessoaDaCasa(banco: Banco, casaToken: string): Promise<Pessoa | null> {
  const l = await banco.getFirstAsync<{
    nome: string;
    endereco: string;
    vizinho_nome: string | null;
    vizinho_telefone: string | null;
  }>('SELECT nome, endereco, vizinho_nome, vizinho_telefone FROM lista_de_nomes WHERE casa_token = ?', [casaToken]);
  if (!l) return null;
  return {
    nome: l.nome,
    endereco: l.endereco,
    vizinho: l.vizinho_nome && l.vizinho_telefone ? { nome: l.vizinho_nome, telefone: l.vizinho_telefone } : null,
  };
}
