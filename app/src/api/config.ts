// Com EXPO_PUBLIC_API_URL definida, o app fala com a API. Sem ela, usa o mock.
// O Expo troca `process.env.EXPO_PUBLIC_*` pelo valor na hora de montar o bundle.
const url = process.env.EXPO_PUBLIC_API_URL?.trim();

export const URL_DA_API: string | null = url ? url : null;
export const MODO: 'api' | 'mock' = URL_DA_API ? 'api' : 'mock';
