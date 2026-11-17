// Peças de tela reutilizadas. Todo toque tem pelo menos 48 pontos e um rótulo para o leitor de tela.
import type { ReactNode } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View, type AccessibilityRole } from 'react-native';
import { ROTULO_DA_SITUACAO, type SituacaoNaTela } from '../dominio/textos';
import { COR, COR_DA_SITUACAO, ESPACO, TAMANHO } from './tema';

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

export function Botao(props: {
  rotulo: string;
  aoTocar: () => void;
  tipo?: TipoDeBotao;
  dica?: string;
  ocupado?: boolean;
  desligado?: boolean;
  papel?: AccessibilityRole;
  selecionado?: boolean;
}) {
  const { rotulo, aoTocar, tipo = 'principal', dica, ocupado = false, desligado = false, papel = 'button', selecionado } = props;
  const corDoTexto = tipo === 'secundario' ? COR.primario : COR.sobrePrimario;
  return (
    <Pressable
      accessibilityRole={papel}
      accessibilityLabel={rotulo}
      accessibilityHint={dica}
      accessibilityState={{ disabled: desligado || ocupado, busy: ocupado, selected: selecionado }}
      disabled={desligado || ocupado}
      onPress={aoTocar}
      style={({ pressed }) => [
        estilo.botao,
        tipo === 'principal' && { backgroundColor: COR.primario },
        tipo === 'perigo' && { backgroundColor: COR.perigo },
        tipo === 'secundario' && estilo.botaoSecundario,
        (desligado || pressed) && { opacity: 0.75 },
      ]}
    >
      {ocupado ? <ActivityIndicator color={corDoTexto} /> : null}
      <Text style={[estilo.textoDoBotao, { color: corDoTexto }]}>{rotulo}</Text>
    </Pressable>
  );
}

/** Selo da situação: cor, símbolo e nome. A cor nunca fica sozinha. */
export function Selo({ situacao, atendida = false }: { situacao: SituacaoNaTela; atendida?: boolean }) {
  const r = ROTULO_DA_SITUACAO[situacao];
  return (
    <View style={[estilo.selo, { backgroundColor: COR_DA_SITUACAO[situacao] }]} importantForAccessibility="no-hide-descendants">
      <Text style={estilo.textoDoSelo}>
        {r.simbolo} {r.nome}
        {atendida ? ' · atendida' : ''}
      </Text>
    </View>
  );
}

export type TipoDeFaixa = 'perigo' | 'aviso' | 'ok' | 'info';

/** Faixa de recado no alto da tela. O leitor de tela anuncia a mudança. */
export function Faixa({ tipo, titulo, children }: { tipo: TipoDeFaixa; titulo: string; children?: ReactNode }) {
  const fundo = { perigo: COR.fundoPerigo, aviso: COR.fundoAviso, ok: COR.fundoOk, info: COR.fundoSuave }[tipo];
  const corDoTitulo = { perigo: COR.perigo, aviso: COR.texto, ok: COR.ok, info: COR.texto }[tipo];
  return (
    <View style={[estilo.faixa, { backgroundColor: fundo }]} accessibilityLiveRegion="polite" accessible>
      <Text style={[estilo.tituloDaFaixa, { color: corDoTitulo }]}>{titulo}</Text>
      {children ? <Text style={estilo.corpo}>{children}</Text> : null}
    </View>
  );
}

export const estilo = StyleSheet.create({
  tela: { flex: 1, backgroundColor: COR.fundo },
  conteudo: { padding: ESPACO.m, gap: ESPACO.m, paddingBottom: ESPACO.g * 2 },
  titulo: { fontSize: TAMANHO.titulo, fontWeight: '700', color: COR.texto },
  subtitulo: { fontSize: TAMANHO.subtitulo, fontWeight: '700', color: COR.texto, marginTop: ESPACO.p },
  corpo: { fontSize: TAMANHO.corpo, lineHeight: TAMANHO.corpo * 1.4, color: COR.texto },
  secundario: { color: COR.textoSecundario, fontSize: TAMANHO.pequeno },
  botao: {
    minHeight: TAMANHO.toqueMinimo,
    borderRadius: 10,
    paddingHorizontal: ESPACO.m,
    paddingVertical: 12,
    alignItems: 'center',
    justifyContent: 'center',
    flexDirection: 'row',
    gap: ESPACO.p,
  },
  botaoSecundario: { backgroundColor: COR.fundo, borderWidth: 2, borderColor: COR.primario },
  textoDoBotao: { fontSize: TAMANHO.corpo, fontWeight: '700', textAlign: 'center' },
  selo: { alignSelf: 'flex-start', borderRadius: 6, paddingHorizontal: 10, paddingVertical: 4 },
  textoDoSelo: { color: '#FFFFFF', fontSize: TAMANHO.pequeno, fontWeight: '700' },
  faixa: { borderRadius: 10, padding: ESPACO.m, gap: 4 },
  tituloDaFaixa: { fontSize: TAMANHO.corpo, fontWeight: '700' },
  cartao: {
    borderWidth: 2,
    borderColor: COR.borda,
    borderRadius: 12,
    padding: ESPACO.m,
    gap: ESPACO.p,
    backgroundColor: COR.fundo,
    minHeight: TAMANHO.toqueMinimo,
  },
  campo: {
    minHeight: TAMANHO.toqueMinimo,
    borderWidth: 2,
    borderColor: COR.borda,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: TAMANHO.corpo,
    color: COR.texto,
    backgroundColor: COR.fundo,
  },
  rotuloDoCampo: { fontSize: TAMANHO.corpo, fontWeight: '700', color: COR.texto },
});
