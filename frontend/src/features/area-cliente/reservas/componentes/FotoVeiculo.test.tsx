import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { FotoVeiculo } from '@/features/area-cliente/reservas/componentes/FotoVeiculo';
import type { FotoCapaVeiculo } from '@/types/fotosVeiculo';

const URL_ASSINADA = 'https://armazenamento.exemplo/bucket/veiculos/7/fotos/abc.jpg?assinatura=teste';
const URL_MINIATURA = 'https://armazenamento.exemplo/bucket/veiculos/7/fotos/abc-mini.jpg?assinatura=teste';

const CAPA: FotoCapaVeiculo = {
  url: URL_ASSINADA,
  urlMiniatura: null,
  expiraEm: '2026-11-16T21:15:00.0000000+00:00',
};

describe('FotoVeiculo (caixa da foto da tela 29)', () => {
  it('com foto de capa: mostra a imagem inteira (object-contain) com a descrição como texto alternativo', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={CAPA} descricao="Jeep Renegade Longitude" />);
    expect(html).toContain(`src="${URL_ASSINADA}"`);
    expect(html).toContain('alt="Jeep Renegade Longitude"');
    expect(html).toContain('object-contain');
    expect(html).toContain('aspect-[16/10]');
    expect(html).not.toContain('directions_car');
  });

  it('a miniatura, quando existir, é a que a tela usa (regra de urlDaImagem)', () => {
    const html = renderToStaticMarkup(
      <FotoVeiculo fotoCapa={{ ...CAPA, urlMiniatura: URL_MINIATURA }} descricao="Jeep Renegade Longitude" />,
    );
    expect(html).toContain(`src="${URL_MINIATURA}"`);
    expect(html).not.toContain(`src="${URL_ASSINADA}"`);
  });

  it('sem foto cadastrada (capa nula): caixa com o ícone de carro e nenhuma imagem', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={null} descricao="Jeep Renegade Longitude" />);
    expect(html).toContain('directions_car');
    expect(html).not.toContain('<img');
  });

  it('resposta antiga, sem o campo fotoCapa: a mesma caixa com o ícone de carro', () => {
    const html = renderToStaticMarkup(<FotoVeiculo descricao="Jeep Renegade Longitude" />);
    expect(html).toContain('directions_car');
    expect(html).not.toContain('<img');
  });

  it('sem compacta (tela 04, LinhaVeiculo): a caixa 16:10 com a imagem inteira, como antes', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={CAPA} descricao="Jeep Renegade Longitude" />);
    expect(html).toContain('aspect-[16/10] w-full rounded p-2');
    expect(html).toContain('object-contain');
    expect(html).not.toContain('object-cover');
    expect(html).not.toContain('h-16 w-24');
  });

  it('compacta (cartão da reserva): 96x64 no celular com a foto cobrindo a caixa (object-cover)', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={CAPA} descricao="Jeep Renegade Longitude" compacta />);
    expect(html).toContain('h-16 w-24');
    // A imagem (img) recebe object-cover; o object-contain só entra a partir de md, por seletor (ver o próximo teste).
    expect(html).toContain('class="h-full w-full object-cover"');
    expect(html).not.toContain('class="h-full w-full object-contain"');
  });

  it('compacta: a partir de md volta à caixa 16:10 com a imagem inteira (desenho 29)', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={CAPA} descricao="Jeep Renegade Longitude" compacta />);
    expect(html).toContain('md:aspect-[16/10]');
    expect(html).toContain('md:[&amp;&gt;img]:object-contain');
    expect(html).toContain('md:p-2');
  });

  it('compacta sem foto: o ícone de carro, reduzido no celular e em tamanho cheio a partir de md', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={null} descricao="Jeep Renegade Longitude" compacta />);
    expect(html).toContain('directions_car');
    expect(html).not.toContain('<img');
    expect(html).toContain('[&amp;&gt;span]:scale-50');
    expect(html).toContain('md:[&amp;&gt;span]:scale-100');
  });

  it('sem classeFundo (tela 04 e demais): a caixa segue com bg-steel-fundo e nenhum tom novo', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={null} descricao="Jeep Renegade Longitude" compacta />);
    expect(html).toContain('overflow-hidden bg-steel-fundo ');
    expect(html).not.toContain('bg-steel-alto');
  });

  it('com classeFundo (cartão ativo do celular): a caixa usa a classe pedida no lugar do fundo padrão', () => {
    const html = renderToStaticMarkup(
      <FotoVeiculo fotoCapa={null} descricao="Jeep Renegade Longitude" compacta classeFundo="bg-steel-suave md:bg-steel-fundo" />,
    );
    expect(html).toContain('overflow-hidden bg-steel-suave md:bg-steel-fundo h-16 w-24');
    expect(html).not.toContain('overflow-hidden bg-steel-fundo');
    expect(html).toContain('directions_car');
  });

  it('a URL assinada só aparece no src da imagem: nunca em atributo de dados', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={CAPA} descricao="Jeep Renegade Longitude" />);
    expect(html).not.toMatch(/data-[\w-]*="[^"]*https?:/);
    expect(html.split(URL_ASSINADA).length - 1).toBe(1);
  });
});
