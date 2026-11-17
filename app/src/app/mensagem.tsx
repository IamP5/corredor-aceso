import * as Clipboard from 'expo-clipboard';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, Text, TextInput, View } from 'react-native';
import type { VerificacaoDeMensagem } from '../api/contratos';
import { textoDoErro } from '../dominio/textos';
import { useCorredor } from '../estado/corredor';
import { Botao, estilo, Faixa, Paragrafo, Subtitulo, Titulo } from '../ui/componentes';

export default function Mensagem() {
  const { api } = useCorredor();
  const [texto, setTexto] = useState('');
  const [resultado, setResultado] = useState<VerificacaoDeMensagem | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [verificando, setVerificando] = useState(false);
  const [copiado, setCopiado] = useState(false);

  async function verificar(mensagem: string) {
    setVerificando(true);
    setCopiado(false);
    try {
      setResultado(await api.verificarMensagem('GrupoDoBairro', mensagem.trim()));
      setErro(null);
    } catch (e) {
      setErro((e as { codigo?: string }).codigo ?? 'ERRO');
    } finally {
      setVerificando(false);
    }
  }

  async function copiar(mensagem: string) {
    await Clipboard.setStringAsync(mensagem);
    setCopiado(true);
  }

  return (
    <KeyboardAvoidingView style={estilo.tela} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={estilo.conteudo} keyboardShouldPersistTaps="handled">
        <Titulo>Antes de mandar no grupo</Titulo>
        <Paragrafo>O app confere se a mensagem expõe a pessoa. O sistema não manda nada: você copia e cola no grupo.</Paragrafo>
        <View style={{ gap: 6 }}>
          <Text nativeID="rotulo-msg" style={estilo.rotuloDoCampo}>Sua mensagem</Text>
          <TextInput
            accessibilityLabelledBy="rotulo-msg"
            accessibilityLabel="Sua mensagem"
            value={texto}
            onChangeText={(t) => {
              setTexto(t);
              setResultado(null);
            }}
            maxLength={500}
            multiline
            style={[estilo.campo, { minHeight: 120, textAlignVertical: 'top' }]}
          />
        </View>
        <Botao rotulo="Verificar mensagem" aoTocar={() => void verificar(texto)} ocupado={verificando} desligado={!texto.trim()} />
        {erro ? <Faixa tipo="aviso" titulo="Não deu para verificar">{textoDoErro(erro)}</Faixa> : null}

        {resultado?.veredito === 'Bloqueada' ? (
          <>
            <Faixa tipo="perigo" titulo="✕ Esta mensagem não pode ir para o grupo">
              {resultado.explicacao}
            </Faixa>
            <Subtitulo>O que o sistema viu</Subtitulo>
            <Paragrafo>{resultado.textoMascarado}</Paragrafo>
            <Subtitulo>Mande esta no lugar</Subtitulo>
            <View style={estilo.cartao}>
              <Paragrafo>{resultado.versaoSegura}</Paragrafo>
            </View>
            <Botao
              rotulo="Usar esta versão"
              dica="Troca o texto e confere de novo"
              aoTocar={() => {
                setTexto(resultado.versaoSegura);
                void verificar(resultado.versaoSegura);
              }}
            />
          </>
        ) : null}

        {resultado?.veredito === 'Liberada' ? (
          <>
            <Faixa tipo="ok" titulo="✓ Pode mandar">A mensagem não expõe ninguém. Copie e cole no grupo.</Faixa>
            <Botao rotulo={copiado ? 'Copiada' : 'Copiar mensagem'} tipo="secundario" aoTocar={() => void copiar(texto)} />
          </>
        ) : null}
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
