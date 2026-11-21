import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { ErroApi } from '@/types/api';
import type { MeuPagamento } from '@/types/pagamentos';
import { TabelaPagamentosCliente, type TabelaPagamentosClienteProps } from './TabelaPagamentosCliente';

function pagamento(parcial: Partial<MeuPagamento>): MeuPagamento {
  return {
    idPagamento: 77,
    tipo: 'DIARIA',
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
    ...parcial,
  };
}

function renderizar(parcial: Partial<TabelaPagamentosClienteProps>): string {
  const props: TabelaPagamentosClienteProps = {
    itens: [],
    carregando: false,
    erro: null,
    total: 0,
    pagina: 1,
    tamanhoPagina: 10,
    atualizando: false,
    comFiltro: false,
    aoMudarPagina: vi.fn(),
    aoTentarNovamente: vi.fn(),
    aoPagarCobranca: vi.fn(),
    ...parcial,
  };
  return renderToStaticMarkup(<TabelaPagamentosCliente {...props} />);
}

/** Botões "Pagar agora" que vieram desligados (com disabled). Numa cobrança elegível, nenhum pode vir assim. */
function botoesDePagarDesligados(html: string): string[] {
  const botoes = html.match(/<button[^>]*>[\s\S]*?<\/button>/g) ?? [];
  return botoes.filter((botao) => botao.includes('Pagar agora') && /\sdisabled[\s=>]/.test(botao));
}

describe('TabelaPagamentosCliente (tela 30)', () => {
  it('pendente: vence em N dias, Pagar agora ligado e Pendente', () => {
    const html = renderizar({ itens: [pagamento({ diasParaVencer: 4 })], total: 1 });
    expect(html).toContain('Vence em 4 dias');
    expect(html).toContain('Pagar agora');
    expect(html).toContain('Pendente');
    expect(html).toContain('Locação nº 123');
    expect(html).toContain('Jeep Renegade Longitude');
    expect(html).toContain('Cobrança nº 77');
    expect(html).toContain('Diária');
    expect(botoesDePagarDesligados(html)).toEqual([]);
  });

  it('pendente vence hoje e amanhã', () => {
    expect(renderizar({ itens: [pagamento({ diasParaVencer: 0 })], total: 1 })).toContain('Vence hoje');
    expect(renderizar({ itens: [pagamento({ diasParaVencer: 1 })], total: 1 })).toContain('Vence amanhã');
  });

  it('atrasado: vencida há N dias, com Pagar agora ligado', () => {
    const html = renderizar({
      itens: [pagamento({ situacao: 'ATRASADO', diasParaVencer: -2, tipo: 'COBRANCA_EXTRA' })],
      total: 1,
    });
    expect(html).toContain('Vencida há 2 dias');
    expect(html).toContain('Atrasado');
    expect(html).toContain('Cobrança extra');
    expect(html).toContain('Pagar agora');
    expect(botoesDePagarDesligados(html)).toEqual([]);
  });

  it('pago: Compensado, Pago via forma e sem botão de pagar', () => {
    const html = renderizar({
      itens: [pagamento({ situacao: 'PAGO', forma: 'PIX', diasParaVencer: -30 })],
      total: 1,
    });
    expect(html).toContain('Compensado');
    expect(html).toContain('Pago via PIX');
    expect(html).not.toContain('Pagar agora');
  });

  it('cancelado: etiqueta Cancelado, sem subtexto e sem botão', () => {
    const html = renderizar({ itens: [pagamento({ situacao: 'CANCELADO' })], total: 1 });
    expect(html).toContain('Cancelado');
    expect(html).not.toContain('Vence');
    expect(html).not.toContain('Pagar agora');
  });

  it('caução: rótulo Caução, situação dela e nenhum botão de pagar', () => {
    const html = renderizar({
      itens: [pagamento({ tipo: 'CAUCAO', situacao: 'PENDENTE', idLocacao: 5, veiculoDescricao: null })],
      total: 1,
    });
    expect(html).toContain('Caução');
    expect(html).toContain('Pendente');
    expect(html).not.toContain('Pagar agora');
  });

  it('caução vencida não mostra Vencida há nem Vence, e a data usa a cor neutra', () => {
    const html = renderizar({
      itens: [pagamento({ tipo: 'CAUCAO', situacao: 'PENDENTE', idLocacao: 5, veiculoDescricao: null, diasParaVencer: -12 })],
      total: 1,
    });
    expect(html).not.toContain('Vencida há');
    expect(html).not.toContain('Vence');
    expect(html).toContain('class="text-dado-tabular text-texto-desenho">22/11/2026</span>');
    expect(html).not.toContain('text-status-perigo');
    expect(html).not.toContain('text-frota-oficina">22/11/2026');
  });

  it('botão de pagar da linha vem ligado, sem aviso de indisponível', () => {
    const html = renderizar({ itens: [pagamento({ diasParaVencer: 4 })], total: 1 });
    const botoes = html.match(/<button[^>]*>[\s\S]*?<\/button>/g) ?? [];
    const pagar = botoes.filter((botao) => botao.includes('Pagar agora'));
    expect(pagar).toHaveLength(1);
    expect(pagar[0]).not.toMatch(/\sdisabled[\s=>]/);
    expect(pagar[0]).not.toMatch(/aria-disabled/);
    expect(pagar[0]).not.toMatch(/\bopacity-60\b/);
    expect(botoesDePagarDesligados(html)).toEqual([]);
    expect(html).not.toContain('Pagamento online disponível em breve.');
  });

  it('cobrança atrasada também tem o botão ligado', () => {
    const html = renderizar({ itens: [pagamento({ situacao: 'ATRASADO', diasParaVencer: -3 })], total: 1 });
    expect(html).toContain('Pagar agora');
    expect(botoesDePagarDesligados(html)).toEqual([]);
  });

  it('origem de venda aparece como Venda nº', () => {
    const html = renderizar({ itens: [pagamento({ tipo: 'VENDA', idLocacao: null, idVenda: 45 })], total: 1 });
    expect(html).toContain('Venda nº 45');
  });

  it('vazio sem filtro e vazio com filtro têm textos diferentes', () => {
    expect(renderizar({})).toContain('Você ainda não tem cobranças.');
    expect(renderizar({ comFiltro: true })).toContain('Nenhuma cobrança neste filtro.');
  });

  it('carregando mostra o esqueleto e nenhum dado', () => {
    const html = renderizar({ carregando: true, itens: [pagamento({})], total: 1 });
    expect(html).toContain('animate-pulse');
    expect(html).not.toContain('Cobrança nº 77');
    expect(html).toContain('aria-busy="true"');
  });

  it('erro sem dados mostra o aviso com Tentar novamente', () => {
    const html = renderizar({ erro: new ErroApi({ status: 500, mensagem: 'Falha.' }) });
    expect(html).toContain('role="alert"');
    expect(html).toContain('Tentar novamente');
    expect(html).toContain('Falha.');
  });

  it('colunas do desenho no cabeçalho, sem a coluna de busca nem de NF-e', () => {
    const html = renderizar({ itens: [pagamento({})], total: 1 });
    for (const titulo of ['Descrição &amp; Serviço', 'Locação Relacionada', 'Vencimento', 'Valor Total', 'Status', 'Ações Rápidas']) {
      expect(html).toContain(titulo);
    }
    expect(html).toContain('cobranças');
    expect(html).toContain('aria-label="Paginação"');
  });
});
