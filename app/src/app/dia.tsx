import { Redirect, router } from 'expo-router';
import { useState } from 'react';
import { Alert, Platform, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { dataDeSp, horaCurta } from '../dominio/hora';
import { textoDoErro, tituloDoRadar, type SituacaoNaTela } from '../dominio/textos';
import { useCorredor } from '../estado/corredor';
import { useAgora, useRadarNaTela } from '../estado/useRadar';
import { estilo, Faixa, Grupo, Icone, Linha, Paragrafo, ToqueDeLinha, vibrar } from '../ui/componentes';
import { FaixaDeConexao } from '../ui/faixaDeConexao';
import { Entra } from '../ui/movimento';
import { COR, COR_DA_SITUACAO, ESPACO, TAMANHO } from '../ui/tema';
import { CANTO, RAIO } from '../ui/plataforma';

/** Pergunta antes de uma ação sem volta, no diálogo do sistema: alerta no iPhone e no Android, confirm na web. */
function confirmar(titulo: string, mensagem: string, acao: string, aoConfirmar: () => void) {
  if (Platform.OS === 'web') {
    if (globalThis.confirm?.(`${titulo}\n\n${mensagem}`)) aoConfirmar();
    return;
  }
  Alert.alert(titulo, mensagem, [
    { text: 'Cancelar', style: 'cancel' },
    { text: acao, style: 'destructive', onPress: aoConfirmar },
  ]);
}

/** Número grande na cor da situação, com o nome embaixo. O nome diz o que a cor quer dizer. */
function Contador({ numero, situacao, nome }: { numero: number; situacao: SituacaoNaTela; nome: string }) {
  return (
    <View style={local.contador}>
      <Text style={[local.numero, { color: COR_DA_SITUACAO[situacao] }]}>{numero}</Text>
      <Text style={estilo.secundario}>{nome}</Text>
    </View>
  );
}

export default function DiaCritico() {
  const { api, sessao, carregando, sincronizar, sincronizando, sair, pendentes } = useCorredor();
  const radar = useRadarNaTela();
  const agora = useAgora();
  const [encerrando, setEncerrando] = useState(false);
  const [erroAoEncerrar, setErroAoEncerrar] = useState<string | null>(null);

  if (carregando) return null;
  if (!sessao) return <Redirect href="/" />;

  const cab = radar?.cabecalho;
  const cards = radar?.cards ?? [];
  const urgentes = cards.filter((c) => c.lista === 'pendente' && c.situacao === 'Vermelho').length;
  const cinzas = cards.filter((c) => c.lista === 'pendente' && c.situacao === 'Cinza').length;
  const atendidas = cards.filter((c) => c.lista === 'atendida').length;
  const velho = cab ? tituloDoRadar(cab.baixadoEm, agora).velho : false;

  function abrirRadar() {
    vibrar();
    router.push('/radar');
  }

  function aoSair() {
    const aviso =
      pendentes > 0
        ? `Existem ${pendentes} visitas que ainda não foram enviadas. Se sair agora, elas se perdem.`
        : 'O celular apaga a lista de nomes e o radar.';
    confirmar('Sair da conta?', aviso, 'Sair', () => void sair().then(() => router.replace('/')));
  }

  async function encerrar(microarea: number) {
    setEncerrando(true);
    setErroAoEncerrar(null);
    try {
      const resultado = await api.encerrarTurno(dataDeSp(new Date()), microarea);
      void sincronizar();
      vibrar('sucesso');
      router.push({ pathname: '/encerrar', params: { resultado: JSON.stringify(resultado) } });
    } catch (e) {
      vibrar('aviso');
      setErroAoEncerrar((e as { codigo?: string }).codigo ?? 'ERRO');
    } finally {
      setEncerrando(false);
    }
  }

  function aoEncerrar(microarea: number) {
    const fila =
      pendentes > 0
        ? `\n\n${pendentes} ${pendentes === 1 ? 'visita ainda não foi enviada' : 'visitas ainda não foram enviadas'}. Elas saem quando a internet voltar e contam no resumo do servidor só depois disso.`
        : '';
    confirmar(
      'Encerrar o turno de hoje?',
      `O sistema guarda o resumo do dia. Depois disso, as casas não ficam mais vermelhas por silêncio ou calor. Só a falta de energia e o aparelho mudo continuam no radar.${fila}`,
      'Encerrar o turno',
      () => void encerrar(microarea),
    );
  }

  return (
    <ScrollView
      style={estilo.tela}
      contentContainerStyle={estilo.conteudo}
      contentInsetAdjustmentBehavior="automatic"
      refreshControl={<RefreshControl refreshing={sincronizando} onRefresh={sincronizar} title="Atualizando" />}
    >
      <FaixaDeConexao />
      <Entra ordem={0}>
        {!cab ? (
          <Faixa tipo="aviso" titulo="Ainda sem radar">
            O celular ainda não baixou o radar. Conecte à internet e puxe a tela para baixo.
          </Faixa>
        ) : cab.turnoAberto ? (
          <View style={[estilo.cartao, local.destaque, { backgroundColor: COR.fundoPerigo }]}>
            <Icone nome="thermometer" cor={COR.perigo} tamanho={28} />
            <View style={local.textoDoDestaque}>
              <Text accessibilityRole="header" style={[estilo.subtitulo, { color: COR.perigo }]}>
                Hoje é dia crítico
              </Text>
              <Paragrafo>
                Calor forte ou risco para os idosos. Vá primeiro às casas que ficaram em silêncio. A lista mostra só quem precisa de você.
              </Paragrafo>
            </View>
          </View>
        ) : (
          <View style={[estilo.cartao, local.destaque]}>
            <Icone nome="checkmark-circle" cor={COR.ok} tamanho={28} />
            <View style={local.textoDoDestaque}>
              <Text accessibilityRole="header" style={estilo.subtitulo}>
                Hoje não é dia crítico
              </Text>
              <Paragrafo secundario>Só aparecem as casas sem energia e os aparelhos sem sinal. Nenhuma casa fica verde fora do dia crítico.</Paragrafo>
            </View>
          </View>
        )}
      </Entra>

      {cab ? (
        <Entra ordem={1}>
          {/* O resumo é a porta do radar: o título leva a ação à direita e o cartão inteiro também abre a lista. */}
          <View style={local.cabecaDoResumo}>
            <Text accessibilityRole="header" style={[estilo.tituloDoGrupo, { flex: 1 }]}>
              {velho ? `Radar das ${horaCurta(cab.baixadoEm)}` : `Radar das ${horaCurta(cab.agoraDoServidor)}`}
            </Text>
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Ver o radar"
              accessibilityHint="Abre a lista de casas"
              onPress={abrirRadar}
              style={({ pressed }) => [local.verRadar, pressed && { opacity: 0.6 }]}
            >
              <Text style={local.textoDeVerRadar}>Ver radar</Text>
              <Icone nome="chevron-forward" cor={COR.primario} tamanho={18} />
            </Pressable>
          </View>
          <ToqueDeLinha
            accessibilityRole="button"
            accessibilityLabel={`Resumo do radar${velho ? ` das ${horaCurta(cab.baixadoEm)}, sem conexão` : ''}. ${urgentes} urgentes. ${cinzas} com aparelho sem sinal. ${atendidas} já atendidas. ${cab.verdes} casas deram sinal.`}
            accessibilityHint="Abre a lista de casas"
            onPress={abrirRadar}
            style={local.resumo}
          >
            <View style={local.contadores}>
              <Contador numero={urgentes} situacao="Vermelho" nome={urgentes === 1 ? 'urgente' : 'urgentes'} />
              <View style={local.divisoriaEmPe} />
              <Contador numero={cinzas} situacao="Cinza" nome="sem sinal" />
              <View style={local.divisoriaEmPe} />
              <Contador numero={atendidas} situacao="Verde" nome={atendidas === 1 ? 'atendida' : 'atendidas'} />
            </View>
            <View style={local.verdes}>
              <Icone nome="checkmark-circle" cor={COR.ok} tamanho={20} />
              <Text style={[estilo.secundario, { flex: 1 }]}>{`${cab.verdes} casas deram sinal${velho ? ` até ${horaCurta(cab.baixadoEm)}` : ''}`}</Text>
            </View>
          </ToqueDeLinha>
        </Entra>
      ) : null}

      <Entra ordem={3}>
        <Grupo>
          <Linha titulo="Escrever para o grupo do bairro" icone="chatbubbles" abreTela aoTocar={() => router.push('/mensagem')} />
          {cab?.turnoAberto ? (
            <Linha titulo="Encerrar o turno" icone="moon" dica="Pergunta antes de encerrar" ocupado={encerrando} aoTocar={() => aoEncerrar(sessao.microarea)} />
          ) : null}
          <Linha titulo="Atualizar agora" icone="refresh" ocupado={sincronizando} aoTocar={() => void sincronizar()} />
        </Grupo>
      </Entra>
      {erroAoEncerrar ? (
        <Faixa tipo="perigo" titulo="Não deu para encerrar">
          {erroAoEncerrar === 'DIA_CRITICO_DESCONHECIDO' ? 'Hoje não tem turno aberto.' : textoDoErro(erroAoEncerrar)}
        </Faixa>
      ) : null}

      <Entra ordem={4}>
        <Grupo titulo="Conta">
          <Linha titulo="Microárea" icone="map" valor={String(sessao.microarea)} />
          <Linha titulo="Sair da conta" icone="log-out-outline" destrutiva aoTocar={aoSair} />
        </Grupo>
      </Entra>
    </ScrollView>
  );
}

const local = StyleSheet.create({
  destaque: { flexDirection: 'row', alignItems: 'flex-start', gap: 12, paddingVertical: 12 },
  textoDoDestaque: { flex: 1, gap: ESPACO.pp },
  cabecaDoResumo: { flexDirection: 'row', alignItems: 'center', minHeight: TAMANHO.toqueMinimo },
  verRadar: { flexDirection: 'row', alignItems: 'center', gap: 2, minHeight: TAMANHO.toqueMinimo, paddingLeft: ESPACO.m, paddingRight: ESPACO.p },
  textoDeVerRadar: { fontSize: TAMANHO.pequeno, fontWeight: '600', color: COR.primario },
  resumo: { backgroundColor: COR.superficie, borderRadius: RAIO.cartao, ...CANTO, overflow: 'hidden' },
  contadores: { flexDirection: 'row', paddingVertical: 12 },
  contador: { flex: 1, paddingHorizontal: 12, gap: 2 },
  divisoriaEmPe: { width: StyleSheet.hairlineWidth, backgroundColor: COR.separador },
  verdes: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: ESPACO.p,
    paddingHorizontal: ESPACO.m,
    paddingVertical: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: COR.separador,
  },
  numero: { fontSize: TAMANHO.numero, lineHeight: TAMANHO.numero * 1.15, fontWeight: '700' },
});
