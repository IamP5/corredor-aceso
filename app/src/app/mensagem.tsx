import * as Clipboard from 'expo-clipboard';
import { useRef, useState } from 'react';
import { Keyboard, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { VerificacaoDeMensagem } from '../api/contratos';
import { textoDoErro } from '../dominio/textos';
import { useCorredor } from '../estado/corredor';
import { BarraDeAcao, useRespiroDaBarra, Botao, CaixaDeTexto, estilo, Faixa, Grupo, vibrar } from '../ui/componentes';
import { Surge } from '../ui/movimento';
import { COR, ESPACO } from '../ui/tema';

const LIMITE = 500;

/** Escrever, conferir, ver o resultado. Mexer no texto volta ao rascunho. */
type Passo =
  | { passo: 'rascunho' }
  | { passo: 'verificando' }
  | { passo: 'erro'; codigo: string }
  | { passo: 'bloqueada'; resultado: Extract<VerificacaoDeMensagem, { veredito: 'Bloqueada' }> }
  | { passo: 'liberada'; copiada: boolean };

/** Texto com cada [TELEFONE], [ENDERECO]... marcado em vermelho, para a ACS ver o que expõe a pessoa. */
function TextoMascarado({ texto }: { texto: string }) {
  // O split com grupo de captura deixa as marcas nas posições ímpares.
  const partes = texto.split(/(\[[A-Z_]+\])/);
  return (
    <Text selectable style={estilo.corpo}>
      {partes.map((parte, i) =>
        i % 2 === 1 ? (
          <Text key={i} style={local.marca}>
            {parte}
          </Text>
        ) : (
          parte
        ),
      )}
    </Text>
  );
}

export default function Mensagem() {
  const respiro = useRespiroDaBarra();
  const { api } = useCorredor();
  const [texto, setTexto] = useState('');
  const [passo, setPasso] = useState<Passo>({ passo: 'rascunho' });
  // Uma resposta que chega depois de a ACS mexer no texto já não vale.
  const pedido = useRef(0);

  async function verificar(mensagem: string) {
    const este = ++pedido.current;
    Keyboard.dismiss();
    setPasso({ passo: 'verificando' });
    try {
      const resultado = await api.verificarMensagem('GrupoDoBairro', mensagem.trim());
      if (este !== pedido.current) return;
      if (resultado.veredito === 'Bloqueada') {
        vibrar('aviso');
        setPasso({ passo: 'bloqueada', resultado });
      } else {
        vibrar('sucesso');
        setPasso({ passo: 'liberada', copiada: false });
      }
    } catch (e) {
      if (este !== pedido.current) return;
      setPasso({ passo: 'erro', codigo: (e as { codigo?: string }).codigo ?? 'ERRO' });
    }
  }

  function aoEditar(t: string) {
    pedido.current++;
    setTexto(t);
    setPasso({ passo: 'rascunho' });
  }

  async function copiar() {
    await Clipboard.setStringAsync(texto);
    setPasso({ passo: 'liberada', copiada: true });
  }

  const acao =
    passo.passo === 'bloqueada' ? (
      <Botao
        rotulo="Usar a versão segura"
        icone="swap-horizontal"
        dica="Troca o texto e confere de novo"
        aoTocar={() => {
          setTexto(passo.resultado.versaoSegura);
          void verificar(passo.resultado.versaoSegura);
        }}
      />
    ) : passo.passo === 'liberada' ? (
      <Botao rotulo={passo.copiada ? 'Copiada' : 'Copiar mensagem'} icone={passo.copiada ? 'checkmark' : 'copy-outline'} aoTocar={() => void copiar()} />
    ) : (
      <Botao
        rotulo="Verificar mensagem"
        icone="shield-checkmark-outline"
        aoTocar={() => void verificar(texto)}
        ocupado={passo.passo === 'verificando'}
        desligado={!texto.trim()}
      />
    );

  return (
    <KeyboardAvoidingView style={estilo.tela} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={[estilo.conteudo, { paddingBottom: ESPACO.g + respiro }]} contentInsetAdjustmentBehavior="automatic" keyboardShouldPersistTaps="handled">
        <CaixaDeTexto
          rotulo="Sua mensagem"
          dicaNoCampo="Escreva a mensagem para o grupo"
          nota="O app confere se a mensagem expõe a pessoa. O sistema não manda nada: você copia e cola no grupo."
          valor={texto}
          aoMudar={aoEditar}
          limite={LIMITE}
          focoAoAbrir
        />

        {passo.passo === 'erro' ? <Faixa tipo="aviso" titulo="Não deu para verificar">{textoDoErro(passo.codigo)}</Faixa> : null}

        {passo.passo === 'bloqueada' ? (
          <Surge style={{ gap: ESPACO.g }}>
            <Faixa tipo="perigo" titulo="Esta mensagem não pode ir para o grupo">
              {passo.resultado.explicacao}
            </Faixa>
            <Grupo titulo="O que o sistema viu">
              <View style={{ padding: ESPACO.m }}>
                <TextoMascarado texto={passo.resultado.textoMascarado} />
              </View>
            </Grupo>
            <Grupo titulo="Versão segura">
              <View style={{ padding: ESPACO.m }}>
                <Text selectable style={estilo.corpo}>
                  {passo.resultado.versaoSegura}
                </Text>
              </View>
            </Grupo>
          </Surge>
        ) : null}

        {passo.passo === 'liberada' ? (
          <Faixa tipo="ok" titulo="Pode mandar">
            A mensagem não expõe ninguém. Copie e cole no grupo.
          </Faixa>
        ) : null}
      </ScrollView>
      <BarraDeAcao>{acao}</BarraDeAcao>
    </KeyboardAvoidingView>
  );
}

const local = StyleSheet.create({
  marca: { color: COR.perigo, fontWeight: '700', backgroundColor: COR.fundoPerigo },
});
