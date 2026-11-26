import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useApolices, useTotalApolices } from '@/features/seguros/hooks/useApolices';
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
    valorPremioStr: `R$ 3.00${id},00`,
    cobertura: 'Compreensiva, terceiros R$ 100 mil',
    diasParaVencer: dias,
    venceEm60Dias: dias >= 0 && dias <= 60,
    ...parcial,
  };
}

function pagina(itens: Apolice[], total: number, tamanhoPagina = 20): RespostaApolices {
  return { itens, total, pagina: 1, tamanhoPagina };
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

  it('cabeçalho do desenho: título, trilha e "+ Cadastrar apólice" com ícone de mais', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }));

    expect(html).toMatch(/<h1[^>]*>Seguros<\/h1>/);
    expect(html).toContain('Operações');
    expect(html).toContain('Gestão de risco e sinistros');
    expect(html).toMatch(/aria-current="page"[^>]*>Apólices de seguro</);
    expect(html).toMatch(/>add<\/span>Cadastrar apólice/);
  });

  it('cartões em linha: cadastradas e a vencer, com o selo e a barra proporcional', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }));

    expect(html).toContain('Apólices cadastradas');
    expect(html).toContain('inclui as vencidas');
    expect(html).toContain('>37<');
    expect(html).toContain('Apólices a vencer (≤ 60 dias)');
    expect(html).toContain('>4<');
    expect(html).toContain('Requer renovação imediata');
    expect(html).toMatch(/style="width:11%"[^>]*data-testid="barra-a-vencer"/);
  });

  it('título "Apólices da Frota em Vigência" com o selo de quantidade e as pílulas com contagem', () => {
    const html = renderizar(consulta({ data: pagina([apolice(1, 30)], 37) }));

    expect(html).toContain('Apólices da Frota em Vigência');
    expect(html).toMatch(/data-testid="selo-quantidade"[^>]*>37 apólices</);
    expect(html).toMatch(/aria-pressed="true"[^>]*>Todas \(37\)/);
    expect(html).toMatch(/aria-pressed="false"[^>]*>(?:(?!<\/button>).)*Vencendo em breve \(4\)/s);
  });

  it('cada linha mostra cobertura sob o número, vigência "→" com o prazo, iniciais, prêmio do servidor e status', () => {
    const html = renderizar(
      consulta({ data: pagina([apolice(1, -5), apolice(2, 0), apolice(3, 31), apolice(4, 216, { venceEm60Dias: false })], 4) }),
    );

    expect(html).toMatch(/AP-1<\/span><span[^>]*>Compreensiva, terceiros R\$ 100 mil</);
    expect(html).toContain('01/01/2026 → 30/12/2026');
    expect(html).toContain('Faltam 31 dias');
    expect(html).toContain('Faltam 216 dias');
    expect(html).toMatch(/data-testid="iniciais-seguradora"[^>]*>PS</);
    expect(html).toContain('R$ 3.001,00');
    expect(html).toContain('Vencida há 5 dias');
    expect(html).toContain('Vence hoje');
    expect(html).toContain('Vence em breve (31 dias)');
    expect(html).toContain('Vigente');
    expect(html).toContain('Editar');
  });

  it('rodapé com "Itens por página" e o texto da paginação', () => {
    const html = renderizar(consulta({ data: pagina([apolice(1, 30)], 45, 10) }), '/back-office/seguros?itens=10');

    expect(filtroPedido().tamanhoPagina).toBe(10);
    expect(html).toContain('Itens por página:');
    expect(html).toMatch(/<option value="10" selected="">10<\/option>/);
    expect(html).toMatch(/Exibindo <span[^>]*>1–10<\/span> de <span[^>]*>45<\/span> apólices/);
  });

  it('pede ao servidor a página e os filtros do endereço, com 20 por página', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }), '/back-office/seguros?veiculo=7&placa=ABC1D23&aVencer=1&pagina=2');

    expect(filtroPedido()).toEqual({ pagina: 2, tamanhoPagina: 20, apenasAVencer: true, idVeiculo: 7 });
    expect(html).toContain('Placa ABC1D23');
    expect(html).toMatch(/aria-pressed="true"[^>]*>(?:(?!<\/button>).)*Vencendo em breve/s);
    expect(html).toContain('Nenhuma apólice para este veículo');
    expect(html).toContain('Ver todas');
  });

  it('enquanto a página carrega, mostra a tabela carregando e os cartões sem número', () => {
    vi.mocked(useApolices).mockReturnValue(consulta({ isPending: true }));
    vi.mocked(useTotalApolices).mockImplementation(() => consulta({ isPending: true }));
    const html = renderToStaticMarkup(
      <MemoryRouter initialEntries={['/back-office/seguros']}>
        <AvisoProvider>
          <GestaoSegurosPagina />
        </AvisoProvider>
      </MemoryRouter>,
    );

    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('Nenhuma apólice cadastrada');
    expect(html).not.toContain('Requer renovação imediata');
    expect(html).not.toContain('data-testid="selo-quantidade"');
    expect(html).toContain('Todas');
    expect(html).not.toContain('Todas (');
  });

  it('sem apólices e sem filtro, convida a cadastrar a primeira', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }));

    expect(html).toContain('Nenhuma apólice cadastrada');
    expect(html).toContain('Cadastrar apólice');
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
