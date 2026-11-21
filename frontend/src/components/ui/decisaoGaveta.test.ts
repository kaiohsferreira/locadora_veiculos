import { describe, expect, it } from 'vitest';
import { decidirFechamentoNativo } from './decisaoGaveta';

describe('decidirFechamentoNativo: o navegador fechou a gaveta sozinho', () => {
  it('com envio em curso, a gaveta é reaberta (nada pode fechar durante a gravação)', () => {
    expect(decidirFechamentoNativo({ bloqueada: true, conectada: true, aberta: false })).toBe('reabrir');
  });

  it('sem envio, avisa quem controla o estado para fechar também (sem página travada)', () => {
    expect(decidirFechamentoNativo({ bloqueada: false, conectada: true, aberta: false })).toBe('avisar');
  });

  it('elemento que já saiu da página não faz nada, mesmo bloqueado', () => {
    expect(decidirFechamentoNativo({ bloqueada: true, conectada: false, aberta: false })).toBe('ignorar');
    expect(decidirFechamentoNativo({ bloqueada: false, conectada: false, aberta: false })).toBe('ignorar');
  });
});

describe('decidirFechamentoNativo: evento "close" atrasado de um fechamento feito pelo próprio código', () => {
  it('diálogo aberto e conectado, sem envio: ignora (o close atrasado chega com a gaveta já reaberta)', () => {
    expect(decidirFechamentoNativo({ bloqueada: false, conectada: true, aberta: true })).toBe('ignorar');
  });

  it('diálogo aberto e conectado, com envio em curso: ignora (nada a fazer)', () => {
    expect(decidirFechamentoNativo({ bloqueada: true, conectada: true, aberta: true })).toBe('ignorar');
  });

  it('diálogo fechado e conectado, com envio em curso: reabre', () => {
    expect(decidirFechamentoNativo({ bloqueada: true, conectada: true, aberta: false })).toBe('reabrir');
  });

  it('diálogo fechado e conectado, sem envio: avisa', () => {
    expect(decidirFechamentoNativo({ bloqueada: false, conectada: true, aberta: false })).toBe('avisar');
  });

  it('diálogo desconectado: ignora, aberto ou fechado', () => {
    expect(decidirFechamentoNativo({ bloqueada: false, conectada: false, aberta: true })).toBe('ignorar');
    expect(decidirFechamentoNativo({ bloqueada: true, conectada: false, aberta: false })).toBe('ignorar');
  });
});
