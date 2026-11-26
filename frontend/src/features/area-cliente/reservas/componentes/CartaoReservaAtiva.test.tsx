import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import { CartaoReservaAtiva } from '@/features/area-cliente/reservas/componentes/CartaoReservaAtiva';
import { RESERVA_ATIVA } from '@/features/area-cliente/reservas/fixturasReservas';
import type { ReservaCliente } from '@/types/reservas';

function renderizar(reserva: ReservaCliente = RESERVA_ATIVA): string {
  return renderToStaticMarkup(
    <AvisoProvider>
      <CartaoReservaAtiva reserva={reserva} nomeCondutor="Marina Costa" aoCancelar={vi.fn()} />
    </AvisoProvider>,
  );
}

/** Classes do elemento cujo texto direto é `texto` (ex.: o span "Criada em ..."). */
function classesDe(html: string, texto: string): string[] {
  const achado = html.match(new RegExp(`class="([^"]*)"[^>]*>${escaparRegex(texto)}<`));
  return achado ? achado[1].split(' ') : [];
}

function escaparRegex(texto: string): string {
  return texto.replace(/[.*+?^${}()|[\]\\/]/g, '\\$&');
}

/** Os links (<a>) da página, com a classe e o texto de cada um. */
function links(html: string): string[] {
  return html.match(/<a [^>]*>[\s\S]*?<\/a>/g) ?? [];
}

describe('CartaoReservaAtiva (tela 28 no celular; tela 29 no computador)', () => {
  it('ordem no celular: código e situação, veículo, roteiro, valor e rodapé', () => {
    const html = renderizar();
    const posicoes = [
      html.indexOf('Código da reserva'),
      html.indexOf('AV-00042'),
      html.indexOf('Ativa<'),
      html.indexOf('Jeep Renegade Longitude'),
      html.indexOf('Roteiro da reserva'),
      html.indexOf('Valor estimado'),
      html.lastIndexOf('Como chegar'),
    ];
    for (const posicao of posicoes) expect(posicao).toBeGreaterThan(-1);
    expect([...posicoes].sort((a, b) => a - b)).toEqual(posicoes);
  });

  it('o cabeçalho: no celular o rótulo fica em cima e o código com o copiar embaixo; a situação fica à direita, sem ordem visual', () => {
    const html = renderizar();
    expect(html).not.toMatch(/[" ]order-\d/);
    expect(html).toContain('flex min-w-0 flex-col gap-1 md:flex-row');
    expect(html.indexOf('Código da reserva')).toBeLessThan(html.indexOf('AV-00042'));
    expect(html.indexOf('AV-00042')).toBeLessThan(html.indexOf('Ativa<'));
  });

  it('"Como chegar" no rodapé só abaixo de md; no roteiro, só a partir de md (no computador)', () => {
    const html = renderizar();
    const anchors = links(html);
    const noRodape = anchors.find((a) => a.includes('Como chegar') && a.includes('md:hidden'));
    const noRoteiro = anchors.find((a) => a.includes('Como chegar') && a.includes('md:inline-flex'));
    expect(noRodape).toBeDefined();
    expect(noRoteiro).toBeDefined();
  });

  it('Condutor e "Criada em" ficam escondidos abaixo de md (o desenho 28 não os tem)', () => {
    const html = renderizar();
    expect(html).toContain('hidden flex-col gap-1 md:flex');
    expect(classesDe(html, 'Criada em 02/11/2026 14:22')).toEqual(expect.arrayContaining(['hidden', 'md:inline']));
  });

  it('a placa aparece abaixo do nome no celular (depois da legenda do ano e da cor)', () => {
    const html = renderizar();
    const legenda = html.indexOf('Ano do modelo 2025 · Azul');
    const placaDoCelular = html.indexOf('mt-1 inline-flex self-start md:hidden');
    expect(legenda).toBeGreaterThan(-1);
    expect(placaDoCelular).toBeGreaterThan(legenda);
    expect(html).toContain('hidden md:inline-flex');
  });

  it('a situação do celular é uma pílula (rounded-full) e volta ao retângulo a partir de md', () => {
    const html = renderizar();
    expect(html).toContain('rounded-full bg-steel-linha px-2.5 py-1 text-micro');
    expect(html).toContain('md:rounded md:px-3 md:text-rotulo');
  });

  it('a foto do veículo é a compacta: 96x64 no celular, com a caixa 16:10 a partir de md', () => {
    const html = renderizar();
    expect(html).toContain('h-16 w-24');
    expect(html).toContain('md:aspect-[16/10]');
    expect(html).toContain('md:[&amp;&gt;img]:object-contain');
  });

  it('a única ação do cartão é o cancelamento (com o botão "Cancelar reserva") e o "Como chegar"', () => {
    const html = renderizar();
    expect(html).toContain('Cancelar reserva');
    expect(html).toContain('Como chegar');
  });

  it('"Cancelar reserva" fica numa linha no celular: não quebra o texto e ocupa o espaço natural (flex-auto)', () => {
    const html = renderizar();
    const botao = html.match(/<button[^>]*class="([^"]*)"[^>]*>(?:(?!<\/button>)[\s\S])*Cancelar reserva<\/span><\/button>/);
    expect(botao).not.toBeNull();
    const classes = (botao?.[1] ?? '').split(' ');
    expect(classes).toEqual(expect.arrayContaining(['whitespace-nowrap', 'flex-auto', 'px-2', 'md:px-4']));
    expect(classes).not.toContain('flex-1');
  });

  it('"Como chegar" no rodapé do celular fica numa linha e não é espremido pelo botão (flex-auto, sem quebra)', () => {
    const html = renderizar();
    const link = links(html).find((a) => a.includes('Como chegar') && a.includes('md:hidden'));
    expect(link).toBeDefined();
    const classes = (link?.match(/<a [^>]*class="([^"]*)"/)?.[1] ?? '').split(' ');
    expect(classes).toEqual(expect.arrayContaining(['flex-auto', 'whitespace-nowrap']));
    expect(classes).not.toContain('flex-1');
  });

  it('a caixa da foto no celular usa o tom do desenho 28 (steel-suave, sobre o painel steel-fundo); no computador volta ao tom de sempre', () => {
    const html = renderizar();
    // A raiz da caixa da foto (a classe de tamanho vem logo depois do fundo): tom suave no celular, normal no md.
    expect(html).toContain('overflow-hidden bg-steel-suave md:bg-steel-fundo h-16 w-24');
    expect(html).not.toContain('bg-steel-alto');
  });
});
