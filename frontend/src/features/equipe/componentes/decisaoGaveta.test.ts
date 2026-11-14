import { describe, expect, it } from 'vitest';
import { decidirFechamentoNativo } from './decisaoGaveta';

describe('decidirFechamentoNativo: o navegador fechou a gaveta sozinho', () => {
  it('com envio em curso, a gaveta é reaberta (nada pode fechar durante a gravação)', () => {
    expect(decidirFechamentoNativo({ bloqueada: true, conectada: true })).toBe('reabrir');
  });

  it('sem envio, avisa quem controla o estado para fechar também (sem página travada)', () => {
    expect(decidirFechamentoNativo({ bloqueada: false, conectada: true })).toBe('avisar');
  });

  it('elemento que já saiu da página não faz nada, mesmo bloqueado', () => {
    expect(decidirFechamentoNativo({ bloqueada: true, conectada: false })).toBe('ignorar');
    expect(decidirFechamentoNativo({ bloqueada: false, conectada: false })).toBe('ignorar');
  });
});
