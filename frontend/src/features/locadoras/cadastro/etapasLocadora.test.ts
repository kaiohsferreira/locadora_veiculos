import { describe, expect, it } from 'vitest';
import { CAMINHOS } from '@/routes/caminhos';
import { caminhoSemDadosEmpresa } from './etapasLocadora';

describe('caminhoSemDadosEmpresa: não entra na etapa 2 ou 3 sem a etapa 1', () => {
  it('a etapa 1 sempre pode abrir, com ou sem dados', () => {
    expect(caminhoSemDadosEmpresa(1, false)).toBeNull();
    expect(caminhoSemDadosEmpresa(1, true)).toBeNull();
  });

  it('a etapa 2 sem dados da etapa 1 volta para a etapa 1', () => {
    expect(caminhoSemDadosEmpresa(2, false)).toBe(CAMINHOS.cadastroLocadoraEmpresa);
  });

  it('a etapa 3 sem dados da etapa 1 volta para a etapa 1', () => {
    expect(caminhoSemDadosEmpresa(3, false)).toBe(CAMINHOS.cadastroLocadoraEmpresa);
  });

  it('com os dados da etapa 1 em memória, as etapas 2 e 3 abrem', () => {
    expect(caminhoSemDadosEmpresa(2, true)).toBeNull();
    expect(caminhoSemDadosEmpresa(3, true)).toBeNull();
  });

  it('o destino é a etapa 1 real do cadastro da locadora', () => {
    expect(CAMINHOS.cadastroLocadoraEmpresa).toBe('/cadastro/locadora/empresa');
  });
});
