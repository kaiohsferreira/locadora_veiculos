import { beforeEach, describe, expect, it, vi } from 'vitest';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { FiltroListagemVeiculos } from '@/types/veiculos';
import { listarVeiculos } from '@/api/veiculos';
import { useListarVeiculos } from './useListarVeiculos';

// O hook só chama useQuery: o dublê captura as opções. Não há React aqui, então o hook roda como função comum.
vi.mock('@tanstack/react-query', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-query')>()),
  useQuery: vi.fn(() => ({})),
}));

// A API não roda nestes testes: a função de busca do hook chama este dublê.
vi.mock('@/api/veiculos', () => ({
  listarVeiculos: vi.fn(async () => ({ itens: [], total: 0, pagina: 1, tamanhoPagina: 10 })),
}));

const useQueryMock = vi.mocked(useQuery);
const listarVeiculosMock = vi.mocked(listarVeiculos);

function opcoesDaUltimaConsulta() {
  const chamada = useQueryMock.mock.calls[useQueryMock.mock.calls.length - 1];
  return chamada[0] as unknown as {
    queryKey: readonly unknown[];
    queryFn: () => Promise<unknown>;
    placeholderData: unknown;
  };
}

describe('useListarVeiculos', () => {
  beforeEach(() => {
    useQueryMock.mockClear();
  });

  it('a chave de cache leva o filtro inteiro: filtros diferentes têm chaves diferentes', () => {
    const porPagina: FiltroListagemVeiculos = { pagina: 1, tamanhoPagina: 10 };
    const outraPagina: FiltroListagemVeiculos = { pagina: 2, tamanhoPagina: 10 };
    const comStatus: FiltroListagemVeiculos = { pagina: 1, tamanhoPagina: 10, status: 'LOCADO' };
    const comBusca: FiltroListagemVeiculos = { pagina: 1, tamanhoPagina: 10, busca: 'Onix' };

    useListarVeiculos(porPagina);
    const chaveA = opcoesDaUltimaConsulta().queryKey;
    useListarVeiculos(outraPagina);
    const chaveB = opcoesDaUltimaConsulta().queryKey;
    useListarVeiculos(comStatus);
    const chaveC = opcoesDaUltimaConsulta().queryKey;
    useListarVeiculos(comBusca);
    const chaveD = opcoesDaUltimaConsulta().queryKey;

    expect(chaveA).not.toEqual(chaveB);
    expect(chaveA).not.toEqual(chaveC);
    expect(chaveA).not.toEqual(chaveD);
    expect(chaveA[0]).toBe('veiculos');
    expect(chaveA[1]).toBe('lista');
  });

  it('mantém a lista anterior enquanto a próxima carrega (keepPreviousData)', () => {
    useListarVeiculos({ pagina: 1, tamanhoPagina: 10 });

    expect(opcoesDaUltimaConsulta().placeholderData).toBe(keepPreviousData);
  });

  it('a função de busca chama a listagem da API com o mesmo filtro', async () => {
    const filtro: FiltroListagemVeiculos = { pagina: 3, tamanhoPagina: 25, idCategoria: 2 };
    useListarVeiculos(filtro);

    await opcoesDaUltimaConsulta().queryFn();

    expect(listarVeiculosMock).toHaveBeenCalledWith(filtro);
  });
});
