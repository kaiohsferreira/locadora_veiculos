import { describe, expect, it } from 'vitest';
import { ROTULO_STATUS_VEICULO } from '@/types/enums';
import {
  calcularSegmentosRosca,
  montarLegendaFrota,
  ORDEM_SITUACOES_FROTA,
} from './frota';

describe('montarLegendaFrota', () => {
  it('traz as quatro situações em operação na ordem do desenho, mesmo as sem veículo', () => {
    const legenda = montarLegendaFrota([{ situacao: 'LOCADO', quantidade: 34 }]);

    expect(legenda.map((linha) => linha.situacao)).toEqual(['LOCADO', 'DISPONIVEL', 'RESERVADO', 'MANUTENCAO']);
    expect(legenda.map((linha) => linha.situacao)).toEqual(ORDEM_SITUACOES_FROTA);
    expect(legenda.find((linha) => linha.situacao === 'DISPONIVEL')).toMatchObject({ quantidade: 0, percentual: 0 });
  });

  it('vendido e baixado não aparecem na legenda nem entram no total da rosca', () => {
    const legenda = montarLegendaFrota([
      { situacao: 'LOCADO', quantidade: 5 },
      { situacao: 'VENDIDO', quantidade: 3 },
      { situacao: 'BAIXADO', quantidade: 2 },
    ]);

    expect(legenda.map((linha) => linha.situacao)).not.toContain('VENDIDO');
    expect(legenda.map((linha) => linha.situacao)).not.toContain('BAIXADO');
    expect(legenda.find((linha) => linha.situacao === 'LOCADO')).toMatchObject({ quantidade: 5, percentual: 100 });
  });

  it('usa os rótulos de StatusVeiculo do projeto (não a categoria "À venda" do desenho)', () => {
    const legenda = montarLegendaFrota([]);

    expect(legenda.map((linha) => linha.rotulo)).toEqual(ORDEM_SITUACOES_FROTA.map((s) => ROTULO_STATUS_VEICULO[s]));
    expect(legenda.map((linha) => linha.rotulo)).not.toContain('À venda (seminovos)');
  });

  it('calcula o percentual de cada situação sobre o total', () => {
    const legenda = montarLegendaFrota([
      { situacao: 'LOCADO', quantidade: 34 },
      { situacao: 'DISPONIVEL', quantidade: 16 },
    ]);

    expect(legenda.find((linha) => linha.situacao === 'LOCADO')?.percentual).toBe(68);
    expect(legenda.find((linha) => linha.situacao === 'DISPONIVEL')?.percentual).toBe(32);
  });

  it('soma quantidades repetidas da mesma situação', () => {
    const legenda = montarLegendaFrota([
      { situacao: 'LOCADO', quantidade: 1 },
      { situacao: 'LOCADO', quantidade: 2 },
    ]);

    expect(legenda.find((linha) => linha.situacao === 'LOCADO')?.quantidade).toBe(3);
  });

  it('total zero: todos os percentuais ficam em zero (sem divisão por zero)', () => {
    const legenda = montarLegendaFrota([]);

    expect(legenda.every((linha) => linha.percentual === 0 && linha.quantidade === 0)).toBe(true);
  });
});

describe('calcularSegmentosRosca', () => {
  it('reproduz as proporções e os deslocamentos da frota em operação (50 veículos: 68, 18, 6 e 8)', () => {
    const legenda = montarLegendaFrota([
      { situacao: 'LOCADO', quantidade: 34 },
      { situacao: 'DISPONIVEL', quantidade: 9 },
      { situacao: 'RESERVADO', quantidade: 3 },
      { situacao: 'MANUTENCAO', quantidade: 4 },
      { situacao: 'VENDIDO', quantidade: 1 },
    ]);

    const segmentos = calcularSegmentosRosca(legenda);

    expect(segmentos.map((item) => item.comprimento)).toEqual([68, 18, 6, 8]);
    expect(segmentos.map((item) => item.deslocamento)).toEqual([0, -68, -86, -92]);
  });

  it('a soma dos comprimentos fecha o círculo (100) quando há veículos', () => {
    const legenda = montarLegendaFrota([
      { situacao: 'LOCADO', quantidade: 7 },
      { situacao: 'DISPONIVEL', quantidade: 5 },
      { situacao: 'BAIXADO', quantidade: 2 },
    ]);

    const soma = calcularSegmentosRosca(legenda).reduce((acumulado, item) => acumulado + item.comprimento, 0);

    expect(soma).toBeCloseTo(100, 10);
  });

  it('não gera arco para situação com zero veículo', () => {
    const legenda = montarLegendaFrota([{ situacao: 'LOCADO', quantidade: 4 }]);

    const segmentos = calcularSegmentosRosca(legenda);

    expect(segmentos).toHaveLength(1);
    expect(segmentos[0]).toMatchObject({ situacao: 'LOCADO', comprimento: 100, deslocamento: 0 });
  });

  it('total zero: nenhum arco (a rosca fica só com a trilha)', () => {
    expect(calcularSegmentosRosca(montarLegendaFrota([]))).toEqual([]);
  });
});
