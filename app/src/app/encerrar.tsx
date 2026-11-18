import { Redirect, router, useLocalSearchParams } from 'expo-router';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { encerramento, type Encerramento } from '../api/contratos';
import { horaCurta } from '../dominio/hora';
import { BarraDeAcao, Botao, Cartao, estilo, Faixa, Grupo, Icone, Linha, Paragrafo, Titulo, useRespiroDaBarra } from '../ui/componentes';
import { Barra, Entra, Salta, useContagem } from '../ui/movimento';
import { COR, COR_DA_SITUACAO, ESPACO, TAMANHO } from '../ui/tema';

/** Lê o resumo que a tela do dia mandou no parâmetro. Quem chega por link, sem resumo, volta para o dia:
 *  esta tela só mostra o resultado, o turno é encerrado na tela do dia, depois da confirmação. */
function lerResultado(texto: string | undefined): Encerramento | null {
  if (!texto) return null;
  try {
    const lido = encerramento.safeParse(JSON.parse(texto));
    return lido.success ? lido.data : null;
  } catch {
    return null;
  }
}

export default function Encerrar() {
  const { resultado: texto } = useLocalSearchParams<{ resultado?: string }>();
  const resultado = lerResultado(texto);
  if (!resultado) return <Redirect href="/dia" />;
  return <Resumo resultado={resultado} />;
}

function Resumo({ resultado }: { resultado: Encerramento }) {
  const r = resultado.resumo;
  const pct = r.casasAtivas > 0 ? Math.round((100 * r.comSinalDeVidaAte10h) / r.casasAtivas) : 0;
  const pctNaTela = useContagem(pct);
  const respiro = useRespiroDaBarra();
  return (
    <View style={estilo.tela}>
      <ScrollView contentContainerStyle={[estilo.conteudo, { paddingBottom: ESPACO.g + respiro }]} contentInsetAdjustmentBehavior="automatic">
        <Entra ordem={0} style={local.cabeca}>
          <Salta>
            <Icone nome="checkmark-circle" cor={COR.ok} tamanho={32} />
          </Salta>
          <View style={{ flex: 1 }}>
            <Titulo>{`Turno encerrado às ${horaCurta(resultado.encerradoEm)}`}</Titulo>
          </View>
        </Entra>
        <Entra ordem={1}>
          <Cartao>
            <View
              accessible
              accessibilityLabel={`${pct} por cento das casas deram sinal de vida até as 10 horas. ${r.comSinalDeVidaAte10h} de ${r.casasAtivas}.`}
              style={{ gap: ESPACO.p }}
            >
              <Text style={local.kpi}>{`${pctNaTela}%`}</Text>
              <Text style={[estilo.corpo, { fontWeight: '600' }]}>com sinal de vida até as 10h</Text>
              <View style={{ marginVertical: ESPACO.pp }}>
                <Barra fracao={pct / 100} cor={COR.primario} trilho={COR.fundoSuave} />
              </View>
              <Paragrafo secundario>{`${r.comSinalDeVidaAte10h} de ${r.casasAtivas} casas. A meta é 100%.`}</Paragrafo>
            </View>
          </Cartao>
        </Entra>
        <Entra ordem={2}>
          <Grupo>
            <Linha
              titulo={r.atendidas === 1 ? 'Casa atendida com alerta' : 'Casas atendidas com alerta'}
              valor={String(r.atendidas)}
              icone="checkmark"
              corDoIcone={COR_DA_SITUACAO.Verde}
            />
            <Linha
              titulo={r.vermelhasPendentes === 1 ? 'Casa urgente sem atendimento' : 'Casas urgentes sem atendimento'}
              valor={String(r.vermelhasPendentes)}
              icone="alert"
              corDoIcone={COR_DA_SITUACAO.Vermelho}
            />
            <Linha
              titulo={r.cinzasPendentes === 1 ? 'Casa com aparelho sem sinal, sem atendimento' : 'Casas com aparelho sem sinal, sem atendimento'}
              valor={String(r.cinzasPendentes)}
              icone="help"
              corDoIcone={COR_DA_SITUACAO.Cinza}
            />
          </Grupo>
        </Entra>
        {r.vermelhasPendentes + r.cinzasPendentes > 0 ? (
          <Entra ordem={3}>
            <Faixa tipo="aviso" titulo="Ainda tem casa sem resposta">
              Avise a UBS. Fora do turno, só os alertas de energia e de aparelho mudo continuam no radar.
            </Faixa>
          </Entra>
        ) : null}
      </ScrollView>
      <BarraDeAcao>
        <Botao rotulo="Voltar ao início" icone="home-outline" aoTocar={() => router.replace('/dia')} />
      </BarraDeAcao>
    </View>
  );
}

const local = StyleSheet.create({
  cabeca: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  kpi: { fontSize: TAMANHO.destaque, lineHeight: TAMANHO.destaque * 1.1, fontWeight: '700', color: COR.primario, fontVariant: ['tabular-nums'] },
});
