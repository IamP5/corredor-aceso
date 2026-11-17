import { Redirect } from 'expo-router';
import { RefreshControl, SectionList, Text, View } from 'react-native';
import type { CardNaTela } from '../banco/cache';
import { textoDosVerdes, tituloDoRadar } from '../dominio/textos';
import { useCorredor } from '../estado/corredor';
import { useAgora, useRadarNaTela } from '../estado/useRadar';
import { CartaoDaCasa } from '../ui/cartaoDaCasa';
import { estilo, Faixa, Paragrafo, Selo, Titulo } from '../ui/componentes';
import { FaixaDeConexao } from '../ui/faixaDeConexao';
import { ESPACO } from '../ui/tema';

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

  const cab = radar?.cabecalho;
  const titulo = cab ? tituloDoRadar(cab.baixadoEm, agora) : null;
  const secoes = SECOES.map((s) => ({ ...s, data: (radar?.cards ?? []).filter((c) => c.lista === s.lista) }));

  return (
    <SectionList
      style={estilo.tela}
      contentContainerStyle={estilo.conteudo}
      sections={secoes}
      keyExtractor={(c) => c.casaToken}
      stickySectionHeadersEnabled={false}
      refreshControl={<RefreshControl refreshing={sincronizando} onRefresh={sincronizar} title="Atualizando o radar" />}
      ListHeaderComponent={
        <View style={{ gap: ESPACO.m }}>
          <FaixaDeConexao />
          {cab && titulo ? (
            <>
              <Titulo>{titulo.titulo}</Titulo>
              {titulo.velho ? (
                <Faixa tipo="aviso" titulo="Este radar é antigo">
                  Uma casa que estava bem pode ter mudado. Atualize assim que tiver internet.
                </Faixa>
              ) : null}
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: ESPACO.p, flexWrap: 'wrap' }}>
                <Selo situacao="Verde" />
                <Paragrafo>{textoDosVerdes(cab.verdes, cab.baixadoEm, titulo.velho)}</Paragrafo>
              </View>
            </>
          ) : (
            <Faixa tipo="aviso" titulo="Ainda sem radar">Conecte à internet e puxe a tela para baixo.</Faixa>
          )}
        </View>
      }
      renderSectionHeader={({ section }) => (
        <Text accessibilityRole="header" style={estilo.subtitulo}>
          {`${section.titulo} (${section.data.length})`}
        </Text>
      )}
      renderSectionFooter={({ section }) => (section.data.length === 0 ? <Paragrafo secundario>{section.vazio}</Paragrafo> : null)}
      renderItem={({ item }) => <CartaoDaCasa card={item} />}
      ItemSeparatorComponent={() => <View style={{ height: ESPACO.m }} />}
    />
  );
}
