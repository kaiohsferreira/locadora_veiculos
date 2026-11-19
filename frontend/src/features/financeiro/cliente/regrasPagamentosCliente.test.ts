import { describe, expect, it } from 'vitest';
import {
  aplicarMudancaFiltro,
  FILTRO_INICIAL_PAGAMENTOS_CLIENTE,
  opcoesPeriodo,
  podePagarNaLinha,
  rotuloForma,
  rotuloOrigem,
  subtextoVencimento,
  temFiltroAplicado,
  textoFaixaPendencias,
  ultimaPaginaPagamentos,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import { mensagemCobrancasQuitadas } from '@/lib/mensagens';
import type { MeuPagamento } from '@/types/pagamentos';

function pagamento(parcial: Partial<MeuPagamento>): MeuPagamento {
  return {
    idPagamento: 77,
    tipo: 'DIARIA',
    valor: 395.6,
    valorStr: 'R$ 395,60',
    dataVencimento: '2026-11-22',
    dataVencimentoStr: '22/11/2026',
    dataPagamentoStr: null,
    diasParaVencer: 3,
    situacao: 'PENDENTE',
    forma: null,
    idLocacao: 123,
    idVenda: null,
    veiculoDescricao: 'Jeep Renegade Longitude',
    ...parcial,
  };
}

describe('faixa de pendência', () => {
  it('sem pendente não há texto (a faixa some)', () => {
    expect(textoFaixaPendencias(0)).toBeNull();
  });

  it('1 cobrança no singular e N no plural', () => {
    expect(textoFaixaPendencias(1)).toBe('Você possui 1 cobrança pendente.');
    expect(textoFaixaPendencias(3)).toBe('Você possui 3 cobranças pendentes.');
  });
});

describe('cartão de quitadas', () => {
  it('singular com 1 e plural com N', () => {
    expect(mensagemCobrancasQuitadas(1)).toBe('1 cobrança quitada');
    expect(mensagemCobrancasQuitadas(4)).toBe('4 cobranças quitadas');
  });
});

describe('subtexto do vencimento', () => {
  it('pendente: hoje, amanhã e N dias', () => {
    expect(subtextoVencimento(pagamento({ diasParaVencer: 0 }))).toBe('Vence hoje');
    expect(subtextoVencimento(pagamento({ diasParaVencer: 1 }))).toBe('Vence amanhã');
    expect(subtextoVencimento(pagamento({ diasParaVencer: 4 }))).toBe('Vence em 4 dias');
  });

  it('atrasado: vencida há N dias (singular com 1)', () => {
    expect(subtextoVencimento(pagamento({ situacao: 'ATRASADO', diasParaVencer: -3 }))).toBe('Vencida há 3 dias');
    expect(subtextoVencimento(pagamento({ situacao: 'ATRASADO', diasParaVencer: -1 }))).toBe('Vencida há 1 dia');
  });

  it('pago: com a forma usada, e nada sem forma', () => {
    expect(subtextoVencimento(pagamento({ situacao: 'PAGO', forma: 'PIX' }))).toBe('Pago via PIX');
    expect(subtextoVencimento(pagamento({ situacao: 'PAGO', forma: 'CARTAO' }))).toBe('Pago via Cartão');
    expect(subtextoVencimento(pagamento({ situacao: 'PAGO', forma: null }))).toBeNull();
  });

  it('cancelado não tem subtexto', () => {
    expect(subtextoVencimento(pagamento({ situacao: 'CANCELADO' }))).toBeNull();
  });

  it('caução pendente com vencimento passado não tem subtexto (não é dívida)', () => {
    expect(subtextoVencimento(pagamento({ tipo: 'CAUCAO', situacao: 'PENDENTE', diasParaVencer: -12 }))).toBeNull();
    expect(subtextoVencimento(pagamento({ tipo: 'CAUCAO', situacao: 'PENDENTE', diasParaVencer: 3 }))).toBeNull();
  });

  it('caução paga com forma mostra Pago via', () => {
    expect(subtextoVencimento(pagamento({ tipo: 'CAUCAO', situacao: 'PAGO', forma: 'CARTAO' }))).toBe('Pago via Cartão');
  });
});

describe('botão de pagar da linha', () => {
  it('aparece só em pendente ou atrasada que não seja caução', () => {
    expect(podePagarNaLinha(pagamento({ situacao: 'PENDENTE' }))).toBe(true);
    expect(podePagarNaLinha(pagamento({ situacao: 'ATRASADO', tipo: 'COBRANCA_EXTRA' }))).toBe(true);
    expect(podePagarNaLinha(pagamento({ situacao: 'PENDENTE', tipo: 'CAUCAO' }))).toBe(false);
    expect(podePagarNaLinha(pagamento({ situacao: 'PAGO' }))).toBe(false);
    expect(podePagarNaLinha(pagamento({ situacao: 'CANCELADO' }))).toBe(false);
  });
});

describe('rótulo da origem', () => {
  it('locação, venda e nenhuma origem', () => {
    expect(rotuloOrigem(pagamento({ idLocacao: 123, idVenda: null }))).toBe('Locação nº 123');
    expect(rotuloOrigem(pagamento({ idLocacao: null, idVenda: 45 }))).toBe('Venda nº 45');
    expect(rotuloOrigem(pagamento({ idLocacao: null, idVenda: null }))).toBeNull();
  });
});

describe('forma de pagamento', () => {
  it('rótulo da lista fechada e nulo para o resto', () => {
    expect(rotuloForma('PIX')).toBe('PIX');
    expect(rotuloForma('BOLETO')).toBe('Boleto');
    expect(rotuloForma(null)).toBeNull();
    expect(rotuloForma('CHEQUE')).toBeNull();
  });
});

describe('mudança de filtro', () => {
  it('trocar aba volta para a página 1', () => {
    const filtro = { ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, pagina: 4 };
    expect(aplicarMudancaFiltro(filtro, { tipo: 'aba', aba: 'PENDENTES' })).toMatchObject({ aba: 'PENDENTES', pagina: 1 });
  });

  it('trocar período volta para a página 1', () => {
    const filtro = { ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, pagina: 2 };
    expect(aplicarMudancaFiltro(filtro, { tipo: 'periodo', periodo: 'TUDO' })).toMatchObject({ periodo: 'TUDO', pagina: 1 });
  });

  it('trocar só a página mantém aba e período', () => {
    const filtro = { ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, aba: 'PAGOS' as const };
    expect(aplicarMudancaFiltro(filtro, { tipo: 'pagina', pagina: 3 })).toMatchObject({ aba: 'PAGOS', pagina: 3 });
  });

  it('filtro inicial é Todos com 12 meses e não conta como filtro aplicado', () => {
    expect(FILTRO_INICIAL_PAGAMENTOS_CLIENTE).toMatchObject({ aba: 'TODOS', periodo: 'DOZE_MESES', pagina: 1 });
    expect(temFiltroAplicado(FILTRO_INICIAL_PAGAMENTOS_CLIENTE)).toBe(false);
    expect(temFiltroAplicado({ ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, aba: 'PENDENTES' })).toBe(true);
    expect(temFiltroAplicado({ ...FILTRO_INICIAL_PAGAMENTOS_CLIENTE, periodo: 'TUDO' })).toBe(true);
  });
});

describe('seletor de período e paginação', () => {
  it('opções na ordem do desenho, com o ano no Ano Atual', () => {
    expect(opcoesPeriodo(2026)).toEqual([
      { valor: 'DOZE_MESES', rotulo: 'Últimos 12 meses' },
      { valor: 'ANO_ATUAL', rotulo: 'Ano Atual (2026)' },
      { valor: 'SEIS_MESES', rotulo: 'Últimos 6 meses' },
      { valor: 'TUDO', rotulo: 'Histórico Completo' },
    ]);
  });

  it('última página existente, com pelo menos 1', () => {
    expect(ultimaPaginaPagamentos(0, 10)).toBe(1);
    expect(ultimaPaginaPagamentos(10, 10)).toBe(1);
    expect(ultimaPaginaPagamentos(21, 10)).toBe(3);
  });
});
