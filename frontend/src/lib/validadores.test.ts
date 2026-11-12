import { describe, expect, it } from 'vitest';
import {
  calcularIdade,
  ehCpfValido,
  ehDataIsoFutura,
  ehDataIsoPassada,
  ehDataIsoValida,
} from '@/lib/validadores';

// Referência fixa: 12/11/2026 (hora local).
const HOJE = new Date(2026, 10, 12, 10, 0);

describe('ehCpfValido', () => {
  it('aceita CPF válido com e sem máscara', () => {
    expect(ehCpfValido('529.982.247-25')).toBe(true);
    expect(ehCpfValido('52998224725')).toBe(true);
    expect(ehCpfValido('123.456.789-09')).toBe(true);
  });

  it('rejeita dígito verificador errado', () => {
    expect(ehCpfValido('529.982.247-26')).toBe(false);
    expect(ehCpfValido('123.456.789-10')).toBe(false);
  });

  it('rejeita sequências com todos os dígitos iguais, mesmo que passem no cálculo', () => {
    expect(ehCpfValido('111.111.111-11')).toBe(false);
    expect(ehCpfValido('00000000000')).toBe(false);
  });

  it('rejeita tamanho diferente de 11 dígitos', () => {
    expect(ehCpfValido('')).toBe(false);
    expect(ehCpfValido('123')).toBe(false);
    expect(ehCpfValido('529982247250')).toBe(false);
  });
});

describe('ehDataIsoValida', () => {
  it('aceita datas que existem no calendário', () => {
    expect(ehDataIsoValida('2026-11-12')).toBe(true);
    expect(ehDataIsoValida('2024-02-29')).toBe(true);
  });

  it('rejeita datas inexistentes ou em formato diferente de AAAA-MM-DD', () => {
    expect(ehDataIsoValida('2025-02-29')).toBe(false);
    expect(ehDataIsoValida('2026-13-01')).toBe(false);
    expect(ehDataIsoValida('2026-00-10')).toBe(false);
    expect(ehDataIsoValida('2026-1-01')).toBe(false);
    expect(ehDataIsoValida('12/11/2026')).toBe(false);
    expect(ehDataIsoValida('')).toBe(false);
  });
});

describe('calcularIdade', () => {
  it('conta anos completos: aniversário no dia já é idade nova', () => {
    expect(calcularIdade('2000-11-12', HOJE)).toBe(26);
  });

  it('não soma o ano enquanto o aniversário não chegou', () => {
    expect(calcularIdade('2000-11-13', HOJE)).toBe(25);
    expect(calcularIdade('2000-12-01', HOJE)).toBe(25);
  });

  it('aceita nascido no mesmo dia do ano anterior e nascido hoje', () => {
    expect(calcularIdade('1990-01-01', HOJE)).toBe(36);
    expect(calcularIdade('2026-11-12', HOJE)).toBe(0);
  });

  it('devolve null para data inválida ou futura', () => {
    expect(calcularIdade('2025-02-29', HOJE)).toBeNull();
    expect(calcularIdade('abc', HOJE)).toBeNull();
    expect(calcularIdade('2026-11-13', HOJE)).toBeNull();
  });
});

describe('ehDataIsoFutura e ehDataIsoPassada', () => {
  it('hoje não é futura nem passada, mesmo com hora avançada', () => {
    const fimDoDia = new Date(2026, 10, 12, 23, 59);
    expect(ehDataIsoFutura('2026-11-12', fimDoDia)).toBe(false);
    expect(ehDataIsoPassada('2026-11-12', fimDoDia)).toBe(false);
  });

  it('identifica datas futuras e passadas pela data, não pela hora', () => {
    expect(ehDataIsoFutura('2026-11-13', HOJE)).toBe(true);
    expect(ehDataIsoPassada('2026-11-11', HOJE)).toBe(true);
    expect(ehDataIsoFutura('2026-11-11', HOJE)).toBe(false);
    expect(ehDataIsoPassada('2026-11-13', HOJE)).toBe(false);
  });

  it('data inválida não é futura nem passada', () => {
    expect(ehDataIsoFutura('2025-02-29', HOJE)).toBe(false);
    expect(ehDataIsoPassada('2025-02-29', HOJE)).toBe(false);
  });
});
