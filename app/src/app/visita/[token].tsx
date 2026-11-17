import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, Text, TextInput, View } from 'react-native';
import type { Desfecho } from '../../api/contratos';
import { TEXTO_DO_DESFECHO } from '../../dominio/textos';
import { useCorredor } from '../../estado/corredor';
import { useRadarNaTela } from '../../estado/useRadar';
import { Botao, estilo, Faixa, Paragrafo, Titulo } from '../../ui/componentes';
import { COR, TAMANHO } from '../../ui/tema';

const OPCOES: Desfecho[] = ['EstavaBem', 'EstavaFora', 'NaoAbriu', 'Encaminhada'];

export default function Visita() {
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
    // Quem abriu a visita por um link não tem para onde voltar: vai para a casa.
    if (router.canGoBack()) router.back();
    else router.replace({ pathname: '/casa/[token]', params: { token } });
  }

  return (
    <KeyboardAvoidingView style={estilo.tela} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={estilo.conteudo} keyboardShouldPersistTaps="handled">
        <Titulo>{nome ? `O que você encontrou na casa de ${nome}?` : 'O que você encontrou na casa?'}</Titulo>
        <View accessibilityRole="radiogroup" style={{ gap: 12 }}>
          {OPCOES.map((d) => {
            const marcada = escolha === d;
            return (
              <Pressable
                key={d}
                accessibilityRole="radio"
                accessibilityState={{ checked: marcada }}
                accessibilityLabel={`${TEXTO_DO_DESFECHO[d].nome}. ${TEXTO_DO_DESFECHO[d].explicacao}`}
                onPress={() => setEscolha(d)}
                style={[estilo.cartao, marcada && { borderColor: COR.primario, borderWidth: 4, backgroundColor: COR.fundoSuave }]}
              >
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
                  <View
                    style={{
                      width: 28,
                      height: 28,
                      borderRadius: 14,
                      borderWidth: 3,
                      borderColor: marcada ? COR.primario : COR.borda,
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    {marcada ? <View style={{ width: 14, height: 14, borderRadius: 7, backgroundColor: COR.primario }} /> : null}
                  </View>
                  <Text style={{ fontSize: TAMANHO.corpo, fontWeight: '700', color: COR.texto, flex: 1 }}>{TEXTO_DO_DESFECHO[d].nome}</Text>
                </View>
                <Paragrafo secundario>{TEXTO_DO_DESFECHO[d].explicacao}</Paragrafo>
              </Pressable>
            );
          })}
        </View>
        {escolha === 'NaoAbriu' ? (
          <Faixa tipo="perigo" titulo="A casa continua urgente">
            Chamar o SAMU não tira a casa do radar. Ela sai quando alguém vir a pessoa.
          </Faixa>
        ) : null}
        <View style={{ gap: 6 }}>
          <Text nativeID="rotulo-obs" style={estilo.rotuloDoCampo}>Observação, se quiser</Text>
          <Paragrafo secundario>Não escreva CPF, documento nem doença.</Paragrafo>
          <TextInput
            accessibilityLabelledBy="rotulo-obs"
            accessibilityLabel="Observação, se quiser"
            value={observacao}
            onChangeText={setObservacao}
            maxLength={500}
            multiline
            style={[estilo.campo, { minHeight: 96, textAlignVertical: 'top' }]}
          />
        </View>
        {!conectado ? (
          <Faixa tipo="aviso" titulo="Sem internet">A visita fica guardada no celular e sai sozinha quando a internet voltar.</Faixa>
        ) : null}
        <Botao rotulo="Salvar visita" aoTocar={aoSalvar} ocupado={salvando} desligado={!escolha} dica={escolha ? undefined : 'Escolha uma opção antes'} />
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
