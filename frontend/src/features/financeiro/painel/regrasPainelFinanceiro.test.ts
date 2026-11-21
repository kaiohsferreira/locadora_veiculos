import { describe, expect, it } from 'vitest';
import type { TituloFinanceiro } from '@/types/painelFinanceiro';
import {
  ajustarPaginaAoTotal,
  ehTituloExibivel,
  podeMostrarDarBaixa,
  situacaoExibida,
  textoLinhaAReceber,
  textoLinhaEmAtraso,
  textoLinhaRecebido,
  textoVencimento,
} from './regrasPainelFinanceiro';

function titulo(parcial: Partial<TituloFinanceiro>): TituloFinanceiro {
  return {
    idPagamento: 1,
    origem: 'LOCACAO',
    origemStr: 'Locação nº 12',
    tipo: 'DIARIA',
    tipoStr: 'Diária',
    clienteNome: 'Rodrigo Alcantara Dias',
    clienteCpfMascarado: '194.***.***-12',
    valor: 1840,
    valorStr: 'R$ 1.840,00',
    dataVencimentoStr: '12/11/2026',
    diasParaVencer: -6,
    situacao: 'PENDENTE',
    dataPagamentoStr: null,
    forma: null,
    podeDarBaixa: true,
    ...parcial,
  };
}

describe('ajustarPaginaAoTotal (página que ainda existe depois de uma baixa)', () => {
  it('a página atual cabe no total: não muda', () => {
    expect(ajustarPaginaAoTotal(1, 45, 20)).toBe(1);
    expect(ajustarPaginaAoTotal(3, 45, 20)).toBe(3);
  });

  it('a última linha da página saiu (ex.: baixa): volta para a última página que existe', () => {
    expect(ajustarPaginaAoTotal(3, 40, 20)).toBe(2);
    expect(ajustarPaginaAoTotal(2, 19, 20)).toBe(1);
  });

  it('sem nenhum título, volta para a página 1', () => {
    expect(ajustarPaginaAoTotal(2, 0, 20)).toBe(1);
    expect(ajustarPaginaAoTotal(1, 0, 20)).toBe(1);
  });
});

describe('textoVencimento (rótulo do vencimento na linha)', () => {
  it('zero dias é "Vence hoje"', () => {
    expect(textoVencimento(0)).toBe('Vence hoje');
  });

  it('um dia para frente é "Vence amanhã"', () => {
    expect(textoVencimento(1)).toBe('Vence amanhã');
  });

  it('mais de um dia para frente é "Vence em N dias"', () => {
    expect(textoVencimento(3)).toBe('Vence em 3 dias');
    expect(textoVencimento(30)).toBe('Vence em 30 dias');
  });

  it('dias passados são "Vencido há N dias"', () => {
    expect(textoVencimento(-6)).toBe('Vencido há 6 dias');
  });

  it('um dia de atraso é singular: "Vencido há 1 dia"', () => {
    expect(textoVencimento(-1)).toBe('Vencido há 1 dia');
  });
});

describe('situacaoExibida (PENDENTE vencido vira ATRASADO na tabela)', () => {
  it('PENDENTE com vencimento passado vira ATRASADO', () => {
    expect(situacaoExibida({ situacao: 'PENDENTE', diasParaVencer: -2 })).toBe('ATRASADO');
  });

  it('PENDENTE que vence hoje continua PENDENTE', () => {
    expect(situacaoExibida({ situacao: 'PENDENTE', diasParaVencer: 0 })).toBe('PENDENTE');
  });

  it('ATRASADO e PAGO voltam como vieram', () => {
    expect(situacaoExibida({ situacao: 'ATRASADO', diasParaVencer: 3 })).toBe('ATRASADO');
    expect(situacaoExibida({ situacao: 'PAGO', diasParaVencer: -30 })).toBe('PAGO');
  });
});

describe('ehTituloExibivel (caução nunca aparece)', () => {
  it('caução fica de fora da lista', () => {
    expect(ehTituloExibivel({ tipo: 'CAUCAO' })).toBe(false);
  });

  it('diária, cobrança extra e venda ficam', () => {
    expect(ehTituloExibivel({ tipo: 'DIARIA' })).toBe(true);
    expect(ehTituloExibivel({ tipo: 'COBRANCA_EXTRA' })).toBe(true);
    expect(ehTituloExibivel({ tipo: 'VENDA' })).toBe(true);
  });
});

describe('podeMostrarDarBaixa (quando o botão Dar baixa aparece)', () => {
  it('aparece em PENDENTE e em ATRASADO quando o servidor libera', () => {
    expect(podeMostrarDarBaixa(titulo({ situacao: 'PENDENTE', diasParaVencer: 4, podeDarBaixa: true }))).toBe(true);
    expect(podeMostrarDarBaixa(titulo({ situacao: 'ATRASADO', diasParaVencer: -6, podeDarBaixa: true }))).toBe(true);
  });

  it('PENDENTE vencido também mostra o botão (vira ATRASADO na tela)', () => {
    expect(podeMostrarDarBaixa(titulo({ situacao: 'PENDENTE', diasParaVencer: -2, podeDarBaixa: true }))).toBe(true);
  });

  it('some em PAGO, mesmo que o servidor mande podeDarBaixa verdadeiro', () => {
    expect(podeMostrarDarBaixa(titulo({ situacao: 'PAGO', diasParaVencer: -30, podeDarBaixa: true }))).toBe(false);
  });

  it('some quando o servidor não libera a baixa', () => {
    expect(podeMostrarDarBaixa(titulo({ situacao: 'PENDENTE', diasParaVencer: 4, podeDarBaixa: false }))).toBe(false);
  });
});

describe('textos dos cartões (singular e plural)', () => {
  it('recebido: um pagamento ou vários', () => {
    expect(textoLinhaRecebido(1)).toBe('1 pagamento recebido');
    expect(textoLinhaRecebido(2)).toBe('2 pagamentos recebidos');
    expect(textoLinhaRecebido(0)).toBe('0 pagamentos recebidos');
  });

  it('a receber: um título ou vários, com separador de milhar', () => {
    expect(textoLinhaAReceber(1)).toBe('1 título');
    expect(textoLinhaAReceber(142)).toBe('142 títulos');
    expect(textoLinhaAReceber(1234)).toBe('1.234 títulos');
  });

  it('em atraso: um título vencido ou vários', () => {
    expect(textoLinhaEmAtraso(1)).toBe('1 título vencido');
    expect(textoLinhaEmAtraso(24)).toBe('24 títulos vencidos');
  });
});
