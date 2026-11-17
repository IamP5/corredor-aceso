// Adaptador do SQLite do Node para a interface `Banco`, só para os testes.
// Assim o SQL do app roda de verdade, com os CHECK e a junção do radar.
import { DatabaseSync } from 'node:sqlite';
import { migrar, type Banco } from '../banco/banco';

export async function abrirBancoDeTeste(): Promise<Banco> {
  const db = new DatabaseSync(':memory:');
  const banco: Banco = {
    async execAsync(sql) {
      db.exec(sql);
    },
    async runAsync(sql, params) {
      return db.prepare(sql).run(...params);
    },
    async getAllAsync<T>(sql: string, params: (string | number | null)[]) {
      return db.prepare(sql).all(...params) as T[];
    },
    async getFirstAsync<T>(sql: string, params: (string | number | null)[]) {
      return (db.prepare(sql).get(...params) as T | undefined) ?? null;
    },
    async withTransactionAsync(tarefa) {
      db.exec('BEGIN');
      try {
        await tarefa();
        db.exec('COMMIT');
      } catch (e) {
        db.exec('ROLLBACK');
        throw e;
      }
    },
  };
  await migrar(banco);
  return banco;
}
