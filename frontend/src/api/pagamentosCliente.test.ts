import { describe, expect, it, vi } from 'vitest';
import { montarParametrosMeusPagamentos, montarParametrosResumoMeusPagamentos } from '@/api/pagamentosCliente';
import type { FiltroMeusPagamentos } from '@/types/pagamentos';

// O cliente HTTP vira dublê: ele lê o navegador ao carregar, que não existe nos testes.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn() },
}));

describe('parâmetros de GET /api/pagamentos/meus', () => {
  it('manda aba, periodo, pagina e tamanhoPagina', () => {
    const filtro: FiltroMeusPagamentos = { aba: 'PENDENTES', periodo: 'SEIS_MESES', pagina: 2, tamanhoPagina: 10 };
    expect(montarParametrosMeusPagamentos(filtro)).toEqual({
      aba: 'PENDENTES',
      periodo: 'SEIS_MESES',
      pagina: 2,
      tamanhoPagina: 10,
    });
  });

  it('não manda busca nem campo fora do contrato', () => {
    const parametros = montarParametrosMeusPagamentos({
      aba: 'TODOS',
      periodo: 'TUDO',
      pagina: 1,
      tamanhoPagina: 10,
    });
    expect(Object.keys(parametros).sort()).toEqual(['aba', 'pagina', 'periodo', 'tamanhoPagina']);
  });
});

describe('parâmetros de GET /api/pagamentos/meus/resumo', () => {
  it('manda só o período', () => {
    expect(montarParametrosResumoMeusPagamentos('ANO_ATUAL')).toEqual({ periodo: 'ANO_ATUAL' });
  });
});
