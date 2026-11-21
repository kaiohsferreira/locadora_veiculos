import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { ErroApi } from '@/types/api';
import type { RespostaIndicadoresFinanceiros, RespostaTitulosFinanceiros, TituloFinanceiro } from '@/types/painelFinanceiro';
import {
  useIndicadoresFinanceiros,
  useTitulosFinanceiros,
} from '@/features/financeiro/hooks/usePainelFinanceiro';
import { termosCortadosPresentesPainel } from '@/features/financeiro/painel/termosCortadosPainel';
import PainelFinanceiroPagina from './PainelFinanceiroPagina';

// Nenhum teste chama a API: as consultas viram dublês.
vi.mock('@/features/financeiro/hooks/usePainelFinanceiro', () => ({
  useIndicadoresFinanceiros: vi.fn(),
  useTitulosFinanceiros: vi.fn(),
  useBaixarPagamento: vi.fn(),
}));

type ConsultaIndicadores = ReturnType<typeof useIndicadoresFinanceiros>;
type ConsultaTitulos = ReturnType<typeof useTitulosFinanceiros>;

const INDICADORES: RespostaIndicadoresFinanceiros = {
  de: '2026-11-01',
  ate: '2026-11-30',
  deStr: '01/11/2026',
  ateStr: '30/11/2026',
  recebidoValor: 980,
  recebidoStr: 'R$ 980,00',
  quantidadeRecebido: 1,
  aReceberValor: 1194.2,
  aReceberStr: 'R$ 1.194,20',
  quantidadeAReceber: 2,
  emAtrasoValor: 1840,
  emAtrasoStr: 'R$ 1.840,00',
  quantidadeEmAtraso: 1,
  ticketMedioValor: 980,
  ticketMedioStr: 'R$ 980,00',
  quantidadeTicket: 1,
};

const TITULO_ATRASADO: TituloFinanceiro = {
  idPagamento: 10,
  origem: 'LOCACAO',
  origemStr: 'Locação nº 12',
  tipo: 'COBRANCA_EXTRA',
  tipoStr: 'Cobrança extra',
  clienteNome: 'Rodrigo Alcantara Dias',
  clienteCpfMascarado: '194.***.***-12',
  valor: 1840,
  valorStr: 'R$ 1.840,00',
  dataVencimentoStr: '12/11/2026',
  diasParaVencer: -6,
  situacao: 'ATRASADO',
  dataPagamentoStr: null,
  forma: null,
  podeDarBaixa: true,
};

const TITULO_CAUCAO: TituloFinanceiro = {
  ...TITULO_ATRASADO,
  idPagamento: 11,
  origemStr: 'Locação nº 13',
  tipo: 'CAUCAO',
  tipoStr: 'Caução',
  clienteNome: 'Caução Teste',
  valorStr: 'R$ 7.777,77',
};

const TITULOS: RespostaTitulosFinanceiros = {
  quantidadeTodos: 2,
  quantidadeEmAtraso: 1,
  quantidadeVenceHoje: 0,
  quantidadePagosRecentes: 0,
  pagina: { itens: [TITULO_ATRASADO, TITULO_CAUCAO], total: 2, pagina: 1, tamanhoPagina: 20 },
};

function consultaIndicadores(parcial: Record<string, unknown>): ConsultaIndicadores {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaIndicadores;
}

function consultaTitulos(parcial: Record<string, unknown>): ConsultaTitulos {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaTitulos;
}

