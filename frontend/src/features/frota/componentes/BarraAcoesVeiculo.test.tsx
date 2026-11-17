import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { BarraAcoesVeiculo } from './BarraAcoesVeiculo';

const BASE = { formId: 'form-veiculo', enviando: false, aoCancelar: () => undefined };

describe('BarraAcoesVeiculo: cadastro', () => {
  it('tem os três botões do desenho: Cancelar / Voltar, Salvar como Rascunho e Salvar e Ativar Veículo', () => {
    const html = renderToStaticMarkup(
      <BarraAcoesVeiculo modo="cadastro" {...BASE} aoSalvarRascunho={() => undefined} />,
    );

    expect(html).toContain('Cancelar / Voltar');
    expect(html).toContain('Salvar como Rascunho');
    expect(html).toContain('Salvar e Ativar Veículo');
  });

  it('o principal envia o formulário pelo id (fica fora do form) e é do tipo submit', () => {
    const html = renderToStaticMarkup(<BarraAcoesVeiculo modo="cadastro" {...BASE} />);

    expect(html).toContain('form="form-veiculo"');
    expect(html).toContain('type="submit"');
  });

  it('é uma barra com nome para leitores de tela', () => {
    const html = renderToStaticMarkup(<BarraAcoesVeiculo modo="cadastro" {...BASE} />);

    expect(html).toContain('<aside aria-label="Ações de cadastro"');
    expect(html).toContain('sticky bottom-0 z-10');
  });

  it('enviando: o principal mostra "Gravando..." e fica ocupado e bloqueado', () => {
    const html = renderToStaticMarkup(<BarraAcoesVeiculo modo="cadastro" {...BASE} enviando />);

    expect(html).toContain('Gravando...');
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('Gravando no Pátio');
  });

  it('enviando: "Cancelar / Voltar" fica desabilitado enquanto o cadastro é gravado', () => {
    const html = renderToStaticMarkup(<BarraAcoesVeiculo modo="cadastro" {...BASE} enviando />);

    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>)[\s\S])*Cancelar \/ Voltar/);
  });

  it('fora do envio: "Cancelar / Voltar" fica habilitado', () => {
    const html = renderToStaticMarkup(<BarraAcoesVeiculo modo="cadastro" {...BASE} />);

    expect(html).not.toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>)[\s\S])*Cancelar \/ Voltar/);
  });

  it('concluído: o principal mostra "Veículo Ativado!" com o ícone done_all, bloqueado', () => {
    const html = renderToStaticMarkup(<BarraAcoesVeiculo modo="cadastro" {...BASE} concluido />);

    expect(html).toContain('Veículo Ativado!');
    expect(html).toContain('>done_all</span>');
    expect(html).toContain('bg-navy-profundo');
    expect(html).toContain('disabled=""');
  });
});

describe('BarraAcoesVeiculo: edição', () => {
  it('o principal vira "Salvar Alterações" e o rascunho some', () => {
    const html = renderToStaticMarkup(<BarraAcoesVeiculo modo="edicao" {...BASE} />);

    expect(html).toContain('Salvar Alterações');
    expect(html).not.toContain('Salvar como Rascunho');
    expect(html).not.toContain('Salvar e Ativar Veículo');
    expect(html).toContain('Cancelar / Voltar');
  });
});
