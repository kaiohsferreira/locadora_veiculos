import { describe, expect, it, vi } from 'vitest';
import { MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import { aplicarErrosDeCampo, obterErroDeCampo, obterMensagemErro } from '@/lib/erros';
import { ErroApi } from '@/types/api';

const ERRO_COM_CAMPOS = new ErroApi({
  status: 400,
  mensagem: 'Alguns dados estão inválidos. Confira os campos destacados.',
  erros: {
    Cpf: ['O CPF é inválido.', 'Outra mensagem do CPF.'],
    senha: ['Informe a senha.'],
  },
});

const CAMPOS_FORMULARIO = ['cpf', 'senha', 'perfil'] as const;

describe('obterErroDeCampo', () => {
  it('devolve a primeira mensagem do campo', () => {
    expect(obterErroDeCampo(ERRO_COM_CAMPOS, 'cpf')).toBe('O CPF é inválido.');
  });

  it('casa o nome do campo sem diferenciar maiúsculas de minúsculas', () => {
    expect(obterErroDeCampo(ERRO_COM_CAMPOS, 'CPF')).toBe('O CPF é inválido.');
    expect(obterErroDeCampo(ERRO_COM_CAMPOS, 'Senha')).toBe('Informe a senha.');
  });

  it('devolve undefined para campo sem erro, erro sem campos ou erro que não é ErroApi', () => {
    expect(obterErroDeCampo(ERRO_COM_CAMPOS, 'perfil')).toBeUndefined();
    expect(obterErroDeCampo(new ErroApi({ status: 500, mensagem: 'x' }), 'cpf')).toBeUndefined();
    expect(obterErroDeCampo(new Error('falha'), 'cpf')).toBeUndefined();
    expect(obterErroDeCampo(null, 'cpf')).toBeUndefined();
  });

  it('ignora campo cuja lista de mensagens está vazia', () => {
    const erro = new ErroApi({ status: 400, mensagem: 'x', erros: { cpf: [] } });
    expect(obterErroDeCampo(erro, 'cpf')).toBeUndefined();
  });
});

describe('aplicarErrosDeCampo', () => {
  it('chama definir para cada campo conhecido que veio com erro e devolve true', () => {
    const definir = vi.fn<(campo: (typeof CAMPOS_FORMULARIO)[number], mensagem: string) => void>();

    const aplicou = aplicarErrosDeCampo(ERRO_COM_CAMPOS, CAMPOS_FORMULARIO, definir);

    expect(aplicou).toBe(true);
    expect(definir).toHaveBeenCalledTimes(2);
    expect(definir).toHaveBeenNthCalledWith(1, 'cpf', 'O CPF é inválido.');
    expect(definir).toHaveBeenNthCalledWith(2, 'senha', 'Informe a senha.');
  });

  it('não chama definir e devolve false quando nenhum campo conhecido tem erro', () => {
    const definir = vi.fn<(campo: string, mensagem: string) => void>();
    const erro = new ErroApi({ status: 401, mensagem: 'x' });

    expect(aplicarErrosDeCampo(erro, CAMPOS_FORMULARIO, definir)).toBe(false);
    expect(definir).not.toHaveBeenCalled();
  });
});

describe('obterMensagemErro', () => {
  it('devolve a mensagem do ErroApi e a padrão para qualquer outro erro', () => {
    expect(obterMensagemErro(ERRO_COM_CAMPOS)).toBe(ERRO_COM_CAMPOS.mensagem);
    expect(obterMensagemErro(new TypeError('texto técnico'))).toBe(MENSAGEM_ERRO_API_PADRAO);
  });
});
