import { describe, expect, it } from 'vitest';
import {
  montarFiltroContagem,
  montarFiltroReservas,
  normalizarBusca,
  TAMANHO_MAXIMO_BUSCA_RESERVAS,
  TAMANHO_PAGINA_ATIVAS,
  TAMANHO_PAGINA_HISTORICO,
  ultimaPagina,
} from '@/features/area-cliente/reservas/parametrosReservas';

describe('tamanho de página por aba', () => {
  it('ativas usa 5 e histórico usa 10', () => {
    expect(montarFiltroReservas('ATIVAS', 1, '').tamanhoPagina).toBe(TAMANHO_PAGINA_ATIVAS);
    expect(montarFiltroReservas('HISTORICO', 1, '').tamanhoPagina).toBe(TAMANHO_PAGINA_HISTORICO);
    expect(TAMANHO_PAGINA_ATIVAS).toBe(5);
    expect(TAMANHO_PAGINA_HISTORICO).toBe(10);
  });
});

describe('normalizarBusca', () => {
  it('apara os espaços das pontas', () => {
    expect(normalizarBusca('  renegade  ')).toBe('renegade');
  });

  it('corta em 60 caracteres e volta a aparar', () => {
    const longa = `${'a'.repeat(59)} ${'b'.repeat(10)}`;
    const resultado = normalizarBusca(longa);
    expect(resultado).toBe('a'.repeat(59));
    expect(normalizarBusca('x'.repeat(200))).toHaveLength(TAMANHO_MAXIMO_BUSCA_RESERVAS);
  });

  it('busca vazia ou só com espaços não vira parâmetro', () => {
    expect(normalizarBusca('')).toBeUndefined();
    expect(normalizarBusca('    ')).toBeUndefined();
  });
});

describe('montarFiltroReservas', () => {
  it('envia a busca só no histórico', () => {
    expect(montarFiltroReservas('HISTORICO', 1, ' AV-00042 ')).toEqual({
      pagina: 1,
      tamanhoPagina: 10,
      situacao: 'HISTORICO',
      busca: 'AV-00042',
    });
    expect(montarFiltroReservas('ATIVAS', 1, 'renegade')).not.toHaveProperty('busca');
  });

  it('busca vazia não aparece no filtro', () => {
    expect(montarFiltroReservas('HISTORICO', 2, '   ')).not.toHaveProperty('busca');
  });

  it('nunca leva status: a tela só usa situacao', () => {
    expect(montarFiltroReservas('ATIVAS', 1, '')).not.toHaveProperty('status');
  });
});

describe('montarFiltroContagem', () => {
  it('pede uma linha só, da primeira página', () => {
    expect(montarFiltroContagem('ATIVAS')).toEqual({ pagina: 1, tamanhoPagina: 1, situacao: 'ATIVAS' });
  });
});

describe('ultimaPagina', () => {
  it('devolve a última página existente', () => {
    expect(ultimaPagina(11, 5)).toBe(3);
    expect(ultimaPagina(10, 5)).toBe(2);
  });

  it('sem registros devolve 1', () => {
    expect(ultimaPagina(0, 10)).toBe(1);
  });
});
