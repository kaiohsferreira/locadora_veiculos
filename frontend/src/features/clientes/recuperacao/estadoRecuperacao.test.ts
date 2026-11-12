import { describe, expect, it } from 'vitest';
import { lerEstadoRecuperacao, montarEstadoRecuperacao } from './estadoRecuperacao';

describe('montarEstadoRecuperacao', () => {
  it('leva perfil e caminho de retorno', () => {
    expect(montarEstadoRecuperacao('FUNCIONARIO', '/back-office/frota')).toEqual({
      perfil: 'FUNCIONARIO',
      de: '/back-office/frota',
    });
  });

  it('sem caminho de retorno (vazio), leva só o perfil', () => {
    expect(montarEstadoRecuperacao('CLIENTE', '')).toEqual({ perfil: 'CLIENTE' });
  });
});

describe('lerEstadoRecuperacao', () => {
  it('lê o que foi montado (ida e volta)', () => {
    const ida = montarEstadoRecuperacao('FUNCIONARIO', '/back-office/frota');
    expect(lerEstadoRecuperacao(ida)).toEqual({ perfil: 'FUNCIONARIO', de: '/back-office/frota' });
  });

  it('ignora campos desconhecidos (CPF, senha, token, qualquer outro)', () => {
    const estado = { perfil: 'CLIENTE', de: '/veiculos', cpf: '52998224725', senha: 'x', token: 't' };
    expect(lerEstadoRecuperacao(estado)).toEqual({ perfil: 'CLIENTE', de: '/veiculos' });
  });

  it('perfil fora dos valores da API vira ausente', () => {
    expect(lerEstadoRecuperacao({ perfil: 'ADMIN' })).toEqual({});
    expect(lerEstadoRecuperacao({ perfil: 'cliente' })).toEqual({});
    expect(lerEstadoRecuperacao({ perfil: 42 })).toEqual({});
  });

  it('de que não é texto, ou vazio, vira ausente', () => {
    expect(lerEstadoRecuperacao({ de: 42 })).toEqual({});
    expect(lerEstadoRecuperacao({ de: '' })).toEqual({});
    expect(lerEstadoRecuperacao({ de: { caminho: '/x' } })).toEqual({});
  });

  it('estado ausente, nulo ou de outro tipo vira objeto vazio', () => {
    expect(lerEstadoRecuperacao(undefined)).toEqual({});
    expect(lerEstadoRecuperacao(null)).toEqual({});
    expect(lerEstadoRecuperacao('texto')).toEqual({});
  });
});
