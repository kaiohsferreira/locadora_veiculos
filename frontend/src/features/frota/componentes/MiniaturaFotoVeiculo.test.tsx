import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MiniaturaFotoVeiculo, type MiniaturaFotoVeiculoProps } from './MiniaturaFotoVeiculo';

const MENSAGEM_LONGA = 'O armazenamento de fotos está indisponível. Tente de novo mais tarde.';

function renderizar(parcial: Partial<MiniaturaFotoVeiculoProps>): string {
  return renderToStaticMarkup(
    <ul>
      <MiniaturaFotoVeiculo
        posicao={2}
        url="blob:previa-2"
        alt="Foto do veículo Jeep Renegade"
        estado="erro"
        mensagemErro={MENSAGEM_LONGA}
        aoRemover={() => undefined}
        aoTentarDeNovo={() => undefined}
        {...parcial}
      />
    </ul>,
  );
}

/**
 * Posição do fim da caixa 4:3 (o elemento com `aspect-[4/3]` e todos os filhos). Conta as `<div>` abertas e fechadas
 * a partir da caixa, sem depender de biblioteca de DOM.
 */
function fimDaCaixa(html: string): { inicio: number; fim: number } {
  const inicio = html.indexOf('aspect-[4/3]');
  const inicioTag = html.lastIndexOf('<div', inicio);
  const marcas = /<div\b[^>]*>|<\/div>/g;
  marcas.lastIndex = inicioTag;
  let profundidade = 0;
  let marca = marcas.exec(html);
  while (marca !== null) {
    if (marca[0] === '</div>') {
      profundidade -= 1;
      if (profundidade === 0) return { inicio: inicioTag, fim: marca.index + marca[0].length };
    } else {
      profundidade += 1;
    }
    marca = marcas.exec(html);
  }
  throw new Error('caixa 4:3 sem fechamento');
}

describe('MiniaturaFotoVeiculo: foto com erro', () => {
  it('traz a mensagem inteira e o botão "Tentar de novo" fora da caixa da imagem, logo abaixo dela', () => {
    const html = renderizar({});
    const { inicio, fim } = fimDaCaixa(html);
    const dentroDaCaixa = html.slice(inicio, fim);
    const abaixoDaCaixa = html.slice(fim);

    expect(dentroDaCaixa).not.toContain(MENSAGEM_LONGA);
    expect(dentroDaCaixa).not.toContain('Tentar de novo');
    expect(abaixoDaCaixa).toContain(MENSAGEM_LONGA);
    expect(abaixoDaCaixa).toContain('Tentar de novo');
    expect(abaixoDaCaixa.indexOf(MENSAGEM_LONGA)).toBeLessThan(abaixoDaCaixa.indexOf('Tentar de novo'));
  });

  it('a mensagem tem quebra de linha e não é cortada (sem truncar nem recorte)', () => {
    const html = renderizar({});

    expect(html).toMatch(/<p role="alert" class="[^"]*break-words[^"]*">/);
    expect(html).not.toContain('truncate');
    expect(html).not.toContain('line-clamp');
  });

  it('o botão "Tentar de novo" é o do componente de base, pequeno, ocupa a largura da célula e quebra o texto dentro dela', () => {
    const html = renderizar({});

    expect(html).toMatch(/<button[^>]*class="[^"]*h-8 px-3[^"]*w-full !h-auto py-1\.5 !whitespace-normal[^"]*"[^>]*>(?:(?!<\/button>)[\s\S])*Tentar de novo<\/button>/);
  });

  it('a caixa continua 4:3 e a faixa de ícone de erro fica dentro dela (o estado não depende só de cor)', () => {
    const html = renderizar({});
    const { inicio, fim } = fimDaCaixa(html);
    const caixa = html.slice(inicio, fim);

    expect(caixa).toContain('aspect-[4/3]');
    expect(caixa).toContain('>error</span>');
    expect(caixa).toContain('bg-status-perigo');
  });

  it('sem aoTentarDeNovo, a mensagem continua abaixo da caixa e não aparece botão', () => {
    const html = renderizar({ aoTentarDeNovo: undefined });

    expect(html).toContain(MENSAGEM_LONGA);
    expect(html).not.toContain('Tentar de novo');
  });

  it('sem mensagem, usa o texto padrão de envio em vez de ficar vazia', () => {
    const html = renderizar({ mensagemErro: null });

    expect(html).toContain('Não foi possível enviar esta foto.');
  });
});

describe('MiniaturaFotoVeiculo: outros estados da fila', () => {
  it('aguardando e enviando ficam dentro da caixa 4:3, com o texto dentro dela', () => {
    const aguardando = renderizar({ estado: 'aguardando', mensagemErro: null, aoTentarDeNovo: undefined });
    const enviando = renderizar({ estado: 'enviando', percentual: 40, mensagemErro: null, aoTentarDeNovo: undefined });

    const caixaAguardando = aguardando.slice(...intervaloDaCaixa(aguardando));
    const caixaEnviando = enviando.slice(...intervaloDaCaixa(enviando));
    expect(caixaAguardando).toContain('Aguardando envio');
    expect(caixaEnviando).toContain('Enviando… 40%');
    expect(caixaEnviando).toContain('<progress');
  });

  it('o estado enviando não tem botão de remover; os demais têm', () => {
    const enviando = renderizar({ estado: 'enviando', percentual: 5, aoTentarDeNovo: undefined });

    expect(enviando).not.toContain('aria-label="Remover imagem 2"');
    expect(renderizar({ estado: 'aguardando', aoTentarDeNovo: undefined })).toContain('aria-label="Remover imagem 2"');
  });
});

describe('MiniaturaFotoVeiculo: prévia até a imagem do servidor carregar', () => {
  it('uma foto gravada com prévia mostra a prévia por cima da imagem do servidor', () => {
    const html = renderizar({
      estado: 'gravada',
      url: 'https://armazenamento.exemplo/foto-1.jpg',
      previaAteCarregar: 'blob:previa-1',
      mensagemErro: null,
      aoTentarDeNovo: undefined,
    });

    expect(html).toContain('src="blob:previa-1"');
    expect(html).toContain('src="https://armazenamento.exemplo/foto-1.jpg"');
    expect(html.indexOf('src="https://armazenamento.exemplo/foto-1.jpg"')).toBeLessThan(html.indexOf('src="blob:previa-1"'));
  });

  it('sem prévia, a foto gravada mostra só a imagem do servidor', () => {
    const html = renderizar({
      estado: 'gravada',
      url: 'https://armazenamento.exemplo/foto-1.jpg',
      previaAteCarregar: null,
      mensagemErro: null,
      aoTentarDeNovo: undefined,
    });

    expect(html).not.toContain('blob:');
    expect(html).toContain('src="https://armazenamento.exemplo/foto-1.jpg"');
  });
});

/** Intervalo [inicio, fim) da caixa 4:3 num trecho de marcação. */
function intervaloDaCaixa(html: string): [number, number] {
  const { inicio, fim } = fimDaCaixa(html);
  return [inicio, fim];
}
