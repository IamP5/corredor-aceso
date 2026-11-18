import { router } from 'expo-router';
import { Platform, StyleSheet, Text, View } from 'react-native';
import type { CardNaTela } from '../banco/cache';
import { motivoCurto, ROTULO_DA_SITUACAO, textoDoAviso, textoDoDesfechoNoCard, textoDoMotivo, type SituacaoNaTela } from '../dominio/textos';
import { estilo, Icone, ToqueDeLinha, vibrar, type NomeDoIcone } from './componentes';
import { COR, COR_DA_SITUACAO, ESPACO, FUNDO_DA_SITUACAO, TAMANHO } from './tema';
import { CANTO, RAIO } from './plataforma';

export const FRASE_DE_SAIDA_AVISADA = 'A pessoa avisou que ia sair hoje.';
export const FRASE_DE_ATENDIDA = 'Já atendida nesta parte do dia.';

/** Frases do card, na ordem em que aparecem. O leitor de tela lê as mesmas frases. */
export function frasesDoCard(card: CardNaTela): string[] {
  const frases: string[] = [];
  if (card.lista === 'aguardando') {
    frases.push(card.saidaAvisada ? FRASE_DE_SAIDA_AVISADA : ROTULO_DA_SITUACAO.Aguardando.acao);
  } else {
    frases.push(textoDoMotivo(card.motivo, card.desde, card.ultimoPacoteEm));
    frases.push(card.lista === 'atendida' ? FRASE_DE_ATENDIDA : ROTULO_DA_SITUACAO[card.situacao].acao);
    const aviso = textoDoAviso(card);
    if (aviso) frases.push(aviso);
  }
  if (card.ultimoDesfecho) {
    frases.push(`Sua visita: ${textoDoDesfechoNoCard(card.ultimoDesfecho.desfecho, card.ultimoDesfecho.em, card.ultimoDesfecho.estado)}`);
  }
  return frases;
}

/** Símbolo redondo no começo da linha. A cor vem junto com um desenho diferente para cada situação. */
const MARCA_DA_SITUACAO: Record<SituacaoNaTela, NomeDoIcone> = {
  Vermelho: 'alert',
  Cinza: 'help',
  Aguardando: 'time-outline',
  Verde: 'checkmark',
};

export const RECUO_DA_DIVISORIA = ESPACO.m + 32 + 12;

/** Linha do radar: quem, onde, por quê e desde quando. O resto fica na tela da casa. */
export function LinhaDaCasa({ card, primeira, ultima }: { card: CardNaTela; primeira: boolean; ultima: boolean }) {
  const nome = card.pessoa?.nome ?? 'Casa sem nome. Atualize a lista.';
  const situacao = ROTULO_DA_SITUACAO[card.situacao].nome;
  const marca: SituacaoNaTela = card.lista === 'atendida' ? 'Verde' : card.situacao;
  const motivo =
    card.lista === 'aguardando'
      ? { texto: card.saidaAvisada ? 'Avisou que ia sair' : 'Sem alerta agora', cor: COR.textoSecundario }
      : {
          texto: motivoCurto(card.motivo, card.desde, card.ultimoPacoteEm),
          cor: card.lista === 'pendente' ? COR_DA_SITUACAO[card.situacao] : COR.textoSecundario,
        };
  const pediuAjuda = card.lista !== 'aguardando' && card.aviso?.resposta === 'PrecisaDeAjuda';
  return (
    <ToqueDeLinha
      accessibilityRole="button"
      accessibilityLabel={`${situacao}${card.lista === 'atendida' ? ', atendida' : ''}. ${nome}. ${card.pessoa?.endereco ?? ''}. ${frasesDoCard(card).join(' ')}`}
      accessibilityHint="Abre a casa e a linha do tempo"
      onPress={() => {
        vibrar();
        router.push({ pathname: '/casa/[token]', params: { token: card.casaToken } });
      }}
      style={[local.linha, primeira && local.primeira, ultima && local.ultima]}
    >
      <View style={[local.marca, { backgroundColor: FUNDO_DA_SITUACAO[marca] }]}>
        <Icone nome={MARCA_DA_SITUACAO[marca]} cor={COR_DA_SITUACAO[marca]} tamanho={18} />
      </View>
      <View style={local.textos}>
        <Text numberOfLines={1} style={local.nome}>
          {nome}
        </Text>
        {card.pessoa ? (
          <Text numberOfLines={1} style={estilo.secundario}>
            {card.pessoa.endereco}
          </Text>
        ) : null}
        <Text style={[estilo.secundario, { color: motivo.cor }]}>{motivo.texto}</Text>
        {pediuAjuda ? <Text style={[estilo.secundario, local.ajuda]}>Vizinho pediu ajuda</Text> : null}
        {card.ultimoDesfecho ? (
          <Text numberOfLines={2} style={estilo.secundario}>
            {`Sua visita: ${textoDoDesfechoNoCard(card.ultimoDesfecho.desfecho, card.ultimoDesfecho.em, card.ultimoDesfecho.estado)}`}
          </Text>
        ) : null}
      </View>
      {Platform.OS !== 'android' ? <Icone nome="chevron-forward" cor={COR.borda} tamanho={18} /> : null}
    </ToqueDeLinha>
  );
}

const local = StyleSheet.create({
  linha: {
    minHeight: 56,
    paddingHorizontal: ESPACO.m,
    paddingVertical: 12,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    backgroundColor: COR.superficie,
    overflow: 'hidden',
  },
  primeira: { borderTopLeftRadius: RAIO.cartao, borderTopRightRadius: RAIO.cartao, ...CANTO },
  ultima: { borderBottomLeftRadius: RAIO.cartao, borderBottomRightRadius: RAIO.cartao, ...CANTO },
  marca: { width: 32, height: 32, borderRadius: 16, alignItems: 'center', justifyContent: 'center' },
  textos: { flex: 1, gap: 2 },
  nome: { fontSize: TAMANHO.corpo, lineHeight: TAMANHO.corpo * 1.3, fontWeight: '600', color: COR.texto },
  ajuda: { color: COR.perigo, fontWeight: '600' },
});
