import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { listarCategorias, PAGINA_CATEGORIAS_SELETOR, TAMANHO_PAGINA_CATEGORIAS_SELETOR } from '@/api/categorias';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn() },
}));

const getMock = vi.mocked(api.get);

describe('listarCategorias', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 100 } });
  });

  it('faz GET em /api/categorias pedindo a primeira página com 100 itens', async () => {
    const resposta = await listarCategorias();

    const [url, configuracao] = getMock.mock.calls[0];
    expect(url).toBe('/api/categorias');
    expect(configuracao?.params).toEqual({ pagina: 1, tamanhoPagina: 100 });
    expect(resposta).toMatchObject({ total: 0, pagina: 1 });
  });

  it('usa as constantes da paginação (1 e 100), dentro do limite da API', () => {
    expect(PAGINA_CATEGORIAS_SELETOR).toBe(1);
    expect(TAMANHO_PAGINA_CATEGORIAS_SELETOR).toBeLessThanOrEqual(100);
  });
});
