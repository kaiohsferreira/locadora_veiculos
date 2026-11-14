import { describe, expect, it } from 'vitest';
import type { FuncionarioEquipe } from '@/types/equipe';
import { iniciaisDoNome, podeDesativar, podeReativar } from './regrasLinha';

function funcionario(parcial: Partial<FuncionarioEquipe> = {}): FuncionarioEquipe {
  return {
    idFuncionario: 1,
    nome: 'Marina Costa',
    cpfMascarado: '341.***.***-12',
    matricula: 'MAT-0001',
    email: 'marina@rotasul.com.br',
    cargo: 'ADMINISTRADOR',
    ativo: true,
    dataAdmissao: '2024-10-01',
    dataAdmissaoStr: '01/10/2024',
    ehVoce: false,
    ...parcial,
  };
}

describe('linha do próprio usuário (Você)', () => {
  it('não oferece desativar para a própria linha', () => {
    expect(podeDesativar(funcionario({ ehVoce: true }))).toBe(false);
  });

  it('oferece desativar para outro funcionário ativo', () => {
    expect(podeDesativar(funcionario({ ehVoce: false, ativo: true }))).toBe(true);
  });

  it('não oferece desativar para quem já está inativo', () => {
    expect(podeDesativar(funcionario({ ativo: false }))).toBe(false);
  });
});

describe('reativar', () => {
  it('aparece só para quem está inativo', () => {
    expect(podeReativar(funcionario({ ativo: false }))).toBe(true);
    expect(podeReativar(funcionario({ ativo: true }))).toBe(false);
  });
});

describe('iniciaisDoNome', () => {
  it('usa o primeiro nome e o último sobrenome', () => {
    expect(iniciaisDoNome('Marina Costa')).toBe('MC');
    expect(iniciaisDoNome('Carlos Eduardo Silveira')).toBe('CS');
  });

  it('com um nome só, usa uma letra', () => {
    expect(iniciaisDoNome('Ana')).toBe('A');
  });

  it('ignora espaços extras e maiúsculas com acento', () => {
    expect(iniciaisDoNome('  josé   da silva ')).toBe('JS');
    expect(iniciaisDoNome('Érica Ávila')).toBe('ÉÁ');
  });

  it('nome vazio devolve texto vazio', () => {
    expect(iniciaisDoNome('   ')).toBe('');
  });
});
