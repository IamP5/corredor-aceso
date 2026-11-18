// Peças de tela reutilizadas, no desenho nativo de cada plataforma: lista agrupada no iOS,
// ripple e botão em pílula no Android. Todo toque tem pelo menos 48 pontos e um rótulo para o leitor de tela.
import Ionicons from '@expo/vector-icons/Ionicons';
import { GlassView } from 'expo-glass-effect';
import * as Haptics from 'expo-haptics';
import { SymbolView, type SymbolViewProps } from 'expo-symbols';
import { Children, createContext, isValidElement, useContext, useEffect, useState, type ComponentProps, type ReactNode } from 'react';
import {
  ActivityIndicator,
  Keyboard,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
  type AccessibilityRole,
  type PressableProps,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import { LayoutAnimationConfig } from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ROTULO_DA_SITUACAO, type SituacaoNaTela } from '../dominio/textos';
import { COR, COR_DA_SITUACAO, ESPACO, FUNDO_DA_SITUACAO, TAMANHO } from './tema';
import { Salta, Surge, Toque } from './movimento';
import { alturaDaBarraDeVidro, CANTO, RAIO, VIDRO } from './plataforma';

export type NomeDoIcone = ComponentProps<typeof Ionicons>['name'];

/** Toque leve no iPhone e no Android. A web não tem motor de vibração. */
export function vibrar(tipo: 'toque' | 'selecao' | 'sucesso' | 'aviso' = 'toque') {
  if (Platform.OS === 'web') return;
  if (tipo === 'selecao') void Haptics.selectionAsync();
  else if (tipo === 'sucesso') void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
  else if (tipo === 'aviso') void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Warning);
  else void Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light);
}

/** O nome do Ionicons que cada tela usa e o SF Symbol que o iPhone desenha no lugar.
 *  Nome sem par aqui cai no Ionicons, como sempre acontece no Android e na web. */
const SIMBOLO_DO_IOS: Partial<Record<NomeDoIcone, SymbolViewProps['name']>> = {
  alert: 'exclamationmark',
  'alert-circle': 'exclamationmark.circle.fill',
  call: 'phone.fill',
  chatbubbles: 'bubble.left.and.bubble.right.fill',
  checkmark: 'checkmark',
  'checkmark-circle': 'checkmark.circle.fill',
  'checkmark-circle-outline': 'checkmark.circle',
  'chevron-forward': 'chevron.right',
  'clipboard-outline': 'doc.on.clipboard',
  'cloud-offline': 'antenna.radiowaves.left.and.right.slash',
  'copy-outline': 'doc.on.doc',
  'exit-outline': 'rectangle.portrait.and.arrow.right',
  flash: 'bolt.fill',
  'flash-off': 'bolt.slash.fill',
  heart: 'heart.fill',
  help: 'questionmark',
  'home-outline': 'house',
  'information-circle': 'info.circle.fill',
  'information-circle-outline': 'info.circle',
  list: 'list.bullet',
  'log-in-outline': 'arrow.right.circle.fill',
  'log-out-outline': 'rectangle.portrait.and.arrow.right',
  map: 'map.fill',
  medkit: 'cross.case.fill',
  moon: 'moon.fill',
  people: 'person.2.fill',
  person: 'person.fill',
  pulse: 'waveform.path.ecg',
  refresh: 'arrow.clockwise',
  'shield-checkmark-outline': 'checkmark.shield',
  'swap-horizontal': 'arrow.left.arrow.right',
  thermometer: 'thermometer',
  'thumbs-up': 'hand.thumbsup.fill',
  'time-outline': 'clock',
  walk: 'figure.walk',
  warning: 'exclamationmark.triangle.fill',
};

