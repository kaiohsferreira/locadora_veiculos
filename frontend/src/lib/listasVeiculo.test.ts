import { describe, expect, it } from 'vitest';
import {
  CORES_VEICULO,
  MARCAS_VEICULO,
  STATUS_INICIAL_VEICULO_TRAVADO,
  corForaDaListaVeiculo,
  opcoesMarcaVeiculo,
} from '@/lib/listasVeiculo';

describe('marcas (lista fechada do Kaio, 16/11/2026)', () => {
  it('tem as 23 marcas na ordem alfabética e com os nomes com acento e hífen como na lista', () => {
    expect(MARCAS_VEICULO).toHaveLength(23);
    expect([...MARCAS_VEICULO]).toEqual([...MARCAS_VEICULO].sort((a, b) => a.localeCompare(b, 'pt-BR')));
    expect(MARCAS_VEICULO).toContain('Citroën');
    expect(MARCAS_VEICULO).toContain('Mercedes-Benz');
    expect(MARCAS_VEICULO).toContain('Caoa Chery');
  });

  it('usa o nome "BYD" da lista do Kaio, e não o rótulo antigo do desenho', () => {
    expect(MARCAS_VEICULO).not.toContain('BYD Eletromobilidade');
  });

  it('opções: as marcas da lista, sem nenhuma extra quando o valor atual está na lista ou vazio', () => {
    expect(opcoesMarcaVeiculo('Fiat')).toHaveLength(MARCAS_VEICULO.length);
    expect(opcoesMarcaVeiculo('')).toHaveLength(MARCAS_VEICULO.length);
    expect(opcoesMarcaVeiculo('  ')).toHaveLength(MARCAS_VEICULO.length);
  });

  it('valor gravado fora da lista aparece como opção extra no fim, sem trocar nada', () => {
    const opcoes = opcoesMarcaVeiculo(' Lotus ');

    expect(opcoes).toHaveLength(MARCAS_VEICULO.length + 1);
    expect(opcoes.at(-1)).toEqual({ valor: 'Lotus', rotulo: 'Lotus' });
  });
});

describe('cores (as quatro amostras do desenho)', () => {
  it('tem as quatro, com a cinza enviando "Cinza Silverstone" e mostrando "Cinza Silver"', () => {
    expect(CORES_VEICULO.map((cor) => cor.valor)).toEqual([
      'Preto Carbon',
      'Branco Polar',
      'Prata Shark',
      'Cinza Silverstone',
    ]);
    expect(CORES_VEICULO[3].rotulo).toBe('Cinza Silver');
  });

  it('valor fora das quatro é marcado como fora da lista; vazio e as quatro não', () => {
    expect(corForaDaListaVeiculo('Azul Marinho')).toBe(true);
    expect(corForaDaListaVeiculo('Prata Shark')).toBe(false);
    expect(corForaDaListaVeiculo('')).toBe(false);
  });
});

describe('status inicial travado', () => {
  it('é só "Disponível para Locação Imediata", com o valor DISPONIVEL (RF-02)', () => {
    expect(STATUS_INICIAL_VEICULO_TRAVADO).toEqual({
      valor: 'DISPONIVEL',
      rotulo: '● Disponível para Locação Imediata',
    });
  });
});
