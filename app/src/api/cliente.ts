// O app fala com a API por esta interface. Existem duas implementações:
// a HTTP, para a API real, e o mock, que segue os mesmos contratos.
import type { z } from 'zod';
import {
  avisos,
  encerramento,
  erroDaApi,
  linhaDoTempo,
  listaDeNomes,
  radar,
  verificacaoDeMensagem,
  visitaRegistrada,
  type Aviso,
  type Canal,
  type Encerramento,
  type ItemDaListaDeNomes,
  type LinhaDoTempo,
  type NovaVisita,
  type Radar,
  type VerificacaoDeMensagem,
  type VisitaRegistrada,
} from './contratos';

export interface ApiCorredor {
  radar(microarea: number): Promise<Radar>;
  linhaDoTempo(casaToken: string): Promise<LinhaDoTempo>;
  listaDeNomes(microarea: number): Promise<ItemDaListaDeNomes[]>;
  registrarVisita(visita: NovaVisita): Promise<VisitaRegistrada>;
  verificarMensagem(canal: Canal, texto: string): Promise<VerificacaoDeMensagem>;
  encerrarTurno(data: string, microarea: number): Promise<Encerramento>;
  avisos(microarea: number): Promise<Aviso[]>;
}

/** A API respondeu com erro. `codigo` vem do corpo `{codigo, mensagem}` quando existe. */
export class ErroHttp extends Error {
  constructor(
    readonly status: number,
    readonly codigo: string,
  ) {
    super(`HTTP ${status} ${codigo}`);
  }
}

/** A resposta não chegou: sem rede, servidor fora do ar ou tempo esgotado. */
export class ErroDeRede extends Error {
  readonly codigo = 'SEM_CONEXAO';
}

const TEMPO_MAXIMO_MS = 10_000;

/** `token` devolve o código de acesso da sessão, mandado como está em `Authorization: Bearer`. */
export function criarApiHttp(urlBase: string, token: () => string | null): ApiCorredor {
  const base = urlBase.replace(/\/+$/, '');

  async function pedir<T>(
    metodo: 'GET' | 'POST',
    caminho: string,
    esquema: z.ZodType<T>,
    corpo?: unknown,
  ): Promise<T> {
    const controle = new AbortController();
    const relogio = setTimeout(() => controle.abort(), TEMPO_MAXIMO_MS);
    const codigo = token();
    let resposta: Response;
    try {
      resposta = await fetch(`${base}/api${caminho}`, {
        method: metodo,
        headers: {
          Accept: 'application/json',
          ...(corpo === undefined ? {} : { 'Content-Type': 'application/json' }),
          ...(codigo ? { Authorization: `Bearer ${codigo}` } : {}),
        },
        body: corpo === undefined ? undefined : JSON.stringify(corpo),
        signal: controle.signal,
      });
    } catch {
      throw new ErroDeRede('sem resposta da API');
    } finally {
      clearTimeout(relogio);
    }
    const texto = await resposta.text();
    const json: unknown = texto ? JSON.parse(texto) : null;
    if (!resposta.ok) {
      const erro = erroDaApi.safeParse(json);
      throw new ErroHttp(resposta.status, erro.success ? erro.data.codigo : `HTTP_${resposta.status}`);
    }
    return esquema.parse(json);
  }

  return {
    radar: (microarea) => pedir('GET', `/radar?microarea=${microarea}`, radar),
    linhaDoTempo: (token) => pedir('GET', `/casas/${token}/linha-do-tempo`, linhaDoTempo),
    listaDeNomes: (microarea) => pedir('GET', `/lista-de-nomes?microarea=${microarea}`, listaDeNomes),
    registrarVisita: (visita) => pedir('POST', '/visitas', visitaRegistrada, visita),
    verificarMensagem: (canal, texto) =>
      pedir('POST', '/mensagens/verificar', verificacaoDeMensagem, { canal, texto }),
    encerrarTurno: (data, microarea) =>
      pedir('POST', `/dias-criticos/${data}/encerramento?microarea=${microarea}`, encerramento),
    avisos: (microarea) => pedir('GET', `/avisos?microarea=${microarea}`, avisos),
  };
}
