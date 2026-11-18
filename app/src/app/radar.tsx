import { Redirect } from 'expo-router';
import { Platform, RefreshControl, SectionList, StyleSheet, Text, View } from 'react-native';
import type { CardNaTela } from '../banco/cache';
import { textoDosVerdes, tituloDoRadar } from '../dominio/textos';
import { useCorredor } from '../estado/corredor';
import { useAgora, useRadarNaTela } from '../estado/useRadar';
import { LinhaDaCasa, RECUO_DA_DIVISORIA } from '../ui/cartaoDaCasa';
import { estilo, Faixa, Icone } from '../ui/componentes';
import { FaixaDeConexao } from '../ui/faixaDeConexao';
import { COR, ESPACO, TAMANHO } from '../ui/tema';
import { CANTO, RAIO } from '../ui/plataforma';

const SECOES: { lista: CardNaTela['lista']; titulo: string; vazio: string }[] = [
  { lista: 'pendente', titulo: 'Precisam de visita', vazio: 'Nenhuma casa precisa de visita agora.' },
  { lista: 'atendida', titulo: 'Já atendidas', vazio: 'Nenhuma casa atendida ainda.' },
  { lista: 'aguardando', titulo: 'Aguardando', vazio: 'Nenhuma casa aguardando.' },
];

export default function Radar() {
  const { sessao, carregando, sincronizar, sincronizando } = useCorredor();
  const radar = useRadarNaTela();
  const agora = useAgora();
  if (carregando) return null;
  if (!sessao) return <Redirect href="/" />;

  // Enquanto o celular lê o radar guardado, nada de "nenhuma casa precisa de visita": seria um falso tudo bem.
  const lendo = radar === null;
  const cab = radar?.cabecalho;
  const titulo = cab ? tituloDoRadar(cab.baixadoEm, agora) : null;
  const secoes = SECOES.map((s) => ({ ...s, data: (radar?.cards ?? []).filter((c) => c.lista === s.lista) }));

  return (
    <SectionList
      style={estilo.tela}
      contentContainerStyle={local.conteudo}
      contentInsetAdjustmentBehavior="automatic"
      sections={secoes}
      keyExtractor={(c) => c.casaToken}
      // No iOS, o título fixo gruda embaixo da barra translúcida e some. As seções são curtas, então ele rola junto.
      stickySectionHeadersEnabled={false}
      refreshControl={<RefreshControl refreshing={sincronizando} onRefresh={sincronizar} title="Atualizando o radar" />}
      ListHeaderComponent={
        <View style={{ gap: ESPACO.m }}>
          <FaixaDeConexao />
          {cab && titulo ? (
            <>
              <View style={{ gap: ESPACO.pp }}>
                <Text accessibilityRole="header" style={estilo.tituloDoGrupo}>
                  {titulo.titulo}
                </Text>
                <View style={local.verdes}>
                  <Icone nome="checkmark-circle" cor={COR.ok} tamanho={20} />
                  <Text style={[estilo.corpo, { flex: 1 }]}>{textoDosVerdes(cab.verdes, cab.baixadoEm, titulo.velho)}</Text>
                </View>
              </View>
              {titulo.velho ? (
                <Faixa tipo="aviso" titulo="Este radar é antigo">
                  Uma casa que estava bem pode ter mudado. Atualize assim que tiver internet.
                </Faixa>
              ) : null}
            </>
          ) : lendo ? null : (
            <Faixa tipo="aviso" titulo="Ainda sem radar">Conecte à internet e puxe a tela para baixo.</Faixa>
          )}
        </View>
      }
      renderSectionHeader={({ section }) => (
        <View
          style={local.cabecaDaSecao}
          accessible
          accessibilityRole="header"
          accessibilityLabel={`${section.titulo}, ${section.data.length === 1 ? '1 casa' : `${section.data.length} casas`}`}
        >
          <Text style={local.tituloDaSecao}>{section.titulo}</Text>
          <Text style={[estilo.secundario, local.contagem]}>{section.data.length}</Text>
        </View>
      )}
      renderSectionFooter={({ section }) =>
        lendo || section.data.length > 0 ? null : section.lista === 'pendente' ? (
          <View style={local.tudoEmDia} accessible accessibilityLabel={section.vazio}>
            <Icone nome="checkmark-circle" cor={COR.ok} tamanho={28} />
            <Text style={[estilo.corpo, { flex: 1 }]}>{section.vazio}</Text>
          </View>
        ) : (
          <Text style={[estilo.secundario, { paddingHorizontal: ESPACO.m }]}>{section.vazio}</Text>
        )
      }
      renderItem={({ item, index, section }) => <LinhaDaCasa card={item} primeira={index === 0} ultima={index === section.data.length - 1} />}
      ItemSeparatorComponent={Divisoria}
    />
  );
}

/** Divisória fina dentro da superfície branca, alinhada com o texto e não com o símbolo. */
function Divisoria() {
  return (
    <View style={{ backgroundColor: COR.superficie }}>
      <View style={local.divisoria} />
    </View>
  );
}

const local = StyleSheet.create({
  conteudo: { paddingHorizontal: ESPACO.m, paddingTop: ESPACO.m, paddingBottom: ESPACO.g * 2 },
  verdes: { flexDirection: 'row', alignItems: 'center', gap: ESPACO.p, paddingHorizontal: ESPACO.m },
  // O fundo cobre as linhas que passam por baixo do título preso no alto.
  cabecaDaSecao: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'space-between',
    gap: ESPACO.p,
    paddingHorizontal: ESPACO.m,
    paddingTop: ESPACO.g,
    paddingBottom: ESPACO.p,
    backgroundColor: COR.fundo,
  },
  tituloDaSecao: {
    flex: 1,
    fontSize: TAMANHO.pequeno,
    fontWeight: '600',
    color: Platform.OS === 'android' ? COR.primario : COR.texto,
  },
  contagem: { fontVariant: ['tabular-nums'] },
  divisoria: { height: StyleSheet.hairlineWidth, backgroundColor: COR.separador, marginLeft: RECUO_DA_DIVISORIA },
  tudoEmDia: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    padding: ESPACO.m,
    minHeight: 56,
    borderRadius: RAIO.cartao,
    ...CANTO,
    backgroundColor: COR.superficie,
  },
});
