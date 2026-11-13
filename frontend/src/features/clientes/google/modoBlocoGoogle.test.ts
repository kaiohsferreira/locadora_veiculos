import { describe, expect, it } from 'vitest';
import type { ReturnGoogleConfiguracaoVO } from '@/types/autenticacao';
import { obterModoBlocoGoogle } from './modoBlocoGoogle';

describe('obterModoBlocoGoogle', () => {
  it('oculto enquanto a configuração não chegou', () => {
    expect(obterModoBlocoGoogle(undefined, true)).toEqual({ tipo: 'oculto' });
  });

  it('oculto quando habilitado é falso (RN3.1), mesmo com modo de teste', () => {
    const config: ReturnGoogleConfiguracaoVO = { habilitado: false, clientId: null, modoTeste: true };
    expect(obterModoBlocoGoogle(config, true)).toEqual({ tipo: 'oculto' });
  });

  it('teste quando modoTeste é verdadeiro em desenvolvimento (sem script externo)', () => {
    const config: ReturnGoogleConfiguracaoVO = { habilitado: true, clientId: null, modoTeste: true };
    expect(obterModoBlocoGoogle(config, true)).toEqual({ tipo: 'teste' });
  });

  it('modoTeste verdadeiro em produção é tratado como Google indisponível (bloco oculto)', () => {
    const config: ReturnGoogleConfiguracaoVO = { habilitado: true, clientId: null, modoTeste: true };
    expect(obterModoBlocoGoogle(config, false)).toEqual({ tipo: 'oculto' });
  });

  it('real com o Client ID quando habilitado e modoTeste falso', () => {
    const config: ReturnGoogleConfiguracaoVO = { habilitado: true, clientId: 'id-publico', modoTeste: false };
    expect(obterModoBlocoGoogle(config, false)).toEqual({ tipo: 'real', clientId: 'id-publico' });
    expect(obterModoBlocoGoogle(config, true)).toEqual({ tipo: 'real', clientId: 'id-publico' });
  });

  it('oculto no modo real sem Client ID (nulo, ausente ou em branco)', () => {
    expect(obterModoBlocoGoogle({ habilitado: true, clientId: null, modoTeste: false }, false)).toEqual({
      tipo: 'oculto',
    });
    expect(obterModoBlocoGoogle({ habilitado: true, modoTeste: false }, false)).toEqual({ tipo: 'oculto' });
    expect(obterModoBlocoGoogle({ habilitado: true, clientId: '  ', modoTeste: false }, false)).toEqual({
      tipo: 'oculto',
    });
  });
});
