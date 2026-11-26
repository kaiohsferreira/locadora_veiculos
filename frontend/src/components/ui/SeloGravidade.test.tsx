import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { SeloGravidade } from './SeloGravidade';
import { estiloSeloGravidade } from './estiloSeloGravidade';

describe('estiloSeloGravidade', () => {
  it('Leve e Média usam os tokens avaria.* e o Grave usa manutencao.alerta-*', () => {
    expect(estiloSeloGravidade('LEVE').cores).toBe('bg-avaria-leve-fundo text-avaria-leve-texto');
    expect(estiloSeloGravidade('MEDIA').cores).toBe('bg-avaria-media-fundo text-avaria-media-texto');
    expect(estiloSeloGravidade('GRAVE').cores).toBe('bg-manutencao-alerta-trilho text-manutencao-alerta-escuro');
  });

  it('ícones: info para Leve, report_problem para Média e error para Grave', () => {
    expect(estiloSeloGravidade('LEVE').icone).toBe('info');
    expect(estiloSeloGravidade('MEDIA').icone).toBe('report_problem');
    expect(estiloSeloGravidade('GRAVE').icone).toBe('error');
  });
});

describe('SeloGravidade', () => {
  it('mostra o nome da gravidade como texto, não só a cor', () => {
    expect(renderToStaticMarkup(<SeloGravidade gravidade="LEVE" />)).toContain('Leve');
    expect(renderToStaticMarkup(<SeloGravidade gravidade="MEDIA" />)).toContain('Média');
    expect(renderToStaticMarkup(<SeloGravidade gravidade="GRAVE" />)).toContain('Grave');
  });

  it('aplica as cores do token da gravidade e o texto em negrito', () => {
    const html = renderToStaticMarkup(<SeloGravidade gravidade="MEDIA" />);

    expect(html).toContain('bg-avaria-media-fundo');
    expect(html).toContain('text-avaria-media-texto');
    expect(html).toContain('font-bold');
    expect(html).not.toContain('bg-avaria-leve-fundo');
  });
});
