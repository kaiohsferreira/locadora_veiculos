import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '@/api/client';
import {
  alterarSituacaoFuncionarioEquipe,
  buscarFuncionarioEquipe,
  cadastrarFuncionarioEquipe,
  editarFuncionarioEquipe,
  listarEquipe,
  montarParametrosListagemEquipe,
} from '@/api/equipe';
import type { FiltroEquipe, RequisicaoCadastroFuncionario, RequisicaoEdicaoFuncionario } from '@/types/equipe';

vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const getMock = vi.mocked(api.get);
const postMock = vi.mocked(api.post);
const putMock = vi.mocked(api.put);
const patchMock = vi.mocked(api.patch);

const FILTRO_BASE: FiltroEquipe = { pagina: 2, tamanhoPagina: 20 };

const CADASTRO: RequisicaoCadastroFuncionario = {
  nome: 'Carlos Eduardo Silveira',
  cpf: '52998224725',
  matricula: 'MAT-0200',
  email: 'carlos@rotasul.com.br',
  dataAdmissao: '2025-02-23',
  cargo: 'ATENDENTE',
  senhaInicial: 'Senha1234',
};

const EDICAO: RequisicaoEdicaoFuncionario = {
  nome: 'Carlos Eduardo Silveira',
  matricula: 'MAT-0200',
  email: 'carlos@rotasul.com.br',
  dataAdmissao: '2025-02-23',
  cargo: 'VENDEDOR',
};

describe('montarParametrosListagemEquipe', () => {
  it('envia pagina e tamanhoPagina e omite busca, cargo e ativo quando ausentes', () => {
    expect(montarParametrosListagemEquipe(FILTRO_BASE)).toEqual({ pagina: 2, tamanhoPagina: 20 });
  });

  it('não envia busca vazia nem só com espaços, e aparada quando preenchida', () => {
    expect(montarParametrosListagemEquipe({ ...FILTRO_BASE, busca: '   ' })).not.toHaveProperty('busca');
    expect(montarParametrosListagemEquipe({ ...FILTRO_BASE, busca: '  Ana  ' }).busca).toBe('Ana');
  });

  it('envia ativo=false (inativos) e não o omite, pois falso é um filtro válido', () => {
    expect(montarParametrosListagemEquipe({ ...FILTRO_BASE, ativo: false })).toMatchObject({ ativo: false });
    expect(montarParametrosListagemEquipe({ ...FILTRO_BASE, ativo: true })).toMatchObject({ ativo: true });
  });

  it('envia o cargo quando escolhido', () => {
    expect(montarParametrosListagemEquipe({ ...FILTRO_BASE, cargo: 'GESTOR_FROTA' })).toMatchObject({
      cargo: 'GESTOR_FROTA',
    });
  });
});

describe('listarEquipe', () => {
  beforeEach(() => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { itens: [], total: 0, pagina: 2, tamanhoPagina: 20 } });
  });

  it('faz GET em /api/equipe com os parâmetros da query e devolve a resposta', async () => {
    const resposta = await listarEquipe({ ...FILTRO_BASE, busca: 'MAT-0001', ativo: true });

    expect(resposta).toMatchObject({ total: 0, pagina: 2 });
    const [url, configuracao] = getMock.mock.calls[0];
    expect(url).toBe('/api/equipe');
    expect(configuracao?.params).toEqual({ pagina: 2, tamanhoPagina: 20, busca: 'MAT-0001', ativo: true });
  });

  it('não coloca a busca na URL', async () => {
    await listarEquipe({ ...FILTRO_BASE, busca: '341.123' });
    const [url] = getMock.mock.calls[0];
    expect(String(url)).not.toContain('341');
    expect(String(url)).not.toContain('?');
  });
});

describe('buscarFuncionarioEquipe', () => {
  it('faz GET em /api/equipe/{id}', async () => {
    getMock.mockReset();
    getMock.mockResolvedValue({ data: { idFuncionario: 7 } });

    await buscarFuncionarioEquipe(7);

    expect(getMock.mock.calls[0][0]).toBe('/api/equipe/7');
  });
});

describe('cadastrarFuncionarioEquipe', () => {
  beforeEach(() => {
    postMock.mockReset();
    postMock.mockResolvedValue({ data: { idFuncionario: 9 } });
  });

  it('faz POST em /api/equipe com o corpo do cadastro, inclusive a senha inicial', async () => {
    await cadastrarFuncionarioEquipe(CADASTRO);

    const [url, corpo] = postMock.mock.calls[0];
    expect(url).toBe('/api/equipe');
    expect(corpo).toEqual(CADASTRO);
  });

  it('nunca manda a senha inicial na URL nem em params', async () => {
    await cadastrarFuncionarioEquipe(CADASTRO);

    const [url, , configuracao] = postMock.mock.calls[0];
    expect(String(url)).not.toContain('Senha1234');
    expect(configuracao).toBeUndefined();
  });
});

describe('editarFuncionarioEquipe', () => {
  beforeEach(() => {
    putMock.mockReset();
    putMock.mockResolvedValue({ data: undefined });
  });

  it('faz PUT em /api/equipe/{id} só com os campos editáveis (sem CPF e sem senha)', async () => {
    await editarFuncionarioEquipe(7, EDICAO);

    const [url, corpo] = putMock.mock.calls[0];
    expect(url).toBe('/api/equipe/7');
    expect(corpo).toEqual(EDICAO);
    expect(corpo).not.toHaveProperty('cpf');
    expect(corpo).not.toHaveProperty('senhaInicial');
  });
});

describe('alterarSituacaoFuncionarioEquipe', () => {
  beforeEach(() => {
    patchMock.mockReset();
    patchMock.mockResolvedValue({ data: undefined });
  });

  it('faz PATCH em /api/equipe/{id}/situacao com { ativo } no corpo', async () => {
    await alterarSituacaoFuncionarioEquipe(7, { ativo: false });

    expect(patchMock.mock.calls[0]).toEqual(['/api/equipe/7/situacao', { ativo: false }]);
  });

  it('reativa com ativo = true', async () => {
    await alterarSituacaoFuncionarioEquipe(7, { ativo: true });

    expect(patchMock.mock.calls[0][1]).toEqual({ ativo: true });
  });
});
