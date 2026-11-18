import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useRef, useState } from 'react';
import { Keyboard, KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { Desfecho } from '../../api/contratos';
import { TEXTO_DO_DESFECHO } from '../../dominio/textos';
import { useCorredor } from '../../estado/corredor';
import { useRadarNaTela } from '../../estado/useRadar';
import { BarraDeAcao, useRespiroDaBarra, Botao, CaixaDeTexto, estilo, Faixa, Icone, Titulo, vibrar } from '../../ui/componentes';
import { Entra, Salta } from '../../ui/movimento';
import { COR, ESPACO } from '../../ui/tema';

const OPCOES: Desfecho[] = ['EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada'];

/** No iPhone a opção escolhida ganha um visto à direita. No Android e na web, um rádio do Material à esquerda. */
const COM_CHECK = Platform.OS === 'ios';

function Radio({ marcada }: { marcada: boolean }) {
  return (
    <View style={[local.radio, { borderColor: marcada ? COR.primario : COR.textoSecundario }]}>
      {marcada ? (
        <Salta>
          <View style={local.miolo} />
        </Salta>
      ) : null}
    </View>
  );
}

export default function Visita() {
  const respiro = useRespiroDaBarra();
  // A observação é o último campo. Com o teclado aberto ela ficaria escondida atrás dele,
  // então a tela rola até o fim quando o campo ganha foco e de novo quando o teclado termina de subir.
  const rolagem = useRef<ScrollView>(null);
  const [escrevendo, setEscrevendo] = useState(false);
  useEffect(() => {
    if (!escrevendo) return;
    const irAoFim = () => rolagem.current?.scrollToEnd({ animated: true });
    irAoFim();
    const ouvinte = Keyboard.addListener('keyboardDidShow', irAoFim);
    return () => ouvinte.remove();
  }, [escrevendo]);
  const { token } = useLocalSearchParams<{ token: string }>();
  const { registrarVisita, conectado } = useCorredor();
  const nome = useRadarNaTela()?.cards.find((c) => c.casaToken === token)?.pessoa?.nome;
  const [escolha, setEscolha] = useState<Desfecho | null>(null);
  const [observacao, setObservacao] = useState('');
  const [salvando, setSalvando] = useState(false);

  async function aoSalvar() {
    if (!escolha) return;
    setSalvando(true);
    await registrarVisita(token, escolha, observacao);
    setSalvando(false);
    vibrar('sucesso');
    // Quem abriu a visita por um link não tem para onde voltar: vai para a casa.
    if (router.canGoBack()) router.back();
    else router.replace({ pathname: '/casa/[token]', params: { token } });
  }

  return (
    <KeyboardAvoidingView style={estilo.tela} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView ref={rolagem} contentContainerStyle={[estilo.conteudo, { paddingBottom: ESPACO.g + respiro }]} contentInsetAdjustmentBehavior="automatic" keyboardShouldPersistTaps="handled">
        <Entra ordem={0}>
          <Titulo>{nome ? `O que você encontrou na casa de ${nome}?` : 'O que você encontrou na casa?'}</Titulo>
        </Entra>
        <Entra ordem={1}>
          <View accessibilityRole="radiogroup" style={estilo.grupo}>
            {OPCOES.map((d, i) => {
              const marcada = escolha === d;
              return (
                <View key={d}>
                  {i > 0 ? <View style={[estilo.divisoria, COM_CHECK ? null : { marginLeft: ESPACO.m + 24 + 12 }]} /> : null}
                  <Pressable
                    accessibilityRole="radio"
                    accessibilityState={{ checked: marcada }}
                    accessibilityLabel={`${TEXTO_DO_DESFECHO[d].nome}. ${TEXTO_DO_DESFECHO[d].explicacao}`}
                    onPress={() => {
                      vibrar('selecao');
                      setEscolha(d);
                    }}
                    android_ripple={{ color: 'rgba(0,0,0,0.08)' }}
                    style={({ pressed }) => [estilo.linha, pressed && Platform.OS !== 'android' && { backgroundColor: COR.fundoSuave }]}
                  >
                    {COM_CHECK ? null : <Radio marcada={marcada} />}
                    <View style={{ flex: 1, gap: 2 }}>
                      <Text style={[estilo.tituloDaLinha, { fontWeight: '600' }]}>{TEXTO_DO_DESFECHO[d].nome}</Text>
                      <Text style={estilo.secundario}>{TEXTO_DO_DESFECHO[d].explicacao}</Text>
                    </View>
                    {COM_CHECK && marcada ? (
                      <Salta>
                        <Icone nome="checkmark" cor={COR.primario} tamanho={24} />
                      </Salta>
                    ) : null}
                  </Pressable>
                </View>
              );
            })}
          </View>
        </Entra>
        {escolha === 'NaoAbriu' ? (
          <Faixa tipo="perigo" titulo="A casa continua urgente">
            Chamar o SAMU não tira a casa do radar. Ela sai quando alguém vir a pessoa.
          </Faixa>
        ) : null}
        <Entra ordem={2}>
          <CaixaDeTexto
            titulo="Observação"
            rotulo="Observação, se quiser"
            dicaNoCampo="Opcional. O que você viu na casa?"
            nota="Não escreva CPF, documento nem doença."
            valor={observacao}
            aoMudar={setObservacao}
            limite={500}
            alturaMinima={96}
            aoFocar={() => setEscrevendo(true)}
            aoSairDoFoco={() => setEscrevendo(false)}
          />
        </Entra>
        {!conectado ? (
          <Faixa tipo="aviso" titulo="Sem internet">A visita fica guardada no celular e sai sozinha quando a internet voltar.</Faixa>
        ) : null}
      </ScrollView>
      <BarraDeAcao>
        <Botao rotulo="Salvar visita" icone="checkmark-circle-outline" aoTocar={aoSalvar} ocupado={salvando} desligado={!escolha} dica={escolha ? undefined : 'Escolha uma opção antes'} />
      </BarraDeAcao>
    </KeyboardAvoidingView>
  );
}

const local = StyleSheet.create({
  radio: { width: 24, height: 24, borderRadius: 12, borderWidth: 2, alignItems: 'center', justifyContent: 'center' },
  miolo: { width: 12, height: 12, borderRadius: 6, backgroundColor: COR.primario },
});
