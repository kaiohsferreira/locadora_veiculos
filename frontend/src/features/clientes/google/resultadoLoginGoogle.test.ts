import { describe, expect, it } from 'vitest';
import type { RespostaLogin, ReturnGoogleLoginVO } from '@/types/autenticacao';
import { converterResultadoLoginGoogle, ehRespostaLoginValida } from './resultadoLoginGoogle';

const SESSAO: RespostaLogin = {
  token: 'token-de-teste',
  tipoToken: 'Bearer',
  expiraEm: '2026-11-12T20:00:00',
  expiraEmStr: '12/11/2026 20:00',
  perfil: 'CLIENTE',
  idUsuario: 7,
  nome: 'Ana Souza',
};

describe('converterResultadoLoginGoogle', () => {
  describe('AUTENTICADO', () => {
    it('devolve a sessão quando ela vem completa', () => {
      const resposta: ReturnGoogleLoginVO = { situacao: 'AUTENTICADO', sessao: SESSAO };
      expect(converterResultadoLoginGoogle(resposta)).toEqual({ situacao: 'AUTENTICADO', sessao: SESSAO });
    });

    it('é incoerente sem sessão (ausente ou nula)', () => {
      expect(converterResultadoLoginGoogle({ situacao: 'AUTENTICADO' })).toBeNull();
      expect(converterResultadoLoginGoogle({ situacao: 'AUTENTICADO', sessao: null })).toBeNull();
    });

    it('é incoerente com sessão sem token', () => {
      expect(
        converterResultadoLoginGoogle({ situacao: 'AUTENTICADO', sessao: { ...SESSAO, token: '  ' } }),
      ).toBeNull();
    });

    it('é incoerente com perfil desconhecido', () => {
      const sessao = { ...SESSAO, perfil: 'ADMIN' as RespostaLogin['perfil'] };
      expect(converterResultadoLoginGoogle({ situacao: 'AUTENTICADO', sessao })).toBeNull();
    });
  });

  describe('CONFIRMACAO_NECESSARIA', () => {
    it('devolve nome e e-mail quando vêm preenchidos', () => {
      expect(
        converterResultadoLoginGoogle({
          situacao: 'CONFIRMACAO_NECESSARIA',
          nome: 'Ana Souza',
          email: 'ana@exemplo.com',
        }),
      ).toEqual({ situacao: 'CONFIRMACAO_NECESSARIA', nome: 'Ana Souza', email: 'ana@exemplo.com' });
    });

    it('trata nome nulo igual a ausente (nome null no resultado)', () => {
      expect(
        converterResultadoLoginGoogle({ situacao: 'CONFIRMACAO_NECESSARIA', nome: null, email: 'ana@exemplo.com' }),
      ).toEqual({ situacao: 'CONFIRMACAO_NECESSARIA', nome: null, email: 'ana@exemplo.com' });
      expect(
        converterResultadoLoginGoogle({ situacao: 'CONFIRMACAO_NECESSARIA', email: 'ana@exemplo.com' }),
      ).toEqual({ situacao: 'CONFIRMACAO_NECESSARIA', nome: null, email: 'ana@exemplo.com' });
    });

    it('é incoerente sem e-mail (nulo ou ausente)', () => {
      expect(converterResultadoLoginGoogle({ situacao: 'CONFIRMACAO_NECESSARIA', nome: 'Ana' })).toBeNull();
      expect(
        converterResultadoLoginGoogle({ situacao: 'CONFIRMACAO_NECESSARIA', nome: 'Ana', email: null }),
      ).toBeNull();
    });

    it('é incoerente com sessão', () => {
      expect(
        converterResultadoLoginGoogle({
          situacao: 'CONFIRMACAO_NECESSARIA',
          sessao: SESSAO,
          email: 'ana@exemplo.com',
        }),
      ).toBeNull();
    });
  });

  describe('CADASTRO_NECESSARIO', () => {
    it('devolve nome e e-mail para pré-preencher o cadastro', () => {
      expect(
        converterResultadoLoginGoogle({
          situacao: 'CADASTRO_NECESSARIO',
          nome: 'Bruno Lima',
          email: 'bruno@exemplo.com',
        }),
      ).toEqual({ situacao: 'CADASTRO_NECESSARIO', nome: 'Bruno Lima', email: 'bruno@exemplo.com' });
    });

    it('é incoerente sem e-mail', () => {
      expect(converterResultadoLoginGoogle({ situacao: 'CADASTRO_NECESSARIO', nome: 'Bruno' })).toBeNull();
    });

    it('é incoerente com sessão', () => {
      expect(
        converterResultadoLoginGoogle({ situacao: 'CADASTRO_NECESSARIO', sessao: SESSAO, email: 'b@e.com' }),
      ).toBeNull();
    });
  });

  describe('CONTA_AMBIGUA', () => {
    it('devolve só a situação, sem nome nem e-mail', () => {
      expect(converterResultadoLoginGoogle({ situacao: 'CONTA_AMBIGUA' })).toEqual({ situacao: 'CONTA_AMBIGUA' });
      expect(
        converterResultadoLoginGoogle({ situacao: 'CONTA_AMBIGUA', nome: null, email: null, sessao: null }),
      ).toEqual({ situacao: 'CONTA_AMBIGUA' });
    });

    it('é incoerente com e-mail ou nome preenchidos', () => {
      expect(converterResultadoLoginGoogle({ situacao: 'CONTA_AMBIGUA', email: 'ana@exemplo.com' })).toBeNull();
      expect(converterResultadoLoginGoogle({ situacao: 'CONTA_AMBIGUA', nome: 'Ana' })).toBeNull();
    });

    it('é incoerente com sessão', () => {
      expect(converterResultadoLoginGoogle({ situacao: 'CONTA_AMBIGUA', sessao: SESSAO })).toBeNull();
    });
  });

  it('é nulo para situação desconhecida', () => {
    expect(converterResultadoLoginGoogle({ situacao: 'OUTRA' as ReturnGoogleLoginVO['situacao'] })).toBeNull();
  });
});

describe('ehRespostaLoginValida', () => {
  it('aceita a sessão completa', () => {
    expect(ehRespostaLoginValida(SESSAO)).toBe(true);
  });

  it('recusa valores que não são objeto, idUsuario não numérico e expiraEm ausente', () => {
    expect(ehRespostaLoginValida(null)).toBe(false);
    expect(ehRespostaLoginValida('texto')).toBe(false);
    expect(ehRespostaLoginValida({ ...SESSAO, idUsuario: 'sete' })).toBe(false);
    expect(ehRespostaLoginValida({ ...SESSAO, expiraEm: undefined })).toBe(false);
  });
});
