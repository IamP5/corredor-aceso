// Todo texto que explica uma situação, um motivo ou um desfecho sai daqui.
// A cor nunca aparece sozinha: cada situação tem nome, símbolo e frase.
import type { Desfecho, MotivoDoAlerta, PerfilDaCasa, TipoDeEvento } from '../api/contratos';
import type { CardNaTela, EstadoDoDesfecho } from '../banco/cache';
import { horaCurta } from './hora';

export type SituacaoNaTela = 'Vermelho' | 'Cinza' | 'Aguardando' | 'Verde';

export interface RotuloDaSituacao {
  /** Palavra que aparece no selo do card. */
  nome: string;
  /** Símbolo que acompanha a cor, para quem não distingue cores. */
  simbolo: string;
  /** O que a ACS deve fazer, em linguagem simples. */
  acao: string;
}

export const ROTULO_DA_SITUACAO: Record<SituacaoNaTela, RotuloDaSituacao> = {
  Vermelho: { nome: 'Urgente', simbolo: '!', acao: 'Precisa de visita agora.' },
  Cinza: { nome: 'Aparelho sem sinal', simbolo: '?', acao: 'Não dá para saber como a pessoa está. Precisa de visita.' },
  Aguardando: { nome: 'Aguardando', simbolo: '…', acao: 'Sem alerta agora. Não precisa ir.' },
  Verde: { nome: 'Deu sinal hoje', simbolo: '✓', acao: 'A pessoa deu sinal de vida nesta parte do dia.' },
};

/** Frase do motivo, com a hora em que o alerta começou. */
export function textoDoMotivo(motivo: MotivoDoAlerta, desde: string, ultimoPacoteEm: string | null = null): string {
  const h = horaCurta(desde);
  switch (motivo) {
    case 'EnergiaEletrodependente':
      return `Sem energia na casa desde ${h}. A pessoa usa aparelho de saúde na tomada.`;
    case 'SemHeartbeat':
      return `Aparelho sem sinal desde ${horaCurta(ultimoPacoteEm ?? desde)}. Não sabemos se a pessoa está bem.`;
    case 'CalorInterno':
      return `Calor forte dentro de casa desde ${h}.`;
    case 'SemMovimentoNaJanela':
      return `Ninguém passou no corredor. Sem sinal de vida desde ${h}.`;
  }
}

/** Motivo em poucas palavras, para a linha do radar. Mesma regra de hora de textoDoMotivo. */
export function motivoCurto(motivo: MotivoDoAlerta, desde: string, ultimoPacoteEm: string | null = null): string {
  const h = horaCurta(motivo === 'SemHeartbeat' ? (ultimoPacoteEm ?? desde) : desde);
  const frase: Record<MotivoDoAlerta, string> = {
    EnergiaEletrodependente: `Sem energia desde ${h}`,
    SemHeartbeat: `Aparelho sem sinal desde ${h}`,
    CalorInterno: `Calor forte em casa desde ${h}`,
    SemMovimentoNaJanela: `Sem movimento desde ${h}`,
  };
  return frase[motivo];
}

export function textoDoPerfil(perfil: PerfilDaCasa): string {
  const contato = { Sozinho: 'A pessoa mora só', Acamado: 'A pessoa está acamada', Coabitado: 'A pessoa mora com mais alguém' }[perfil.contato];
  return perfil.eletrodependente ? `${contato}. Usa aparelho de saúde na tomada.` : `${contato}.`;
}

export const TEXTO_DO_DESFECHO: Record<Desfecho, { nome: string; explicacao: string }> = {
  EstavaBem: { nome: 'Estava bem', explicacao: 'Vi a pessoa e ela está bem.' },
  EstavaFora: { nome: 'Estava fora', explicacao: 'Fui até a casa e a pessoa tinha saído.' },
  NaoAbriu: { nome: 'Não abriu, chamei o SAMU', explicacao: 'Ninguém atendeu e chamei o SAMU.' },
  Encaminhada: { nome: 'Encaminhei', explicacao: 'Vi a pessoa e encaminhei para a UBS ou o hospital.' },
};

/** O que aparece no card depois de a ACS registrar um desfecho. */
export function textoDoDesfechoNoCard(desfecho: Desfecho, em: string, estado: EstadoDoDesfecho): string {
  const base = desfecho === 'NaoAbriu' ? `SAMU chamado às ${horaCurta(em)}` : `${TEXTO_DO_DESFECHO[desfecho].nome} às ${horaCurta(em)}`;
  const envio = { pendente: 'Ainda não enviado, sai sozinho quando a conexão voltar.', enviado: 'Enviado.', recusado: 'Não enviado.' }[estado];
  return `${base}. ${envio}`;
}

/** Motivo da recusa, em texto. `ultimo_erro` guarda só o código. */
export function textoDoErro(codigo: string | null): string {
  switch (codigo) {
    case 'TOKEN_INATIVO':
      return 'Esta casa saiu do monitoramento. Fale com a UBS.';
    case 'VISITA_NO_FUTURO':
      return 'A hora do celular está errada. Acerte o relógio e registre de novo.';
    case 'SEM_CONEXAO':
      return 'Sem conexão com o sistema. O app tenta de novo sozinho.';
    case 'MOTOR_INDISPONIVEL':
      return 'O sistema está fora do ar. O app tenta de novo sozinho.';
    case 'CAMPO_INVALIDO':
      return 'Algum campo ficou errado. Registre de novo.';
    case null:
      return '';
    default:
      return 'O sistema não aceitou. Fale com a UBS.';
  }
}

export function textoDoAviso(card: Extract<CardNaTela, { lista: 'pendente' | 'atendida' }>): string | null {
  if (!card.aviso) return null;
  const nome = card.pessoa?.vizinho?.nome;
  const avisado = `Vizinho de confiança${nome ? ` (${nome})` : ''} avisado às ${horaCurta(card.aviso.em)}`;
  if (card.aviso.resposta === 'EstaBem') {
    // A regra de energia só aceita a visita da ACS (5.7 do modelo de domínio).
    const soAcs = card.motivo === 'EnergiaEletrodependente' && card.lista === 'pendente' ? ' Mesmo assim, só a sua visita tira este alerta.' : '';
    return `${avisado} e respondeu que a pessoa está bem.${soAcs}`;
  }
  if (card.aviso.resposta === 'PrecisaDeAjuda') return `${avisado} e pediu ajuda.`;
  return `${avisado} e ainda não respondeu.`;
}

export const TEXTO_DO_EVENTO: Record<TipoDeEvento, string> = {
  MOVIMENTO: 'Passou no corredor',
  BOTAO_BEM: 'Apertou o botão: estou bem',
  BOTAO_SAIR: 'Apertou duas vezes: vou sair',
  TELEMETRIA: 'Leitura do aparelho',
};

/** Título do radar. Radar com mais de 2 minutos se declara velho. */
export function tituloDoRadar(baixadoEm: string, agora: Date): { titulo: string; velho: boolean } {
  const velho = agora.getTime() - new Date(baixadoEm).getTime() > 2 * 60 * 1000;
  return {
    titulo: velho ? `Radar das ${horaCurta(baixadoEm)}, sem conexão` : 'Radar de agora',
    velho,
  };
}

export function textoDosVerdes(verdes: number, baixadoEm: string, velho: boolean): string {
  const casas = verdes === 1 ? '1 casa deu sinal' : `${verdes} casas deram sinal`;
  return velho ? `${casas} até ${horaCurta(baixadoEm)}` : `${casas} nesta parte do dia`;
}
