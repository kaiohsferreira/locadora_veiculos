import { expect, it } from 'vitest';
import type { ContextoDevolucao } from '@/types/devolucao';
import { motivoBloqueioSimulacao } from './validacaoCobrancasExtras';
it('contexto sem comparação exige vistoria de saída', () => {
  expect(motivoBloqueioSimulacao({ comparacao: null } as ContextoDevolucao)).toContain('comparação');
});
