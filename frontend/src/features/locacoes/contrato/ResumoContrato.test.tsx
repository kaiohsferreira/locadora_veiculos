import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { DOCUMENTO_CONTRATO } from './contratoLocacao.fixture';
import { CartoesResumo } from './ResumoContrato';

describe('CartoesResumo', () => {
  it('mostra o cartão Diárias e o de Caução na grade 2x2', () => {
    const html = renderToStaticMarkup(<CartoesResumo documento={DOCUMENTO_CONTRATO} />);
    expect(html).toContain('grid grid-cols-2 gap-1');
    expect(html).toContain('>Diárias<');
    expect(html).toContain('4 x R$ 229,00');
    expect(html).toContain('R$ 1.200,00');
    expect(html.split('bg-steel-fundo p-2').length - 1).toBe(4);
  });

  it('com caução zero, o cartão de Caução diz "Sem caução" e o de Diárias continua', () => {
    const semCaucao = {
      ...DOCUMENTO_CONTRATO,
      locacao: { ...DOCUMENTO_CONTRATO.locacao, valorCaucaoStr: null },
    };
    const html = renderToStaticMarkup(<CartoesResumo documento={semCaucao} />);
    expect(html).toContain('Sem caução');
    expect(html).toContain('4 x R$ 229,00');
  });

  it('sem nível de combustível na vistoria, o cartão de Combustível mostra "Não informada" e não inventa percentual', () => {
    const semNivel = {
      ...DOCUMENTO_CONTRATO,
      vistoriaEntrada: { ...DOCUMENTO_CONTRATO.vistoriaEntrada, nivelCombustivel: null },
    };
    const html = renderToStaticMarkup(<CartoesResumo documento={semNivel} />);
    expect(html).toContain('>Combustível<');
    expect(html).toContain('>Não informada<');
    expect(html).not.toContain('na retirada');
  });

  it('com uma diária, o cartão mostra "1 x" com o valor da diária', () => {
    const umaDiaria = {
      ...DOCUMENTO_CONTRATO,
      locacao: { ...DOCUMENTO_CONTRATO.locacao, diariasPrevistas: 1 },
    };
    const html = renderToStaticMarkup(<CartoesResumo documento={umaDiaria} />);
    expect(html).toContain('1 x R$ 229,00');
  });
});