export function Icone({ nome, cor = COR.texto, tamanho = 22 }: { nome: NomeDoIcone; cor?: string; tamanho?: number }) {
  const simbolo = Platform.OS === 'ios' ? SIMBOLO_DO_IOS[nome] : undefined;
  if (simbolo) {
    return <SymbolView name={simbolo} tintColor={cor} size={tamanho} accessibilityElementsHidden importantForAccessibility="no" />;
  }
  return <Ionicons name={nome} color={cor} size={tamanho} accessibilityElementsHidden importantForAccessibility="no" />;
}

export function Titulo({ children }: { children: ReactNode }) {
  return (
    <Text accessibilityRole="header" style={estilo.titulo}>
      {children}
    </Text>
  );
}

export function Subtitulo({ children }: { children: ReactNode }) {
  return (
    <Text accessibilityRole="header" style={estilo.subtitulo}>
      {children}
    </Text>
  );
}

export function Paragrafo({ children, secundario = false }: { children: ReactNode; secundario?: boolean }) {
  return <Text style={[estilo.corpo, secundario && estilo.secundario]}>{children}</Text>;
}

type TipoDeBotao = 'principal' | 'secundario' | 'perigo';

const VISUAL_DO_BOTAO: Record<TipoDeBotao, { fundo: string; texto: string; ripple: string }> = {
  principal: { fundo: COR.primario, texto: COR.sobrePrimario, ripple: 'rgba(255,255,255,0.24)' },
  secundario: { fundo: COR.fundoPrimario, texto: COR.primario, ripple: 'rgba(11,92,173,0.16)' },
  perigo: { fundo: COR.perigo, texto: '#FFFFFF', ripple: 'rgba(255,255,255,0.24)' },
};

export function Botao(props: {
  rotulo: string;
  aoTocar: () => void;
  tipo?: TipoDeBotao;
  icone?: NomeDoIcone;
  dica?: string;
  ocupado?: boolean;
  desligado?: boolean;
  papel?: AccessibilityRole;
  selecionado?: boolean;
}) {
  const { rotulo, aoTocar, tipo = 'principal', icone, dica, ocupado = false, desligado = false, papel = 'button', selecionado } = props;
  const visual = VISUAL_DO_BOTAO[tipo];
  const parado = desligado || ocupado;
  const deVidro = useContext(SobreVidro);
  const acessibilidade = {
    accessibilityRole: papel,
    accessibilityLabel: rotulo,
    accessibilityHint: dica,
    accessibilityState: { disabled: parado, busy: ocupado, selected: selecionado },
    disabled: parado,
    onPress: () => {
      vibrar();
      aoTocar();
    },
  };
  const miolo = (
    <>
      {/* O ícone novo salta quando troca, como no "Copiada". Na primeira vez ele já chega parado. */}
      <LayoutAnimationConfig skipEntering>
        {ocupado ? (
          <ActivityIndicator color={visual.texto} />
        ) : icone ? (
          <Salta key={icone}>
            <Icone nome={icone} cor={visual.texto} tamanho={20} />
          </Salta>
        ) : null}
      </LayoutAnimationConfig>
      <Text style={[estilo.textoDoBotao, { color: visual.texto }]}>{rotulo}</Text>
    </>
  );
  // Na barra de ação do iOS 26 o botão é uma cápsula de vidro tingida, que flutua sobre o conteúdo.
  // O próprio vidro reage ao dedo, então aqui não entra o encolher do Toque.
  if (deVidro) {
    return (
      <GlassView glassEffectStyle="regular" tintColor={visual.fundo} isInteractive style={[estilo.capsulaDeVidro, desligado && { opacity: 0.5 }]}>
        <Pressable {...acessibilidade} style={estilo.botao}>
          {miolo}
        </Pressable>
      </GlassView>
    );
  }
  return (
    <Toque {...acessibilidade} android_ripple={{ color: visual.ripple }} style={[estilo.botao, { backgroundColor: visual.fundo }, desligado && { opacity: 0.45 }]}>
      {miolo}
    </Toque>
  );
}

