import { Redirect, router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, Text, TextInput, View } from 'react-native';
import { useCorredor } from '../estado/corredor';
import { FORMATO_DO_ACS_ID } from '../estado/sessao';
import { Botao, estilo, Faixa, Paragrafo, Titulo } from '../ui/componentes';

export default function Entrada() {
  const { sessao, carregando, entrar, modo } = useCorredor();
  const [acsId, setAcsId] = useState(modo === 'mock' ? 'acs-demo' : '');
  const [microarea, setMicroarea] = useState('3');
  const [erro, setErro] = useState<string | null>(null);
  const [entrando, setEntrando] = useState(false);

  if (carregando) return null;
  if (sessao) return <Redirect href="/dia" />;

  async function aoEntrar() {
    const id = acsId.trim().toLowerCase();
    const area = Number(microarea);
    if (!FORMATO_DO_ACS_ID.test(id)) return setErro('O código da ACS usa letras minúsculas, números e hífen, de 3 a 40 letras.');
    if (!Number.isInteger(area) || area <= 0) return setErro('A microárea é um número, como 3.');
    setErro(null);
    setEntrando(true);
    await entrar({ acsId: id, microarea: area });
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
          <Text nativeID="rotulo-acs" style={estilo.rotuloDoCampo}>Seu código de ACS</Text>
          <TextInput
            accessibilityLabelledBy="rotulo-acs"
            accessibilityLabel="Seu código de ACS"
            value={acsId}
            onChangeText={setAcsId}
            autoCapitalize="none"
            autoCorrect={false}
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
