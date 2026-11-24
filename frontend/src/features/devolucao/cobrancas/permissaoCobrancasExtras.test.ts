import { describe, expect, it } from 'vitest';
import { podeApurarCobrancasExtras } from './permissaoCobrancasExtras';

describe('podeApurarCobrancasExtras (simular e apurar: só ATENDENTE e ADMINISTRADOR)', () => {
  it.each(['ATENDENTE', 'ADMINISTRADOR'] as const)('libera %s', (cargo) => {
    expect(podeApurarCobrancasExtras(cargo)).toBe(true);
  });

  it.each(['VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO'] as const)('nega %s', (cargo) => {
    expect(podeApurarCobrancasExtras(cargo)).toBe(false);
  });

  it('nega sessão sem cargo (cliente ou sessão vazia)', () => {
    expect(podeApurarCobrancasExtras(null)).toBe(false);
  });
});