/** Selo da situação: cor, símbolo e nome. A cor nunca fica sozinha. */
export function Selo({ situacao, atendida = false }: { situacao: SituacaoNaTela; atendida?: boolean }) {
  const r = ROTULO_DA_SITUACAO[situacao];
  return (
    <View style={[estilo.selo, { backgroundColor: FUNDO_DA_SITUACAO[situacao] }]} importantForAccessibility="no-hide-descendants">
      <Text style={[estilo.textoDoSelo, { color: COR_DA_SITUACAO[situacao] }]}>
        {r.simbolo} {r.nome}
        {atendida ? ' · atendida' : ''}
      </Text>
    </View>
  );
}

export type TipoDeFaixa = 'perigo' | 'aviso' | 'ok' | 'info';

const VISUAL_DA_FAIXA: Record<TipoDeFaixa, { fundo: string; titulo: string; icone: NomeDoIcone }> = {
  perigo: { fundo: COR.fundoPerigo, titulo: COR.perigo, icone: 'alert-circle' },
  aviso: { fundo: COR.fundoAviso, titulo: COR.aviso, icone: 'warning' },
  ok: { fundo: COR.fundoOk, titulo: COR.ok, icone: 'checkmark-circle' },
  info: { fundo: COR.fundoPrimario, titulo: COR.primario, icone: 'information-circle' },
};

/** Faixa de recado. O leitor de tela anuncia a mudança. */
export function Faixa({ tipo, titulo, children }: { tipo: TipoDeFaixa; titulo: string; children?: ReactNode }) {
  const visual = VISUAL_DA_FAIXA[tipo];
  return (
    <Surge>
      <View style={[estilo.faixa, { backgroundColor: visual.fundo }]} accessibilityLiveRegion="polite" accessible>
        <Icone nome={visual.icone} cor={visual.titulo} tamanho={24} />
        <View style={{ flex: 1, gap: 2 }}>
          <Text style={[estilo.tituloDaFaixa, { color: visual.titulo }]}>{titulo}</Text>
          {children ? <Text style={estilo.corpo}>{children}</Text> : null}
        </View>
      </View>
    </Surge>
  );
}

/** Seção de lista agrupada: título acima, linhas num cartão branco com divisória fina, nota abaixo. */
export function Grupo({ titulo, nota, children }: { titulo?: string; nota?: string; children: ReactNode }) {
  const linhas = Children.toArray(children).filter(isValidElement);
  return (
    <View style={{ gap: ESPACO.p }}>
      {titulo ? (
        <Text accessibilityRole="header" style={estilo.tituloDoGrupo}>
          {titulo}
        </Text>
      ) : null}
      <View style={estilo.grupo}>
        {linhas.map((linha, i) => (
          <View key={linha.key ?? i}>
            {i > 0 ? <View style={estilo.divisoria} /> : null}
            {linha}
          </View>
        ))}
      </View>
      {nota ? <Text style={[estilo.secundario, { paddingHorizontal: ESPACO.m }]}>{nota}</Text> : null}
    </View>
  );
}

/** Linha de lista: ícone num quadrado tingido, texto, valor à direita e seta quando abre outra tela.
 *  `corDoIcone` pinta o quadrado de cor cheia, para quando a cor diz alguma coisa. */
