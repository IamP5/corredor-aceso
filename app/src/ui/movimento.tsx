// Movimento do app. As durações e curvas seguem o Material 3 e as molas seguem o iOS.
// O Reanimated respeita "Reduzir movimento" do sistema: com a opção ligada, nada desliza nem cresce.
import { useEffect, useState, type ReactNode } from 'react';
import { Platform, Pressable, StyleSheet, type PressableProps, type StyleProp, type ViewStyle } from 'react-native';
import Animated, {
  Easing,
  FadeIn,
  FadeInDown,
  FadeOut,
  LinearTransition,
  useAnimatedStyle,
  useReducedMotion,
  useSharedValue,
  withRepeat,
  withSpring,
  withTiming,
  ZoomIn,
} from 'react-native-reanimated';

/** Nenhuma entrada passa de 300 ms. */
export const DURACAO = { rapida: 150, media: 250, lenta: 300 } as const;
/** Curva "emphasized decelerate" do Material 3: entra rápido e pousa devagar. */
export const CURVA = Easing.bezier(0.05, 0.7, 0.1, 1);
/** Mola curta e sem balanço, como a dos botões do iOS. */
export const MOLA = { damping: 22, stiffness: 320, mass: 0.7 } as const;
/** Tempo que a barra leva para encher. O número que a acompanha conta no mesmo ritmo. */
const ENCHE = 900;

/** Quando um vizinho aparece ou some, os outros deslizam para o lugar. */
export const ACOMODA = LinearTransition.duration(DURACAO.media).easing(CURVA);
const ENTRA_SUAVE = FadeIn.duration(DURACAO.media).easing(CURVA);
const SAI_SUAVE = FadeOut.duration(DURACAO.rapida);

// Na web, o Reanimated quebra ao limpar uma entrada feita sob medida (valores iniciais próprios ou mola)
// quando o bloco sai da tela antes de a animação acabar. Lá ficam as entradas de fábrica.
const NA_WEB = Platform.OS === 'web';

const PressableAnimado = Animated.createAnimatedComponent(Pressable);

/** Botão que encolhe um pouco sob o dedo. Só botões: linha de lista usa ToqueDeLinha, que não encolhe. */
export function Toque({
  escala = 0.97,
  style,
  onPressIn,
  onPressOut,
  children,
  ...resto
}: Omit<PressableProps, 'style' | 'children'> & {
  escala?: number;
  style?: StyleProp<ViewStyle>;
  children: ReactNode;
}) {
  const pressao = useSharedValue(0);
  // A opacidade animada entra por cima da do estilo. Sem multiplicar, o botão desligado pareceria ligado.
  const opacidadeDoEstilo = StyleSheet.flatten(style)?.opacity;
  const base = typeof opacidadeDoEstilo === 'number' ? opacidadeDoEstilo : 1;
  const animado = useAnimatedStyle(() => ({
    transform: [{ scale: 1 - (1 - escala) * pressao.value }],
    opacity: Platform.OS === 'android' ? base : base * (1 - 0.12 * pressao.value),
  }));
  return (
    <PressableAnimado
      {...resto}
      onPressIn={(e) => {
        pressao.value = withTiming(1, { duration: 90 });
        onPressIn?.(e);
      }}
      onPressOut={(e) => {
        pressao.value = withSpring(0, MOLA);
        onPressOut?.(e);
      }}
      style={[style, animado]}
    >
      {children}
    </PressableAnimado>
  );
}

/** Bloco que sobe e aparece ao abrir a tela. `ordem` atrasa cada bloco um pouco, em cascata. */
export function Entra({ ordem = 0, style, children }: { ordem?: number; style?: StyleProp<ViewStyle>; children: ReactNode }) {
  const cascata = FadeInDown.duration(DURACAO.lenta)
    .delay(Math.min(ordem, 8) * 45)
    .easing(CURVA);
  return (
    <Animated.View
      entering={
        NA_WEB
          ? cascata
          : cascata.withInitialValues({
              opacity: 0,
              transform: [{ translateY: 12 }],
            })
      }
      layout={NA_WEB ? undefined : ACOMODA}
      style={style}
    >
      {children}
    </Animated.View>
  );
}

/** Bloco que aparece e some no lugar: avisos, resultados e erros. */
export function Surge({ style, children }: { style?: StyleProp<ViewStyle>; children: ReactNode }) {
  return (
    <Animated.View entering={ENTRA_SUAVE} exiting={SAI_SUAVE} layout={ACOMODA} style={style}>
      {children}
    </Animated.View>
  );
}

/** Marca que salta ao ser escolhida: o visto do iOS e o miolo do rádio do Android. */
export function Salta({ style, children }: { style?: StyleProp<ViewStyle>; children: ReactNode }) {
  return (
    <Animated.View
      entering={NA_WEB ? ZoomIn.duration(DURACAO.media) : ZoomIn.springify().damping(MOLA.damping).stiffness(MOLA.stiffness).mass(MOLA.mass)}
      exiting={SAI_SUAVE}
      style={style}
    >
      {children}
    </Animated.View>
  );
}

/** Barra de progresso que enche até `fracao`, de 0 a 1. */
export function Barra({ fracao, cor, trilho, altura = 8 }: { fracao: number; cor: string; trilho: string; altura?: number }) {
  const cheio = useSharedValue(0);
  useEffect(() => {
    cheio.value = withTiming(Math.max(0, Math.min(1, fracao)), {
      duration: ENCHE,
      easing: CURVA,
    });
  }, [cheio, fracao]);
  const animado = useAnimatedStyle(() => ({ width: `${cheio.value * 100}%` }));
  return (
    <Animated.View
      style={{
        height: altura,
        borderRadius: altura / 2,
        backgroundColor: trilho,
        overflow: 'hidden',
      }}
    >
      <Animated.View style={[{ height: altura, borderRadius: altura / 2, backgroundColor: cor }, animado]} />
    </Animated.View>
  );
}

/** Número que conta de 0 até `alvo` junto com a Barra. Com "Reduzir movimento", mostra o valor final direto. */
export function useContagem(alvo: number): number {
  const reduzido = useReducedMotion();
  const [valor, setValor] = useState(0);
  useEffect(() => {
    if (reduzido) return;
    const curva = CURVA.factory();
    const inicio = Date.now();
    let quadro = requestAnimationFrame(function passo() {
      const t = Math.min(1, (Date.now() - inicio) / ENCHE);
      setValor(Math.round(alvo * curva(t)));
      if (t < 1) quadro = requestAnimationFrame(passo);
    });
    return () => cancelAnimationFrame(quadro);
  }, [alvo, reduzido]);
  return reduzido ? alvo : valor;
}

/** Retângulo que pulsa enquanto o conteúdo carrega. */
export function Esqueleto({ largura, altura = 18, cor }: { largura: ViewStyle['width']; altura?: number; cor: string }) {
  const pulso = useSharedValue(1);
  useEffect(() => {
    pulso.value = withRepeat(withTiming(0.45, { duration: 800, easing: Easing.inOut(Easing.quad) }), -1, true);
  }, [pulso]);
  const animado = useAnimatedStyle(() => ({ opacity: pulso.value }));
  return (
    <Animated.View
      style={[
        {
          width: largura,
          height: altura,
          borderRadius: altura / 2,
          backgroundColor: cor,
        },
        animado,
      ]}
    />
  );
}
