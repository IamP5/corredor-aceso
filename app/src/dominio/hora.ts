// São Paulo não tem horário de verão desde 2019, então o fuso é sempre -03:00.
// Fixar o deslocamento evita depender do Intl do Hermes e deixa o teste determinístico.
const DESLOCAMENTO_MS = -3 * 60 * 60 * 1000;

function noRelogioDeSp(instante: Date): Date {
  return new Date(instante.getTime() + DESLOCAMENTO_MS);
}

const dois = (n: number) => String(n).padStart(2, '0');

/** Hora ISO em UTC com `Z`, a forma que o SQLite do app grava. */
export function paraUtc(iso: string): string {
  return new Date(iso).toISOString();
}

/** "10h31", no relógio de São Paulo. */
export function horaCurta(iso: string): string {
  const sp = noRelogioDeSp(new Date(iso));
  return `${dois(sp.getUTCHours())}h${dois(sp.getUTCMinutes())}`;
}

/** "2026-11-18", a data de São Paulo, usada na rota do dia crítico. */
export function dataDeSp(instante: Date): string {
  const sp = noRelogioDeSp(instante);
  return `${sp.getUTCFullYear()}-${dois(sp.getUTCMonth() + 1)}-${dois(sp.getUTCDate())}`;
}

/** "2026-11-18T10:31:00-03:00", a forma que a API usa. */
export function isoDeSp(instante: Date): string {
  const sp = noRelogioDeSp(instante);
  return `${dataDeSp(instante)}T${dois(sp.getUTCHours())}:${dois(sp.getUTCMinutes())}:${dois(sp.getUTCSeconds())}-03:00`;
}

/** O instante de hoje, em São Paulo, à hora e ao minuto dados. */
export function hojeAs(agora: Date, horas: number, minutos: number, segundos = 0): Date {
  return new Date(`${dataDeSp(agora)}T${dois(horas)}:${dois(minutos)}:${dois(segundos)}-03:00`);
}
