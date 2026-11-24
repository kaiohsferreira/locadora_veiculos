import { describe, expect, it } from 'vitest';
import { MENSAGENS_RETIRADA as M, mensagemAcaoLinhaRetirada } from './mensagensRetirada';

describe('mensagemAcaoLinhaRetirada (nome da ação da linha para o leitor de tela)', () => {
  it('junta o texto visível do botão e o código da reserva da linha', () => {
    expect(mensagemAcaoLinhaRetirada(M.entradaAcaoReserva, 'AV-00042')).toBe('Iniciar retirada AV-00042');
    expect(mensagemAcaoLinhaRetirada(M.continuarVistoria, 'AV-00046')).toBe('Continuar para vistoria AV-00046');
  });

  it('linha de locação sem código usa "Locação nº {id}"', () => {
    expect(mensagemAcaoLinhaRetirada(M.continuarVistoria, M.locacaoNumero(931))).toBe('Continuar para vistoria Locação nº 931');
  });

  it('sem referência (vazia ou só com espaços) não há nome: o link fica com o texto visível', () => {
    expect(mensagemAcaoLinhaRetirada(M.entradaAcaoReserva, '')).toBeUndefined();
    expect(mensagemAcaoLinhaRetirada(M.continuarVistoria, '   ')).toBeUndefined();
  });

  it('referência com espaços nas pontas não deixa espaço sobrando no nome', () => {
    expect(mensagemAcaoLinhaRetirada(M.entradaAcaoReserva, ' AV-00042 ')).toBe('Iniciar retirada AV-00042');
  });
});

describe('botão de volta dos estados de erro da tela 16', () => {
  it('usa o texto aprovado pelo Kaio em 24/11/2026', () => {
    expect(M.voltarRetiradas).toBe('Voltar às retiradas');
  });
});
