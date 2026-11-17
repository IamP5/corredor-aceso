import { useCorredor } from '../estado/corredor';
import { textoDoErro } from '../dominio/textos';
import { Faixa } from './componentes';

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
    return <Faixa tipo="info" titulo="Modo de demonstração">Os nomes e as casas são simulados.</Faixa>;
  }
  return null;
}
