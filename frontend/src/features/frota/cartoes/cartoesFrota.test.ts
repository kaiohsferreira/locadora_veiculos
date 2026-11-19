import { describe, expect, it } from 'vitest';
import type { FrotaPorSituacao, RespostaVisaoGeral } from '@/types/painel';
import { contarPilulasFrota, formatarPercentualFrota, montarCartoesFrota } from './cartoesFrota';

/** Resposta mínima do painel: só os campos que a tela 11 lê. */
function respostaCom(frotaPorSituacao: FrotaPorSituacao[], extras: Partial<RespostaVisaoGeral> = {}): RespostaVisaoGeral {
  const totalVeiculos = frotaPorSituacao
    .filter((item) => ['LOCADO', 'DISPONIVEL', 'RESERVADO', 'MANUTENCAO'].includes(item.situacao))
    .reduce((soma, item) => soma + item.quantidade, 0);
  return {
    periodo: 'TRINTA_DIAS',
    periodoInicioStr: '',
    periodoFimStr: '',
    primeiroAcesso: false,
    valoresVisiveis: false,
    receitaPeriodo: null,
    receitaPeriodoStr: '',
    aReceber: null,
    aReceberStr: '',
    quantidadeCobrancasEmAberto: 0,
    emAtraso: null,
    emAtrasoStr: '',
    quantidadeCobrancasVencidas: 0,
    taxaOcupacaoPercentual: 0,
    veiculosLocados: 0,
    totalVeiculos,
    locacoesAtivas: 0,
    devolucoesPrevistasHoje: 0,
    frotaPorSituacao,
    ...extras,
  };
}

/** Os seis status: a frota em operação tem 50 veículos (34 + 9 + 3 + 4); vendido e baixado ficam fora do total. */
const SEIS_STATUS: FrotaPorSituacao[] = [
  { situacao: 'LOCADO', quantidade: 34 },
  { situacao: 'DISPONIVEL', quantidade: 9 },
  { situacao: 'RESERVADO', quantidade: 3 },
  { situacao: 'MANUTENCAO', quantidade: 4 },
  { situacao: 'VENDIDO', quantidade: 1 },
  { situacao: 'BAIXADO', quantidade: 2 },
];

function cartao(cartoes: ReturnType<typeof montarCartoesFrota>, chave: string) {
  const achado = cartoes.find((item) => item.chave === chave);
  if (achado === undefined) throw new Error(`cartão ${chave} não montado`);
  return achado;
}

describe('montarCartoesFrota: números e percentuais com os seis status', () => {
  const cartoes = montarCartoesFrota(respostaCom(SEIS_STATUS, { veiculosNovosNoMes: 32 }));

  it('monta os cinco cartões na ordem do desenho, com Reservados no quinto lugar', () => {
    expect(cartoes.map((item) => item.chave)).toEqual(['total', 'disponiveis', 'locados', 'oficina', 'reservados']);
  });

  it('total da frota é o totalVeiculos (frota em operação, sem vendido e baixado)', () => {
    expect(cartao(cartoes, 'total').valor).toBe('50');
  });

  it('disponíveis, locados, oficina e reservados saem da quantidade do status', () => {
    expect(cartao(cartoes, 'disponiveis').valor).toBe('9');
    expect(cartao(cartoes, 'locados').valor).toBe('34');
    expect(cartao(cartoes, 'oficina').valor).toBe('4');
    expect(cartao(cartoes, 'reservados').valor).toBe('3');
  });

  it('percentuais sobre a frota em operação (50), com uma casa decimal e vírgula', () => {
    expect(cartao(cartoes, 'disponiveis').apoio).toBe('18,0% de disponibilidade');
    expect(cartao(cartoes, 'locados').apoio).toBe('68,0% taxa ocupação');
    expect(cartao(cartoes, 'oficina').apoio).toBe('8,0% em serviço');
    expect(cartao(cartoes, 'reservados').apoio).toBe('6,0% em reserva');
  });

  it('vendido e baixado não entram no denominador dos percentuais', () => {
    const semVendidoEBaixado = montarCartoesFrota(respostaCom(SEIS_STATUS.slice(0, 4)));

    expect(cartao(semVendidoEBaixado, 'disponiveis').apoio).toBe(cartao(cartoes, 'disponiveis').apoio);
    expect(cartao(semVendidoEBaixado, 'locados').apoio).toBe(cartao(cartoes, 'locados').apoio);
  });

  it('a unidade e o chip vêm dos textos do desenho', () => {
    expect(cartao(cartoes, 'total').unidade).toBe('unidades');
    expect(cartao(cartoes, 'locados')).toMatchObject({ unidade: 'em uso', chip: 'Em Contrato' });
    expect(cartao(cartoes, 'oficina')).toMatchObject({ unidade: 'ativos', chip: 'Em Revisão' });
    expect(cartao(cartoes, 'disponiveis').chip).toBe('Disponível');
    expect(cartao(cartoes, 'reservados').chip).toBe('Reservado');
  });
});

