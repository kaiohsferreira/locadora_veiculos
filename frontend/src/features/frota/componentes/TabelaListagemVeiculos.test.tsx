import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import type { VeiculoRetorno } from '@/types/veiculos';
import { TabelaListagemVeiculos } from './TabelaListagemVeiculos';

// A tabela não chama a API: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const VEICULO: VeiculoRetorno = {
  idVeiculo: 7,
  idCategoria: 2,
  categoriaNome: 'Grupo B',
  placa: 'BRA2E19',
  chassi: '9BD1237A8P849102',
  renavam: '00984521360',
  marca: 'Fiat',
  modelo: 'Argo Drive 1.0',
  anoFabricacao: 2023,
  anoModelo: 2024,
  cor: 'Prata',
  kmAtual: 24850,
  valorDiaria: 229,
  valorDiariaStr: 'R$ 229,00',
  valorVenda: null,
  valorVendaStr: '',
  status: 'DISPONIVEL',
  dataAquisicao: null,
  dataAquisicaoStr: '',
  fornecedor: null,
  numeroNotaFiscalCompra: null,
  locadoraNomeFantasia: 'Rota Sul',
  locadoraCidade: 'Campinas',
  locadoraUf: 'SP',
  fotoCapa: null,
  erroFotoCapa: null,
};

type Parametros = Partial<Parameters<typeof TabelaListagemVeiculos>[0]>;

function renderizar(parcial: Parametros = {}): string {
  return renderToStaticMarkup(
    <MemoryRouter>
      <TabelaListagemVeiculos itens={[VEICULO]} carregando={false} vazio={<p>vazio da tela</p>} {...parcial} />
    </MemoryRouter>,
  );
}

describe('TabelaListagemVeiculos: colunas do desenho 12', () => {
  it('traz as seis colunas com os textos do desenho', () => {
    const marcado = renderizar();

    for (const titulo of [
      'Identificação / Placa',
      'Modelo &amp; Especificação',
      'Categoria',
      'Odômetro',
      'Status Operacional',
      'Ações',
    ]) {
      expect(marcado).toContain(titulo);
    }
    expect(marcado.match(/<th[^>]*scope="col"/g)?.length).toBe(6);
  });

  it('não tem olho (Ver detalhes), caixa de seleção, pátio, câmbio nem motor', () => {
    const marcado = renderizar();

    expect(marcado).not.toContain('Ver Detalhes');
    expect(marcado).not.toContain('>visibility<');
    expect(marcado).not.toContain('check_box_outline_blank');
    expect(marcado.toLowerCase()).not.toContain('pátio');
    expect(marcado).not.toContain('Km Atual &amp; Motor');
  });

  it('a coluna de ações fica por último e não some (tem nowrap e alinhamento à direita)', () => {
    const marcado = renderizar();

    expect(marcado).toMatch(/<th[^>]*text-right[^>]*>Ações<\/th>/);
    expect(marcado).toMatch(/<th[^>]*whitespace-nowrap[^>]*text-right[^>]*>Ações<\/th>/);
  });
});

describe('TabelaListagemVeiculos: dados da linha', () => {
  it('placa no emblema, modelo com marca, ano e chassi abreviado, categoria, km e situação', () => {
    const marcado = renderizar();

    expect(marcado).toContain('BRA2E19');
    expect(marcado).toContain('Fiat Argo Drive 1.0');
    expect(marcado).toContain('2024 • Chassi: 9BD12...9102');
    expect(marcado).toContain('Grupo B');
    expect(marcado).toContain('24.850 km');
    expect(marcado).toContain('Disponível');
  });

  it('o chassi inteiro fica no title da linha do chassi (e não aparece no texto visível)', () => {
    const marcado = renderizar();

    expect(marcado).toContain('title="9BD1237A8P849102"');
    expect(marcado).not.toContain('>9BD1237A8P849102<');
  });

  it('a capa sem foto mostra o ícone de carro (sem url)', () => {
    const marcado = renderizar();

    expect(marcado).toContain('directions_car');
  });

  it('situação em manutenção usa o texto do status do projeto', () => {
    const marcado = renderizar({ itens: [{ ...VEICULO, status: 'MANUTENCAO' }] });

    expect(marcado).toContain('Em manutenção');
  });
});

describe('TabelaListagemVeiculos: ações da linha', () => {
  it('o lápis abre o cadastro do veículo, com nome acessível pela placa, para todo funcionário', () => {
    const marcado = renderizar();

    expect(marcado).toContain('href="/back-office/frota/veiculos/7/editar"');
    expect(marcado).toContain('aria-label="Editar veículo BRA2E19"');
    expect(marcado).toContain('title="Editar"');
  });

  it('sem o menu (quem não pode trocar status), a linha tem só o lápis e nenhum menu', () => {
    const marcado = renderizar();

    expect(marcado).not.toContain('Menu operacional do veículo');
    expect(marcado).not.toContain('title="Menu Operacional"');
    expect(marcado).toContain('href="/back-office/frota/veiculos/7/editar"');
  });

  it('com o menu (quem pode trocar status), cada linha tem o botão de três pontos com o nome da placa', () => {
    const marcado = renderizar({ acoesStatus: { idEmAndamento: null, aoEscolher: () => undefined } });

    expect(marcado).toContain('aria-label="Menu operacional do veículo BRA2E19"');
    expect(marcado).toContain('title="Menu Operacional"');
    expect(marcado.indexOf('title="Editar"')).toBeLessThan(marcado.indexOf('title="Menu Operacional"'));
  });
});

describe('TabelaListagemVeiculos: estados', () => {
  it('carregando: linhas de esqueleto e aria-busy', () => {
    const marcado = renderizar({ carregando: true, itens: [] });

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('Fiat Argo');
  });

  it('vazio: mostra o conteúdo de vazio na linha', () => {
    const marcado = renderizar({ itens: [] });

    expect(marcado).toContain('vazio da tela');
  });
});
