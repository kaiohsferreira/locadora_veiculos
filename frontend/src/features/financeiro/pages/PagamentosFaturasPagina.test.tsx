import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { useMeusPagamentos, useResumoMeusPagamentos } from '@/features/financeiro/hooks/usePagamentosCliente';
import type { MeuPagamento, ResumoMeusPagamentos } from '@/types/pagamentos';
import PagamentosFaturasPagina from './PagamentosFaturasPagina';

// Nenhum teste chama a API: as consultas viram dublês.
vi.mock('@/features/financeiro/hooks/usePagamentosCliente', () => ({
  useMeusPagamentos: vi.fn(),
  useResumoMeusPagamentos: vi.fn(),
  CHAVE_PAGAMENTOS_CLIENTE: 'pagamentos-cliente',
}));

type ConsultaLista = ReturnType<typeof useMeusPagamentos>;
type ConsultaResumo = ReturnType<typeof useResumoMeusPagamentos>;

const RESUMO: ResumoMeusPagamentos = {
  anoExercicio: 2026,
  totalLiquidadoAno: 4763.85,
  totalLiquidadoAnoStr: 'R$ 4.763,85',
  quantidadeQuitadasAno: 4,
  totalPendente: 395.6,
  totalPendenteStr: 'R$ 395,60',
  quantidadePendentes: 1,
  quantidadeAbaPendentes: 1,
  quantidadeAbaPagos: 4,
};

const PENDENTE: MeuPagamento = {
  idPagamento: 77,
  tipo: 'COBRANCA_EXTRA',
  valor: 395.6,
  valorStr: 'R$ 395,60',
  dataVencimento: '2026-11-22',
  dataVencimentoStr: '22/11/2026',
  dataPagamentoStr: null,
  diasParaVencer: 4,
  situacao: 'PENDENTE',
  forma: null,
  idLocacao: 123,
  idVenda: null,
  veiculoDescricao: 'Jeep Renegade Longitude',
};

const PAGO: MeuPagamento = {
  ...PENDENTE,
  idPagamento: 78,
  tipo: 'DIARIA',
  valorStr: 'R$ 980,00',
  situacao: 'PAGO',
  forma: 'PIX',
  dataVencimentoStr: '18/10/2026',
  diasParaVencer: -30,
  idLocacao: 90,
  veiculoDescricao: 'Toyota Corolla Altis Hybrid',
};

function consultaLista(parcial: Record<string, unknown>): ConsultaLista {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaLista;
}

function consultaResumo(parcial: Record<string, unknown>): ConsultaResumo {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaResumo;
}

/** Termos que não podem aparecer em nenhum ponto da tela (contrato pagamento, o_que_nao_aparece). */
const TERMOS_PROIBIDOS = [
  'IRPF',
  'NF-e',
  'Gerenciar Cartões',
  'Exportar',
  'Baixar NF',
  '6x',
  '#LOC',
  '#FAT',
  'QR',
  'Relatório Anual',
  'Forma Padrão',
  'Mastercard',
  'Buscar por',
  'Pagamento Rápido',
  'Efetuar Liquidação',
  'Parcelamento',
];

function renderizar(): string {
  return renderToStaticMarkup(<PagamentosFaturasPagina />);
}

/** Todo botão que contém o texto de pagar ou regularizar precisa estar desligado. */
function botoesSemDesligar(html: string): string[] {
  const botoes = html.match(/<button(?![^>]*(?<![-\w])disabled)[^>]*>[\s\S]*?<\/button>/g) ?? [];
  return botoes.filter((botao) => /Pagar|Regularizar/.test(botao));
}

