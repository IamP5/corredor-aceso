// O pedaço da API do expo-sqlite que o app usa. O `SQLiteDatabase` do expo-sqlite
// cabe nesta interface, e o teste usa o SQLite do Node com a mesma forma.
import { ESQUEMA_V1 } from './esquema';

export type Valor = string | number | null;

export interface Banco {
  execAsync(sql: string): Promise<void>;
  runAsync(sql: string, params: Valor[]): Promise<unknown>;
  getAllAsync<T>(sql: string, params: Valor[]): Promise<T[]>;
  getFirstAsync<T>(sql: string, params: Valor[]): Promise<T | null>;
  withTransactionAsync(tarefa: () => Promise<void>): Promise<void>;
}

export const NOME_DO_BANCO = 'corredor.db';

/** Roda no `onInit` do `SQLiteProvider`. Só cria tabela. Nenhuma migração apaga coluna. */
export async function migrar(banco: Banco): Promise<void> {
  const linha = await banco.getFirstAsync<{ user_version: number }>('PRAGMA user_version', []);
  if ((linha?.user_version ?? 0) >= 1) return;
  await banco.withTransactionAsync(async () => {
    await banco.execAsync(ESQUEMA_V1);
    await banco.execAsync('PRAGMA user_version = 1');
  });
}

/** Saída da conta: apaga as seis tabelas. */
export async function apagarTudo(banco: Banco): Promise<void> {
  await banco.withTransactionAsync(async () => {
    for (const tabela of ['sincronizacao', 'lista_de_nomes', 'radar_cabecalho', 'radar_card', 'aviso', 'desfecho']) {
      await banco.runAsync(`DELETE FROM ${tabela}`, []);
    }
  });
}
