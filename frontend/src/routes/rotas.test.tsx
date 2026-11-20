import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import type { RouteObject } from 'react-router-dom';
import { ProvedorFiltrosFrota } from '@/features/frota/listagem/ProvedorFiltrosFrota';
import { CAMINHOS } from './caminhos';
import { CadastroVeiculoPagina, ListagemFrotaPagina, PainelFrotaPagina } from './paginas';
import { rotas } from './rotas';

// A árvore de rotas é lida como dado: o roteador do navegador não é criado nos testes (não há janela aqui).
vi.mock('react-router-dom', async (importOriginal) => {
  const original = await importOriginal<typeof import('react-router-dom')>();
  return { ...original, createBrowserRouter: vi.fn(() => ({})) };
});
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

/** Rota do back office (o caminho /back-office, com as rotas filhas). */
function rotaBackOffice(): RouteObject {
  const achada = rotas.find((rota) => rota.path === CAMINHOS.backOffice);
  if (!achada) throw new Error('rota do back office não encontrada');
  return achada;
}

/** Procura, em profundidade, a rota de índice (index: true) dentro de uma lista de rotas. */
function acharIndice(lista: readonly RouteObject[] | undefined): RouteObject | undefined {
  for (const rota of lista ?? []) {
    if (rota.index) return rota;
    const dentro = acharIndice(rota.children);
    if (dentro) return dentro;
  }
  return undefined;
}

describe('rotas: índice do back office', () => {
  it('/back-office redireciona para a visão geral (não mais para a frota)', () => {
    const indice = acharIndice(rotaBackOffice().children);
    const elemento = indice?.element as ReactElement<{ to: string; replace: boolean }> | undefined;

    expect(elemento?.props.to).toBe(CAMINHOS.visaoGeral);
    expect(elemento?.props.to).not.toBe(CAMINHOS.frota);
    expect(elemento?.props.replace).toBe(true);
  });

  it('o destino do redirecionamento tem a rota de visão geral registrada', () => {
    const visaoGeral = acharRotaPorCaminho(rotaBackOffice().children, CAMINHOS.visaoGeral);

    expect(visaoGeral).toBeDefined();
  });
});

describe('rotas: edição de veículo', () => {
  it('a rota de edição fica no back office protegido para funcionário, como a de cadastro', () => {
    const raiz = rotaBackOffice();
    const protegida = raiz.element as ReactElement<{ perfisPermitidos: string[] }> | undefined;
    const edicao = acharRotaPorCaminho(raiz.children, CAMINHOS.frotaEditarVeiculo);
    const cadastro = acharRotaPorCaminho(raiz.children, CAMINHOS.frotaNovoVeiculo);

    expect(protegida?.props.perfisPermitidos).toEqual(['funcionario']);
    expect(edicao).toBeDefined();
    expect(cadastro).toBeDefined();
  });

  it('usa a mesma página do cadastro enquanto a tela de edição não existe', () => {
    const raiz = rotaBackOffice();
    const edicao = acharRotaPorCaminho(raiz.children, CAMINHOS.frotaEditarVeiculo);
    const cadastro = acharRotaPorCaminho(raiz.children, CAMINHOS.frotaNovoVeiculo);
    const elementoEdicao = edicao?.element as ReactElement | undefined;
    const elementoCadastro = cadastro?.element as ReactElement | undefined;

    expect(elementoEdicao?.type).toBe(elementoCadastro?.type);
    expect(elementoEdicao?.type).toBe(CadastroVeiculoPagina);
  });
});

describe('rotas: frota com filtros compartilhados (Painel e Lista)', () => {
  it('Painel e Lista ficam dentro do provedor de filtros, na rota-pai sem caminho', () => {
    const provedor = acharRotaPorTipoDeElemento(rotaBackOffice().children, ProvedorFiltrosFrota);

    expect(provedor).toBeDefined();
    expect(provedor?.path).toBeUndefined();
    expect(acharRotaPorCaminho(provedor?.children, CAMINHOS.frota)).toBeDefined();
    expect(acharRotaPorCaminho(provedor?.children, CAMINHOS.frotaVeiculos)).toBeDefined();
  });

  it('/back-office/frota/veiculos abre a listagem nova (tela 12); /back-office/frota segue na tela 11', () => {
    const provedor = acharRotaPorTipoDeElemento(rotaBackOffice().children, ProvedorFiltrosFrota);
    const lista = acharRotaPorCaminho(provedor?.children, CAMINHOS.frotaVeiculos);
    const painel = acharRotaPorCaminho(provedor?.children, CAMINHOS.frota);

    expect((lista?.element as ReactElement | undefined)?.type).toBe(ListagemFrotaPagina);
    expect((painel?.element as ReactElement | undefined)?.type).toBe(PainelFrotaPagina);
  });

  it('novo veículo e edição ficam fora do provedor de filtros', () => {
    const provedor = acharRotaPorTipoDeElemento(rotaBackOffice().children, ProvedorFiltrosFrota);

    expect(acharRotaPorCaminho(provedor?.children, CAMINHOS.frotaNovoVeiculo)).toBeUndefined();
    expect(acharRotaPorCaminho(provedor?.children, CAMINHOS.frotaEditarVeiculo)).toBeUndefined();
    expect(acharRotaPorCaminho(rotaBackOffice().children, CAMINHOS.frotaNovoVeiculo)).toBeDefined();
  });
});

/** Procura, em profundidade, a rota cujo elemento é do tipo dado (ex.: o provedor da frota). */
function acharRotaPorTipoDeElemento(lista: readonly RouteObject[] | undefined, tipo: unknown): RouteObject | undefined {
  for (const rota of lista ?? []) {
    if ((rota.element as ReactElement | undefined)?.type === tipo) return rota;
    const dentro = acharRotaPorTipoDeElemento(rota.children, tipo);
    if (dentro) return dentro;
  }
  return undefined;
}

function acharRotaPorCaminho(lista: readonly RouteObject[] | undefined, caminho: string): RouteObject | undefined {
  for (const rota of lista ?? []) {
    if (rota.path === caminho) return rota;
    const dentro = acharRotaPorCaminho(rota.children, caminho);
    if (dentro) return dentro;
  }
  return undefined;
}
it('políticas de cobranças extras ficam sob proteção exclusiva de administrador', () => {
  function localizar(lista: readonly RouteObject[] | undefined): RouteObject | undefined {
    for (const rota of lista ?? []) {
      if (rota.children?.some(filha => filha.path === CAMINHOS.configuracoesCobrancasExtras)) return rota;
      const encontrada = localizar(rota.children); if (encontrada) return encontrada;
    }
    return undefined;
  }
  const pai = localizar(rotas);
  expect(pai).toBeDefined();
  if (!pai) throw new Error('rota administrativa de políticas não encontrada');
  expect((pai.element as ReactElement<{ cargosPermitidos: string[] }>).props.cargosPermitidos).toEqual(['ADMINISTRADOR']);
  expect(CAMINHOS.configuracoesCobrancasExtras).toBe('/back-office/configuracoes/cobrancas-extras');
});
