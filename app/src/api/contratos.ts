// Contratos de docs/arquitetura/03-modelo-de-dominio.md, seção 6.3.
// Cada resposta da API passa por um destes esquemas antes de entrar no app.
import { z } from 'zod';

export const casaToken = z.string().regex(/^[0-9a-f]{32}$/);
const hora = z.string().min(1);

export const perfilDeContato = z.enum(['Sozinho', 'Acamado', 'Coabitado']);
export type PerfilDeContato = z.infer<typeof perfilDeContato>;

export const perfilDaCasa = z.object({
  contato: perfilDeContato,
  eletrodependente: z.boolean(),
});
export type PerfilDaCasa = z.infer<typeof perfilDaCasa>;

export const motivoDoAlerta = z.enum([
  'EnergiaEletrodependente',
  'SemHeartbeat',
  'CalorInterno',
  'SemMovimentoNaJanela',
]);
export type MotivoDoAlerta = z.infer<typeof motivoDoAlerta>;

export const cardComAlerta = z.object({
  casaToken,
  situacao: z.enum(['Vermelho', 'Cinza']),
  motivo: motivoDoAlerta,
  desde: hora,
  atendida: z.boolean(),
  perfil: perfilDaCasa,
  ultimoPacoteEm: hora.nullable(),
});
export type CardComAlerta = z.infer<typeof cardComAlerta>;

export const cardAguardando = z.object({
  casaToken,
  saidaAvisada: z.boolean(),
});
export type CardAguardando = z.infer<typeof cardAguardando>;

export const radar = z.object({
  turnoAberto: z.boolean(),
  agora: hora,
  pendentes: z.array(cardComAlerta),
  atendidas: z.array(cardComAlerta),
  aguardando: z.array(cardAguardando),
  verdes: z.number().int().nonnegative(),
});
export type Radar = z.infer<typeof radar>;

export const tipoDeEvento = z.enum(['MOVIMENTO', 'BOTAO_BEM', 'BOTAO_SAIR', 'TELEMETRIA']);
export type TipoDeEvento = z.infer<typeof tipoDeEvento>;

export const origemDaConfirmacao = z.enum(['Acs', 'Vizinho']);
export const resultadoDaConfirmacao = z.enum(['PessoaVista', 'AusenciaConfirmada']);

export const linhaDoTempo = z.object({
  casaToken,
  ultimoPacoteEm: hora.nullable(),
  eventos: z.array(
    z.object({
      seq: z.number().int(),
      tipo: tipoDeEvento,
      ocorridoEm: hora,
      recebidoEm: hora,
      tempC: z.number().nullable(),
      tensaoV: z.number().nullable(),
      bufferizado: z.boolean(),
    }),
  ),
  confirmacoes: z.array(
    z.object({
      origem: origemDaConfirmacao,
      resultado: resultadoDaConfirmacao,
      quando: hora,
    }),
  ),
});
export type LinhaDoTempo = z.infer<typeof linhaDoTempo>;
export type EventoDaLinha = LinhaDoTempo['eventos'][number];
export type ConfirmacaoDaLinha = LinhaDoTempo['confirmacoes'][number];

export const itemDaListaDeNomes = z.object({
  casaToken,
  nome: z.string().min(1),
  endereco: z.string().min(1),
  vizinho: z.object({ nome: z.string(), telefone: z.string() }).nullable(),
});
export const listaDeNomes = z.array(itemDaListaDeNomes);
export type ItemDaListaDeNomes = z.infer<typeof itemDaListaDeNomes>;

export const desfecho = z.enum(['EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada']);
export type Desfecho = z.infer<typeof desfecho>;

export interface NovaVisita {
  id: string;
  casaToken: string;
  ocorridaEm: string;
  desfecho: Desfecho;
  observacao: string | null;
}

export const visitaRegistrada = z.object({ id: z.string(), registradaEm: hora });
export type VisitaRegistrada = z.infer<typeof visitaRegistrada>;

export const canal = z.enum(['GrupoDoBairro']);
export type Canal = z.infer<typeof canal>;

export const verificacaoDeMensagem = z.discriminatedUnion('veredito', [
  z.object({
    id: z.string(),
    veredito: z.literal('Liberada'),
    textoMascarado: z.string(),
    tiposDeDado: z.array(z.string()),
    explicacao: z.null(),
    versaoSegura: z.null(),
  }),
  z.object({
    id: z.string(),
    veredito: z.literal('Bloqueada'),
    textoMascarado: z.string(),
    tiposDeDado: z.array(z.string()),
    explicacao: z.string(),
    versaoSegura: z.string(),
  }),
]);
export type VerificacaoDeMensagem = z.infer<typeof verificacaoDeMensagem>;

export const encerramento = z.object({
  data: z.string(),
  encerradoEm: hora,
  resumo: z.object({
    casasAtivas: z.number().int(),
    comSinalDeVidaAte10h: z.number().int(),
    atendidas: z.number().int(),
    vermelhasPendentes: z.number().int(),
    cinzasPendentes: z.number().int(),
  }),
});
export type Encerramento = z.infer<typeof encerramento>;

export const erroDaApi = z.object({ codigo: z.string(), mensagem: z.string() });

export const avisos = z.array(
  z.object({
    id: z.string(),
    casaToken,
    motivo: motivoDoAlerta,
    desde: hora,
    registradoEm: hora,
    resposta: z.enum(['EstaBem', 'PrecisaDeAjuda']).nullable(),
    respondidoEm: hora.nullable(),
  }),
);
export type Aviso = z.infer<typeof avisos>[number];
