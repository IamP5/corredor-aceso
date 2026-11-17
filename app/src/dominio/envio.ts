// O que a fila faz com a resposta de POST /api/visitas.
// Tabela da seção 6.3 de docs/arquitetura/04-modelo-de-dados.md.
import { ErroDeRede, ErroHttp } from '../api/cliente';

export type DecisaoDeEnvio =
  | { tipo: 'enviado' }
  | { tipo: 'tentarDeNovo'; codigo: string }
  | { tipo: 'pedirEntrada' }
  | { tipo: 'recusado'; codigo: string };

/** `erro` é `null` quando a API respondeu 200 ou 201. */
export function decidirEnvio(erro: unknown): DecisaoDeEnvio {
  if (erro === null) return { tipo: 'enviado' };
  if (erro instanceof ErroDeRede) return { tipo: 'tentarDeNovo', codigo: erro.codigo };
  if (erro instanceof ErroHttp) {
    if (erro.status === 401) return { tipo: 'pedirEntrada' };
    if (erro.status >= 500) return { tipo: 'tentarDeNovo', codigo: erro.codigo };
    if (erro.status >= 400) return { tipo: 'recusado', codigo: erro.codigo };
  }
  // Resposta 2xx que não segue o contrato: a visita pode ter sido gravada, então o
  // app repete o mesmo id, que a API trata como repetição.
  return { tipo: 'tentarDeNovo', codigo: 'RESPOSTA_INVALIDA' };
}
