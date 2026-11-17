// Gera src/api/mock/casas.ts a partir da semente simulada da API.
// Rode da pasta app/: npx tsx ferramentas/gerar-casas-do-mock.ts
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';

interface CasaDaSemente {
  n: number;
  nome: string;
  endereco: string;
  perfil: { contato: string; eletrodependente: boolean };
  vizinho: { nome: string; telefone: string } | null;
}

const semente = JSON.parse(
  readFileSync('../docs/arquitetura/referencia/dados/semente/casas.json', 'utf8'),
) as { microareaId: number; casas: CasaDaSemente[] };

const casas = semente.casas.map((c) => ({
  n: c.n,
  casaToken: createHash('md5').update(`casa-${c.n}`).digest('hex'),
  nome: c.nome,
  endereco: c.endereco,
  perfil: c.perfil,
  vizinho: c.vizinho ?? null,
}));

const corpo = `// Gerado por ferramentas/gerar-casas-do-mock.ts a partir da semente simulada.
// Nomes, endereços e telefones são inventados. O DDD 00 não existe.
import type { PerfilDaCasa } from '../contratos';

export interface CasaDoMock {
  n: number;
  casaToken: string;
  nome: string;
  endereco: string;
  perfil: PerfilDaCasa;
  vizinho: { nome: string; telefone: string } | null;
}

export const MICROAREA_DO_MOCK = ${semente.microareaId};

export const CASAS_DO_MOCK: readonly CasaDoMock[] = ${JSON.stringify(casas, null, 2)};
`;

writeFileSync('src/api/mock/casas.ts', corpo);
console.log(`${casas.length} casas gravadas em src/api/mock/casas.ts`);
