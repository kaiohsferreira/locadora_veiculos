import { beforeEach, describe, expect, it, vi } from 'vitest';
import { QueryClient, type UseMutationOptions, type UseQueryOptions } from '@tanstack/react-query';
import { useDevolucao, useRegistrarSaida, chaveDevolucao } from './useDevolucao';
import type { ComparacaoSaida, ContextoDevolucao, RegistrarSaida } from '@/types/devolucao';
import { buscarContextoDevolucao, registrarSaida } from '@/api/devolucao';
import { ErroApi } from '@/types/api';

let cliente: QueryClient;
let opcoes: UseMutationOptions<ComparacaoSaida, unknown, RegistrarSaida>;
let consulta: UseQueryOptions;
vi.mock('@tanstack/react-query', async importar => {
  const real = await importar<typeof import('@tanstack/react-query')>();
  return { ...real, useQueryClient: () => cliente, useMutation: (valor: typeof opcoes) => { opcoes = valor; return valor; }, useQuery: (valor: UseQueryOptions) => { consulta = valor; return valor; } };
});
vi.mock('@/api/devolucao', () => ({ buscarContextoDevolucao: vi.fn(), registrarSaida: vi.fn() }));
describe('cache da devolução', () => {
  beforeEach(() => { cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } }); vi.clearAllMocks(); });
  it('isola a chave e desabilita ID inválido', () => {
    useDevolucao(null); expect(consulta.enabled).toBe(false); expect(consulta.retry).toBe(false);
    useDevolucao(42); expect(consulta.queryKey).toEqual(['devolucao', 42]); expect(consulta.enabled).toBe(true);
    expect(chaveDevolucao(43)).not.toEqual(chaveDevolucao(42)); expect(buscarContextoDevolucao).not.toHaveBeenCalled();
  });
  it('cancela GET antigo antes de publicar saída e recarrega contexto', async () => {
    const ordem: string[] = [];
    vi.spyOn(cliente, 'cancelQueries').mockImplementation(async () => { ordem.push('cancelar'); });
    const atualizar = vi.spyOn(cliente, 'setQueryData');
    vi.spyOn(cliente, 'invalidateQueries').mockImplementation(async () => { ordem.push('recarregar'); });
    const contexto = { locacao: { idLocacao: 42 }, comparacao: null } as ContextoDevolucao;
    cliente.setQueryData(chaveDevolucao(42), contexto);
    cliente.setQueryData(['vistoria-entrada', 42], 'entrada preservada');
    cliente.setQueryData(['retirada', 42], 'retirada preservada');
    useRegistrarSaida(42);
    const comparacao = { comparacaoRealizada: true } as ComparacaoSaida;
    await opcoes.onSuccess?.(comparacao, {} as RegistrarSaida, undefined, {} as never);
    expect(ordem).toEqual(['cancelar', 'recarregar']); expect(atualizar).toHaveBeenCalled();
    expect(cliente.getQueryData<ContextoDevolucao>(chaveDevolucao(42))?.comparacao).toBe(comparacao);
    expect(cliente.getQueryData(['vistoria-entrada', 42])).toBe('entrada preservada');
    expect(cliente.getQueryData(['retirada', 42])).toBe('retirada preservada');
  });
  it.each([409, 0])('erro %s recarrega para recuperar persistência e nunca repete POST', async status => {
    const invalidar = vi.spyOn(cliente, 'invalidateQueries').mockResolvedValue();
    useRegistrarSaida(42);
    expect(opcoes.retry).toBe(false);
    await opcoes.onError?.(new ErroApi({ status, mensagem: 'Conflito.' }), {} as RegistrarSaida, undefined, {} as never);
    expect(invalidar).toHaveBeenCalledWith({ queryKey: ['devolucao', 42] }); expect(registrarSaida).not.toHaveBeenCalled();
  });
});
