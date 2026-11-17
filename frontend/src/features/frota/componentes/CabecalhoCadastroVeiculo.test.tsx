import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { CabecalhoCadastroVeiculo } from './CabecalhoCadastroVeiculo';

function renderizar(modo: 'cadastro' | 'edicao'): string {
  return renderToStaticMarkup(
    <MemoryRouter>
      <CabecalhoCadastroVeiculo modo={modo} caminhoFrota="/back-office/frota" />
    </MemoryRouter>,
  );
}

describe('CabecalhoCadastroVeiculo: cadastro', () => {
  it('mostra a trilha "Frota > Cadastrar Veículo", o título e o texto de apoio do desenho', () => {
    const html = renderizar('cadastro');

    expect(html).toContain('href="/back-office/frota"');
    expect(html).toContain('Frota');
    expect(html).toContain('Cadastrar Veículo');
    expect(html).toContain('<h1');
    expect(html).toContain('Cadastrar Novo Veículo');
    expect(html).toContain(
      'Preencha os dados técnicos, comerciais e de aquisição para integrar o ativo à frota operacional.',
    );
  });

  it('não mostra a etiqueta "ATIVO NOVO" (decisão 5)', () => {
    expect(renderizar('cadastro')).not.toContain('ATIVO NOVO');
  });
});

describe('CabecalhoCadastroVeiculo: edição', () => {
  it('título e último item da trilha viram "Editar Veículo", com o texto de apoio da edição', () => {
    const html = renderizar('edicao');

    expect(html).toContain('Editar Veículo');
    expect(html).not.toContain('Cadastrar Novo Veículo');
    expect(html).not.toContain('Cadastrar Veículo');
    expect(html).toContain('Atualize os dados técnicos, comerciais e de aquisição do veículo.');
    expect(html).not.toContain('integrar o ativo à frota operacional');
  });

  it('sem texto de apoio (veículo não encontrado): o cabeçalho não mostra apoio nenhum', () => {
    const html = renderToStaticMarkup(
      <MemoryRouter>
        <CabecalhoCadastroVeiculo modo="edicao" caminhoFrota="/back-office/frota" mostrarApoio={false} />
      </MemoryRouter>,
    );

    expect(html).toContain('Editar Veículo');
    expect(html).not.toContain('Atualize os dados técnicos');
    expect(html).not.toContain('<p');
  });
});
