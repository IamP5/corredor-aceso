import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Linking, ScrollView, View } from 'react-native';
import type { LinhaDoTempo } from '../../api/contratos';
import { desfechosDaCasa, dispensarRecusado, type DesfechoNoCelular } from '../../banco/fila';
import { horaCurta } from '../../dominio/hora';
import { TEXTO_DO_DESFECHO, TEXTO_DO_EVENTO, textoDoDesfechoNoCard, textoDoErro, textoDoPerfil } from '../../dominio/textos';
import { useCorredor } from '../../estado/corredor';
import { useRadarNaTela } from '../../estado/useRadar';
import { frasesDoCard } from '../../ui/cartaoDaCasa';
import { Botao, estilo, Faixa, Paragrafo, Selo, Subtitulo, Titulo } from '../../ui/componentes';
import { FaixaDeConexao } from '../../ui/faixaDeConexao';

type ItemDaLinha = { quando: string; texto: string };

function itensDaLinha(linha: LinhaDoTempo): ItemDaLinha[] {
  const eventos = linha.eventos.map((e) => {
    let texto = TEXTO_DO_EVENTO[e.tipo];
    if (e.tipo === 'TELEMETRIA') {
      const partes = [e.tempC === null ? null : `${e.tempC.toFixed(1).replace('.', ',')} °C`, e.tensaoV === null ? null : e.tensaoV < 100 ? `sem energia (${e.tensaoV} V)` : `energia normal (${e.tensaoV} V)`];
      texto = `${texto}: ${partes.filter(Boolean).join(', ')}`;
    }
    if (e.bufferizado) texto += `. Chegou atrasado, às ${horaCurta(e.recebidoEm)}`;
    return { quando: e.ocorridoEm, texto };
  });
  const confirmacoes = linha.confirmacoes.map((c) => ({
    quando: c.quando,
    texto: `${c.origem === 'Acs' ? 'Visita da ACS' : 'Resposta do vizinho'}: ${c.resultado === 'PessoaVista' ? 'pessoa vista bem' : 'pessoa fora de casa'}`,
  }));
  return [...eventos, ...confirmacoes].sort((a, b) => new Date(b.quando).getTime() - new Date(a.quando).getTime());
}

export default function Casa() {
  const { token } = useLocalSearchParams<{ token: string }>();
  const { api, banco, versao, aposGravar, conectado } = useCorredor();
  const radar = useRadarNaTela();
  const card = radar?.cards.find((c) => c.casaToken === token);
  const [linha, setLinha] = useState<LinhaDoTempo | null>(null);
  const [erroDaLinha, setErroDaLinha] = useState<string | null>(null);
  const [desfechos, setDesfechos] = useState<DesfechoNoCelular[]>([]);

  useEffect(() => {
    void desfechosDaCasa(banco, token).then(setDesfechos);
  }, [banco, token, versao]);

  useEffect(() => {
    let viva = true;
    api
      .linhaDoTempo(token)
      .then((l) => viva && (setLinha(l), setErroDaLinha(null)))
      .catch((e: { codigo?: string }) => viva && setErroDaLinha(e.codigo ?? 'ERRO'));
    return () => {
      viva = false;
    };
  }, [api, token, versao, conectado]);

  const pessoa = card?.pessoa ?? null;

  return (
    <ScrollView style={estilo.tela} contentContainerStyle={estilo.conteudo}>
      <FaixaDeConexao />
      {card ? <Selo situacao={card.situacao} atendida={card.lista === 'atendida'} /> : <Selo situacao="Verde" />}
      <Titulo>{pessoa?.nome ?? 'Casa sem nome. Atualize a lista.'}</Titulo>
      {pessoa ? <Paragrafo>{pessoa.endereco}</Paragrafo> : null}
      {card && card.lista !== 'aguardando' ? <Paragrafo secundario>{textoDoPerfil(card.perfil)}</Paragrafo> : null}
      {card ? (
        frasesDoCard(card).map((f) => <Paragrafo key={f}>{f}</Paragrafo>)
      ) : (
        <Paragrafo>Esta casa não está no radar: a pessoa deu sinal nesta parte do dia.</Paragrafo>
      )}

      <Botao
        rotulo="Registrar visita"
        dica="Abre a tela para dizer o que encontrou na casa"
        aoTocar={() => router.push({ pathname: '/visita/[token]', params: { token } })}
      />

      {pessoa?.vizinho ? (
        <View style={estilo.cartao}>
          <Subtitulo>Vizinho de confiança</Subtitulo>
          <Paragrafo>{`${pessoa.vizinho.nome}, ${pessoa.vizinho.telefone}`}</Paragrafo>
          <Botao
            rotulo={`Ligar para ${pessoa.vizinho.nome}`}
            tipo="secundario"
            aoTocar={() => void Linking.openURL(`tel:${pessoa.vizinho?.telefone.replace(/\D/g, '')}`)}
          />
        </View>
      ) : null}

      {desfechos.length > 0 ? <Subtitulo>Suas visitas nesta casa</Subtitulo> : null}
      {desfechos.map((d) => (
        <View key={d.id} style={estilo.cartao}>
          <Paragrafo>{textoDoDesfechoNoCard(d.desfecho, d.ocorridaEm, d.estado)}</Paragrafo>
          {d.estado === 'pendente' && d.ultimoErro ? <Paragrafo secundario>{textoDoErro(d.ultimoErro)}</Paragrafo> : null}
          {d.estado === 'recusado' ? (
            <>
              <Faixa tipo="perigo" titulo="Não enviado">{textoDoErro(d.ultimoErro)}</Faixa>
              <Botao
                rotulo={`Dispensar a visita "${TEXTO_DO_DESFECHO[d.desfecho].nome}"`}
                tipo="secundario"
                aoTocar={() => void dispensarRecusado(banco, d.id).then(aposGravar)}
              />
            </>
          ) : null}
        </View>
      ))}

      <Subtitulo>Últimas 24 horas</Subtitulo>
      {linha ? (
        <>
          <Paragrafo secundario>
            {linha.ultimoPacoteEm ? `O aparelho falou com o sistema pela última vez às ${horaCurta(linha.ultimoPacoteEm)}.` : 'O aparelho nunca falou com o sistema.'}
          </Paragrafo>
          {itensDaLinha(linha).map((i) => (
            <View key={`${i.quando}-${i.texto}`} style={{ flexDirection: 'row', gap: 12 }} accessible accessibilityLabel={`${horaCurta(i.quando)}. ${i.texto}`}>
              <Paragrafo>{horaCurta(i.quando)}</Paragrafo>
              <View style={{ flex: 1 }}>
                <Paragrafo>{i.texto}</Paragrafo>
              </View>
            </View>
          ))}
        </>
      ) : erroDaLinha ? (
        <Faixa tipo="aviso" titulo="Linha do tempo indisponível">
          {erroDaLinha === 'SEM_CONEXAO' ? 'A linha do tempo precisa de internet. O resto da tela funciona sem ela.' : textoDoErro(erroDaLinha)}
        </Faixa>
      ) : (
        <Paragrafo secundario>Carregando a linha do tempo.</Paragrafo>
      )}
    </ScrollView>
  );
}
