// Mock da API com os mesmos contratos da seção 6.3. Ele guarda estado em memória:
// uma visita registrada muda o radar seguinte, como a API real faria.
// O cenário é o fluxo 3 do event storming às 10h31 de um dia crítico aberto às 05h.
// Este arquivo não importa nada do React Native, então o servidor mock HTTP também o usa.
import { ErroDeRede, ErroHttp, type ApiCorredor } from '../cliente';
import type {
  Aviso,
  CardAguardando,
  CardComAlerta,
  ConfirmacaoDaLinha,
  Desfecho,
  Encerramento,
  EventoDaLinha,
  MotivoDoAlerta,
  NovaVisita,
  Radar,
  VerificacaoDeMensagem,
  VisitaRegistrada,
} from '../contratos';
import { dataDeSp, hojeAs, isoDeSp } from '../../dominio/hora';
import { CASAS_DO_MOCK, MICROAREA_DO_MOCK, type CasaDoMock } from './casas';

export interface OpcoesDoMock {
  /** Quando devolve false, toda chamada falha como se não houvesse rede. */
  conectado?: () => boolean;
  /** Relógio real. O teste passa um relógio fixo. */
  agoraReal?: () => Date;
  latenciaMs?: number;
}

type Card =
  | { lista: 'pendente' | 'atendida'; card: CardComAlerta }
  | { lista: 'aguardando'; card: CardAguardando };

const TOLERANCIA_DO_RELOGIO_MS = 2 * 60 * 1000;

