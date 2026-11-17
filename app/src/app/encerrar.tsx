import { router } from 'expo-router';
import { useState } from 'react';
import { ScrollView, View } from 'react-native';
import type { Encerramento } from '../api/contratos';
import { dataDeSp, horaCurta } from '../dominio/hora';
import { textoDoErro } from '../dominio/textos';
import { useCorredor } from '../estado/corredor';
import { Botao, estilo, Faixa, Paragrafo, Subtitulo, Titulo } from '../ui/componentes';

export default function Encerrar() {
  const { api, sessao, sincronizar, pendentes } = useCorredor();
  const [resultado, setResultado] = useState<Encerramento | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [encerrando, setEncerrando] = useState(false);

  async function encerrar() {
    if (!sessao) return;
    setEncerrando(true);
    try {
      setResultado(await api.encerrarTurno(dataDeSp(new Date()), sessao.microarea));
      setErro(null);
      void sincronizar();
    } catch (e) {
      setErro((e as { codigo?: string }).codigo ?? 'ERRO');
    } finally {
      setEncerrando(false);
    }
  }

  if (resultado) {
    const r = resultado.resumo;
    const pct = r.casasAtivas > 0 ? Math.round((100 * r.comSinalDeVidaAte10h) / r.casasAtivas) : 0;
    return (
      <ScrollView style={estilo.tela} contentContainerStyle={estilo.conteudo}>
        <Titulo>{`Turno encerrado às ${horaCurta(resultado.encerradoEm)}`}</Titulo>
        <View style={estilo.cartao} accessible accessibilityLabel={`${pct} por cento das casas deram sinal de vida até as 10 horas. ${r.comSinalDeVidaAte10h} de ${r.casasAtivas}.`}>
          <Subtitulo>{`${pct}% com sinal de vida até as 10h`}</Subtitulo>
          <Paragrafo>{`${r.comSinalDeVidaAte10h} de ${r.casasAtivas} casas. A meta é 100%.`}</Paragrafo>
        </View>
        <Paragrafo>{`${r.atendidas} ${r.atendidas === 1 ? 'casa atendida' : 'casas atendidas'} com alerta.`}</Paragrafo>
        <Paragrafo>{r.vermelhasPendentes === 1 ? '! 1 casa urgente ficou sem atendimento.' : `! ${r.vermelhasPendentes} casas urgentes ficaram sem atendimento.`}</Paragrafo>
        <Paragrafo>{r.cinzasPendentes === 1 ? '? 1 casa com aparelho sem sinal ficou sem atendimento.' : `? ${r.cinzasPendentes} casas com aparelho sem sinal ficaram sem atendimento.`}</Paragrafo>
        {r.vermelhasPendentes + r.cinzasPendentes > 0 ? (
          <Faixa tipo="aviso" titulo="Ainda tem casa sem resposta">
            Avise a UBS. Fora do turno, só os alertas de energia e de aparelho mudo continuam no radar.
          </Faixa>
        ) : null}
        <Botao rotulo="Voltar ao início" aoTocar={() => router.replace('/dia')} />
      </ScrollView>
    );
  }

  return (
    <ScrollView style={estilo.tela} contentContainerStyle={estilo.conteudo}>
      <Titulo>Encerrar o turno de hoje?</Titulo>
      <Paragrafo>O sistema guarda o resumo do dia. Depois disso, as casas não ficam mais vermelhas por silêncio ou calor.</Paragrafo>
      <Paragrafo>Só a falta de energia e o aparelho mudo continuam no radar.</Paragrafo>
      {pendentes > 0 ? (
        <Faixa tipo="aviso" titulo="Visitas ainda no celular">
          {`${pendentes} ${pendentes === 1 ? 'visita ainda não foi enviada' : 'visitas ainda não foram enviadas'}. Elas saem quando a internet voltar e contam no resumo do servidor só depois disso.`}
        </Faixa>
      ) : null}
      {erro ? <Faixa tipo="perigo" titulo="Não deu para encerrar">{erro === 'DIA_CRITICO_DESCONHECIDO' ? 'Hoje não tem turno aberto.' : textoDoErro(erro)}</Faixa> : null}
      <Botao rotulo="Encerrar o turno" tipo="perigo" aoTocar={() => void encerrar()} ocupado={encerrando} />
      <Botao rotulo="Voltar" tipo="secundario" aoTocar={() => router.back()} />
    </ScrollView>
  );
}
