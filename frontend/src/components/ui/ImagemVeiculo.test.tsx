import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { ImagemVeiculo } from './ImagemVeiculo';

describe('ImagemVeiculo', () => {
  it('com URL, mostra a imagem com carregamento preguiçoso, decodificação assíncrona e sem referrer', () => {
    const html = renderToStaticMarkup(<ImagemVeiculo url="https://armazenamento.exemplo/a.jpg" alt="Frente do carro" />);

    expect(html).toContain('<img');
    expect(html).toContain('src="https://armazenamento.exemplo/a.jpg"');
    expect(html).toContain('alt="Frente do carro"');
    expect(html).toContain('loading="lazy"');
    expect(html).toContain('decoding="async"');
    expect(html).toContain('referrerPolicy="no-referrer"');
    expect(html).not.toContain('directions_car');
  });

  it('ajuste "cover" usa object-cover e "contain" usa object-contain', () => {
    const cover = renderToStaticMarkup(<ImagemVeiculo url="https://a/1.jpg" alt="" />);
    const contain = renderToStaticMarkup(<ImagemVeiculo url="https://a/1.jpg" alt="" ajuste="contain" />);

    expect(cover).toContain('object-cover');
    expect(cover).not.toContain('object-contain');
    expect(contain).toContain('object-contain');
    expect(contain).not.toContain('object-cover');
  });

  it('sem URL (nula ou ausente), mostra o ícone de carro e nenhuma imagem', () => {
    for (const url of [null, undefined]) {
      const html = renderToStaticMarkup(<ImagemVeiculo url={url} alt="Foto" tamanhoIcone={48} />);

      expect(html).not.toContain('<img');
      expect(html).toContain('directions_car');
      expect(html).toContain('font-size:48px');
      expect(html).toContain('text-borda-forte');
    }
  });

  it('a classe recebida define a proporção e o tamanho da caixa, sobre a base do componente', () => {
    const html = renderToStaticMarkup(<ImagemVeiculo url={null} alt="" className="aspect-[16/10] w-full" />);

    expect(html).toContain('aspect-[16/10] w-full');
    expect(html).toContain('bg-steel-fundo');
    expect(html).toContain('overflow-hidden');
  });

  it('o texto alternativo vazio deixa a imagem decorativa', () => {
    const html = renderToStaticMarkup(<ImagemVeiculo url="https://a/1.jpg" alt="" />);

    expect(html).toContain('alt=""');
  });
});
