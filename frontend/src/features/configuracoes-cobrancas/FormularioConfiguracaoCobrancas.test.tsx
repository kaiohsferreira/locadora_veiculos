import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import { FormularioConfiguracaoCobrancas } from './FormularioConfiguracaoCobrancas';
vi.mock('@/features/frota/hooks/useCategorias', () => ({ useCategorias: () => ({ data: { itens: [{ idCategoria: 1, nome: 'Econômico' }] }, isPending: false, isError: false }) }));
vi.mock('./useConfiguracoesCobrancas', () => ({ useSalvarConfiguracaoCobrancas: () => ({ isPending: false }) }));
describe('formulário de política acessível', () => {
  it('exige dez campos e seleção explícita de todos os modos sem valores iniciais', () => {
    const html = renderToStaticMarkup(<AvisoProvider><FormularioConfiguracaoCobrancas aoConcluir={vi.fn()} aoCancelar={vi.fn()} /></AvisoProvider>);
    expect(html.match(/aria-required="true"/g)).toHaveLength(10);
    expect(html.match(/<option value="">/g)).toHaveLength(5);
    expect(html).not.toContain('selected=""');
    const inputs = html.match(/<input\b[^>]*>/g) ?? [];
    expect(inputs).toHaveLength(5);
    expect(inputs.every(input => !/value="[^"]+"/.test(input))).toBe(true);
    expect(html).toContain('min-w-0'); expect(html).toContain('grid-cols-1');
    expect(html).toContain('Base de cálculo das avarias');
  });
});