export function Linha(props: {
  titulo: string;
  detalhe?: string;
  valor?: string;
  icone?: NomeDoIcone;
  corDoIcone?: string;
  destrutiva?: boolean;
  abreTela?: boolean;
  ocupado?: boolean;
  dica?: string;
  /** O detalhe pode ser selecionado e copiado, como um telefone. */
  detalheSelecionavel?: boolean;
  aoTocar?: () => void;
}) {
  const { titulo, detalhe, valor, icone, corDoIcone, destrutiva = false, abreTela = false, ocupado = false, dica, detalheSelecionavel = false, aoTocar } = props;
  const corDoTitulo = destrutiva ? COR.perigo : aoTocar && !abreTela ? COR.primario : COR.texto;
  const quadrado = destrutiva
    ? { fundo: COR.fundoPerigo, icone: COR.perigo }
    : corDoIcone
      ? { fundo: corDoIcone, icone: '#FFFFFF' }
      : { fundo: COR.fundoPrimario, icone: COR.primario };
  const conteudo = (
    <>
      {icone ? (
        <View style={[estilo.quadradoDoIcone, { backgroundColor: quadrado.fundo }]}>
          <Icone nome={icone} cor={quadrado.icone} tamanho={18} />
        </View>
      ) : null}
      <View style={{ flex: 1, gap: 2 }}>
        <Text style={[estilo.tituloDaLinha, { color: corDoTitulo }]}>{titulo}</Text>
        {detalhe ? (
          <Text selectable={detalheSelecionavel} style={estilo.secundario}>
            {detalhe}
          </Text>
        ) : null}
      </View>
      {valor ? <Text style={estilo.secundario}>{valor}</Text> : null}
      {ocupado ? <ActivityIndicator color={COR.textoSecundario} /> : null}
      {abreTela && Platform.OS !== 'android' ? <Icone nome="chevron-forward" cor={COR.borda} tamanho={20} /> : null}
    </>
  );
  if (!aoTocar) {
    return (
      <View style={estilo.linha} accessible accessibilityLabel={[titulo, detalhe, valor].filter(Boolean).join('. ')}>
        {conteudo}
      </View>
    );
  }
  return (
    <ToqueDeLinha
      accessibilityRole="button"
      accessibilityLabel={[titulo, detalhe, valor].filter(Boolean).join('. ')}
      accessibilityHint={dica}
      accessibilityState={{ busy: ocupado, disabled: ocupado }}
      disabled={ocupado}
      onPress={() => {
        vibrar();
        aoTocar();
      }}
      style={estilo.linha}
    >
      {conteudo}
    </ToqueDeLinha>
  );
}

/** Toque de linha de lista: a linha não encolhe. Escurece o fundo no iPhone e na web e faz o ripple no Android. */
export function ToqueDeLinha({ style, children, ...resto }: Omit<PressableProps, 'style' | 'children'> & { style?: StyleProp<ViewStyle>; children: ReactNode }) {
  return (
    <Pressable
      {...resto}
      android_ripple={{ color: 'rgba(0,0,0,0.08)' }}
      style={({ pressed }) => [style, pressed && Platform.OS !== 'android' && { backgroundColor: COR.fundoSuave }]}
    >
      {children}
    </Pressable>
  );
}

/** Teclado aberto ou fechado, avisado antes da animação no iPhone. */
function useTecladoAberto(): boolean {
  const [aberto, setAberto] = useState(() => Keyboard.isVisible());
  useEffect(() => {
    const ios = Platform.OS === 'ios';
    const abriu = Keyboard.addListener(ios ? 'keyboardWillShow' : 'keyboardDidShow', () => setAberto(true));
    const fechou = Keyboard.addListener(ios ? 'keyboardWillHide' : 'keyboardDidHide', () => setAberto(false));
    return () => {
      abriu.remove();
      fechou.remove();
    };
  }, []);
  return aberto;
}

/** Diz ao Botao que ele está na barra de ação de vidro. */
const SobreVidro = createContext(false);

/** Respiro que o conteúdo rolável precisa no fim para não ficar preso embaixo da barra de vidro.
 *  Fora do iOS 26 a barra é sólida e fica abaixo do conteúdo, então o respiro é zero. */
export function useRespiroDaBarra(botoes = 1): number {
  const margem = useSafeAreaInsets();
  return VIDRO ? alturaDaBarraDeVidro(botoes) + Math.max(margem.bottom, ESPACO.m) : 0;
}

