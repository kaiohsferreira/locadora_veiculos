import { beforeEach, describe, expect, it } from 'vitest';
import {
  guardarPreenchimentoGoogle,
  limparPreenchimentoGoogle,
  obterPreenchimentoGoogle,
  type PreenchimentoGoogle,
} from './preenchimentoGoogle';

const DADOS: PreenchimentoGoogle = { nome: 'Bruno Lima', email: 'bruno@exemplo.com' };

describe('preenchimentoGoogle (memória)', () => {
  beforeEach(() => {
    limparPreenchimentoGoogle();
  });

  it('começa sem pré-preenchimento', () => {
    expect(obterPreenchimentoGoogle()).toBeNull();
  });

  it('guarda e devolve nome e e-mail', () => {
    guardarPreenchimentoGoogle(DADOS);
    expect(obterPreenchimentoGoogle()).toEqual(DADOS);
  });

  it('devolve uma cópia: alterar o retorno não muda o dado guardado', () => {
    guardarPreenchimentoGoogle(DADOS);
    const obtido = obterPreenchimentoGoogle();
    if (obtido !== null) obtido.email = 'outro@exemplo.com';
    expect(obterPreenchimentoGoogle()?.email).toBe('bruno@exemplo.com');
  });

  it('não é afetado por mudança no objeto que foi guardado', () => {
    const dados: PreenchimentoGoogle = { ...DADOS };
    guardarPreenchimentoGoogle(dados);
    dados.nome = 'Outro';
    expect(obterPreenchimentoGoogle()?.nome).toBe('Bruno Lima');
  });

  it('substitui o anterior ao guardar de novo', () => {
    guardarPreenchimentoGoogle(DADOS);
    guardarPreenchimentoGoogle({ nome: null, email: 'ana@exemplo.com' });
    expect(obterPreenchimentoGoogle()).toEqual({ nome: null, email: 'ana@exemplo.com' });
  });

  it('limpa e limpar sem dado não dá erro', () => {
    guardarPreenchimentoGoogle(DADOS);
    limparPreenchimentoGoogle();
    expect(obterPreenchimentoGoogle()).toBeNull();
    expect(() => limparPreenchimentoGoogle()).not.toThrow();
  });
});
