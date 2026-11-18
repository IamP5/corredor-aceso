import { Text, View } from 'react-native';
import { useCorredor } from '../estado/corredor';
import { textoDoErro } from '../dominio/textos';
import { estilo, Faixa, Icone } from './componentes';
import { Surge } from './movimento';
import { COR, ESPACO } from './tema';

/** Diz se o celular está sem internet e quantas visitas esperam para sair. */
export function FaixaDeConexao() {
  const { conectado, pendentes, erroDeSincronizacao, modo } = useCorredor();
  const fila = pendentes === 1 ? '1 visita espera para ser enviada.' : `${pendentes} visitas esperam para ser enviadas.`;
  if (!conectado) {
    return (
      <Faixa tipo="aviso" titulo="Sem internet">
        {`O radar mostra a última lista baixada. ${pendentes > 0 ? fila : 'As visitas ficam guardadas no celular.'}`}
      </Faixa>
    );
  }
  if (erroDeSincronizacao) {
    return (
      <Faixa tipo="aviso" titulo="Não deu para atualizar">
        {`${textoDoErro(erroDeSincronizacao)} ${pendentes > 0 ? fila : ''}`.trim()}
      </Faixa>
    );
  }
  if (pendentes > 0) return <Faixa tipo="info" titulo="Enviando visitas">{fila}</Faixa>;
  if (modo === 'mock') {
    return (
      <Surge>
        <View
          style={{ flexDirection: 'row', alignItems: 'center', gap: 6, paddingHorizontal: ESPACO.m }}
          accessible
          accessibilityLabel="Modo de demonstração. Os nomes e as casas são simulados."
        >
          <Icone nome="information-circle-outline" cor={COR.textoSecundario} tamanho={18} />
          <Text style={[estilo.secundario, { flex: 1 }]}>Dados simulados para demonstração</Text>
        </View>
      </Surge>
    );
  }
  return null;
}
