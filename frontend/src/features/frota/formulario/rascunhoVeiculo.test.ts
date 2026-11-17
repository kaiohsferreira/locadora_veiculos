import { describe, expect, it } from 'vitest';
import { valoresIniciaisCadastro } from '@/lib/conversaoVeiculo';
import type { ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import {
  VERSAO_RASCUNHO_VEICULO,
  apagarRascunhoVeiculo,
  chaveRascunhoVeiculo,
  guardarRascunhoVeiculo,
  lerRascunhoArmazenado,
  lerRascunhoVeiculo,
  serializarRascunhoVeiculo,
  type ArmazenamentoRascunho,
} from './rascunhoVeiculo';

const PREENCHIDO: ValoresFormularioVeiculo = {
  ...valoresIniciaisCadastro(),
  placa: 'BRA2E19',
  renavam: '00984521360',
  valorDiaria: '229,00',
};

/** Armazenamento em memória com a mesma forma do localStorage. */
function armazenamentoFalso(): ArmazenamentoRascunho & { guardado: Map<string, string> } {
  const guardado = new Map<string, string>();
  return {
    guardado,
    getItem: (chave) => guardado.get(chave) ?? null,
    setItem: (chave, valor) => {
      guardado.set(chave, valor);
    },
    removeItem: (chave) => {
      guardado.delete(chave);
    },
  };
}

describe('chaveRascunhoVeiculo', () => {
  it('separa por usuário e por locadora', () => {
    const chave = chaveRascunhoVeiculo({ idUsuario: 3, idEmpresa: 9 });
    expect(chave).toContain('usuario-3');
    expect(chave).toContain('locadora-9');
    expect(chave).not.toBe(chaveRascunhoVeiculo({ idUsuario: 4, idEmpresa: 9 }));
    expect(chave).not.toBe(chaveRascunhoVeiculo({ idUsuario: 3, idEmpresa: 10 }));
  });

  it('sem usuário ou sem locadora na sessão, não há rascunho', () => {
    expect(chaveRascunhoVeiculo({ idUsuario: undefined, idEmpresa: 9 })).toBeNull();
    expect(chaveRascunhoVeiculo({ idUsuario: 3, idEmpresa: undefined })).toBeNull();
  });
});

describe('serializar e ler o rascunho', () => {
  it('volta exatamente os valores gravados', () => {
    expect(lerRascunhoVeiculo(serializarRascunhoVeiculo(PREENCHIDO))).toEqual(PREENCHIDO);
  });

  it('grava a versão do formato', () => {
    const gravado = JSON.parse(serializarRascunhoVeiculo(PREENCHIDO)) as { versao: number };
    expect(gravado.versao).toBe(VERSAO_RASCUNHO_VEICULO);
  });

  it('ausente, JSON inválido ou versão antiga: nulo', () => {
    expect(lerRascunhoVeiculo(null)).toBeNull();
    expect(lerRascunhoVeiculo('{não é json')).toBeNull();
    expect(lerRascunhoVeiculo(JSON.stringify({ versao: 0, valores: PREENCHIDO }))).toBeNull();
  });

  it('dado que não tem a forma certa: nulo (nunca preenche com dado parcial)', () => {
    expect(lerRascunhoVeiculo(JSON.stringify([1, 2]))).toBeNull();
    expect(lerRascunhoVeiculo(JSON.stringify({ versao: 1 }))).toBeNull();
    expect(lerRascunhoVeiculo(JSON.stringify({ versao: 1, valores: { placa: 'AAA' } }))).toBeNull();
    expect(
      lerRascunhoVeiculo(JSON.stringify({ versao: 1, valores: { ...PREENCHIDO, kmAtual: 12 } })),
    ).toBeNull();
  });
});

describe('guardar, ler e apagar com armazenamento', () => {
  it('guarda e lê o rascunho da chave', () => {
    const armazenamento = armazenamentoFalso();
    const chave = chaveRascunhoVeiculo({ idUsuario: 1, idEmpresa: 2 });
    expect(guardarRascunhoVeiculo(armazenamento, chave, PREENCHIDO)).toBe(true);
    expect(lerRascunhoArmazenado(armazenamento, chave)).toEqual(PREENCHIDO);
  });

  it('a chave de outra sessão não lê o rascunho', () => {
    const armazenamento = armazenamentoFalso();
    guardarRascunhoVeiculo(armazenamento, chaveRascunhoVeiculo({ idUsuario: 1, idEmpresa: 2 }), PREENCHIDO);
    expect(lerRascunhoArmazenado(armazenamento, chaveRascunhoVeiculo({ idUsuario: 5, idEmpresa: 2 }))).toBeNull();
  });

  it('apagar tira o rascunho', () => {
    const armazenamento = armazenamentoFalso();
    const chave = chaveRascunhoVeiculo({ idUsuario: 1, idEmpresa: 2 });
    guardarRascunhoVeiculo(armazenamento, chave, PREENCHIDO);
    apagarRascunhoVeiculo(armazenamento, chave);
    expect(lerRascunhoArmazenado(armazenamento, chave)).toBeNull();
  });

  it('sem armazenamento ou sem chave: não grava, não lê, não apaga e não lança', () => {
    expect(guardarRascunhoVeiculo(null, 'x', PREENCHIDO)).toBe(false);
    expect(guardarRascunhoVeiculo(armazenamentoFalso(), null, PREENCHIDO)).toBe(false);
    expect(lerRascunhoArmazenado(null, 'x')).toBeNull();
    expect(() => apagarRascunhoVeiculo(null, 'x')).not.toThrow();
  });

  it('armazenamento que falha (cota cheia ou bloqueado): gravar devolve falso, ler devolve nulo, apagar não lança', () => {
    const quebrado: ArmazenamentoRascunho = {
      getItem: () => {
        throw new Error('bloqueado');
      },
      setItem: () => {
        throw new Error('cota cheia');
      },
      removeItem: () => {
        throw new Error('bloqueado');
      },
    };
    expect(guardarRascunhoVeiculo(quebrado, 'x', PREENCHIDO)).toBe(false);
    expect(lerRascunhoArmazenado(quebrado, 'x')).toBeNull();
    expect(() => apagarRascunhoVeiculo(quebrado, 'x')).not.toThrow();
  });
});
