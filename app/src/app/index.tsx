import { Redirect, router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useCorredor } from '../estado/corredor';
import { lerCodigoDeAcesso } from '../estado/sessao';
import { Botao, Cartao, estilo, Faixa, Icone, Titulo } from '../ui/componentes';
import { Entra } from '../ui/movimento';
import { COR, ESPACO } from '../ui/tema';
import { CANTO } from '../ui/plataforma';

export default function Entrada() {
  const { sessao, carregando, entrar, modo } = useCorredor();
  const [codigo, setCodigo] = useState(modo === 'mock' ? 'acs-demo' : '');
  const [microarea, setMicroarea] = useState('3');
  const [erro, setErro] = useState<string | null>(null);
  const [entrando, setEntrando] = useState(false);

  if (carregando) return null;
  if (sessao) return <Redirect href="/dia" />;

  async function aoEntrar() {
    const token = lerCodigoDeAcesso(codigo);
    const area = Number(microarea);
    if (!token) return setErro('Digite o código de acesso que a UBS entregou, sem espaços.');
    if (!Number.isInteger(area) || area <= 0) return setErro('A microárea é um número, como 3.');
    setErro(null);
    setEntrando(true);
    await entrar({ token, microarea: area });
    setEntrando(false);
    router.replace('/dia');
  }

  return (
    <SafeAreaView style={estilo.tela} edges={['top', 'bottom']}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView contentContainerStyle={[estilo.conteudo, local.conteudo]} keyboardShouldPersistTaps="handled">
          <Entra ordem={0} style={local.marca}>
            <View style={local.quadradoDaMarca}>
              <Icone nome="heart" cor={COR.sobrePrimario} tamanho={40} />
            </View>
            <Titulo>Corredor Aceso</Titulo>
            <Text style={[estilo.secundario, local.centro]}>
              Em dia de calor forte ou de falta de luz, este app mostra quais idosos da sua microárea ficaram sem dar sinal.
            </Text>
          </Entra>
          {modo === 'mock' ? (
            <Entra ordem={1}>
              <Faixa tipo="info" titulo="Modo de demonstração">
                Sem endereço da API, o app usa dados simulados. Nenhuma pessoa é real.
              </Faixa>
            </Entra>
          ) : null}
          <Entra ordem={2}>
            <Cartao>
              <View style={{ gap: ESPACO.m }}>
                <View style={{ gap: 6 }}>
                  <Text nativeID="rotulo-acs" style={estilo.rotuloDoCampo}>Seu código de acesso</Text>
                  <TextInput
                    accessibilityLabelledBy="rotulo-acs"
                    accessibilityLabel="Seu código de acesso"
                    value={codigo}
                    onChangeText={setCodigo}
                    autoCapitalize="none"
                    autoCorrect={false}
                    secureTextEntry
                    style={estilo.campo}
                  />
                </View>
                <View style={{ gap: 6 }}>
                  <Text nativeID="rotulo-microarea" style={estilo.rotuloDoCampo}>Número da microárea</Text>
                  <TextInput
                    accessibilityLabelledBy="rotulo-microarea"
                    accessibilityLabel="Número da microárea"
                    value={microarea}
                    onChangeText={setMicroarea}
                    keyboardType="number-pad"
                    style={estilo.campo}
                  />
                </View>
              </View>
            </Cartao>
          </Entra>
          {erro ? <Faixa tipo="perigo" titulo="Confira os dados">{erro}</Faixa> : null}
          <Entra ordem={3}>
            <Botao rotulo="Entrar" icone="log-in-outline" aoTocar={aoEntrar} ocupado={entrando} />
          </Entra>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const local = StyleSheet.create({
  conteudo: { flexGrow: 1, justifyContent: 'center', paddingTop: ESPACO.g * 2 },
  marca: { alignItems: 'center', gap: ESPACO.p },
  quadradoDaMarca: {
    width: 80,
    height: 80,
    borderRadius: 22,
    ...CANTO,
    backgroundColor: COR.primario,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: ESPACO.p,
  },
  centro: { textAlign: 'center' },
});
