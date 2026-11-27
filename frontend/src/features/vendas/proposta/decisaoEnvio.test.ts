import { describe, expect, it } from 'vitest';
import { ErroApi } from '@/types/api';
import { decidirAntesDoEnvio, decidirDepoisDoEnvio } from './decisaoEnvio';

describe('decisão do envio da proposta', () => {
  it('sem login: vai para o login', () => {
    expect(decidirAntesDoEnvio(false, false)).toBe('irParaLogin');
  });

  it('com login de quem não é cliente: avisa que só cliente envia', () => {
    expect(decidirAntesDoEnvio(true, false)).toBe('avisarSoCliente');
  });

  it('com login de cliente: envia', () => {
    expect(decidirAntesDoEnvio(true, true)).toBe('enviar');
  });

  it('401 no envio (a sessão acabou): vai para o login', () => {
    expect(decidirDepoisDoEnvio(new ErroApi({ status: 401, mensagem: 'Sessão expirada.' }))).toBe('irParaLogin');
  });

  it('sucesso: mostra a confirmação', () => {
    expect(decidirDepoisDoEnvio(null)).toBe('mostrarConfirmacao');
  });

  it('outros erros (409 do cadastro, 404, 400, rede): mostra o erro na tela', () => {
    expect(decidirDepoisDoEnvio(new ErroApi({ status: 409, mensagem: 'Cadastro em análise.' }))).toBe('mostrarErro');
    expect(decidirDepoisDoEnvio(new ErroApi({ status: 404, mensagem: 'x' }))).toBe('mostrarErro');
    expect(decidirDepoisDoEnvio(new Error('falha de rede'))).toBe('mostrarErro');
  });
});
