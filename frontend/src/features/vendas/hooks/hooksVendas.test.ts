import { beforeEach, describe, expect, it, vi } from 'vitest';
import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query';
import { buscarVeiculoAVenda, enviarPropostaCompra, listarCatalogo, simularParcelamento } from '@/api/vendas';
import { chaveCatalogo, chaveSimulacao, chaveVeiculoAVenda } from './chavesVendas';
import { useCatalogoSeminovos } from './useCatalogoSeminovos';
import { useEnviarPropostaCompra } from './useEnviarPropostaCompra';
import { useSimulacaoParcelamento } from './useSimulacaoParcelamento';
import { useVeiculoAVenda } from './useVeiculoAVenda';

vi.mock('@tanstack/react-query', () => ({
  keepPreviousData: Symbol('keepPreviousData'),
  useQuery: vi.fn(),
  useMutation: vi.fn(),
}));
vi.mock('@/api/vendas', () => ({
  buscarVeiculoAVenda: vi.fn(),
  simularParcelamento: vi.fn(),
  enviarPropostaCompra: vi.fn(),
  listarCatalogo: vi.fn(),
}));
// Sem espera nos testes: o debounce devolve o valor recebido.
vi.mock('@/hooks/useDebounce', () => ({
  useDebounce: <T,>(valor: T): T => valor,
}));

const useQueryMock = vi.mocked(useQuery);
const useMutationMock = vi.mocked(useMutation);

interface OpcoesConsulta {
  queryKey: unknown[];
  queryFn: () => Promise<unknown>;
  enabled: boolean;
  placeholderData?: unknown;
}

/** Opções que o hook passou ao useQuery na última chamada. */
function opcoesConsulta(): OpcoesConsulta {
  return useQueryMock.mock.calls[useQueryMock.mock.calls.length - 1][0] as unknown as OpcoesConsulta;
}

describe('useVeiculoAVenda', () => {
  beforeEach(() => useQueryMock.mockReset());

  it('consulta o detalhe público pelo id', async () => {
    useVeiculoAVenda(4);
    const opcoes = opcoesConsulta();

    expect(opcoes.queryKey).toEqual(chaveVeiculoAVenda(4));
    expect(opcoes.enabled).toBe(true);
    await opcoes.queryFn();
    expect(buscarVeiculoAVenda).toHaveBeenCalledWith(4);
  });

  it('sem id, a consulta fica desligada', () => {
    useVeiculoAVenda(null);

    expect(opcoesConsulta().enabled).toBe(false);
  });
});

describe('useSimulacaoParcelamento', () => {
  beforeEach(() => useQueryMock.mockReset());

  it('com parâmetros válidos, simula e mantém o resultado anterior enquanto o novo chega', async () => {
    useSimulacaoParcelamento(4, 0, 3, 10000);
    const opcoes = opcoesConsulta();

    expect(opcoes.enabled).toBe(true);
    expect(opcoes.queryKey).toEqual(chaveSimulacao(4, { valorEntrada: 0, numeroParcelas: 3 }));
    expect(opcoes.placeholderData).toBe(keepPreviousData);
    await opcoes.queryFn();
    expect(simularParcelamento).toHaveBeenCalledWith(4, { valorEntrada: 0, numeroParcelas: 3 });
  });

  it.each([
    ['parcelas acima de 60', 0, 61],
    ['parcelas zero', 0, 0],
    ['entrada igual ao valor de venda', 10000, 12],
    ['campo de entrada vazio', null, 12],
    ['campo de parcelas vazio', 0, null],
  ])('não chama a API com %s', (_caso, valorEntrada, numeroParcelas) => {
    useSimulacaoParcelamento(4, valorEntrada as number | null, numeroParcelas as number | null, 10000);

    expect(opcoesConsulta().enabled).toBe(false);
  });

  it('sem veículo não simula', () => {
    useSimulacaoParcelamento(null, 0, 3, 10000);

    expect(opcoesConsulta().enabled).toBe(false);
  });
});

describe('useEnviarPropostaCompra', () => {
  beforeEach(() => useMutationMock.mockReset());

  it('usa o envio da API como função da mutação', () => {
    useEnviarPropostaCompra();
    const opcoes = useMutationMock.mock.calls[0][0] as unknown as { mutationFn: unknown };

    expect(opcoes.mutationFn).toBe(enviarPropostaCompra);
  });
});

describe('useCatalogoSeminovos', () => {
  beforeEach(() => useQueryMock.mockReset());

  it('consulta a página do filtro e mantém a lista anterior enquanto a nova chega', async () => {
    const filtro = { pagina: 2, tamanhoPagina: 20, marca: 'Jeep' };
    useCatalogoSeminovos(filtro);
    const opcoes = opcoesConsulta();

    expect(opcoes.queryKey).toEqual(chaveCatalogo(filtro));
    expect(opcoes.placeholderData).toBe(keepPreviousData);
    await opcoes.queryFn();
    expect(listarCatalogo).toHaveBeenCalledWith(filtro);
  });
});
