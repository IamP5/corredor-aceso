import { router } from 'expo-router';
import { Pressable, Text, View } from 'react-native';
import type { CardNaTela } from '../banco/cache';
import { ROTULO_DA_SITUACAO, textoDoAviso, textoDoDesfechoNoCard, textoDoMotivo } from '../dominio/textos';
import { estilo, Paragrafo, Selo } from './componentes';
import { COR, COR_DA_SITUACAO } from './tema';

/** Frases do card, na ordem em que aparecem. O leitor de tela lê as mesmas frases. */
export function frasesDoCard(card: CardNaTela): string[] {
  const frases: string[] = [];
  if (card.lista === 'aguardando') {
    frases.push(card.saidaAvisada ? 'A pessoa avisou que ia sair hoje.' : ROTULO_DA_SITUACAO.Aguardando.acao);
  } else {
    frases.push(textoDoMotivo(card.motivo, card.desde, card.ultimoPacoteEm));
    frases.push(card.lista === 'atendida' ? 'Já atendida nesta parte do dia.' : ROTULO_DA_SITUACAO[card.situacao].acao);
    const aviso = textoDoAviso(card);
    if (aviso) frases.push(aviso);
  }
  if (card.ultimoDesfecho) {
    frases.push(`Sua visita: ${textoDoDesfechoNoCard(card.ultimoDesfecho.desfecho, card.ultimoDesfecho.em, card.ultimoDesfecho.estado)}`);
  }
  return frases;
}

export function CartaoDaCasa({ card }: { card: CardNaTela }) {
  const nome = card.pessoa?.nome ?? 'Casa sem nome. Atualize a lista.';
  const frases = frasesDoCard(card);
  const situacao = ROTULO_DA_SITUACAO[card.situacao].nome;
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${situacao}${card.lista === 'atendida' ? ', atendida' : ''}. ${nome}. ${card.pessoa?.endereco ?? ''}. ${frases.join(' ')}`}
      accessibilityHint="Abre a casa e a linha do tempo"
      onPress={() => router.push({ pathname: '/casa/[token]', params: { token: card.casaToken } })}
      style={({ pressed }) => [
        estilo.cartao,
        { borderLeftWidth: 10, borderLeftColor: COR_DA_SITUACAO[card.situacao] },
        pressed && { backgroundColor: COR.fundoSuave },
      ]}
    >
      <Selo situacao={card.situacao} atendida={card.lista === 'atendida'} />
      <Text style={[estilo.subtitulo, { marginTop: 0 }]}>{nome}</Text>
      {card.pessoa ? <Paragrafo secundario>{card.pessoa.endereco}</Paragrafo> : null}
      <View style={{ gap: 4 }}>
        {frases.map((f) => (
          <Paragrafo key={f}>{f}</Paragrafo>
        ))}
      </View>
    </Pressable>
  );
}