export function criarApiMock(opcoes: OpcoesDoMock = {}): ApiCorredor {
  const conectado = opcoes.conectado ?? (() => true);
  const agoraReal = opcoes.agoraReal ?? (() => new Date());
  const latenciaMs = opcoes.latenciaMs ?? 0;

  // Relógio de demo: começa às 10h31 de hoje e anda junto com o relógio real.
  const inicioReal = agoraReal();
  const inicioDemo = hojeAs(inicioReal, 10, 31);
  const agoraDemo = () => new Date(inicioDemo.getTime() + (agoraReal().getTime() - inicioReal.getTime()));
  const as = (h: number, m: number, s = 0) => isoDeSp(hojeAs(inicioReal, h, m, s));

  const casaPorN = (n: number) => CASAS_DO_MOCK[n - 1];
  const casaPorToken = new Map(CASAS_DO_MOCK.map((c) => [c.casaToken, c]));

  const comAlerta = (
    casa: CasaDoMock,
    situacao: CardComAlerta['situacao'],
    motivo: MotivoDoAlerta,
    desde: string,
    ultimoPacoteEm: string | null,
    atendida = false,
  ): CardComAlerta => ({ casaToken: casa.casaToken, situacao, motivo, desde, atendida, perfil: casa.perfil, ultimoPacoteEm });

  const cards = new Map<string, Card>();
  const ordem: string[] = [];
  const por = (c: Card) => {
    cards.set(c.card.casaToken, c);
    ordem.push(c.card.casaToken);
  };
  por({ lista: 'pendente', card: comAlerta(casaPorN(2), 'Vermelho', 'EnergiaEletrodependente', as(9, 52), as(10, 30, 40)) });
  por({ lista: 'pendente', card: comAlerta(casaPorN(7), 'Vermelho', 'SemMovimentoNaJanela', as(10, 0), as(10, 30, 52)) });
  por({ lista: 'pendente', card: comAlerta(casaPorN(10), 'Cinza', 'SemHeartbeat', as(9, 10), as(9, 10)) });
  por({ lista: 'atendida', card: comAlerta(casaPorN(14), 'Cinza', 'SemHeartbeat', as(8, 40), as(8, 40), true) });
  por({ lista: 'aguardando', card: { casaToken: casaPorN(4).casaToken, saidaAvisada: true } });
  for (const n of [9, 15, 3, 12, 22, 32, 36, 39]) {
    por({ lista: 'aguardando', card: { casaToken: casaPorN(n).casaToken, saidaAvisada: false } });
  }
  let verdes = CASAS_DO_MOCK.length - cards.size;
  let turnoAberto = true;
  let encerramento: Encerramento | null = null;

  const confirmacoes = new Map<string, ConfirmacaoDaLinha[]>([
    [casaPorN(14).casaToken, [{ origem: 'Acs', resultado: 'PessoaVista', quando: as(10, 20) }]],
  ]);
  const visitas = new Map<string, VisitaRegistrada>();

  // Avisos ao vizinho que o Cadastro mandou hoje. Só casas com vizinho indicado.
  const aviso = (n: number, motivo: MotivoDoAlerta, desde: string, registradoEm: string, respondidoEm: string | null): Aviso => ({
    id: idDaMensagem(`aviso-${n}-${desde}`),
    casaToken: casaPorN(n).casaToken,
    motivo,
    desde,
    registradoEm,
    resposta: respondidoEm ? 'EstaBem' : null,
    respondidoEm,
  });
  const avisosDoDia: Aviso[] = [
    aviso(2, 'EnergiaEletrodependente', as(9, 52), as(9, 52, 12), as(10, 2)),
    aviso(5, 'SemMovimentoNaJanela', as(10, 0), as(10, 0, 12), as(10, 5)),
    aviso(6, 'SemMovimentoNaJanela', as(10, 0), as(10, 0, 12), null),
    aviso(7, 'SemMovimentoNaJanela', as(10, 0), as(10, 0, 12), null),
    aviso(10, 'SemHeartbeat', as(9, 10), as(9, 40, 12), null),
  ];

  async function chamada<T>(fazer: () => T): Promise<T> {
    if (latenciaMs > 0) await new Promise((r) => setTimeout(r, latenciaMs));
    if (!conectado()) throw new ErroDeRede('sem rede no mock');
    return fazer();
  }

  function listar(lista: Card['lista']) {
    return ordem.map((t) => cards.get(t)).filter((c): c is Card => c?.lista === lista);
  }

  function moverPara(token: string, novo: Card) {
    ordem.splice(ordem.indexOf(token), 1);
    por(novo);
  }

  function virouVerde(token: string) {
    cards.delete(token);
    ordem.splice(ordem.indexOf(token), 1);
    if (turnoAberto) verdes += 1;
  }

  /** Efeito da visita no radar: fluxo 7 do event storming e seção 5.7 do modelo de domínio. */
  function aplicarVisita(token: string, desfecho: Desfecho, quando: string) {
    const pessoaVista = desfecho === 'EstavaBem' || desfecho === 'Encaminhada';
    if (desfecho === 'NaoAbriu') return;
    const lista = confirmacoes.get(token) ?? [];
    lista.push({ origem: 'Acs', resultado: pessoaVista ? 'PessoaVista' : 'AusenciaConfirmada', quando });
    confirmacoes.set(token, lista);

    const atual = cards.get(token);
    if (!atual) return;
    if (atual.lista === 'aguardando') {
      if (pessoaVista) virouVerde(token);
      return;
    }
    const semMovimento = atual.card.motivo === 'SemMovimentoNaJanela';
    if (pessoaVista && semMovimento) virouVerde(token);
    else if (pessoaVista !== semMovimento) {
      moverPara(token, { lista: 'atendida', card: { ...atual.card, atendida: true } });
    }
  }

  function eventosDa(casa: CasaDoMock): EventoDaLinha[] {
    const eventos: EventoDaLinha[] = [];
    let seq = 1800 + casa.n * 10;
    const evento = (tipo: EventoDaLinha['tipo'], h: number, m: number, extra: Partial<EventoDaLinha> = {}) => {
      eventos.push({
        seq: seq++,
        tipo,
        ocorridoEm: as(h, m),
        recebidoEm: as(h, m, 1),
        tempC: null,
        tensaoV: null,
        bufferizado: false,
        ...extra,
      });
    };
    const telemetria = (h: number, m: number, tempC: number, tensaoV: number) =>
      evento('TELEMETRIA', h, m, { tempC, tensaoV });

    switch (casa.n) {
      case 2:
        telemetria(8, 0, 29.5, 127);
        telemetria(9, 0, 30.1, 126);
        telemetria(9, 52, 30.4, 0);
        telemetria(10, 30, 30.9, 0);
        break;
      case 4:
        evento('MOVIMENTO', 6, 40);
        evento('BOTAO_SAIR', 7, 30);
        telemetria(9, 0, 29.8, 127);
        break;
      case 7:
        evento('MOVIMENTO', 3, 12);
        telemetria(8, 0, 30.2, 127);
        telemetria(10, 0, 31.0, 127);
        break;
      case 10:
        evento('MOVIMENTO', 8, 10);
        telemetria(9, 0, 29.9, 127);
        break;
      case 14:
        evento('MOVIMENTO', 7, 5);
        telemetria(8, 0, 29.4, 127);
        evento('MOVIMENTO', 8, 38, { recebidoEm: as(8, 40), bufferizado: true });
        break;
      default:
        if (casa.perfil.contato === 'Sozinho') {
          evento('MOVIMENTO', 6, 12 + (casa.n % 40));
          evento('BOTAO_BEM', 8, 5 + (casa.n % 30));
        }
        telemetria(9, 0, 29 + (casa.n % 3), 127);
        if (casa.perfil.contato === 'Sozinho' && casa.n !== 9 && casa.n !== 15) evento('MOVIMENTO', 10, 10 + (casa.n % 15));
    }
    return eventos;
  }

  function verificar(texto: string): VerificacaoDeMensagem {
    const regras: { tipo: string; mascara: string; padrao: RegExp; frase: string }[] = [
      { tipo: 'Cpf', mascara: '[CPF]', padrao: /\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b/g, frase: 'traz o documento da pessoa' },
      { tipo: 'Telefone', mascara: '[TELEFONE]', padrao: /\(?\d{2}\)?\s?9?\d{4}-?\d{4}/g, frase: 'traz um telefone' },
      { tipo: 'MoraSozinho', mascara: '[MORA_SOZINHO]', padrao: /mora sozinh[oa]/gi, frase: 'diz que a pessoa mora sozinha' },
      {
        tipo: 'Saude',
        mascara: '[SAUDE]',
        padrao: /diur[ée]tico|rem[ée]dio|insulina|diabet\w*|press[ãa]o alta|oxig[êe]nio|acamad[oa]|hipertens\w*/gi,
        frase: 'fala da saúde dela',
      },
      { tipo: 'Endereco', mascara: '[ENDERECO]', padrao: /\b(rua|avenida|travessa)\s[\wÀ-ú ]+,?\s?\d+/gi, frase: 'diz onde a pessoa mora' },
    ];
    let mascarado = texto;
    const achados = regras.filter((r) => {
      const achou = r.padrao.test(texto);
      r.padrao.lastIndex = 0;
      if (achou) mascarado = mascarado.replace(r.padrao, r.mascara);
      return achou;
    });
    const id = idDaMensagem(texto);
    if (achados.length === 0) {
      return { id, veredito: 'Liberada', textoMascarado: texto, tiposDeDado: [], explicacao: null, versaoSegura: null };
    }
    const frases = achados.map((a) => a.frase);
    const juntas = frases.length === 1 ? frases[0] : `${frases.slice(0, -1).join(', ')} e ${frases[frases.length - 1]}`;
    return {
      id,
      veredito: 'Bloqueada',
      textoMascarado: mascarado,
      tiposDeDado: achados.map((a) => a.tipo),
      explicacao: `Essa mensagem ${juntas}. No grupo do bairro, qualquer um lê.`,
      versaoSegura: 'Preciso de ajuda para uma visita agora. Quem puder, me chame no privado.',
    };
  }

  return {
    radar: (microarea) =>
      chamada((): Radar => {
        if (microarea !== MICROAREA_DO_MOCK) throw new ErroHttp(403, 'MICROAREA_DE_OUTRA_ACS');
        const comCard = (l: 'pendente' | 'atendida') =>
          listar(l).map((c) => c.card as CardComAlerta);
        return {
          turnoAberto,
          agora: isoDeSp(agoraDemo()),
          pendentes: comCard('pendente'),
          atendidas: comCard('atendida'),
          aguardando: listar('aguardando').map((c) => c.card as CardAguardando),
          verdes,
        };
      }),

    linhaDoTempo: (token) =>
      chamada(() => {
        const casa = casaPorToken.get(token);
        if (!casa) throw new ErroHttp(404, 'CASA_DESCONHECIDA');
        const card = cards.get(token);
        const eventos = eventosDa(casa);
        const ultimo = card?.lista !== 'aguardando' && card ? card.card.ultimoPacoteEm : eventos.at(-1)?.recebidoEm;
        return {
          casaToken: token,
          ultimoPacoteEm: ultimo ?? null,
          eventos,
          confirmacoes: confirmacoes.get(token) ?? [],
        };
      }),

    listaDeNomes: (microarea) =>
      chamada(() => {
        if (microarea !== MICROAREA_DO_MOCK) throw new ErroHttp(403, 'MICROAREA_DE_OUTRA_ACS');
        return CASAS_DO_MOCK.map(({ casaToken, nome, endereco, vizinho }) => ({ casaToken, nome, endereco, vizinho }));
      }),

    registrarVisita: (visita: NovaVisita) =>
      chamada(() => {
        const repetida = visitas.get(visita.id);
        if (repetida) return repetida;
        if (!casaPorToken.has(visita.casaToken)) throw new ErroHttp(422, 'TOKEN_INATIVO');
        if (new Date(visita.ocorridaEm).getTime() > agoraReal().getTime() + TOLERANCIA_DO_RELOGIO_MS) {
          throw new ErroHttp(422, 'VISITA_NO_FUTURO');
        }
        const registrada = { id: visita.id, registradaEm: isoDeSp(agoraDemo()) };
        visitas.set(visita.id, registrada);
        aplicarVisita(visita.casaToken, visita.desfecho, isoDeSp(agoraDemo()));
        return registrada;
      }),

    verificarMensagem: (_canal, texto) => chamada(() => verificar(texto)),

    avisos: (microarea) =>
      chamada(() => {
        if (microarea !== MICROAREA_DO_MOCK) throw new ErroHttp(403, 'MICROAREA_DE_OUTRA_ACS');
        return avisosDoDia;
      }),

    encerrarTurno: (data) =>
      chamada((): Encerramento => {
        if (data !== dataDeSp(agoraDemo())) throw new ErroHttp(404, 'DIA_CRITICO_DESCONHECIDO');
        if (encerramento) return encerramento;
        const pendentes = listar('pendente').map((c) => c.card as CardComAlerta);
        encerramento = {
          data,
          encerradoEm: isoDeSp(agoraDemo()),
          resumo: {
            casasAtivas: CASAS_DO_MOCK.length,
            comSinalDeVidaAte10h: CASAS_DO_MOCK.length - 6,
            atendidas: listar('atendida').length,
            vermelhasPendentes: pendentes.filter((c) => c.situacao === 'Vermelho').length,
            cinzasPendentes: pendentes.filter((c) => c.situacao === 'Cinza').length,
          },
        };
        // Fora do turno só valem energia e silêncio do aparelho. Ninguém fica verde.
        turnoAberto = false;
        verdes = 0;
        for (const casa of CASAS_DO_MOCK) {
          const card = cards.get(casa.casaToken);
          const ficaComAlerta =
            card && card.lista !== 'aguardando' && ['EnergiaEletrodependente', 'SemHeartbeat'].includes(card.card.motivo);
          if (!ficaComAlerta) {
            if (card) ordem.splice(ordem.indexOf(casa.casaToken), 1);
            por({ lista: 'aguardando', card: { casaToken: casa.casaToken, saidaAvisada: false } });
          }
        }
        return encerramento;
      }),
  };
}

function idDaMensagem(texto: string): string {
  let h = 0;
  for (const c of texto) h = (h * 31 + c.charCodeAt(0)) >>> 0;
  const hex = h.toString(16).padStart(8, '0');
  return `${hex}-0000-4000-8000-${hex}${hex.slice(0, 4)}`;
}
