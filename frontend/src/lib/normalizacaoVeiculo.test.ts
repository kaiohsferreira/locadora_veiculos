import { describe, expect, it } from 'vitest';
import {
  CARACTERE_NULO,
  normalizarAnoDigitado,
  normalizarChassiDigitado,
  normalizarKmDigitado,
  normalizarPlacaDigitada,
  normalizarPlacaParaServidor,
  normalizarRenavamDigitado,
  normalizarTextoDigitado,
} from '@/lib/normalizacaoVeiculo';

describe('normalizarPlacaDigitada', () => {
  it('põe em maiúsculas e tira hífen e espaço ("bra-2e19" vira "BRA2E19")', () => {
    expect(normalizarPlacaDigitada('bra-2e19')).toBe('BRA2E19');
    expect(normalizarPlacaDigitada('abc 1234')).toBe('ABC1234');
  });

  it('corta em 7 caracteres: o oitavo não entra', () => {
    expect(normalizarPlacaDigitada('ABC12345')).toBe('ABC1234');
  });
});

describe('normalizarPlacaParaServidor', () => {
  it('manda a placa sem hífen e em maiúsculas, como o servidor normaliza', () => {
    expect(normalizarPlacaParaServidor('abc-1234')).toBe('ABC1234');
    expect(normalizarPlacaParaServidor(' bra2e19 ')).toBe('BRA2E19');
  });
});

describe('normalizarChassiDigitado', () => {
  it('põe em maiúsculas, só letras e números, até 17; a letra O entra e a validação recusa', () => {
    expect(normalizarChassiDigitado('9bw-zzz377o t004251')).toBe('9BWZZZ377OT004251');
    expect(normalizarChassiDigitado('A'.repeat(20))).toHaveLength(17);
  });
});

describe('normalizarRenavamDigitado', () => {
  it('só dígitos, até 11, e os zeros à esquerda ficam', () => {
    expect(normalizarRenavamDigitado('00984521360')).toBe('00984521360');
    expect(normalizarRenavamDigitado('1a2b')).toBe('12');
    expect(normalizarRenavamDigitado('1'.repeat(13))).toHaveLength(11);
  });
});

describe('normalizarAnoDigitado', () => {
  it('só dígitos, até quatro', () => {
    expect(normalizarAnoDigitado('20a23')).toBe('2023');
    expect(normalizarAnoDigitado('202345')).toBe('2023');
  });
});

describe('normalizarKmDigitado', () => {
  it('só dígitos: ponto e sinal de menos não entram', () => {
    expect(normalizarKmDigitado('12.500')).toBe('12500');
    expect(normalizarKmDigitado('-5')).toBe('5');
  });
});

describe('normalizarTextoDigitado', () => {
  it('tira o caractere nulo, que o banco recusaria; os espaços ficam para o validador', () => {
    expect(normalizarTextoDigitado(`Fi${CARACTERE_NULO}at`)).toBe('Fiat');
    expect(normalizarTextoDigitado('  Argo  ')).toBe('  Argo  ');
  });
});