describe('PainelFinanceiroPagina: com dados', () => {
  beforeEach(() => {
    vi.mocked(useIndicadoresFinanceiros).mockReturnValue(consultaIndicadores({ data: INDICADORES }));
    vi.mocked(useTitulosFinanceiros).mockReturnValue(consultaTitulos({ data: TITULOS }));
  });

  it('mostra cabeçalho, período, cartões, abas com contadores e a linha da tabela', () => {
    const marcado = renderToStaticMarkup(<PainelFinanceiroPagina />);

    expect(marcado).toContain('Dashboard &amp; Contas a Receber');
    expect(marcado).toContain('Período: 01/11/2026 a 30/11/2026');
    expect(marcado).toContain('Recebido no Mês');
    expect(marcado).toContain('Todos os Títulos (2)');
    expect(marcado).toContain('Em Atraso (1)');
    expect(marcado).toContain('Locação nº 12');
    expect(marcado).toContain('>Dar baixa</button>');
  });

  it('nunca mostra caução (a linha de caução vem da API e é descartada)', () => {
    const marcado = renderToStaticMarkup(<PainelFinanceiroPagina />);

    expect(marcado).not.toContain('R$ 7.777,77');
    expect(marcado).not.toContain('Caução Teste');
  });

  it('não mostra nenhum dos termos cortados do desenho 09', () => {
    const marcado = renderToStaticMarkup(<PainelFinanceiroPagina />);

    expect(termosCortadosPresentesPainel(marcado)).toEqual([]);
  });
});

describe('PainelFinanceiroPagina: estados', () => {
  it('carregando: mostra os esqueletos e o texto para leitor de tela, sem linhas', () => {
    vi.mocked(useIndicadoresFinanceiros).mockReturnValue(consultaIndicadores({ isPending: true }));
    vi.mocked(useTitulosFinanceiros).mockReturnValue(consultaTitulos({ isPending: true }));

    const marcado = renderToStaticMarkup(<PainelFinanceiroPagina />);

    expect(marcado).toContain('Carregando...');
    expect(marcado).toContain('animate-pulse');
    expect(marcado).not.toContain('Dar baixa');
  });

  it('erro na tabela: mostra a mensagem do erro e o botão de tentar de novo, sem a tabela', () => {
    vi.mocked(useIndicadoresFinanceiros).mockReturnValue(consultaIndicadores({ data: INDICADORES }));
    vi.mocked(useTitulosFinanceiros).mockReturnValue(
      consultaTitulos({
        isError: true,
        error: new ErroApi({ status: 500, mensagem: 'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.' }),
      }),
    );

    const marcado = renderToStaticMarkup(<PainelFinanceiroPagina />);

    expect(marcado).toContain('Ocorreu um erro inesperado no servidor. Tente novamente em instantes.');
    expect(marcado).toContain('Tentar novamente');
    expect(marcado).not.toContain('Locação nº 12');
  });

  it('erro nos indicadores: mostra o erro no lugar dos cartões e a tabela continua', () => {
    vi.mocked(useIndicadoresFinanceiros).mockReturnValue(
      consultaIndicadores({ isError: true, error: new ErroApi({ status: 0, mensagem: 'Falha de conexão.' }) }),
    );
    vi.mocked(useTitulosFinanceiros).mockReturnValue(consultaTitulos({ data: TITULOS }));

    const marcado = renderToStaticMarkup(<PainelFinanceiroPagina />);

    expect(marcado).toContain('Falha de conexão.');
    expect(marcado).not.toContain('Recebido no Mês');
    expect(marcado).toContain('Locação nº 12');
  });

  it('vazio: mostra a mensagem da aba sem títulos', () => {
    vi.mocked(useIndicadoresFinanceiros).mockReturnValue(consultaIndicadores({ data: INDICADORES }));
    vi.mocked(useTitulosFinanceiros).mockReturnValue(
      consultaTitulos({
        data: { ...TITULOS, pagina: { itens: [], total: 0, pagina: 1, tamanhoPagina: 20 } },
      }),
    );

    const marcado = renderToStaticMarkup(<PainelFinanceiroPagina />);

    expect(marcado).toContain('Nenhum título neste filtro.');
    expect(marcado).not.toContain('Nenhum registro encontrado.');
    expect(marcado).not.toContain('aria-label="Paginação"');
  });
});
