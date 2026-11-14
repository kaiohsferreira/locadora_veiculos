import { describe, expect, it } from 'vitest';
import { dataBrParaIso, isoParaDataBr, mascararDataBr } from './datasBr';

describe('mascararDataBr', () => {
  it('monta DD/MM/AAAA conforme a digitação e ignora o que não é dígito', () => {
    expect(mascararDataBr('2')).toBe('2');
    expect(mascararDataBr('23')).toBe('23');
    expect(mascararDataBr('230')).toBe('23/0');
    expect(mascararDataBr('2302')).toBe('23/02');
    expect(mascararDataBr('23022025')).toBe('23/02/2025');
    expect(mascararDataBr('23/02/2025')).toBe('23/02/2025');
  });

  it('não passa de 8 dígitos', () => {
    expect(mascararDataBr('230220251234')).toBe('23/02/2025');
  });
});

describe('dataBrParaIso', () => {
  it('converte DD/MM/AAAA real para AAAA-MM-DD', () => {
    expect(dataBrParaIso('23/02/2025')).toBe('2025-02-23');
    expect(dataBrParaIso('01/12/2024')).toBe('2024-12-01');
  });

  it('aceita 29 de fevereiro só em ano bissexto', () => {
    expect(dataBrParaIso('29/02/2024')).toBe('2024-02-29');
    expect(dataBrParaIso('29/02/2025')).toBeNull();
    expect(dataBrParaIso('29/02/2000')).toBe('2000-02-29');
    expect(dataBrParaIso('29/02/1900')).toBeNull();
  });

  it('recusa dias e meses que não existem', () => {
    expect(dataBrParaIso('31/04/2025')).toBeNull();
    expect(dataBrParaIso('31/12/2025')).toBe('2025-12-31');
    expect(dataBrParaIso('00/01/2025')).toBeNull();
    expect(dataBrParaIso('12/13/2025')).toBeNull();
  });

  it('recusa texto incompleto ou fora do formato', () => {
    expect(dataBrParaIso('')).toBeNull();
    expect(dataBrParaIso('23/02/25')).toBeNull();
    expect(dataBrParaIso('2025-02-23')).toBeNull();
    expect(dataBrParaIso('23/02/2025 ')).toBeNull();
  });
});

describe('isoParaDataBr', () => {
  it('converte AAAA-MM-DD para DD/MM/AAAA', () => {
    expect(isoParaDataBr('2025-02-23')).toBe('23/02/2025');
  });

  it('devolve texto vazio para o que não é data ISO válida', () => {
    expect(isoParaDataBr('')).toBe('');
    expect(isoParaDataBr('23/02/2025')).toBe('');
    expect(isoParaDataBr('2025-02-30')).toBe('');
  });

  it('é o inverso de dataBrParaIso para datas válidas', () => {
    expect(dataBrParaIso(isoParaDataBr('2024-02-29'))).toBe('2024-02-29');
  });
});
