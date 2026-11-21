import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import {
  buscarIndicadoresFinanceiros,
  listarTitulosFinanceiros,
  montarParametrosTitulosFinanceiros,
} from '@/api/painelFinanceiro';
import type { RespostaTitulosFinanceiros } from '@/types/painelFinanceiro';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const getMock = vi.mocked(api.get);

const RESPOSTA_TITULOS: RespostaTitulosFinanceiros = {
  quantidadeTodos: 3,
  quantidadeEmAtraso: 1,
  quantidadeVenceHoje: 0,
  quantidadePagosRecentes: 1,
  pagina: { itens: [], total: 0, pagina: 2, tamanhoPagina: 20 },
};

describe('montarParametrosTitulosFinanceiros', () => {
  it('envia aba, página e tamanho da página, sem período', () => {
    expect(montarParametrosTitulosFinanceiros({ aba: 'EM_ATRASO', pagina: 2, tamanhoPagina: 20 })).toEqual({
      aba: 'EM_ATRASO',
      pagina: 2,
      tamanhoPagina: 20,
    });
  });
});

describe('buscarIndicadoresFinanceiros', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { recebidoStr: 'R$ 0,00' } });
  });

  it('chama GET /api/painel/financeiro sem parâmetros (o servidor escolhe o mês corrente)', async () => {
    await buscarIndicadoresFinanceiros();

    expect(getMock).toHaveBeenCalledWith('/api/painel/financeiro');
  });
});

describe('listarTitulosFinanceiros', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: RESPOSTA_TITULOS });
  });

  it('chama GET /api/painel/financeiro/titulos com aba, página e tamanho na query', async () => {
    await listarTitulosFinanceiros({ aba: 'EM_ATRASO', pagina: 2, tamanhoPagina: 20 });

    expect(getMock).toHaveBeenCalledWith('/api/painel/financeiro/titulos', {
      params: { aba: 'EM_ATRASO', pagina: 2, tamanhoPagina: 20 },
    });
  });

  it('devolve o corpo da resposta sem alterar os contadores nem a página', async () => {
    const dados = await listarTitulosFinanceiros({ aba: 'TODOS', pagina: 2, tamanhoPagina: 20 });

    expect(dados).toEqual(RESPOSTA_TITULOS);
  });
});
