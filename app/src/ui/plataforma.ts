// Medidas que mudam com a plataforma. Ficam fora de tema.ts para que
// ferramentas/medir-contraste.ts leia as cores sem carregar o react-native.
import { isGlassEffectAPIAvailable, isLiquidGlassAvailable } from 'expo-glass-effect';
import { Platform } from 'react-native';

/** Liquid Glass só existe do iOS 26 em diante. No resto, as mesmas peças usam superfície sólida. */
export const VIDRO = Platform.OS === 'ios' && isLiquidGlassAvailable() && isGlassEffectAPIAvailable();

const RAIO_DO_CARTAO = Platform.OS === 'ios' ? 24 : 16;

/** Canto de uma peça que mora dentro de outra: o raio de fora menos o respiro entre as duas.
 *  Assim as duas curvas ficam paralelas, como nos cantos do iOS. */
export function raioInterno(externo: number, respiro: number): number {
  return Math.max(externo - respiro, 6);
}

export const RAIO = {
  cartao: RAIO_DO_CARTAO,
  /** Campo e quadrado de ícone ficam a 16 pontos da borda do cartão. */
  campo: raioInterno(RAIO_DO_CARTAO, 16),
  /** Botão e selo são cápsulas: o raio é sempre metade da altura. */
  botao: 999,
  selo: 999,
} as const;

/** Curva contínua da Apple (o "squircle") no lugar do arco de círculo. Só o iOS desenha; os outros ignoram. */
export const CANTO = { borderCurve: 'continuous' } as const;

/** Altura que a barra de ação de vidro cobre do conteúdo, sem contar a margem da barra de gestos.
 *  Cada botão tem 52 pontos e fica a 12 do vizinho e do topo da barra. */
export function alturaDaBarraDeVidro(botoes: number): number {
  return 12 + botoes * 52 + (botoes - 1) * 12;
}
