import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { valoresIniciaisCadastro } from '@/lib/conversaoVeiculo';
import { CamposIdentificacaoVeiculo } from './CamposIdentificacaoVeiculo';

function renderizar(): string {
  return renderToStaticMarkup(
    <CamposIdentificacaoVeiculo valores={valoresIniciaisCadastro()} erros={{}} aoMudar={() => undefined} />,
  );
}

describe('CamposIdentificacaoVeiculo', () => {
  it('mostra o título da seção, a etiqueta e os rótulos do desenho, na ordem da tela', () => {
    const html = renderizar();
    const rotulos = [
      '1. Identificação Técnica do Veículo',
      'Dados Obrigatórios',
      'Placa (Mercosul)',
      'Chassi (VIN - 17 Dígitos)',
      'Renavam (11 Dígitos)',
      'Marca Fabricante',
      'Modelo e Versão Completa',
      'Ano Fabricação',
      'Ano Modelo',
      'Cor Externa / Acabamento',
    ];

    let posicaoAnterior = -1;
    for (const rotulo of rotulos) {
      const posicao = html.indexOf(rotulo);
      expect(posicao, rotulo).toBeGreaterThan(posicaoAnterior);
      posicaoAnterior = posicao;
    }
  });

  it('o rótulo da placa e o apoio "Padrão Brasil" ficam numa linha só, sem quebra e sem espaço fixo entre eles', () => {
    const html = renderizar();
    const linha = html.match(/<div class="flex flex-col gap-0\.5 sm:flex-row[^"]*">/)?.[0] ?? '';

    expect(linha).toContain('sm:justify-between');
    expect(linha).not.toContain('sm:gap-2');
    expect(html).toMatch(/<label[^>]*class="[^"]*whitespace-nowrap[^"]*">Placa \(Mercosul\)/);
    expect(html).toMatch(/<span id="[^"]+" class="[^"]*whitespace-nowrap[^"]*">Padrão Brasil<\/span>/);
  });

  it('a placa tem o apoio "Padrão Brasil", o emblema BR e o exemplo ABC1D23', () => {
    const html = renderizar();

    expect(html).toContain('Padrão Brasil');
    expect(html).toContain('>BR</span>');
    expect(html).toContain('placeholder="ABC1D23"');
    expect(html).toContain('maxLength="7"');
  });

  it('chassi com 17 caracteres e renavam com 11, como o desenho', () => {
    const html = renderizar();

    expect(html).toContain('maxLength="17"');
    expect(html).toContain('maxLength="11"');
  });

  it('o chassi tem o ícone "verified" decorativo à direita, como o desenho', () => {
    const html = renderizar();

    expect(html).toContain('>verified</span>');
  });

  it('o campo do chassi tem recuo de 16px dos dois lados, como o desenho (px-space-md)', () => {
    const html = renderizar();
    const campoChassi = html.match(/<input[^>]*name="chassi"[^>]*>/)?.[0] ?? '';

    expect(campoChassi).toContain('pr-4');
    expect(campoChassi).not.toContain('pr-12');
  });

  it('não tem textos sem lastro do desenho (decisão 5)', () => {
    const html = renderizar();

    expect(html).not.toContain('Conforme Resolução CONTRAN 780');
    expect(html).not.toContain('Validação automática de dígito verificador');
    expect(html).not.toContain('Base Nacional DENATRAN');
  });

  it('nada vem pré-marcado: sem "Jeep", sem cor marcada e com "Selecione" na marca', () => {
    const html = renderizar();

    expect(html).toContain('<option value="" selected="">Selecione</option>');
    expect(html).not.toContain('<option value="Jeep" selected');
    expect(html).not.toContain('checked=""');
  });

  it('a lista de marcas tem as da lista do Kaio, em ordem', () => {
    const html = renderizar();

    expect(html).toContain('>Audi</option>');
    expect(html).toContain('>Citroën</option>');
    expect(html).toContain('>Volvo</option>');
  });

  it('a placa e os campos obrigatórios têm aria-required sem asterisco visível', () => {
    const html = renderizar();

    expect(html).toContain('aria-required="true"');
    expect(html).not.toContain('>*</span>');
  });

  it('os quatro swatches de cor do desenho aparecem como opções de rádio', () => {
    const html = renderizar();

    expect(html.match(/type="radio"/g)).toHaveLength(4);
    expect(html).toContain('>Preto Carbon</span>');
    expect(html).toContain('>Cinza Silver</span>');
  });
});
