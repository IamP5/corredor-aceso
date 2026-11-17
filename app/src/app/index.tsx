import { Redirect, router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, Text, TextInput, View } from 'react-native';
import { useCorredor } from '../estado/corredor';
import { lerCodigoDeAcesso } from '../estado/sessao';
import { Botao, estilo, Faixa, Paragrafo, Titulo } from '../ui/componentes';

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
    <KeyboardAvoidingView style={estilo.tela} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={estilo.conteudo} keyboardShouldPersistTaps="handled">
        <Titulo>Corredor Aceso</Titulo>
        <Paragrafo>Em dia de calor forte ou de falta de luz, este app mostra quais idosos da sua microárea ficaram sem dar sinal.</Paragrafo>
        {modo === 'mock' ? (
          <Faixa tipo="info" titulo="Modo de demonstração">
            Sem endereço da API, o app usa dados simulados. Nenhuma pessoa é real.
          </Faixa>
        ) : null}
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
        {erro ? <Faixa tipo="perigo" titulo="Confira os dados">{erro}</Faixa> : null}
        <Botao rotulo="Entrar" aoTocar={aoEntrar} ocupado={entrando} />
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
