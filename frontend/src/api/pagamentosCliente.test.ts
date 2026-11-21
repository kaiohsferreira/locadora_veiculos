import { describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import {
  caminhoGerarPixMeuPagamento,
  gerarPixMeuPagamento,
  montarParametrosMeusPagamentos,
  montarParametrosResumoMeusPagamentos,
} from '@/api/pagamentosCliente';
import type { CobrancaPix, FiltroMeusPagamentos } from '@/types/pagamentos';

// O cliente HTTP vira dublê: ele lê o navegador ao carregar, que não existe nos testes.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn() },
}));

const COBRANCA_PIX: CobrancaPix = {
  idPagamento: 77,
  forma: 'PIX',
  valor: 395.6,
  valorStr: 'R$ 395,60',
  qrCodeBase64: 'iVBORw0KGgo=',
  copiaECola: 'FAKE-COPIA-PAG-77',
  expiraEmStr: '20/11/2026 15:30:00',
  segundosParaExpirar: 900,
};

describe('POST /api/pagamentos/meus/{id}/pix (gerar ou reabrir o PIX do cliente)', () => {
  it('monta o caminho com o id da cobrança', () => {
    expect(caminhoGerarPixMeuPagamento(77)).toBe('/api/pagamentos/meus/77/pix');
  });

  it('chama a rota sem corpo: nenhum argumento além do caminho', () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: COBRANCA_PIX });
    return gerarPixMeuPagamento(77).then((cobranca) => {
      expect(api.post).toHaveBeenLastCalledWith('/api/pagamentos/meus/77/pix');
      expect(vi.mocked(api.post).mock.lastCall).toHaveLength(1);
      expect(cobranca).toEqual(COBRANCA_PIX);
    });
  });
});

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
