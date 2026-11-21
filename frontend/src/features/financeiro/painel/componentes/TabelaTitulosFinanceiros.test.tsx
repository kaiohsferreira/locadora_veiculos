import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { TituloFinanceiro } from '@/types/painelFinanceiro';
import { TabelaTitulosFinanceiras } from './TabelaTitulosFinanceiros';

function titulo(parcial: Partial<TituloFinanceiro>): TituloFinanceiro {
  return {
    idPagamento: 1,
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
    ...parcial,
  };
}

const CABECALHOS = [
  'Origem / Contrato',
  'Cliente / Devedor',
  'Tipo do Lançamento',
  'Valor Bruto',
  'Vencimento',
  'Status Operacional',
  'Ação Direta',
];

function renderizar(itens: readonly TituloFinanceiro[], carregando = false): string {
  return renderToStaticMarkup(
    <TabelaTitulosFinanceiras itens={itens} carregando={carregando} aoDarBaixa={() => undefined} />,
  );
}

describe('TabelaTitulosFinanceiras: colunas e linhas', () => {
  it('mostra os sete cabeçalhos do desenho, na ordem', () => {
    const marcado = renderizar([titulo({})]);

    let ultimaPosicao = -1;
    for (const cabecalho of CABECALHOS) {
      const posicao = marcado.indexOf(`>${cabecalho}</th>`);
      expect(posicao).toBeGreaterThan(ultimaPosicao);
      ultimaPosicao = posicao;
    }
  });

  it('mostra origem, cliente com CPF mascarado, tipo, valor e vencimento de uma linha em atraso', () => {
    const marcado = renderizar([titulo({})]);

    expect(marcado).toContain('Locação nº 12');
    expect(marcado).toContain('Rodrigo Alcantara Dias');
    expect(marcado).toContain('CPF: 194.***.***-12');
    expect(marcado).toContain('Cobrança extra');
    expect(marcado).toContain('R$ 1.840,00');
    expect(marcado).toContain('12/11/2026');
    expect(marcado).toContain('Vencido há 6 dias');
  });

  it('linha em atraso leva o selo Em Atraso e o fundo de alerta, com o botão Dar baixa', () => {
    const marcado = renderizar([titulo({})]);

    expect(marcado).toContain('Em Atraso');
    expect(marcado).toContain('bg-manutencao-alerta-trilho/10');
    expect(marcado).toContain('>Dar baixa</button>');
  });

  it('origem venda usa o selo de venda e o texto do servidor', () => {
    const marcado = renderizar([
      titulo({ origem: 'VENDA', origemStr: 'Venda nº 9', tipo: 'VENDA', tipoStr: 'Venda', situacao: 'PENDENTE', diasParaVencer: 4 }),
    ]);

    expect(marcado).toContain('Venda nº 9');
    expect(marcado).toContain('bg-financeiro-origem-venda');
  });

  it('sem CPF (nulo), não mostra a linha de CPF', () => {
    const marcado = renderizar([titulo({ clienteCpfMascarado: null })]);

    expect(marcado).toContain('Rodrigo Alcantara Dias');
    expect(marcado).not.toContain('CPF:');
  });
});

describe('TabelaTitulosFinanceiras: situações e prazos', () => {
  it('PENDENTE que vence amanhã mostra o prazo e o selo Pendente, com o botão', () => {
    const marcado = renderizar([
      titulo({ situacao: 'PENDENTE', diasParaVencer: 1, dataVencimentoStr: '19/11/2026' }),
    ]);

    expect(marcado).toContain('Vence amanhã');
    expect(marcado).toContain('Pendente');
    expect(marcado).toContain('>Dar baixa</button>');
  });

  it('PENDENTE com vencimento passado aparece como Em Atraso (regra da tela)', () => {
    const marcado = renderizar([titulo({ situacao: 'PENDENTE', diasParaVencer: -2 })]);

    expect(marcado).toContain('Em Atraso');
    expect(marcado).not.toContain('>Pendente<');
  });

  it('PAGO não tem botão Dar baixa e mostra a data do pagamento e o selo Compensado / Pago', () => {
    const marcado = renderizar([
      titulo({ situacao: 'PAGO', diasParaVencer: -3, podeDarBaixa: false, dataPagamentoStr: '18/11/2026' }),
    ]);

    expect(marcado).not.toContain('Dar baixa');
    expect(marcado).toContain('Compensado / Pago');
    expect(marcado).toContain('Pago em 18/11/2026');
    expect(marcado).not.toContain('bg-manutencao-alerta-trilho/10');
  });

  it('sem a liberação do servidor (podeDarBaixa falso), não mostra o botão mesmo em PENDENTE', () => {
    const marcado = renderizar([titulo({ podeDarBaixa: false, situacao: 'PENDENTE', diasParaVencer: 3 })]);

    expect(marcado).not.toContain('Dar baixa');
  });
});

describe('TabelaTitulosFinanceiras: estados', () => {
  it('sem linhas mostra a mensagem de lista vazia e nenhuma linha de dados', () => {
    const marcado = renderizar([]);

    expect(marcado).toContain('Nenhum título neste filtro.');
    expect(marcado).not.toContain('Dar baixa');
  });

  it('carregando mostra esqueletos, o texto para leitor de tela e nenhuma linha de dados', () => {
    const marcado = renderizar([titulo({})], true);

    expect(marcado).toContain('Carregando...');
    expect(marcado).toContain('animate-pulse');
    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('Rodrigo Alcantara Dias');
  });

  it('tem legenda para leitor de tela', () => {
    expect(renderizar([])).toContain('<caption class="sr-only">Títulos financeiros</caption>');
  });
});
