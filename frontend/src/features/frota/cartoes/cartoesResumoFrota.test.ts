import { describe, expect, it } from 'vitest';
import type { RespostaVisaoGeral } from '@/types/painel';
import {
  formatarTaxaOcupacaoFrota,
  montarResumoCartoesFrota,
  proporcaoAnelFrota,
  textoEncerramHojeFrota,
} from './cartoesResumoFrota';

function visao(parcial: Partial<RespostaVisaoGeral>): RespostaVisaoGeral {
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
    totalVeiculos: 0,
    locacoesAtivas: 0,
    devolucoesPrevistasHoje: 0,
    frotaPorSituacao: [],
    ...parcial,
  };
}

describe('montarResumoCartoesFrota', () => {
  it('sem resposta (carregando ou erro), não há número nenhum', () => {
    expect(montarResumoCartoesFrota(undefined)).toBeNull();
  });

  it('conta cada situação dos cartões e deixa de fora as que não têm cartão', () => {
    const resumo = montarResumoCartoesFrota(
      visao({
        frotaPorSituacao: [
          { situacao: 'DISPONIVEL', quantidade: 9 },
          { situacao: 'LOCADO', quantidade: 34 },
          { situacao: 'MANUTENCAO', quantidade: 4 },
          { situacao: 'RESERVADO', quantidade: 3 },
          { situacao: 'VENDIDO', quantidade: 1 },
          { situacao: 'BAIXADO', quantidade: 2 },
        ],
      }),
    );

    expect(resumo).toEqual(expect.objectContaining({ disponiveis: 9, locados: 34, emManutencao: 4 }));
  });

  it('situação que não está na lista conta como zero (sem quebrar)', () => {
    const resumo = montarResumoCartoesFrota(visao({ frotaPorSituacao: [{ situacao: 'LOCADO', quantidade: 2 }] }));

    expect(resumo).toEqual(expect.objectContaining({ disponiveis: 0, locados: 2, emManutencao: 0 }));
  });

  it('frota sem nenhum veículo: todos os cartões de situação em zero', () => {
    const resumo = montarResumoCartoesFrota(visao({ frotaPorSituacao: [], taxaOcupacaoPercentual: 0 }));

    expect(resumo).toEqual(expect.objectContaining({ disponiveis: 0, locados: 0, emManutencao: 0 }));
  });

  it('leva a taxa de ocupação e as devoluções previstas para hoje da visão geral', () => {
    const resumo = montarResumoCartoesFrota(visao({ taxaOcupacaoPercentual: 78.5, devolucoesPrevistasHoje: 31 }));

    expect(resumo?.taxaOcupacaoPercentual).toBe(78.5);
    expect(resumo?.devolucoesPrevistasHoje).toBe(31);
  });
});

describe('proporcaoAnelFrota (anel e barra da taxa)', () => {
  it('zero fica vazio', () => {
    expect(proporcaoAnelFrota(0)).toBe(0);
  });

  it('valor do meio passa como está', () => {
    expect(proporcaoAnelFrota(78.5)).toBe(78.5);
  });

  it('cem por cento enche o anel', () => {
    expect(proporcaoAnelFrota(100)).toBe(100);
  });

  it('acima de cem limita a cem', () => {
    expect(proporcaoAnelFrota(120)).toBe(100);
  });

  it('abaixo de zero ou sem número fica vazio', () => {
    expect(proporcaoAnelFrota(-5)).toBe(0);
    expect(proporcaoAnelFrota(Number.NaN)).toBe(0);
  });
});

describe('formatarTaxaOcupacaoFrota', () => {
  it('uma casa decimal, vírgula e porcentagem', () => {
    expect(formatarTaxaOcupacaoFrota(78.5)).toBe('78,5%');
    expect(formatarTaxaOcupacaoFrota(68)).toBe('68,0%');
  });

  it('zero sai com uma casa também', () => {
    expect(formatarTaxaOcupacaoFrota(0)).toBe('0,0%');
  });
});

describe('textoEncerramHojeFrota', () => {
  it('zero: "Nenhuma encerra hoje"', () => {
    expect(textoEncerramHojeFrota(0)).toBe('Nenhuma encerra hoje');
  });

  it('um: "1 encerra hoje"', () => {
    expect(textoEncerramHojeFrota(1)).toBe('1 encerra hoje');
  });

  it('mais de um: "N encerram hoje", com o número formatado', () => {
    expect(textoEncerramHojeFrota(31)).toBe('31 encerram hoje');
    expect(textoEncerramHojeFrota(1200)).toBe('1.200 encerram hoje');
  });
});
