import { describe, expect, it } from 'vitest';
import { MENSAGEM_ACESSO_NEGADO, MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import { mensagemErroTrocaStatus } from './mapeamentoErroTrocaStatus';

describe('mensagemErroTrocaStatus', () => {
  it('409: mostra a frase do servidor (reserva, locação ou status só por fluxo)', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'O veículo tem reserva em aberto. O status só pode ser alterado depois que a reserva for cancelada ou concluída.' });

    expect(mensagemErroTrocaStatus(erro)).toBe(erro.mensagem);
  });

  it('404: mostra a frase do servidor (veículo inexistente, desativado ou de outra locadora)', () => {
    const erro = new ErroApi({ status: 404, mensagem: 'O veículo informado não foi encontrado.' });

    expect(mensagemErroTrocaStatus(erro)).toBe('O veículo informado não foi encontrado.');
  });

  it('403: mostra o texto padrão de acesso negado, não a frase do servidor', () => {
    const erro = new ErroApi({ status: 403, mensagem: 'Acesso negado pelo servidor.' });

    expect(mensagemErroTrocaStatus(erro)).toBe(MENSAGEM_ACESSO_NEGADO);
  });

  it('500 e demais: a mensagem pronta do cliente HTTP', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.' });

    expect(mensagemErroTrocaStatus(erro)).toBe(erro.mensagem);
  });

  it('erro que não é da API: mensagem padrão, nunca a técnica', () => {
    expect(mensagemErroTrocaStatus(new Error('TypeError: x is undefined'))).toBe(MENSAGEM_ERRO_API_PADRAO);
  });
});
