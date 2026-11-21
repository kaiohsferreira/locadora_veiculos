import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useApolices, useTotalApolices } from '@/features/seguros/hooks/useApolices';
import { formatarMoeda } from '@/lib/formatters';
import { ErroApi } from '@/types/api';
import type { Apolice, FiltroApolices, RespostaApolices } from '@/types/seguros';
import GestaoSegurosPagina from './GestaoSegurosPagina';

// Nenhum teste chama a API: o cliente HTTP vira dublê e os hooks devolvem consultas prontas.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));
vi.mock('@/features/seguros/hooks/useApolices', () => ({
  useApolices: vi.fn(),
  useTotalApolices: vi.fn(),
  useApolice: vi.fn(),
}));
vi.mock('@/features/seguros/hooks/useBuscaVeiculoPlaca', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/features/seguros/hooks/useBuscaVeiculoPlaca')>()),
  useBuscaVeiculoPlaca: () => ({ data: undefined, isError: false, error: null }),
}));

type ConsultaApolices = ReturnType<typeof useApolices>;

function apolice(id: number, dias: number, parcial: Partial<Apolice> = {}): Apolice {
  return {
    idSeguro: id,
    idVeiculo: 10 + id,
    placaVeiculo: `ABC1D2${id}`,
    seguradora: 'Porto Seguro',
    numeroApolice: `AP-${id}`,
    dataInicio: '2026-01-01',
    dataInicioStr: '01/01/2026',
    dataFim: '2026-12-30',
    dataFimStr: '30/12/2026',
    valorPremio: 3000 + id,
    valorPremioStr: '',
    cobertura: null,
    diasParaVencer: dias,
    venceEm60Dias: dias >= 0 && dias <= 60,
    ...parcial,
  };
}

function pagina(itens: Apolice[], total: number): RespostaApolices {
  return { itens, total, pagina: 1, tamanhoPagina: 20 };
}

function consulta(parcial: Record<string, unknown>): ConsultaApolices {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    isPlaceholderData: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaApolices;
}

function renderizar(lista: ConsultaApolices, rota = '/back-office/seguros'): string {
  vi.mocked(useApolices).mockReturnValue(lista);
  vi.mocked(useTotalApolices).mockImplementation((apenasAVencer: boolean) =>
    consulta({ data: pagina([], apenasAVencer ? 4 : 37) }),
  );
  return renderToStaticMarkup(
    <MemoryRouter initialEntries={[rota]}>
      <AvisoProvider>
        <GestaoSegurosPagina />
      </AvisoProvider>
    </MemoryRouter>,
  );
}

function filtroPedido(): FiltroApolices {
  const chamadas = vi.mocked(useApolices).mock.calls;
  return chamadas[chamadas.length - 1][0];
}

describe('GestaoSegurosPagina (P9-FE-01)', () => {
  beforeEach(() => {
    vi.mocked(useApolices).mockReset();
    vi.mocked(useTotalApolices).mockReset();
  });

  it('mostra os indicadores "Apólices cadastradas" e "Vencem em até 60 dias"', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }));

    expect(html).toContain('Apólices cadastradas');
    expect(html).toContain('inclui as vencidas');
    expect(html).toContain('37');
    expect(html).toContain('Vencem em até 60 dias');
    expect(html).toContain('>4<');
  });

  it('cada apólice mostra o vencimento com texto, não só cor', () => {
    const html = renderizar(
      consulta({ data: pagina([apolice(1, -5), apolice(2, 0), apolice(3, 30), apolice(4, 90, { venceEm60Dias: false })], 4) }),
    );

    expect(html).toContain('Vencida há 5 dias');
    expect(html).toContain('Vence hoje');
    expect(html).toContain('Vence em 30 dias');
    expect(html).toContain('Em dia');
    expect(html).toContain('até 30/12/2026');
    expect(html).toContain(formatarMoeda(3001));
    expect(html).toContain('01/01/2026 – 30/12/2026');
    expect(html).toContain('Editar');
  });

  it('pede ao servidor a página e os filtros do endereço, com 20 por página', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }), '/back-office/seguros?veiculo=7&placa=ABC1D23&aVencer=1&pagina=2');

    expect(filtroPedido()).toEqual({ pagina: 2, tamanhoPagina: 20, apenasAVencer: true, idVeiculo: 7 });
    expect(html).toContain('Placa ABC1D23');
    expect(html).toContain('aria-pressed="true"');
    expect(html).toContain('Nenhuma apólice para este veículo');
    expect(html).toContain('Ver todas');
  });

  it('sem apólices e sem filtro, convida a cadastrar a primeira', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }));

    expect(html).toContain('Nenhuma apólice cadastrada');
    expect(html).toContain('Nova apólice');
  });

  it('com "a vencer" e nenhuma, explica que nada vence nos próximos 60 dias', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }), '/back-office/seguros?aVencer=1');

    expect(html).toContain('Nenhuma apólice vence nos próximos 60 dias');
  });

  it('falha de conexão mostra o estado de erro com nova tentativa', () => {
    const html = renderizar(consulta({ isError: true, error: new ErroApi({ status: 0, mensagem: 'Sem conexão.' }) }));

    expect(html).toContain('Não foi possível carregar as apólices.');
    expect(html).toContain('Sem conexão.');
    expect(html).toContain('Tentar novamente');
  });
});
