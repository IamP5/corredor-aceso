// Liga a API, o banco do celular e a rede. As telas leem o banco e chamam estas ações.
import NetInfo from '@react-native-community/netinfo';
import * as Crypto from 'expo-crypto';
import { useSQLiteContext } from 'expo-sqlite';
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { criarApiHttp, type ApiCorredor } from '../api/cliente';
import { MODO, URL_DA_API } from '../api/config';
import type { Desfecho } from '../api/contratos';
import { criarApiMock } from '../api/mock/mock';
import { apagarTudo, type Banco } from '../banco/banco';
import { trocarAvisos, trocarListaDeNomes, trocarRadar } from '../banco/cache';
import { contarPendentes, enviarFila, registrarDesfecho } from '../banco/fila';
import { gravarSessao, lerSessao, type Sessao } from './sessao';

const INTERVALO_DE_REENVIO_MS = 15_000;

interface Corredor {
  banco: Banco;
  api: ApiCorredor;
  modo: typeof MODO;
  sessao: Sessao | null;
  carregando: boolean;
  conectado: boolean;
  sincronizando: boolean;
  /** Muda a cada gravação no banco. As telas releem quando ela muda. */
  versao: number;
  pendentes: number;
  /** Código do último erro de sincronização, ou `null`. */
  erroDeSincronizacao: string | null;
  entrar(sessao: Sessao): Promise<void>;
  sair(): Promise<void>;
  sincronizar(): Promise<void>;
  registrarVisita(casaToken: string, desfecho: Desfecho, observacao: string | null): Promise<void>;
  aposGravar(): void;
}

const Contexto = createContext<Corredor | null>(null);

export function useCorredor(): Corredor {
  const c = useContext(Contexto);
  if (!c) throw new Error('useCorredor fora do ProvedorDoCorredor');
  return c;
}

export function ProvedorDoCorredor({ children }: { children: ReactNode }) {
  const banco = useSQLiteContext() as Banco;
  const [sessao, setSessao] = useState<Sessao | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [conectado, setConectado] = useState(true);
  const [sincronizando, setSincronizando] = useState(false);
  const [versao, setVersao] = useState(0);
  const [pendentes, setPendentes] = useState(0);
  const [erroDeSincronizacao, setErro] = useState<string | null>(null);
  const conectadoRef = useRef(true);
  const sessaoRef = useRef<Sessao | null>(null);
  sessaoRef.current = sessao;

  const api = useMemo<ApiCorredor>(
    () =>
      URL_DA_API
        ? criarApiHttp(URL_DA_API, () => sessaoRef.current?.acsId ?? null)
        : criarApiMock({ conectado: () => conectadoRef.current, latenciaMs: 300 }),
    [],
  );

  const aposGravar = useCallback(() => {
    setVersao((v) => v + 1);
    void contarPendentes(banco).then(setPendentes);
  }, [banco]);

  const enviar = useCallback(async () => {
    const r = await enviarFila(banco, api);
    if (r.enviados > 0 || r.recusados > 0 || r.parouPor) aposGravar();
    if (r.parouPor?.tipo === 'pedirEntrada') {
      // 401: a fila fica guardada e a ACS entra de novo.
      await gravarSessao(null);
      setSessao(null);
    }
  }, [banco, api, aposGravar]);

  const sincronizar = useCallback(async () => {
    const s = sessaoRef.current;
    if (!s) return;
    setSincronizando(true);
    try {
      await enviar();
      const [lista, radar, avisos] = await Promise.allSettled([
        api.listaDeNomes(s.microarea),
        api.radar(s.microarea),
        api.avisos(s.microarea),
      ]);
      const agora = new Date();
      // A lista de nomes entra antes do radar, para o card novo já chegar com nome.
      if (lista.status === 'fulfilled') await trocarListaDeNomes(banco, lista.value, agora);
      if (radar.status === 'fulfilled') await trocarRadar(banco, s.microarea, radar.value, agora);
      if (avisos.status === 'fulfilled') await trocarAvisos(banco, avisos.value, agora);
      const falha = [lista, radar, avisos].find((r) => r.status === 'rejected');
      setErro(falha ? ((falha.reason as { codigo?: string }).codigo ?? 'ERRO') : null);
    } finally {
      setSincronizando(false);
      aposGravar();
    }
  }, [api, banco, enviar, aposGravar]);

  useEffect(() => {
    void (async () => {
      setSessao(await lerSessao());
      setPendentes(await contarPendentes(banco));
      setCarregando(false);
    })();
  }, [banco]);

  // Ao abrir e ao voltar a rede, o app envia a fila e baixa o radar.
  useEffect(() => {
    if (!carregando && sessao) void sincronizar();
  }, [carregando, sessao, sincronizar]);

  useEffect(
    () =>
      NetInfo.addEventListener((estado) => {
        const agora = estado.isConnected !== false;
        const voltou = agora && !conectadoRef.current;
        conectadoRef.current = agora;
        setConectado(agora);
        if (voltou) void sincronizar();
      }),
    [sincronizar],
  );

  // A rede pode estar ligada e a API fora do ar. Enquanto houver fila, tenta de novo.
  useEffect(() => {
    if (pendentes === 0) return;
    const relogio = setInterval(() => void enviar(), INTERVALO_DE_REENVIO_MS);
    return () => clearInterval(relogio);
  }, [pendentes, enviar]);

  const valor: Corredor = {
    banco,
    api,
    modo: MODO,
    sessao,
    carregando,
    conectado,
    sincronizando,
    versao,
    pendentes,
    erroDeSincronizacao,
    aposGravar,
    sincronizar,
    async entrar(nova) {
      await gravarSessao(nova);
      setSessao(nova);
    },
    async sair() {
      await apagarTudo(banco);
      await gravarSessao(null);
      setSessao(null);
      aposGravar();
    },
    async registrarVisita(casaToken, desfecho, observacao) {
      await registrarDesfecho(banco, { id: Crypto.randomUUID(), casaToken, desfecho, observacao, ocorridaEm: new Date() });
      aposGravar();
      void enviar();
    },
  };

  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>;
}
