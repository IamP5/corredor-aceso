import { useEffect, useState } from 'react';
import { lerRadarNaTela, type CabecalhoDoRadar, type CardNaTela } from '../banco/cache';
import { useCorredor } from './corredor';

/** O radar guardado no celular. Relê a cada gravação no banco. */
export function useRadarNaTela(): { cabecalho: CabecalhoDoRadar | null; cards: CardNaTela[] } | null {
  const { banco, versao } = useCorredor();
  const [dados, setDados] = useState<Awaited<ReturnType<typeof lerRadarNaTela>> | null>(null);
  useEffect(() => {
    let viva = true;
    void lerRadarNaTela(banco).then((d) => viva && setDados(d));
    return () => {
      viva = false;
    };
  }, [banco, versao]);
  return dados;
}

/** Relógio que muda a cada `intervaloMs`, para o radar velho se declarar velho sem toque. */
export function useAgora(intervaloMs = 30_000): Date {
  const [agora, setAgora] = useState(() => new Date());
  useEffect(() => {
    const relogio = setInterval(() => setAgora(new Date()), intervaloMs);
    return () => clearInterval(relogio);
  }, [intervaloMs]);
  return agora;
}
