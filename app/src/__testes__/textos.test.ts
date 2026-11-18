import type { MotivoDoAlerta } from '../api/contratos';
import {
  motivoCurto,
  ROTULO_DA_SITUACAO,
  textoDoDesfechoNoCard,
  textoDoErro,
  textoDoMotivo,
  tituloDoRadar,
  textoDosVerdes,
} from '../dominio/textos';
import { dataDeSp, horaCurta, isoDeSp } from '../dominio/hora';

describe('situação em texto', () => {
  it('cada situação tem nome, símbolo e ação diferentes, então a cor nunca fica sozinha', () => {
    const rotulos = Object.values(ROTULO_DA_SITUACAO);
    for (const campo of ['nome', 'simbolo', 'acao'] as const) {
      const valores = rotulos.map((r) => r[campo]);
      expect(valores.every((v) => v.length > 0)).toBe(true);
      expect(new Set(valores).size).toBe(valores.length);
    }
  });

  it.each<[MotivoDoAlerta, string]>([
    ['EnergiaEletrodependente', 'Sem energia na casa desde 09h52. A pessoa usa aparelho de saúde na tomada.'],
    ['SemHeartbeat', 'Aparelho sem sinal desde 09h52. Não sabemos se a pessoa está bem.'],
    ['CalorInterno', 'Calor forte dentro de casa desde 09h52.'],
    ['SemMovimentoNaJanela', 'Ninguém passou no corredor. Sem sinal de vida desde 09h52.'],
  ])('%s vira frase com a hora de São Paulo', (motivo, frase) => {
    expect(textoDoMotivo(motivo, '2026-11-18T12:52:00Z')).toBe(frase);
  });

  it('cada motivo curto é diferente e traz a hora', () => {
    const motivos: MotivoDoAlerta[] = ['EnergiaEletrodependente', 'SemHeartbeat', 'CalorInterno', 'SemMovimentoNaJanela'];
    const frases = motivos.map((m) => motivoCurto(m, '2026-11-18T12:52:00Z'));
    expect(new Set(frases).size).toBe(motivos.length);
    for (const f of frases) expect(f).toContain('09h52');
    expect(motivoCurto('SemHeartbeat', '2026-11-18T12:40:00Z', '2026-11-18T09:10:00-03:00')).toBe('Aparelho sem sinal desde 09h10');
  });

  it('o cinza mostra a hora do último pacote', () => {
    expect(textoDoMotivo('SemHeartbeat', '2026-11-18T12:40:00Z', '2026-11-18T09:10:00-03:00')).toContain('desde 09h10');
  });

  it('o desfecho NaoAbriu aparece como SAMU chamado', () => {
    expect(textoDoDesfechoNoCard('NaoAbriu', '2026-11-18T13:40:00Z', 'enviado')).toBe('SAMU chamado às 10h40. Enviado.');
    expect(textoDoDesfechoNoCard('EstavaBem', '2026-11-18T13:12:00Z', 'pendente')).toBe(
      'Estava bem às 10h12. Ainda não enviado, sai sozinho quando a conexão voltar.',
    );
  });

  it('todo código de erro vira frase, nunca o código cru', () => {
    for (const codigo of ['TOKEN_INATIVO', 'VISITA_NO_FUTURO', 'SEM_CONEXAO', 'QUALQUER_OUTRO']) {
      expect(textoDoErro(codigo)).not.toContain(codigo);
      expect(textoDoErro(codigo).length).toBeGreaterThan(10);
    }
  });

  it('radar com mais de 2 minutos se declara velho', () => {
    const baixado = '2026-11-18T13:31:00.000Z';
    expect(tituloDoRadar(baixado, new Date('2026-11-18T13:32:00Z'))).toEqual({ titulo: 'Radar de agora', velho: false });
    expect(tituloDoRadar(baixado, new Date('2026-11-18T13:34:00Z'))).toEqual({ titulo: 'Radar das 10h31, sem conexão', velho: true });
    expect(textoDosVerdes(27, baixado, true)).toBe('27 casas deram sinal até 10h31');
  });
});

describe('hora de São Paulo', () => {
  it('converte perto da meia-noite sem errar o dia', () => {
    const instante = new Date('2026-11-19T02:30:00Z');
    expect(dataDeSp(instante)).toBe('2026-11-18');
    expect(isoDeSp(instante)).toBe('2026-11-18T23:30:00-03:00');
    expect(horaCurta('2026-11-18T23:30:00-03:00')).toBe('23h30');
  });
});
