import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoFotosReservas } from '@/features/area-cliente/reservas/componentes/AvisoFotosReservas';

const MENSAGEM_SERVIDOR = 'Não foi possível carregar as fotos agora. Tente novamente em instantes.';

describe('AvisoFotosReservas', () => {
  it('mostra a mensagem do servidor como papel de status, uma vez, com ícone decorativo', () => {
    const html = renderToStaticMarkup(<AvisoFotosReservas mensagem={MENSAGEM_SERVIDOR} />);
    expect(html).toMatch(/^<output class="[^"]*">/);
    expect(html.split(MENSAGEM_SERVIDOR).length - 1).toBe(1);
    expect(html).toContain('image_not_supported');
    expect(html).toMatch(/<span[^>]*aria-hidden="true"[^>]*>image_not_supported<\/span>/);
  });

  it('não usa papel de alerta: o erro de foto não bloqueia a lista', () => {
    const html = renderToStaticMarkup(<AvisoFotosReservas mensagem={MENSAGEM_SERVIDOR} />);
    expect(html).not.toContain('role="alert"');
  });

  it('a cor não é a única informação: o texto da mensagem sai dentro do aviso', () => {
    const html = renderToStaticMarkup(<AvisoFotosReservas mensagem={MENSAGEM_SERVIDOR} />);
    expect(html).toMatch(/<span>Não foi possível carregar as fotos agora\. Tente novamente em instantes\.<\/span>/);
  });
});
