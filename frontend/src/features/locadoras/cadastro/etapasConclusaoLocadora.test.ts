import { describe, expect, it } from 'vitest';
import { CAMINHOS } from '@/routes/caminhos';
import { caminhoSemCadastroConcluido } from './etapasLocadora';

describe('caminhoSemCadastroConcluido: a confirmação (etapa 3) só abre com a conta criada', () => {
  it('com a conta criada em memória, a confirmação abre', () => {
    expect(caminhoSemCadastroConcluido(true, true)).toBeNull();
    expect(caminhoSemCadastroConcluido(false, true)).toBeNull();
  });

  it('sem a conta criada, mas com a etapa 1 em memória, volta para a etapa 2 (responsável)', () => {
    expect(caminhoSemCadastroConcluido(true, false)).toBe(CAMINHOS.cadastroLocadoraResponsavel);
  });

  it('sem a conta criada e sem a etapa 1 (recarregou a página), volta para a etapa 1', () => {
    expect(caminhoSemCadastroConcluido(false, false)).toBe(CAMINHOS.cadastroLocadoraEmpresa);
  });
});
