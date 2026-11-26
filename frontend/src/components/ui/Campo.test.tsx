import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { Campo } from './Campo';

describe('Campo: apoio do rótulo', () => {
  it('mantém o apoio na linha do rótulo em qualquer largura quando apoioRotuloEmLinha é usada', () => {
    const html = renderToStaticMarkup(<Campo rotulo="Data de devolução" apoioRotulo="3 diárias" apoioRotuloEmLinha />);
    expect(html).toMatch(/<div class="flex items-center justify-between gap-2"><label[^>]*>Data de devolução<\/label><span [^>]*>3 diárias<\/span><\/div>/);
    expect(html).not.toContain('sm:flex-row');
  });

  it('sem a prop nova, a linha do rótulo continua empilhada abaixo de sm como antes', () => {
    const html = renderToStaticMarkup(<Campo rotulo="Data de retirada" apoioRotulo="Opcional" />);
    expect(html).toContain('flex flex-col gap-0.5 sm:flex-row sm:items-center sm:justify-between sm:gap-2');
  });

  it('aplica a classe do apoio quando ela é informada e o estilo padrão quando não é', () => {
    const comClasse = renderToStaticMarkup(<Campo rotulo="Devolução" apoioRotulo="4 diárias" apoioRotuloClasse="text-micro" />);
    expect(comClasse).toMatch(/<span id="[^"]*-apoio" class="text-micro">4 diárias<\/span>/);
  });
});
