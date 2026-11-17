// A sessão da ACS fica no expo-secure-store, nunca no SQLite (04-modelo-de-dados.md, seção 6).
// No navegador o secure-store não existe, e a versão web do app usa o localStorage.
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

// O token é o código de acesso que a UBS entrega a cada ACS. A API o recebe em `Authorization: Bearer`
// e descobre sozinha quem é a ACS e a microárea dela (api/README.md, "Autenticação"). É segredo: não aparece na tela.
export interface Sessao {
  token: string;
  microarea: number;
}

const CHAVE = 'corredor.sessao';

/** O código como a ACS digitou, sem espaço nas pontas. Maiúsculas valem: o token é comparado byte a byte. */
export function lerCodigoDeAcesso(texto: string): string | null {
  const codigo = texto.trim();
  return /^\S{3,200}$/.test(codigo) ? codigo : null;
}

export async function lerSessao(): Promise<Sessao | null> {
  const texto = Platform.OS === 'web' ? globalThis.localStorage?.getItem(CHAVE) : await SecureStore.getItemAsync(CHAVE);
  if (!texto) return null;
  const s = JSON.parse(texto) as Partial<Sessao>;
  return typeof s.token === 'string' && typeof s.microarea === 'number' ? { token: s.token, microarea: s.microarea } : null;
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
