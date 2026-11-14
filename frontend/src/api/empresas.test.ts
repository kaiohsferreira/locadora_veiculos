import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import { cadastrarLocadora, verificarCadastroEmpresa } from '@/api/empresas';

vi.mock('@/api/client', () => ({
  api: { post: vi.fn() },
}));

const postMock = vi.mocked(api.post);

describe('verificarCadastroEmpresa', () => {
  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: { existe: true } });
  });

  it('faz POST em /api/empresas/verificar-cadastro com { campo, valor } no corpo e devolve a resposta', async () => {
    const resposta = await verificarCadastroEmpresa({ campo: 'CNPJ', valor: '11222333000181' });

    expect(resposta).toEqual({ existe: true });
    expect(postMock).toHaveBeenCalledTimes(1);
    const [url, corpo] = postMock.mock.calls[0];
    expect(url).toBe('/api/empresas/verificar-cadastro');
    expect(corpo).toEqual({ campo: 'CNPJ', valor: '11222333000181' });
  });

  it('nunca manda o valor na URL nem em params', async () => {
    await verificarCadastroEmpresa({ campo: 'EMAIL', valor: 'contato@rotasul.com.br' });

    const [url, , configuracao] = postMock.mock.calls[0];
    expect(String(url)).not.toContain('rotasul');
    expect(String(url)).not.toContain('?');
    expect(configuracao).not.toHaveProperty('params');
  });

  it('usa timeout de 5 segundos e repassa o signal de cancelamento', async () => {
    const controle = new AbortController();
    await verificarCadastroEmpresa({ campo: 'CNPJ', valor: '11222333000181' }, controle.signal);

    const [, , configuracao] = postMock.mock.calls[0];
    expect(configuracao?.timeout).toBe(5000);
    expect(configuracao?.signal).toBe(controle.signal);
  });

  it('propaga o erro da API para quem chama (o hook decide ignorar)', async () => {
    const erro = new Error('limite');
    postMock.mockRejectedValue(erro);

    await expect(verificarCadastroEmpresa({ campo: 'CNPJ', valor: '11222333000181' })).rejects.toBe(erro);
  });
});

describe('cadastrarLocadora', () => {
  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: { idEmpresa: 7 } });
  });

  it('faz POST em /api/empresas com o corpo do cadastro e devolve a resposta', async () => {
    const requisicao = {
      razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
      nomeFantasia: 'Rota Sul Locadora',
      cnpj: '11222333000181',
      telefone: '5133429000',
      email: 'contato@rotasul.com.br',
      cep: '90200310',
      logradouro: 'Av. Severo Dullius',
      numero: '1240',
      complemento: null,
      bairro: 'São João',
      cidade: 'Porto Alegre',
      uf: 'RS',
      responsavelNome: 'Marina Costa',
      responsavelCpf: '52998224725',
      responsavelEmail: 'marina@rotasul.com.br',
      senha: 'Senha1234',
      aceiteTermos: true,
    };

    const resposta = await cadastrarLocadora(requisicao);

    expect(resposta).toEqual({ idEmpresa: 7 });
    const [url, corpo] = postMock.mock.calls[0];
    expect(url).toBe('/api/empresas');
    expect(corpo).toEqual(requisicao);
  });
});
