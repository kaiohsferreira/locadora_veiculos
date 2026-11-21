import { describe, expect, it, vi } from 'vitest';
import { isValidElement } from 'react';
import type { RouteObject } from 'react-router-dom';
import type { CargoFuncionario } from '@/types/enums';
import { CAMINHOS } from './caminhos';
import { rotas } from './rotas';

// A árvore de rotas é lida como ela é montada no app. O roteador do navegador não existe no ambiente de teste (sem DOM),
// então é trocado por um dublê; a API também não é chamada (a camada HTTP usa `window` ao ser importada).
vi.mock('react-router-dom', async (importOriginal) => {
  const original = await importOriginal<typeof import('react-router-dom')>();
  return { ...original, createBrowserRouter: vi.fn(() => ({})) };
});
vi.mock('@/api/client', () => ({ api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() } }));

/** Cargos do RotaPorCargo mais próximo que envolve a rota (o elemento com a prop cargosPermitidos). */
function cargosDoAncestralMaisProximo(cadeia: readonly RouteObject[]): readonly CargoFuncionario[] | undefined {
  for (const rota of [...cadeia].reverse()) {
    if (isValidElement(rota.element)) {
      const propriedades = rota.element.props as { cargosPermitidos?: readonly CargoFuncionario[] };
      if (propriedades.cargosPermitidos !== undefined) return propriedades.cargosPermitidos;
    }
  }
  return undefined;
}

/** Percorre a árvore até achar o caminho e devolve os cargos da proteção que o envolve. */
function cargosDaRota(caminho: string, arvore: readonly RouteObject[], ancestrais: readonly RouteObject[] = []): readonly CargoFuncionario[] | undefined {
  for (const rota of arvore) {
    const cadeia = [...ancestrais, rota];
    if (rota.path === caminho) return cargosDoAncestralMaisProximo(cadeia);
    if (rota.children !== undefined) {
      const achado = cargosDaRota(caminho, rota.children, cadeia);
      if (achado !== undefined) return achado;
    }
  }
  return undefined;
}

describe('rota da tela 09 (painel financeiro) em rotas.tsx', () => {
  it('só FINANCEIRO e ADMINISTRADOR abrem o painel: nenhum outro cargo, nem ATENDENTE', () => {
    const cargos = cargosDaRota(CAMINHOS.financeiro, rotas);

    expect(cargos).toBeDefined();
    expect([...(cargos ?? [])].sort()).toEqual(['ADMINISTRADOR', 'FINANCEIRO']);
  });
});