/** Barra presa embaixo da tela com a ação principal, ao alcance do polegar. Aceita um ou dois botões, o principal em cima.
 *  Fica fora do ScrollView. Com o teclado aberto ela sobe junto e larga a margem da barra de gestos.
 *  No iOS 26 ela não tem fundo: o botão de vidro flutua e o conteúdo rola por baixo dele. */
export function BarraDeAcao({ children }: { children: ReactNode }) {
  const margem = useSafeAreaInsets();
  const teclado = useTecladoAberto();
  const embaixo = teclado ? 12 : Math.max(margem.bottom, ESPACO.m);
  if (VIDRO) {
    const botoes = Children.toArray(children).filter(isValidElement).length;
    return (
      <View pointerEvents="box-none" style={[estilo.barraDeVidro, { paddingBottom: embaixo, marginTop: -(alturaDaBarraDeVidro(botoes) + embaixo) }]}>
        <SobreVidro.Provider value>{children}</SobreVidro.Provider>
      </View>
    );
  }
  return <View style={[estilo.barraDeAcao, { paddingBottom: embaixo }]}>{children}</View>;
}

/** Caixa de texto de várias linhas: superfície branca sem borda, com o contador de letras no canto.
 *  O título e a nota ficam fora dela, como numa seção de lista agrupada. */
export function CaixaDeTexto(props: {
  rotulo: string;
  valor: string;
  aoMudar: (texto: string) => void;
  limite: number;
  dicaNoCampo?: string;
  titulo?: string;
  nota?: string;
  alturaMinima?: number;
  focoAoAbrir?: boolean;
  aoFocar?: () => void;
  aoSairDoFoco?: () => void;
}) {
  const { rotulo, valor, aoMudar, limite, dicaNoCampo, titulo, nota, alturaMinima = 140, focoAoAbrir = false, aoFocar, aoSairDoFoco } = props;
  return (
    <View style={{ gap: ESPACO.p }}>
      {titulo ? (
        <Text accessibilityRole="header" style={estilo.tituloDoGrupo}>
          {titulo}
        </Text>
      ) : null}
      <View style={estilo.caixaDeTexto}>
        <TextInput
          accessibilityLabel={rotulo}
          accessibilityHint={nota}
          placeholder={dicaNoCampo}
          placeholderTextColor={COR.textoSecundario}
          value={valor}
          onChangeText={aoMudar}
          maxLength={limite}
          multiline
          autoFocus={focoAoAbrir}
          onFocus={aoFocar}
          onBlur={aoSairDoFoco}
          style={[estilo.textoDaCaixa, { minHeight: alturaMinima }]}
        />
        <Text
          accessibilityLabel={`${valor.length} de ${limite} caracteres`}
          style={[estilo.secundario, estilo.contadorDaCaixa, valor.length >= limite && { color: COR.perigo }]}
        >
          {`${valor.length}/${limite}`}
        </Text>
      </View>
      {nota ? <Text style={[estilo.secundario, { paddingHorizontal: ESPACO.m }]}>{nota}</Text> : null}
    </View>
  );
}

/** Cartão branco de conteúdo livre. */
export function Cartao({ children }: { children: ReactNode }) {
  return <View style={estilo.cartao}>{children}</View>;
}

