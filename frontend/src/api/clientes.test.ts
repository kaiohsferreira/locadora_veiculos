import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { verificarCadastroCliente } from '@/api/clientes';

vi.mock('@/api/client', () => ({
  api: { post: vi.fn() },
}));

const postMock = vi.mocked(api.post);

describe('verificarCadastroCliente', () => {
  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: { existe: true } });
  });

  it('faz POST em /api/clientes/verificar-cadastro com { campo, valor } no corpo e devolve a resposta', async () => {
    const resposta = await verificarCadastroCliente({ campo: 'CPF', valor: '52998224725' });

    expect(resposta).toEqual({ existe: true });
    expect(postMock).toHaveBeenCalledTimes(1);
    const [url, corpo] = postMock.mock.calls[0];
    expect(url).toBe('/api/clientes/verificar-cadastro');
    expect(corpo).toEqual({ campo: 'CPF', valor: '52998224725' });
  });

  it('nunca manda o valor na URL nem em params', async () => {
    await verificarCadastroCliente({ campo: 'EMAIL', valor: 'mariana@email.com' });

    const [url, , configuracao] = postMock.mock.calls[0];
    expect(String(url)).not.toContain('mariana');
    expect(String(url)).not.toContain('?');
    expect(configuracao).not.toHaveProperty('params');
  });

  it('usa timeout de 5 segundos e repassa o signal de cancelamento', async () => {
    const controle = new AbortController();
    await verificarCadastroCliente({ campo: 'CNH', valor: '12345678901' }, controle.signal);

    const [, , configuracao] = postMock.mock.calls[0];
    expect(configuracao?.timeout).toBe(5000);
    expect(configuracao?.signal).toBe(controle.signal);
  });

  it('propaga o erro da API para quem chama (o hook decide ignorar)', async () => {
    const erro = new Error('falha');
    postMock.mockRejectedValue(erro);

    await expect(verificarCadastroCliente({ campo: 'CPF', valor: '52998224725' })).rejects.toBe(erro);
  });
});
