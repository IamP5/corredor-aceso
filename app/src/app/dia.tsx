import { Redirect, router } from 'expo-router';
import { Alert, Platform, RefreshControl, ScrollView, View } from 'react-native';
import { horaCurta } from '../dominio/hora';
import { tituloDoRadar } from '../dominio/textos';
import { useCorredor } from '../estado/corredor';
import { useAgora, useRadarNaTela } from '../estado/useRadar';
import { Botao, estilo, Faixa, Paragrafo, Subtitulo, Titulo } from '../ui/componentes';
import { FaixaDeConexao } from '../ui/faixaDeConexao';

function confirmar(titulo: string, mensagem: string, aoConfirmar: () => void) {
  if (Platform.OS === 'web') {
    if (globalThis.confirm?.(`${titulo}\n\n${mensagem}`)) aoConfirmar();
    return;
  }
  Alert.alert(titulo, mensagem, [
    { text: 'Cancelar', style: 'cancel' },
    { text: 'Sair', style: 'destructive', onPress: aoConfirmar },
  ]);
}

export default function DiaCritico() {
  const { sessao, carregando, sincronizar, sincronizando, sair, pendentes } = useCorredor();
  const radar = useRadarNaTela();
  const agora = useAgora();

  if (carregando) return null;
  if (!sessao) return <Redirect href="/" />;

  const cab = radar?.cabecalho;
  const cards = radar?.cards ?? [];
  const urgentes = cards.filter((c) => c.lista === 'pendente' && c.situacao === 'Vermelho').length;
  const cinzas = cards.filter((c) => c.lista === 'pendente' && c.situacao === 'Cinza').length;
  const atendidas = cards.filter((c) => c.lista === 'atendida').length;
  const velho = cab ? tituloDoRadar(cab.baixadoEm, agora).velho : false;

  function aoSair() {
    const aviso =
      pendentes > 0
        ? `Existem ${pendentes} visitas que ainda não foram enviadas. Se sair agora, elas se perdem.`
        : 'O celular apaga a lista de nomes e o radar.';
    confirmar('Sair da conta?', aviso, () => void sair().then(() => router.replace('/')));
  }

  return (
    <ScrollView
      style={estilo.tela}
      contentContainerStyle={estilo.conteudo}
      refreshControl={<RefreshControl refreshing={sincronizando} onRefresh={sincronizar} title="Atualizando" />}
    >
      <FaixaDeConexao />
      {!cab ? (
        <Faixa tipo="aviso" titulo="Ainda sem radar">
          O celular ainda não baixou o radar. Conecte à internet e puxe a tela para baixo.
        </Faixa>
      ) : cab.turnoAberto ? (
        <View style={{ gap: 8 }}>
          <Titulo>Hoje é dia crítico</Titulo>
          <Paragrafo>
            Calor forte ou risco para os idosos. Vá primeiro às casas que ficaram em silêncio. A lista mostra só quem precisa de você.
          </Paragrafo>
        </View>
      ) : (
        <View style={{ gap: 8 }}>
          <Titulo>Hoje não é dia crítico</Titulo>
          <Paragrafo>Só aparecem as casas sem energia e os aparelhos sem sinal. Nenhuma casa fica verde fora do dia crítico.</Paragrafo>
        </View>
      )}

      {cab ? (
        <View style={estilo.cartao} accessible accessibilityLabel={`Resumo do radar${velho ? ` das ${horaCurta(cab.baixadoEm)}, sem conexão` : ''}. ${urgentes} urgentes. ${cinzas} com aparelho sem sinal. ${atendidas} já atendidas. ${cab.verdes} casas deram sinal.`}>
          <Subtitulo>{velho ? `Radar das ${horaCurta(cab.baixadoEm)}` : `Radar das ${horaCurta(cab.agoraDoServidor)}`}</Subtitulo>
          <Paragrafo>{`! ${urgentes} ${urgentes === 1 ? 'casa urgente' : 'casas urgentes'}`}</Paragrafo>
          <Paragrafo>{`? ${cinzas} com aparelho sem sinal`}</Paragrafo>
          <Paragrafo>{`${atendidas} já ${atendidas === 1 ? 'atendida' : 'atendidas'}`}</Paragrafo>
          <Paragrafo>{`✓ ${cab.verdes} casas deram sinal${velho ? ` até ${horaCurta(cab.baixadoEm)}` : ''}`}</Paragrafo>
        </View>
      ) : null}

      <Botao rotulo="Ver o radar" dica="Abre a lista de casas" aoTocar={() => router.push('/radar')} />
      <Botao rotulo="Escrever para o grupo do bairro" tipo="secundario" aoTocar={() => router.push('/mensagem')} />
      {cab?.turnoAberto ? <Botao rotulo="Encerrar o turno" tipo="secundario" aoTocar={() => router.push('/encerrar')} /> : null}
      <Botao rotulo="Atualizar agora" tipo="secundario" ocupado={sincronizando} aoTocar={() => void sincronizar()} />
      <Paragrafo secundario>{`ACS ${sessao.acsId}, microárea ${sessao.microarea}.`}</Paragrafo>
      <Botao rotulo="Sair da conta" tipo="secundario" aoTocar={aoSair} />
    </ScrollView>
  );
}
