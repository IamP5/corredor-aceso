// Cores, letras e espaçamentos do app. Segue o desenho das listas agrupadas do iOS
// e do Material 3 no Android. Os pares de texto e fundo ficam listados em PARES_DE_CONTRASTE,
// e o teste de contraste mede cada um contra 4,5:1.
import type { SituacaoNaTela } from '../dominio/textos';

export const COR = {
  fundo: '#F2F2F7',
  superficie: '#FFFFFF',
  fundoSuave: '#E9E9EF',
  texto: '#1C1C1E',
  textoSecundario: '#55555B',
  separador: '#C6C6C8',
  borda: '#8E8E93',
  primario: '#0B5CAD',
  sobrePrimario: '#FFFFFF',
  fundoPrimario: '#E3EDF8',
  perigo: '#B3261E',
  fundoPerigo: '#FBE9E7',
  aviso: '#7A4F00',
  fundoAviso: '#FFF3D1',
  ok: '#1E6B34',
  fundoOk: '#E4F3E8',
} as const;

/** Cor forte de cada situação: barra do card e texto do selo. */
export const COR_DA_SITUACAO: Record<SituacaoNaTela, string> = {
  Vermelho: '#B3261E',
  Cinza: '#4B5563',
  Aguardando: '#7A4F00',
  Verde: '#1E6B34',
};

/** Fundo claro do selo de cada situação. */
export const FUNDO_DA_SITUACAO: Record<SituacaoNaTela, string> = {
  Vermelho: COR.fundoPerigo,
  Cinza: '#ECEDF0',
  Aguardando: COR.fundoAviso,
  Verde: COR.fundoOk,
};

/** Tamanhos de letra. Nada fica abaixo de 17 pontos. */
export const TAMANHO = {
  destaque: 52,
  numero: 34,
  tituloGrande: 32,
  titulo: 26,
  subtitulo: 20,
  corpo: 18,
  pequeno: 17,
  toqueMinimo: 48,
} as const;

export const ESPACO = { pp: 4, p: 8, m: 16, g: 24 } as const;

/** Todo par de cor de texto e cor de fundo que aparece na tela. */
export const PARES_DE_CONTRASTE: { nome: string; texto: string; fundo: string }[] = [
  { nome: 'texto no fundo', texto: COR.texto, fundo: COR.fundo },
  { nome: 'texto secundário no fundo', texto: COR.textoSecundario, fundo: COR.fundo },
  { nome: 'texto na superfície', texto: COR.texto, fundo: COR.superficie },
  { nome: 'texto secundário na superfície', texto: COR.textoSecundario, fundo: COR.superficie },
  { nome: 'texto no fundo suave', texto: COR.texto, fundo: COR.fundoSuave },
  { nome: 'texto secundário no fundo suave', texto: COR.textoSecundario, fundo: COR.fundoSuave },
  { nome: 'botão principal', texto: COR.sobrePrimario, fundo: COR.primario },
  { nome: 'botão secundário', texto: COR.primario, fundo: COR.fundoPrimario },
  { nome: 'link na superfície', texto: COR.primario, fundo: COR.superficie },
  { nome: 'link no fundo', texto: COR.primario, fundo: COR.fundo },
  { nome: 'perigo na superfície', texto: COR.perigo, fundo: COR.superficie },
  { nome: 'linha de ação pressionada', texto: COR.primario, fundo: COR.fundoSuave },
  { nome: 'linha destrutiva pressionada', texto: COR.perigo, fundo: COR.fundoSuave },
  { nome: 'texto no fundo de perigo', texto: COR.texto, fundo: COR.fundoPerigo },
  { nome: 'título de perigo no fundo de perigo', texto: COR.perigo, fundo: COR.fundoPerigo },
  { nome: 'texto no fundo de aviso', texto: COR.texto, fundo: COR.fundoAviso },
  { nome: 'título de aviso no fundo de aviso', texto: COR.aviso, fundo: COR.fundoAviso },
  { nome: 'texto no fundo ok', texto: COR.texto, fundo: COR.fundoOk },
  { nome: 'título ok no fundo ok', texto: COR.ok, fundo: COR.fundoOk },
  { nome: 'texto no fundo primário', texto: COR.texto, fundo: COR.fundoPrimario },
  { nome: 'botão de perigo', texto: '#FFFFFF', fundo: COR.perigo },
  ...(Object.keys(COR_DA_SITUACAO) as SituacaoNaTela[]).flatMap((s) => [
    { nome: `selo ${s}`, texto: COR_DA_SITUACAO[s], fundo: FUNDO_DA_SITUACAO[s] },
    { nome: `número ${s} na superfície`, texto: COR_DA_SITUACAO[s], fundo: COR.superficie },
    { nome: `motivo ${s} na linha pressionada`, texto: COR_DA_SITUACAO[s], fundo: COR.fundoSuave },
    { nome: `ícone branco no quadrado ${s}`, texto: '#FFFFFF', fundo: COR_DA_SITUACAO[s] },
  ]),
];

/** Razão de contraste da WCAG 2.1 entre duas cores #RRGGBB. */
export function razaoDeContraste(a: string, b: string): number {
  const luminancia = (hex: string) => {
    const [r, g, bl] = [1, 3, 5].map((i) => {
      const c = parseInt(hex.slice(i, i + 2), 16) / 255;
      return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * bl;
  };
  const [claro, escuro] = [luminancia(a), luminancia(b)].sort((x, y) => y - x);
  return (claro + 0.05) / (escuro + 0.05);
}
