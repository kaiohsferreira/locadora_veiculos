import { describe, expect, it } from 'vitest';
import { converterSaida, criarEsquemaSaida, type CamposSaida } from './validacaoSaida';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';

const campos: CamposSaida = { kmRegistrado: '120', nivelCombustivel: '0,50', observacoes: '  ok  ', idsAvariasEntrada: [7], novasAvarias: [{ descricao: '  Risco  ', gravidade: 'LEVE', valorEstimado: '12,34' }], comparacaoConferida: true };
const esquema = criarEsquemaSaida(120, [7, 8]);
describe('validação da saída RF-22', () => {
  it('normaliza dados e envia somente campos contratuais', () => {
    const convertido = converterSaida(esquema.parse(campos));
    expect(convertido).toEqual({ kmRegistrado: 120, nivelCombustivel: 0.5, observacoes: 'ok', idsAvariasEntrada: [7], novasAvarias: [{ descricao: 'Risco', gravidade: 'LEVE', valorEstimado: 12.34 }] });
    expect(convertido).not.toHaveProperty('comparacaoConferida');
  });
  it.each(['119', '-1', '120.5', '', '2147483648'])('rejeita km %s', kmRegistrado => expect(esquema.safeParse({ ...campos, kmRegistrado }).success).toBe(false));
  it.each(['1,01', '-0,1', '0,123', '', '1e0'])('rejeita combustível %s', nivelCombustivel => expect(esquema.safeParse({ ...campos, nivelCombustivel }).success).toBe(false));
  it.each(['', 'a'.repeat(201), 'Risco\0'])('rejeita descrição inválida', descricao => expect(esquema.safeParse({ ...campos, novasAvarias: [{ ...campos.novasAvarias[0], descricao }] }).success).toBe(false));
  it.each(['-1', '100000000', '0.001', 'abc'])('rejeita valor %s', valorEstimado => expect(esquema.safeParse({ ...campos, novasAvarias: [{ ...campos.novasAvarias[0], valorEstimado }] }).success).toBe(false));
  it('aceita zero novas avarias e extremos de decimal', () => {
    expect(esquema.safeParse({ ...campos, novasAvarias: [], nivelCombustivel: '0' }).success).toBe(true);
    expect(esquema.safeParse({ ...campos, nivelCombustivel: '1', novasAvarias: [{ descricao: 'x', gravidade: 'GRAVE', valorEstimado: '99999999,99' }] }).success).toBe(true);
  });
  it('exige confirmação e IDs únicos da entrada', () => {
    expect(esquema.safeParse({ ...campos, comparacaoConferida: false }).success).toBe(false);
    for (const idsAvariasEntrada of [[7, 7], [9], [0]]) expect(esquema.safeParse({ ...campos, idsAvariasEntrada }).success).toBe(false);
  });
  it('observações com nulo falham', () => {
    expect(esquema.safeParse({ ...campos, observacoes: '\0' }).success).toBe(false);
  });
  it('exige o valor estimado de cada nova avaria e não coage o vazio para zero', () => {
    const resultado = esquema.safeParse({ ...campos, novasAvarias: [{ ...campos.novasAvarias[0], valorEstimado: '' }] });
    expect(resultado.success).toBe(false);
    expect(resultado.error?.issues[0]?.message).toBe('Informe o valor estimado de cada nova avaria (use 0,00 quando não houver custo).');
  });
  it.each(['0', '0,00', '12,34', '99999999,99'])('aceita o valor estimado %s', valorEstimado => expect(esquema.safeParse({ ...campos, novasAvarias: [{ ...campos.novasAvarias[0], valorEstimado }] }).success).toBe(true));
  it('valor 0,00 chega ao envio como zero, não como nulo', () => {
    expect(converterSaida(esquema.parse({ ...campos, novasAvarias: [{ ...campos.novasAvarias[0], valorEstimado: '0,00' }] })).novasAvarias[0].valorEstimado).toBe(0);
  });
  it('valor negativo ou fora da faixa usa a mensagem de faixa', () => {
    const resultado = esquema.safeParse({ ...campos, novasAvarias: [{ ...campos.novasAvarias[0], valorEstimado: '-1' }] });
    expect(resultado.error?.issues[0]?.message).toBe(M.valorFaixa);
  });
});
