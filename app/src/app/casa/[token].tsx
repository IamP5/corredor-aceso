import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Linking, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { LinhaDoTempo, MotivoDoAlerta } from '../../api/contratos';
import type { CardNaTela, EstadoDoDesfecho } from '../../banco/cache';
import { desfechosDaCasa, dispensarRecusado, type DesfechoNoCelular } from '../../banco/fila';
import { horaCurta } from '../../dominio/hora';
import {
  ROTULO_DA_SITUACAO,
  TEXTO_DO_DESFECHO,
  TEXTO_DO_EVENTO,
  textoDoAviso,
  textoDoDesfechoNoCard,
  textoDoErro,
  textoDoMotivo,
  textoDoPerfil,
} from '../../dominio/textos';
import { useCorredor } from '../../estado/corredor';
import { useRadarNaTela } from '../../estado/useRadar';
import { FRASE_DE_ATENDIDA, FRASE_DE_SAIDA_AVISADA } from '../../ui/cartaoDaCasa';
import { BarraDeAcao, useRespiroDaBarra, Botao, Cartao, estilo, Faixa, Grupo, Icone, Linha, Selo, Titulo, type NomeDoIcone } from '../../ui/componentes';
import { FaixaDeConexao } from '../../ui/faixaDeConexao';
import { Esqueleto, Surge } from '../../ui/movimento';
import { COR, COR_DA_SITUACAO, ESPACO } from '../../ui/tema';

const ICONE_DO_MOTIVO: Record<MotivoDoAlerta, NomeDoIcone> = {
  EnergiaEletrodependente: 'flash',
  SemHeartbeat: 'cloud-offline',
  CalorInterno: 'thermometer',
  SemMovimentoNaJanela: 'walk',
};

const ICONE_DA_VISITA: Record<EstadoDoDesfecho, NomeDoIcone> = {
  enviado: 'checkmark-circle',
  pendente: 'time-outline',
  recusado: 'alert-circle',
};

/** Um fato da casa por linha, com o ícone que diz do que se trata. */
type Fato = { texto: string; icone: NomeDoIcone; cor?: string };

function fatosDaCasa(card: CardNaTela): Fato[] {
  const fatos: Fato[] = [];
  if (card.lista === 'aguardando') {
    fatos.push({ texto: card.saidaAvisada ? FRASE_DE_SAIDA_AVISADA : ROTULO_DA_SITUACAO.Aguardando.acao, icone: 'time-outline' });
  } else {
    fatos.push({ texto: textoDoMotivo(card.motivo, card.desde, card.ultimoPacoteEm), icone: ICONE_DO_MOTIVO[card.motivo], cor: COR_DA_SITUACAO[card.situacao] });
    fatos.push(
      card.lista === 'atendida'
        ? { texto: FRASE_DE_ATENDIDA, icone: 'checkmark-circle' }
        : { texto: ROTULO_DA_SITUACAO[card.situacao].acao, icone: 'alert-circle' },
    );
    fatos.push({ texto: textoDoPerfil(card.perfil), icone: 'person' });
    const aviso = textoDoAviso(card);
    if (aviso) fatos.push({ texto: aviso, icone: 'people', cor: card.aviso?.resposta === 'PrecisaDeAjuda' ? COR.perigo : undefined });
  }
  if (card.ultimoDesfecho) {
    const { desfecho, em, estado } = card.ultimoDesfecho;
    fatos.push({ texto: `Sua visita: ${textoDoDesfechoNoCard(desfecho, em, estado)}`, icone: 'clipboard-outline' });
  }
  return fatos;
}

type ItemDaLinha = { quando: string; texto: string; icone: NomeDoIcone; alerta: boolean };

const ICONE_DO_EVENTO: Record<LinhaDoTempo['eventos'][number]['tipo'], NomeDoIcone> = {
  MOVIMENTO: 'walk',
  BOTAO_BEM: 'thumbs-up',
  BOTAO_SAIR: 'exit-outline',
  TELEMETRIA: 'pulse',
};

const LARGURAS_DO_ESQUELETO = ['70%', '55%', '80%'] as const;

