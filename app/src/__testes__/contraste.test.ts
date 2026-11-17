import { PARES_DE_CONTRASTE, razaoDeContraste } from '../ui/tema';

describe('contraste', () => {
  it('confere a fórmula com valores conhecidos', () => {
    expect(razaoDeContraste('#000000', '#FFFFFF')).toBeCloseTo(21, 5);
    expect(razaoDeContraste('#767676', '#FFFFFF')).toBeCloseTo(4.54, 2);
  });

  it.each(PARES_DE_CONTRASTE)('$nome passa de 4,5:1', ({ texto, fundo }) => {
    expect(razaoDeContraste(texto, fundo)).toBeGreaterThanOrEqual(4.5);
  });
});
