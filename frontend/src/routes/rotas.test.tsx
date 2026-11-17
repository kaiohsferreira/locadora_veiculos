import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import type { RouteObject } from 'react-router-dom';
import { CAMINHOS } from './caminhos';
import { CadastroVeiculoPagina } from './paginas';
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

function acharRotaPorCaminho(lista: readonly RouteObject[] | undefined, caminho: string): RouteObject | undefined {
  for (const rota of lista ?? []) {
    if (rota.path === caminho) return rota;
    const dentro = acharRotaPorCaminho(rota.children, caminho);
    if (dentro) return dentro;
  }
  return undefined;
}
