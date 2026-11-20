import { describe, expect, it, vi } from 'vitest';
import type { QueryClient } from '@tanstack/react-query';
import { invalidarAposMudancaManutencao } from './invalidarManutencao';
import {
  CHAVE_MANUTENCOES,
  chaveAlertasRevisao,
  chaveFamiliaManutencao,
  chaveListagemManutencoes,
  chaveResumoManutencao,
} from './chavesManutencao';

describe('chaves da manutenção', () => {
  it('a família de manutenção é a raiz que as mutações invalidam (lista, resumo e alertas)', () => {
    expect(CHAVE_MANUTENCOES).toBe('manutencoes');
    expect(chaveFamiliaManutencao()).toEqual(['manutencoes']);
    expect(chaveResumoManutencao()[0]).toBe(CHAVE_MANUTENCOES);
    expect(chaveAlertasRevisao()[0]).toBe(CHAVE_MANUTENCOES);
  });

  it('a listagem leva o filtro inteiro, para cada combinação ter a sua chave', () => {
    const filtro = { pagina: 2, tamanhoPagina: 10, tipo: 'CORRETIVA' as const };

    expect(chaveListagemManutencoes(filtro)).toEqual(['manutencoes', 'lista', filtro]);
    expect(chaveListagemManutencoes({ ...filtro, pagina: 3 })).not.toEqual(chaveListagemManutencoes(filtro));
  });
});

describe('invalidarAposMudancaManutencao', () => {
  it('invalida a manutenção, a frota (o status do veículo muda) e a visão geral', async () => {
    const invalidateQueries = vi.fn().mockResolvedValue(undefined);
    const cliente = { invalidateQueries } as unknown as QueryClient;

    await invalidarAposMudancaManutencao(cliente);

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['manutencoes'] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['veiculos'] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['painel-visao-geral'] });
  });
});
