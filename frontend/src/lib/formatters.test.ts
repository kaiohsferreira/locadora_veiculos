import { describe, expect, it } from 'vitest';
import { formatarCep, formatarCpf, formatarTelefone, limitarDigitos } from '@/lib/formatters';

describe('formatarCpf', () => {
  it('formata valor parcial enquanto o usuário digita', () => {
    expect(formatarCpf('')).toBe('');
    expect(formatarCpf('123')).toBe('123');
    expect(formatarCpf('1234')).toBe('123.4');
    expect(formatarCpf('123456')).toBe('123.456');
    expect(formatarCpf('1234567')).toBe('123.456.7');
  });

  it('formata CPF completo, com ou sem máscara de entrada', () => {
    expect(formatarCpf('12345678909')).toBe('123.456.789-09');
    expect(formatarCpf('123.456.789-09')).toBe('123.456.789-09');
  });

  it('corta em 11 dígitos quando recebe mais', () => {
    expect(formatarCpf('1234567890123')).toBe('123.456.789-01');
  });
});

describe('formatarTelefone', () => {
  it('formata valor parcial com DDD', () => {
    expect(formatarTelefone('')).toBe('');
    expect(formatarTelefone('1')).toBe('(1');
    expect(formatarTelefone('11')).toBe('(11');
    expect(formatarTelefone('113')).toBe('(11) 3');
    expect(formatarTelefone('1134567')).toBe('(11) 3456-7');
  });

  it('formata fixo e celular completos', () => {
    expect(formatarTelefone('1134567890')).toBe('(11) 3456-7890');
    expect(formatarTelefone('11987654321')).toBe('(11) 98765-4321');
  });

  it('corta em 11 dígitos quando recebe mais', () => {
    expect(formatarTelefone('119876543219')).toBe('(11) 98765-4321');
  });
});

describe('formatarCep', () => {
  it('formata valor parcial sem o hífen até o quinto dígito', () => {
    expect(formatarCep('')).toBe('');
    expect(formatarCep('0131')).toBe('0131');
    expect(formatarCep('01310')).toBe('01310');
    expect(formatarCep('013101')).toBe('01310-1');
  });

  it('formata CEP completo e corta em 8 dígitos', () => {
    expect(formatarCep('01310100')).toBe('01310-100');
    expect(formatarCep('0131010012')).toBe('01310-100');
  });
});

describe('limitarDigitos', () => {
  it('remove tudo que não é dígito, mantendo a máscara fora do valor', () => {
    expect(limitarDigitos('123.456.789-09', 11)).toBe('12345678909');
  });

  it('corta no máximo informado', () => {
    expect(limitarDigitos('1234567890123', 11)).toBe('12345678901');
  });

  it('devolve valor menor que o máximo sem alterar', () => {
    expect(limitarDigitos('12a3', 11)).toBe('123');
  });

  it('devolve vazio para texto sem dígitos ou máximo zero e negativo', () => {
    expect(limitarDigitos('abc', 5)).toBe('');
    expect(limitarDigitos('12345', 0)).toBe('');
    expect(limitarDigitos('12345', -3)).toBe('');
  });
});
