import { describe, expect, it, vi } from 'vitest';
import { chaveRetiradasDeHoje } from './chavesPainelOperacional';
import { opcoesRetiradasDeHoje } from './useRetiradasDeHoje';

// A camada HTTP usa `window` ao ser importada (sem DOM aqui); a consulta não é executada neste teste.
vi.mock('@/api/painelOperacional', () => ({ buscarRetiradasDeHoje: vi.fn() }));

describe('opcoesRetiradasDeHoje (lista da página de entrada)', () => {
  it('usa a chave da lista de hoje, que a abertura da locação invalida pela família', () => {
    expect(opcoesRetiradasDeHoje().queryKey).toEqual(chaveRetiradasDeHoje());
    expect(opcoesRetiradasDeHoje().queryKey).toEqual(['painel-operacional', 'hoje']);
  });

  it('busca a cada abertura da página (sem cache de 30 s, para não mostrar a lista velha)', () => {
    expect(opcoesRetiradasDeHoje().staleTime).toBe(0);
  });
});
