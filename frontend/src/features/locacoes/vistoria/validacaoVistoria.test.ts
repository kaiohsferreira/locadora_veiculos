import { describe, expect, it } from 'vitest';
import { converterVistoriaEntrada, esquemaVistoriaEntrada, lerIdLocacao } from './validacaoVistoria';

describe('vistoria de entrada: validação e conversão', () => {
  const valido = { kmRegistrado: '12345', nivelCombustivel: '0,75', observacoes: '' };

  it.each(['', ' ', '-1', '1.5', '1,5', '1e3', 'Infinity', '2147483648'])('rejeita quilometragem inválida %s', (kmRegistrado) => {
    expect(esquemaVistoriaEntrada.safeParse({ ...valido, kmRegistrado }).success).toBe(false);
  });

  it.each(['', ' ', '-0.1', '1,01', '2', '0,001', '0.999', 'NaN', '1e-2', '.5'])('rejeita combustível inválido %s', (nivelCombustivel) => {
    expect(esquemaVistoriaEntrada.safeParse({ ...valido, nivelCombustivel }).success).toBe(false);
  });

  it.each(['0', '0,00', '0.5', '0,75', '1', '1,00'])('aceita combustível %s', (nivelCombustivel) => {
    expect(esquemaVistoriaEntrada.safeParse({ ...valido, nivelCombustivel }).success).toBe(true);
  });

  it('preserva zero e converte vírgula, trim e vazio para null sem enviar campos de servidor', () => {
    const campos = esquemaVistoriaEntrada.parse({ kmRegistrado: ' 0 ', nivelCombustivel: ' 0,75 ', observacoes: '  ' });
    expect(converterVistoriaEntrada(campos)).toEqual({ kmRegistrado: 0, nivelCombustivel: 0.75, observacoes: null });
  });

  it('apara observações e rejeita caractere nulo', () => {
    expect(converterVistoriaEntrada({ ...valido, observacoes: '  Pintura conferida.  ' }).observacoes).toBe('Pintura conferida.');
    expect(esquemaVistoriaEntrada.safeParse({ ...valido, observacoes: 'A\0B' }).success).toBe(false);
  });

  it.each([undefined, '', '0', '-1', '1.5', '1x', '2147483648', '9007199254740993'])('recusa id de locação inválido %s', (id) => {
    expect(lerIdLocacao(id)).toBeNull();
  });

  it('aceita um id de locação positivo', () => expect(lerIdLocacao('42')).toBe(42));
});