describe('PagamentosFaturasPagina (tela 30)', () => {
  beforeEach(() => {
    vi.mocked(useMeusPagamentos).mockReset();
    vi.mocked(useResumoMeusPagamentos).mockReset();
  });

  it('mostra título, subtítulo, faixa, cartões, abas com contagem e a cobrança', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(consultaResumo({ data: RESUMO }));
    vi.mocked(useMeusPagamentos).mockReturnValue(
      consultaLista({ data: { itens: [PENDENTE, PAGO], total: 2, pagina: 1, tamanhoPagina: 10 } }),
    );

    const html = renderizar();
    expect(html).toContain('Pagamentos e Faturas');
    expect(html).toContain('Consulte o extrato detalhado das suas cobranças.');
    expect(html).toContain('Você possui 1 cobrança pendente.');
    expect(html).toContain('Pagar Fatura R$ 395,60');
    expect(html).toContain('Pendentes (1)');
    expect(html).toContain('Pagos (4)');
    expect(html).toContain('Todos');
    expect(html).not.toMatch(/Todos \(/);
    expect(html).toContain('Locação nº 123');
    expect(html).toContain('Pago via PIX');
  });

  it('nenhum botão de pagar fica ligado, e o aviso aparece', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(consultaResumo({ data: RESUMO }));
    vi.mocked(useMeusPagamentos).mockReturnValue(
      consultaLista({ data: { itens: [PENDENTE], total: 1, pagina: 1, tamanhoPagina: 10 } }),
    );

    const html = renderizar();
    expect(html).toContain('Pagar agora');
    expect(html).toContain('Regularizar');
    expect(html).toContain('Pagamento online disponível em breve.');
    expect(botoesSemDesligar(html)).toEqual([]);
  });

  it('nenhum termo que a tela 30 não pode mostrar aparece', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(consultaResumo({ data: RESUMO }));
    vi.mocked(useMeusPagamentos).mockReturnValue(
      consultaLista({ data: { itens: [PENDENTE, PAGO], total: 2, pagina: 1, tamanhoPagina: 10 } }),
    );

    const html = renderizar();
    for (const termo of TERMOS_PROIBIDOS) {
      expect(html, `termo proibido: ${termo}`).not.toContain(termo);
    }
  });

  it('sem pendente a faixa some e o cartão mostra zero sem botão', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(
      consultaResumo({
        data: { ...RESUMO, quantidadePendentes: 0, quantidadeAbaPendentes: 0, totalPendenteStr: 'R$ 0,00' },
      }),
    );
    vi.mocked(useMeusPagamentos).mockReturnValue(
      consultaLista({ data: { itens: [PAGO], total: 1, pagina: 1, tamanhoPagina: 10 } }),
    );

    const html = renderizar();
    expect(html).not.toContain('Você possui');
    expect(html).not.toContain('Pagar Fatura');
    expect(html).not.toContain('Regularizar');
    expect(html).toContain('R$ 0,00');
  });

  it('lista vazia sem filtro: texto de vazio do cliente', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(consultaResumo({ data: RESUMO }));
    vi.mocked(useMeusPagamentos).mockReturnValue(
      consultaLista({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } }),
    );

    expect(renderizar()).toContain('Você ainda não tem cobranças.');
  });

  it('erro da lista mostra Tentar novamente', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(consultaResumo({ data: RESUMO }));
    vi.mocked(useMeusPagamentos).mockReturnValue(consultaLista({ isError: true, error: new Error('falha') }));

    const html = renderizar();
    expect(html).toContain('Tentar novamente');
    expect(html).toContain('role="alert"');
  });

  it('erro do resumo mostra o aviso próprio, sem cartões de valor', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(consultaResumo({ isError: true, error: new Error('falha') }));
    vi.mocked(useMeusPagamentos).mockReturnValue(consultaLista({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } }));

    const html = renderizar();
    expect(html).toContain('Não foi possível carregar o resumo das cobranças.');
    expect(html).not.toContain('Total Liquidado no Ano');
  });

  it('abas em tablist, com a aba Todos selecionada ao abrir', () => {
    vi.mocked(useResumoMeusPagamentos).mockReturnValue(consultaResumo({ data: RESUMO }));
    vi.mocked(useMeusPagamentos).mockReturnValue(consultaLista({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } }));

    const html = renderizar();
    expect(html).toContain('role="tablist"');
    expect(html).toContain('aria-selected="true"');
  });
});