function itensDaLinha(linha: LinhaDoTempo): ItemDaLinha[] {
  const eventos = linha.eventos.map((e) => {
    let texto = TEXTO_DO_EVENTO[e.tipo];
    const semEnergia = e.tipo === 'TELEMETRIA' && e.tensaoV !== null && e.tensaoV < 100;
    if (e.tipo === 'TELEMETRIA') {
      const partes = [e.tempC === null ? null : `${e.tempC.toFixed(1).replace('.', ',')} °C`, e.tensaoV === null ? null : e.tensaoV < 100 ? `sem energia (${e.tensaoV} V)` : `energia normal (${e.tensaoV} V)`];
      texto = `${texto}: ${partes.filter(Boolean).join(', ')}`;
    }
    if (e.bufferizado) texto += `. Chegou atrasado, às ${horaCurta(e.recebidoEm)}`;
    return { quando: e.ocorridoEm, texto, icone: semEnergia ? 'flash-off' : ICONE_DO_EVENTO[e.tipo], alerta: semEnergia } satisfies ItemDaLinha;
  });
  const confirmacoes = linha.confirmacoes.map(
    (c) =>
      ({
        quando: c.quando,
        texto: `${c.origem === 'Acs' ? 'Visita da ACS' : 'Resposta do vizinho'}: ${c.resultado === 'PessoaVista' ? 'pessoa vista bem' : 'pessoa fora de casa'}`,
        icone: c.origem === 'Acs' ? 'medkit' : 'people',
        alerta: false,
      }) satisfies ItemDaLinha,
  );
  return [...eventos, ...confirmacoes].sort((a, b) => new Date(b.quando).getTime() - new Date(a.quando).getTime());
}

