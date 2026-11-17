// A sessão da ACS fica no expo-secure-store, nunca no SQLite (04-modelo-de-dados.md, seção 6).
// No navegador o secure-store não existe, e a versão web do app usa o localStorage.
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

export interface Sessao {
  acsId: string;
  microarea: number;
}

const CHAVE = 'corredor.sessao';

export const FORMATO_DO_ACS_ID = /^[a-z0-9-]{3,40}$/;

export async function lerSessao(): Promise<Sessao | null> {
  const texto = Platform.OS === 'web' ? globalThis.localStorage?.getItem(CHAVE) : await SecureStore.getItemAsync(CHAVE);
  if (!texto) return null;
  const s = JSON.parse(texto) as Partial<Sessao>;
  return typeof s.acsId === 'string' && typeof s.microarea === 'number' ? { acsId: s.acsId, microarea: s.microarea } : null;
}

export async function gravarSessao(sessao: Sessao | null): Promise<void> {
  if (Platform.OS === 'web') {
    if (sessao) globalThis.localStorage?.setItem(CHAVE, JSON.stringify(sessao));
    else globalThis.localStorage?.removeItem(CHAVE);
    return;
  }
  if (sessao) await SecureStore.setItemAsync(CHAVE, JSON.stringify(sessao));
  else await SecureStore.deleteItemAsync(CHAVE);
}
