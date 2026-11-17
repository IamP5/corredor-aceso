// Fila de desfechos. Seção 6.3 de 04-modelo-de-dados.md.
// O id nasce no celular e é a chave de idempotência de POST /api/visitas.
import type { ApiCorredor } from '../api/cliente';
import type { Desfecho } from '../api/contratos';
import { decidirEnvio } from '../dominio/envio';
import { isoDeSp } from '../dominio/hora';
import type { Banco } from './banco';
import type { EstadoDoDesfecho } from './cache';

export interface DesfechoNoCelular {
  id: string;
  casaToken: string;
  ocorridaEm: string;
  desfecho: Desfecho;
  observacao: string | null;
  estado: EstadoDoDesfecho;
  tentativas: number;
  ultimoErro: string | null;
  registradaEm: string | null;
}

interface LinhaDeDesfecho {
  id: string;
  casa_token: string;
  ocorrida_em: string;
  desfecho: Desfecho;
  observacao: string | null;
  estado: EstadoDoDesfecho;
  tentativas: number;
  ultimo_erro: string | null;
  registrada_em: string | null;
}

const paraDesfecho = (l: LinhaDeDesfecho): DesfechoNoCelular => ({
  id: l.id,
  casaToken: l.casa_token,
  ocorridaEm: l.ocorrida_em,
  desfecho: l.desfecho,
  observacao: l.observacao,
  estado: l.estado,
  tentativas: l.tentativas,
  ultimoErro: l.ultimo_erro,
  registradaEm: l.registrada_em,
});

/** Grava o desfecho antes de qualquer chamada de rede. `ocorridaEm` é a hora do toque. */
export async function registrarDesfecho(
  banco: Banco,
  novo: { id: string; casaToken: string; desfecho: Desfecho; observacao: string | null; ocorridaEm: Date },
): Promise<void> {
  const observacao = novo.observacao?.trim() ? novo.observacao.trim().slice(0, 500) : null;
  await banco.runAsync(
    `INSERT INTO desfecho (id, casa_token, ocorrida_em, desfecho, observacao) VALUES (?, ?, ?, ?, ?)`,
    [novo.id, novo.casaToken, novo.ocorridaEm.toISOString(), novo.desfecho, observacao],
  );
}

export async function desfechosDaCasa(banco: Banco, casaToken: string): Promise<DesfechoNoCelular[]> {
  const linhas = await banco.getAllAsync<LinhaDeDesfecho>(
    'SELECT * FROM desfecho WHERE casa_token = ? ORDER BY ocorrida_em DESC',
    [casaToken],
  );
  return linhas.map(paraDesfecho);
}

export async function contarPendentes(banco: Banco): Promise<number> {
  const l = await banco.getFirstAsync<{ n: number }>(`SELECT count(*) AS n FROM desfecho WHERE estado = 'pendente'`, []);
  return l?.n ?? 0;
}

/** A ACS dispensa um desfecho recusado depois de ler o motivo. */
export async function dispensarRecusado(banco: Banco, id: string): Promise<void> {
  await banco.runAsync(`DELETE FROM desfecho WHERE id = ? AND estado = 'recusado'`, [id]);
}

export interface ResultadoDoEnvio {
  enviados: number;
  recusados: number;
  /** Por que a fila parou antes de esvaziar. `null` quando esvaziou. */
  parouPor: null | { tipo: 'tentarDeNovo'; codigo: string } | { tipo: 'pedirEntrada' };
}

const emAndamento = new WeakMap<Banco, Promise<ResultadoDoEnvio>>();

/**
 * Envia a linha pendente mais antiga, uma por vez, até a fila esvaziar ou uma falha
 * pedir para esperar. Duas chamadas ao mesmo tempo dividem o mesmo envio.
 */
export function enviarFila(banco: Banco, api: ApiCorredor): Promise<ResultadoDoEnvio> {
  const atual = emAndamento.get(banco);
  if (atual) return atual;
  const envio = esvaziar(banco, api).finally(() => emAndamento.delete(banco));
  emAndamento.set(banco, envio);
  return envio;
}

async function esvaziar(banco: Banco, api: ApiCorredor): Promise<ResultadoDoEnvio> {
  const resultado: ResultadoDoEnvio = { enviados: 0, recusados: 0, parouPor: null };
  for (;;) {
    const linha = await banco.getFirstAsync<LinhaDeDesfecho>(
      `SELECT * FROM desfecho WHERE estado = 'pendente' ORDER BY ocorrida_em, id LIMIT 1`,
      [],
    );
    if (!linha) return resultado;

    let registradaEm: string | null = null;
    let erro: unknown = null;
    try {
      const resposta = await api.registrarVisita({
        id: linha.id,
        casaToken: linha.casa_token,
        ocorridaEm: isoDeSp(new Date(linha.ocorrida_em)),
        desfecho: linha.desfecho,
        observacao: linha.observacao,
      });
      registradaEm = new Date(resposta.registradaEm).toISOString();
    } catch (e) {
      erro = e;
    }

    const decisao = decidirEnvio(erro);
    switch (decisao.tipo) {
      case 'enviado':
        await banco.runAsync(
          `UPDATE desfecho SET estado = 'enviado', registrada_em = ?, observacao = NULL, ultimo_erro = NULL WHERE id = ?`,
          [registradaEm, linha.id],
        );
        resultado.enviados += 1;
        break;
      case 'recusado':
        await banco.runAsync(
          `UPDATE desfecho SET estado = 'recusado', tentativas = tentativas + 1, ultimo_erro = ? WHERE id = ?`,
          [decisao.codigo, linha.id],
        );
        resultado.recusados += 1;
        break;
      case 'tentarDeNovo':
        await banco.runAsync(`UPDATE desfecho SET tentativas = tentativas + 1, ultimo_erro = ? WHERE id = ?`, [
          decisao.codigo,
          linha.id,
        ]);
        resultado.parouPor = decisao;
        return resultado;
      case 'pedirEntrada':
        resultado.parouPor = decisao;
        return resultado;
    }
  }
}
