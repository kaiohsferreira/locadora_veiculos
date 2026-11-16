import { describe, expect, it } from 'vitest';
import { calcularDiarias, podeCancelarReserva, valorPorDiaria } from '@/features/area-cliente/reservas/calculosReserva';

describe('calcularDiarias', () => {
  it('14 a 18 do mês dá 4 diárias', () => {
    expect(calcularDiarias('2026-11-14', '2026-11-18')).toBe(4);
  });

  it('conta certo na virada de mês e de ano', () => {
    expect(calcularDiarias('2026-10-30', '2026-11-02')).toBe(3);
    expect(calcularDiarias('2026-12-30', '2027-01-03')).toBe(4);
  });

  it('conta certo em ano bissexto', () => {
    expect(calcularDiarias('2028-02-28', '2028-03-01')).toBe(2);
  });

  it('mesma data ou data invertida dão 0', () => {
    expect(calcularDiarias('2026-11-14', '2026-11-14')).toBe(0);
    expect(calcularDiarias('2026-11-18', '2026-11-14')).toBe(0);
  });

  it('texto inválido ou data que não existe dá 0', () => {
    expect(calcularDiarias('14/11/2026', '18/11/2026')).toBe(0);
    expect(calcularDiarias('2026-02-30', '2026-03-02')).toBe(0);
    expect(calcularDiarias('', '2026-11-18')).toBe(0);
  });
});

describe('valorPorDiaria', () => {
  it('divide o valor estimado pelas diárias, com centavos', () => {
    expect(valorPorDiaria(1146.16, 4)).toBe(286.54);
  });

  it('valor nulo ou zero diárias devolve null', () => {
    expect(valorPorDiaria(null, 4)).toBeNull();
    expect(valorPorDiaria(1146.16, 0)).toBeNull();
  });
});

describe('podeCancelarReserva', () => {
  it('ATIVA e CONFIRMADA podem ser canceladas; CANCELADA e CONCLUIDA não', () => {
    expect(podeCancelarReserva('ATIVA')).toBe(true);
    expect(podeCancelarReserva('CONFIRMADA')).toBe(true);
    expect(podeCancelarReserva('CANCELADA')).toBe(false);
    expect(podeCancelarReserva('CONCLUIDA')).toBe(false);
  });
});
