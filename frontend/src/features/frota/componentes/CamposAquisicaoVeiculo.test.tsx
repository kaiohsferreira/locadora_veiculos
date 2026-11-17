import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { valoresIniciaisCadastro } from '@/lib/conversaoVeiculo';
import { CamposAquisicaoVeiculo } from './CamposAquisicaoVeiculo';

function renderizar(): string {
  return renderToStaticMarkup(
    <CamposAquisicaoVeiculo valores={valoresIniciaisCadastro()} erros={{}} aoMudar={() => undefined} />,
  );
}

describe('CamposAquisicaoVeiculo', () => {
  it('mostra a seção "3. Registro de Aquisição e Fornecedor", a etiqueta "Auditoria Contábil" e os quatro campos, em ordem', () => {
    const html = renderizar();
    const ordem = [
      '3. Registro de Aquisição e Fornecedor',
      'Auditoria Contábil',
      'Data da Compra',
      'Odômetro de Entrega',
      'Fornecedor / Concessionária',
      'Nota Fiscal (NF-e)',
    ];

    let posicaoAnterior = -1;
    for (const texto of ordem) {
      const posicao = html.indexOf(texto);
      expect(posicao, texto).toBeGreaterThan(posicaoAnterior);
      posicaoAnterior = posicao;
    }
  });

  it('a data é de calendário e não vem preenchida', () => {
    const html = renderizar();

    expect(html).toContain('type="date"');
    expect(html).not.toContain('value="2024-10-15"');
  });

  it('o odômetro tem o sufixo "KM" e é obrigatório sem asterisco visível', () => {
    const html = renderizar();

    expect(html).toContain('>KM</span>');
    expect(html).toContain('aria-required="true"');
    expect(html).not.toContain('>*</span>');
  });

  it('só o odômetro é obrigatório: data, fornecedor e nota fiscal são opcionais (um único aria-required)', () => {
    const html = renderizar();

    expect(html.match(/aria-required/g)).toHaveLength(1);
  });
});