export default function Casa() {
  const respiro = useRespiroDaBarra();
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

  // Enquanto o celular lê o radar guardado, a casa não aparece como "deu sinal": seria um falso tudo bem.
  const lendo = radar === null;
  const pessoa = card?.pessoa ?? null;

  const itens = linha ? itensDaLinha(linha) : [];

  return (
    <View style={estilo.tela}>
      <ScrollView style={estilo.tela} contentContainerStyle={[estilo.conteudo, { paddingBottom: ESPACO.g + respiro }]} contentInsetAdjustmentBehavior="automatic">
        <FaixaDeConexao />
        <View style={local.heroi}>
          {card ? <Selo situacao={card.situacao} atendida={card.lista === 'atendida'} /> : lendo ? null : <Selo situacao="Verde" />}
          <Titulo>{pessoa?.nome ?? 'Casa sem nome. Atualize a lista.'}</Titulo>
          {pessoa ? (
            <Text selectable style={[estilo.corpo, { color: COR.textoSecundario }]}>
              {pessoa.endereco}
            </Text>
          ) : null}
        </View>

        {lendo ? null : (
          <Grupo titulo="Situação">
            {card ? (
              fatosDaCasa(card).map((f) => <Linha key={f.texto} titulo={f.texto} icone={f.icone} corDoIcone={f.cor} />)
            ) : (
              <Linha titulo="Esta casa não está no radar: a pessoa deu sinal nesta parte do dia." icone="checkmark-circle" corDoIcone={COR_DA_SITUACAO.Verde} />
            )}
          </Grupo>
        )}

        {pessoa?.vizinho ? (
          <Grupo titulo="Vizinho de confiança">
            <Linha
              titulo={`Ligar para ${pessoa.vizinho.nome}`}
              detalhe={pessoa.vizinho.telefone}
              detalheSelecionavel
              icone="call"
              aoTocar={() => void Linking.openURL(`tel:${pessoa.vizinho?.telefone.replace(/\D/g, '')}`)}
            />
          </Grupo>
        ) : null}

        {desfechos.length > 0 ? (
          <Grupo titulo="Suas visitas nesta casa">
            {desfechos.map((d) => (
              <View key={d.id}>
                <Linha
                  titulo={textoDoDesfechoNoCard(d.desfecho, d.ocorridaEm, d.estado)}
                  detalhe={d.estado === 'pendente' && d.ultimoErro ? textoDoErro(d.ultimoErro) : undefined}
                  icone={ICONE_DA_VISITA[d.estado]}
                  corDoIcone={d.estado === 'recusado' ? COR.perigo : undefined}
                />
                {d.estado === 'recusado' ? (
                  <View style={local.recusado}>
                    <Faixa tipo="perigo" titulo="Não enviado">{textoDoErro(d.ultimoErro)}</Faixa>
                    <Botao
                      rotulo={`Dispensar a visita "${TEXTO_DO_DESFECHO[d.desfecho].nome}"`}
                      tipo="secundario"
                      aoTocar={() => void dispensarRecusado(banco, d.id).then(aposGravar)}
                    />
                  </View>
                ) : null}
              </View>
            ))}
          </Grupo>
        ) : null}

        <View style={{ gap: ESPACO.p }}>
          <Text accessibilityRole="header" style={estilo.tituloDoGrupo}>
            Últimas 24 horas
          </Text>
          {linha ? (
            <Surge>
              <Cartao>
                <Text style={estilo.secundario}>
                  {linha.ultimoPacoteEm ? `O aparelho falou com o sistema pela última vez às ${horaCurta(linha.ultimoPacoteEm)}.` : 'O aparelho nunca falou com o sistema.'}
                </Text>
                <View style={{ marginTop: ESPACO.p }}>
                  {itens.map((i, n) => (
                    <View key={`${i.quando}-${i.texto}`} style={local.evento} accessible accessibilityLabel={`${horaCurta(i.quando)}. ${i.texto}`}>
                      <Text style={[estilo.secundario, local.hora]}>{horaCurta(i.quando)}</Text>
                      <View style={local.trilho}>
                        <View style={[local.marca, { backgroundColor: i.alerta ? COR.fundoPerigo : COR.fundoPrimario }]}>
                          <Icone nome={i.icone} cor={i.alerta ? COR.perigo : COR.primario} tamanho={16} />
                        </View>
                        {n < itens.length - 1 ? <View style={local.linhaDoTrilho} /> : null}
                      </View>
                      <View style={local.textoDoEvento}>
                        <Text style={[estilo.corpo, i.alerta && local.textoDeAlerta]}>{i.texto}</Text>
                      </View>
                    </View>
                  ))}
                </View>
              </Cartao>
            </Surge>
          ) : erroDaLinha ? (
            <Faixa tipo="aviso" titulo="Linha do tempo indisponível">
              {erroDaLinha === 'SEM_CONEXAO' ? 'A linha do tempo precisa de conexão com o sistema. O resto da tela funciona sem ela.' : textoDoErro(erroDaLinha)}
            </Faixa>
          ) : (
            <Surge>
              <View style={[estilo.cartao, local.carregando]} accessible accessibilityLabel="Carregando a linha do tempo.">
                {LARGURAS_DO_ESQUELETO.map((largura) => (
                  <View key={largura} style={local.esqueleto}>
                    <Esqueleto largura={48} cor={COR.fundoSuave} />
                    <Esqueleto largura={largura} cor={COR.fundoSuave} />
                  </View>
                ))}
              </View>
            </Surge>
          )}
        </View>
      </ScrollView>
      <BarraDeAcao>
        <Botao
          rotulo="Registrar visita"
          icone="clipboard-outline"
          dica="Abre a tela para dizer o que encontrou na casa"
          aoTocar={() => router.push({ pathname: '/visita/[token]', params: { token } })}
        />
      </BarraDeAcao>
    </View>
  );
}

const local = StyleSheet.create({
  heroi: { gap: ESPACO.p, paddingHorizontal: ESPACO.m },
  recusado: { paddingHorizontal: ESPACO.m, paddingBottom: ESPACO.m, gap: ESPACO.p },
  evento: { flexDirection: 'row', gap: 12 },
  hora: { width: 60, paddingTop: 3 },
  trilho: { width: 28, alignItems: 'center' },
  marca: { width: 28, height: 28, borderRadius: 14, alignItems: 'center', justifyContent: 'center' },
  linhaDoTrilho: { flex: 1, width: 2, minHeight: ESPACO.m, backgroundColor: COR.separador, marginVertical: ESPACO.pp },
  textoDoEvento: { flex: 1, paddingTop: 2, paddingBottom: ESPACO.m },
  textoDeAlerta: { color: COR.perigo, fontWeight: '600' },
  carregando: { gap: ESPACO.m, paddingVertical: ESPACO.m + ESPACO.pp },
  esqueleto: { flexDirection: 'row', alignItems: 'center', gap: 24 },
});