export const estilo = StyleSheet.create({
  tela: { flex: 1, backgroundColor: COR.fundo },
  conteudo: { padding: ESPACO.m, gap: ESPACO.g, paddingBottom: ESPACO.g * 2 },
  titulo: { fontSize: TAMANHO.titulo, lineHeight: TAMANHO.titulo * 1.2, fontWeight: '700', color: COR.texto, letterSpacing: Platform.OS === 'ios' ? 0.2 : 0 },
  subtitulo: { fontSize: TAMANHO.subtitulo, lineHeight: TAMANHO.subtitulo * 1.25, fontWeight: '600', color: COR.texto },
  corpo: { fontSize: TAMANHO.corpo, lineHeight: TAMANHO.corpo * 1.4, color: COR.texto },
  secundario: { color: COR.textoSecundario, fontSize: TAMANHO.pequeno, lineHeight: TAMANHO.pequeno * 1.35 },
  botao: {
    minHeight: 52,
    borderRadius: RAIO.botao,
    paddingHorizontal: ESPACO.g,
    paddingVertical: 14,
    alignItems: 'center',
    justifyContent: 'center',
    flexDirection: 'row',
    gap: ESPACO.p,
    overflow: 'hidden',
  },
  capsulaDeVidro: { borderRadius: RAIO.botao, overflow: 'hidden' },
  textoDoBotao: { fontSize: TAMANHO.corpo, fontWeight: '600', textAlign: 'center' },
  selo: { alignSelf: 'flex-start', borderRadius: RAIO.selo, paddingHorizontal: 12, paddingVertical: 4 },
  textoDoSelo: { fontSize: TAMANHO.pequeno, fontWeight: '700' },
  faixa: { borderRadius: RAIO.cartao, ...CANTO, padding: ESPACO.m, gap: 12, flexDirection: 'row', alignItems: 'flex-start' },
  tituloDaFaixa: { fontSize: TAMANHO.corpo, fontWeight: '700' },
  tituloDoGrupo: {
    fontSize: TAMANHO.pequeno,
    fontWeight: '600',
    color: Platform.OS === 'android' ? COR.primario : COR.textoSecundario,
    paddingHorizontal: ESPACO.m,
  },
  grupo: { backgroundColor: COR.superficie, borderRadius: RAIO.cartao, ...CANTO, overflow: 'hidden' },
  divisoria: { height: StyleSheet.hairlineWidth, backgroundColor: COR.separador, marginLeft: ESPACO.m },
  linha: {
    minHeight: 56,
    paddingHorizontal: ESPACO.m,
    paddingVertical: 12,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    backgroundColor: COR.superficie,
  },
  tituloDaLinha: { fontSize: TAMANHO.corpo, fontWeight: '500' },
  quadradoDoIcone: {
    width: 32,
    height: 32,
    borderRadius: Platform.OS === 'android' ? 16 : RAIO.campo,
    ...CANTO,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cartao: {
    borderRadius: RAIO.cartao,
    ...CANTO,
    padding: ESPACO.m,
    gap: ESPACO.p,
    backgroundColor: COR.superficie,
    minHeight: TAMANHO.toqueMinimo,
  },
  campo: {
    minHeight: 52,
    borderWidth: 1,
    borderColor: COR.borda,
    borderRadius: RAIO.campo,
    ...CANTO,
    paddingHorizontal: ESPACO.m,
    paddingVertical: 12,
    fontSize: TAMANHO.corpo,
    color: COR.texto,
    backgroundColor: COR.superficie,
  },
  caixaDeTexto: { backgroundColor: COR.superficie, borderRadius: RAIO.cartao, ...CANTO, paddingTop: 12, overflow: 'hidden' },
  textoDaCaixa: {
    paddingHorizontal: ESPACO.m,
    paddingTop: 0,
    paddingBottom: ESPACO.p,
    fontSize: TAMANHO.corpo,
    lineHeight: TAMANHO.corpo * 1.4,
    color: COR.texto,
    textAlignVertical: 'top',
  },
  contadorDaCaixa: { alignSelf: 'flex-end', paddingHorizontal: ESPACO.m, paddingBottom: 12, fontVariant: ['tabular-nums'] },
  rotuloDoCampo: { fontSize: TAMANHO.pequeno, fontWeight: '600', color: COR.texto },
  barraDeVidro: { paddingHorizontal: ESPACO.m, paddingTop: 12, gap: 12 },
  barraDeAcao: {
    gap: 12,
    paddingHorizontal: ESPACO.m,
    paddingTop: 12,
    backgroundColor: COR.fundo,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: COR.separador,
  },
});
