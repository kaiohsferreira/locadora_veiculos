import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { ErroApi } from '@/types/api';
import { CHAVE_PAINEL_FINANCEIRO } from '@/features/financeiro/hooks/chavesPainelFinanceiro';
import { CHAVE_VISAO_GERAL } from '@/features/visao-geral/chaveVisaoGeral';
import {
  invalidarPainelFinanceiro,
  useBaixarPagamento,
  type AlvoInvalidacaoPainelFinanceiro,
} from './usePainelFinanceiro';

// A camada de API carrega o cliente HTTP, que usa `window` ao ser importado: no ambiente de teste ele é trocado por um mock.
vi.mock('@/api/client', () => ({ api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() } }));

// Sem DOM: a mutação e o cliente do TanStack Query viram dublês. O teste pega as opções que o hook passa (onSettled).
vi.mock('@tanstack/react-query', async (importOriginal) => {
  const original = await importOriginal<typeof import('@tanstack/react-query')>();
  return { ...original, useMutation: vi.fn(), useQueryClient: vi.fn() };
});

describe('invalidarPainelFinanceiro (raiz das consultas do painel)', () => {
  it('invalida a raiz do painel, para indicadores e tabela serem recarregados juntos', () => {
    const invalidateQueries = vi.fn<AlvoInvalidacaoPainelFinanceiro['invalidateQueries']>(() => Promise.resolve());
    invalidarPainelFinanceiro({ invalidateQueries });

    expect(invalidateQueries).toHaveBeenCalledTimes(1);
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: [CHAVE_PAINEL_FINANCEIRO] });
  });

  it('não devolve a promise da recarga (o onSettled da baixa não fica esperando a rede)', () => {
    const resultado: unknown = invalidarPainelFinanceiro({
      invalidateQueries: () => new Promise<unknown>(() => undefined),
    });
    expect(resultado).toBeUndefined();
  });
});

describe('useBaixarPagamento (a mutação recarrega o painel depois de sucesso e de erro)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('o onSettled invalida o painel e a visão geral tanto no sucesso quanto no erro', () => {
    const invalidateQueries = vi.fn<AlvoInvalidacaoPainelFinanceiro['invalidateQueries']>(() => Promise.resolve());
    vi.mocked(useQueryClient).mockReturnValue({ invalidateQueries } as unknown as QueryClient);
    vi.mocked(useMutation).mockImplementation(((opcoes: unknown) => opcoes) as unknown as typeof useMutation);

    const opcoes = useBaixarPagamento() as unknown as { onSettled: (...argumentos: unknown[]) => unknown };

    // O TanStack chama onSettled nos dois casos: (dados, erro, variáveis).
    opcoes.onSettled({}, undefined, undefined);
    expect(invalidateQueries).toHaveBeenCalledTimes(2);
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: [CHAVE_PAINEL_FINANCEIRO] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: [CHAVE_VISAO_GERAL] });

    invalidateQueries.mockClear();
    opcoes.onSettled(undefined, new ErroApi({ status: 409, mensagem: 'Este pagamento já foi baixado.' }), undefined);
    expect(invalidateQueries).toHaveBeenCalledTimes(2);
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: [CHAVE_PAINEL_FINANCEIRO] });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: [CHAVE_VISAO_GERAL] });
  });
});