describe('montarCartoesFrota: novos neste mês', () => {
  it('leva veiculosNovosNoMes para o cartão total', () => {
    const cartoes = montarCartoesFrota(respostaCom(SEIS_STATUS, { veiculosNovosNoMes: 32 }));

    expect(cartao(cartoes, 'total').novosNoMes).toBe(32);
  });

  it('servidor antigo (campo ausente): não quebra e não mostra novos', () => {
    const cartoes = montarCartoesFrota(respostaCom(SEIS_STATUS));

    expect(cartao(cartoes, 'total').novosNoMes).toBeNull();
    expect(cartao(cartoes, 'total').valor).toBe('50');
  });

  it('zero novos é um número válido (não some)', () => {
    const cartoes = montarCartoesFrota(respostaCom(SEIS_STATUS, { veiculosNovosNoMes: 0 }));

    expect(cartao(cartoes, 'total').novosNoMes).toBe(0);
  });
});

describe('montarCartoesFrota: total zero e sem resposta', () => {
  it('total zero: todos os percentuais saem "0%" e os números, zero', () => {
    const cartoes = montarCartoesFrota(respostaCom([]));

    expect(cartao(cartoes, 'total').valor).toBe('0');
    expect(cartao(cartoes, 'disponiveis')).toMatchObject({ valor: '0', apoio: '0% de disponibilidade' });
    expect(cartao(cartoes, 'locados').apoio).toBe('0% taxa ocupação');
    expect(cartao(cartoes, 'oficina').apoio).toBe('0% em serviço');
    expect(cartao(cartoes, 'reservados').apoio).toBe('0% em reserva');
  });

  it('sem resposta (carregando ou erro): números com valor ausente e nenhum percentual inventado', () => {
    const cartoes = montarCartoesFrota(undefined);

    expect(cartoes.every((item) => item.valor === '—')).toBe(true);
    expect(cartoes.every((item) => item.apoio === null)).toBe(true);
    expect(cartao(cartoes, 'locados').apoio).toBeNull();
  });

  it('arredonda com uma casa decimal (1/3 e 2/3)', () => {
    const cartoes = montarCartoesFrota(
      respostaCom([
        { situacao: 'DISPONIVEL', quantidade: 1 },
        { situacao: 'LOCADO', quantidade: 2 },
      ]),
    );

    expect(cartao(cartoes, 'disponiveis').apoio).toBe('33,3% de disponibilidade');
    expect(cartao(cartoes, 'locados').apoio).toBe('66,7% taxa ocupação');
  });
});

describe('formatarPercentualFrota', () => {
  it('zero sai como "0%"; outros valores com uma casa decimal', () => {
    expect(formatarPercentualFrota(0)).toBe('0%');
    expect(formatarPercentualFrota(63.63636)).toBe('63,6%');
    expect(formatarPercentualFrota(100)).toBe('100,0%');
  });

  it('sem valor sai como o valor ausente', () => {
    expect(formatarPercentualFrota(undefined)).toBe('—');
  });
});

describe('contarPilulasFrota', () => {
  it('pílula "Todos" soma os seis status; cada uma das outras traz o seu status', () => {
    const contagem = contarPilulasFrota(SEIS_STATUS);

    expect(contagem).toEqual({
      '': 53,
      DISPONIVEL: 9,
      RESERVADO: 3,
      LOCADO: 34,
      MANUTENCAO: 4,
      VENDIDO: 1,
      BAIXADO: 2,
    });
  });

  it('um status que não veio na lista conta zero', () => {
    const contagem = contarPilulasFrota([{ situacao: 'LOCADO', quantidade: 5 }]);

    expect(contagem).toMatchObject({ '': 5, LOCADO: 5, RESERVADO: 0, MANUTENCAO: 0 });
  });

  it('sem lista (carregando ou erro): nulo, e a tela mostra o valor ausente', () => {
    expect(contarPilulasFrota(undefined)).toBeNull();
  });

  it('o status repetido soma (a conta não sobrescreve)', () => {
    const contagem = contarPilulasFrota([
      { situacao: 'LOCADO', quantidade: 1 },
      { situacao: 'LOCADO', quantidade: 2 },
    ]);

    expect(contagem?.LOCADO).toBe(3);
    expect(contagem?.['']).toBe(3);
  });
});
