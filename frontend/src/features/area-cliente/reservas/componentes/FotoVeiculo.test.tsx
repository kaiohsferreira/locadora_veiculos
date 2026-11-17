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

  it('a URL assinada só aparece no src da imagem: nunca em atributo de dados', () => {
    const html = renderToStaticMarkup(<FotoVeiculo fotoCapa={CAPA} descricao="Jeep Renegade Longitude" />);
    expect(html).not.toMatch(/data-[\w-]*="[^"]*https?:/);
    expect(html.split(URL_ASSINADA).length - 1).toBe(1);
  });
});
