import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { ImagemVeiculo } from '@/components/ui';
import type { VeiculoRetorno } from '@/types/veiculos';
import { TabelaVeiculos } from './TabelaVeiculos';

// A tabela não chama a API: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));

// A imagem da capa vira espião que repassa tudo ao componente real: dá para conferir o `aoFalhar` ligado à tabela.
vi.mock('@/components/ui', async (importOriginal) => {
  const original = await importOriginal<typeof import('@/components/ui')>();
  return { ...original, ImagemVeiculo: vi.fn(original.ImagemVeiculo) };
});

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

function renderizar(parcial: Partial<Parameters<typeof TabelaVeiculos>[0]> = {}): string {
  return renderToStaticMarkup(
    <MemoryRouter>
      <TabelaVeiculos itens={[VEICULO]} carregando={false} vazio={<p>vazio da tela</p>} {...parcial} />
    </MemoryRouter>,
  );
}

describe('TabelaVeiculos: colunas', () => {
  it('traz as seis colunas do desenho, com os textos trocados pelo contrato', () => {
    const marcado = renderizar();

    for (const titulo of ['Veículo &amp; Grupo', 'Placa Mercosul', 'Chassi &amp; Ano', 'Km Atual', 'Status Operacional', 'Ações']) {
      expect(marcado).toContain(titulo);
    }
    expect(marcado).not.toContain('Km Atual &amp; Motor');
    expect(marcado).not.toContain('Pátio Atual');
  });

  it('o cabeçalho tem escopo de coluna e a tabela tem legenda para leitor de tela', () => {
    const marcado = renderizar();

    expect(marcado.match(/<th[^>]*scope="col"/g)?.length).toBe(6);
    expect(marcado).toContain('<caption class="sr-only">Veículos da frota</caption>');
  });

  it('não tem caixa de seleção nem coluna de pátio, câmbio ou motor', () => {
    const marcado = renderizar();

    expect(marcado).not.toContain('check_box_outline_blank');
    expect(marcado).not.toContain('Pátio');
    expect(marcado).not.toContain('Km Atual &amp; Motor');
  });
});

describe('TabelaVeiculos: dados da linha', () => {
  it('mostra marca e modelo, a categoria, a placa, o chassi com os anos, o km e a situação', () => {
    const marcado = renderizar();

    expect(marcado).toContain('Fiat Argo Drive 1.0');
    expect(marcado).toContain('Grupo B');
    expect(marcado).toContain('BRA2E19');
    expect(marcado).toContain('9BD1237A8P849102');
    expect(marcado).toContain('2023 / 2024');
    expect(marcado).toContain('24.850 km');
    expect(marcado).toContain('Disponível');
  });

  it('a descrição da categoria (da lista de categorias) aparece ao lado do grupo, como no desenho', () => {
    const marcado = renderizar({
      categorias: [
        { idCategoria: 2, nome: 'Grupo B', descricao: 'Hatch Econômico', valorDiariaBase: 150, valorDiariaBaseStr: 'R$ 150,00' },
      ],
    });

    expect(marcado).toContain('Hatch Econômico');
  });

  it('sem a categoria na lista, a linha mostra só o grupo (nenhuma descrição inventada)', () => {
    const marcado = renderizar({ categorias: [] });

    expect(marcado).toContain('Grupo B');
    expect(marcado).not.toContain('Hatch');
  });

  it('cada título do cabeçalho tem exatamente as classes de rótulo e o alinhamento (o texto pode quebrar linha)', () => {
    const marcado = renderizar();
    const cabecalhos = marcado.match(/<th(?=[\s>])[^>]*>/g) ?? [];

    expect(cabecalhos).toHaveLength(6);
    for (const cabecalho of cabecalhos) {
      expect(cabecalho).toMatch(
        /^<th scope="col" class="px-4 py-3 text-micro font-medium uppercase tracking-wider text-texto-apoio (text-left|text-right)">$/,
      );
    }
  });

  it('a placa antiga aparece com o hífen do formato de exibição', () => {
    const marcado = renderizar({ itens: [{ ...VEICULO, placa: 'ABC1234' }] });

    expect(marcado).toContain('ABC-1234');
  });

  it('situação em manutenção usa o texto do status do projeto', () => {
    const marcado = renderizar({ itens: [{ ...VEICULO, status: 'MANUTENCAO' }] });

    expect(marcado).toContain('Em manutenção');
    expect(marcado).toContain('text-frota-oficina');
  });
});

describe('TabelaVeiculos: botão Detalhes', () => {
  it('abre a edição do veículo (caminho da rota com o id) e tem nome acessível com a placa', () => {
    const marcado = renderizar();

    expect(marcado).toContain('href="/back-office/frota/veiculos/7/editar"');
    expect(marcado).toContain('Detalhes');
    expect(marcado).toContain('do veículo BRA2E19');
  });

  it('sem a propriedade acoesStatus (ex.: tela 12), não tem menu de três pontos e a coluna fica como antes', () => {
    const marcado = renderizar();

    expect(marcado).not.toContain('more_vert');
    expect(marcado).not.toContain('Menu Operacional');
    expect(marcado).not.toContain('Menu operacional do veículo');
    expect(marcado).toContain('>Detalhes<');
  });
});

describe('TabelaVeiculos: menu de troca de status (etapa 2)', () => {
  const ACOES_LIVRES = { idEmAndamento: null, aoEscolher: () => undefined };

  /** Trecho de abertura do botão do menu da linha (a tag inteira). */
  function botoesDoMenu(marcado: string): string[] {
    return marcado.match(/<button[^>]*title="Menu Operacional"[^>]*>/g) ?? [];
  }

  it('com acoesStatus, cada linha tem o botão ao lado do Detalhes, com o título e o nome com a placa', () => {
    const marcado = renderizar({ acoesStatus: ACOES_LIVRES });

    expect(botoesDoMenu(marcado)).toHaveLength(1);
    expect(marcado).toContain('aria-label="Menu operacional do veículo BRA2E19"');
    expect(marcado).toContain('aria-haspopup="menu"');
    expect(botoesDoMenu(marcado)[0]).toContain('h-8 w-8');
    expect(marcado).toContain('more_vert');
    expect(marcado).toContain('font-size:18px');
    expect(marcado.indexOf('>Detalhes<')).toBeLessThan(marcado.indexOf('Menu Operacional'));
  });

  it('o botão aparece uma vez por linha (duas linhas, dois botões)', () => {
    const marcado = renderizar({
      itens: [VEICULO, { ...VEICULO, idVeiculo: 8, placa: 'ONX9A11', status: 'MANUTENCAO' }],
      acoesStatus: ACOES_LIVRES,
    });

    expect(botoesDoMenu(marcado)).toHaveLength(2);
    expect(marcado).toContain('aria-label="Menu operacional do veículo ONX9A11"');
  });

  it('a linha com troca em andamento fica com o botão em aria-disabled (sem disabled, para o foco não se perder)', () => {
    const marcado = renderizar({ acoesStatus: { idEmAndamento: 7, aoEscolher: () => undefined } });

    expect(botoesDoMenu(marcado)[0]).toContain('aria-disabled="true"');
    expect(botoesDoMenu(marcado)[0]).not.toContain('disabled=""');
  });

  it('com um pedido em curso em outra linha, o botão desta linha também fica bloqueado (um pedido por vez)', () => {
    const marcado = renderizar({ acoesStatus: { idEmAndamento: 99, aoEscolher: () => undefined } });

    expect(botoesDoMenu(marcado)[0]).toContain('aria-disabled="true"');
  });

  it('sem pedido em curso, o botão está habilitado e fechado (aria-expanded false)', () => {
    const marcado = renderizar({ acoesStatus: ACOES_LIVRES });

    expect(botoesDoMenu(marcado)[0]).not.toContain('aria-disabled');
    expect(botoesDoMenu(marcado)[0]).toContain('aria-expanded="false"');
  });

  it('o menu fechado não aparece no HTML (só abre por clique)', () => {
    const marcado = renderizar({ acoesStatus: ACOES_LIVRES });

    expect(marcado).not.toContain('role="menu"');
    expect(marcado).not.toContain('Enviar para manutenção');
  });

  it('linha em RESERVADO ou LOCADO também tem o botão (o menu mostra a frase)', () => {
    const marcado = renderizar({ itens: [{ ...VEICULO, status: 'LOCADO' }], acoesStatus: ACOES_LIVRES });

    expect(botoesDoMenu(marcado)).toHaveLength(1);
    expect(botoesDoMenu(marcado)[0]).not.toContain('aria-haspopup');
  });
});

describe('TabelaVeiculos: capa com e sem foto', () => {
  it('com capa, a imagem usa a URL da capa', () => {
    const marcado = renderizar({
      itens: [{ ...VEICULO, fotoCapa: { url: 'https://arquivos.exemplo/capa.jpg', urlMiniatura: null, expiraEm: '2026-11-19T12:00:00Z' } }],
    });

    expect(marcado).toContain('src="https://arquivos.exemplo/capa.jpg"');
    expect(marcado).toContain('alt="Foto do veículo Fiat Argo Drive 1.0"');
  });

  it('sem capa (ou com a foto sem arquivo), mostra o ícone de carro e nenhuma imagem', () => {
    const marcado = renderizar();

    expect(marcado).not.toContain('<img');
    expect(marcado).toContain('directions_car');
  });

  it('foto com falha: a tabela liga a falha da capa ao callback da tela (que refaz a consulta)', () => {
    const aoFalharFotoCapa = vi.fn();
    vi.mocked(ImagemVeiculo).mockClear();
    renderizar({ aoFalharFotoCapa });

    const props = vi.mocked(ImagemVeiculo).mock.calls[0]?.[0];
    expect(props?.aoFalhar).toBeTypeOf('function');
    props?.aoFalhar?.();
    expect(aoFalharFotoCapa).toHaveBeenCalledTimes(1);
  });
});

describe('TabelaVeiculos: estados', () => {
  it('carregando: linhas de esqueleto, sem dados e sem o vazio', () => {
    const marcado = renderizar({ itens: [], carregando: true });

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('vazio da tela');
    expect(marcado).not.toContain('Detalhes');
    expect(marcado.match(/animate-pulse/g)?.length).toBe(5 * 6);
  });

  it('sem linhas (e sem carregar): mostra o vazio da tela', () => {
    const marcado = renderizar({ itens: [] });

    expect(marcado).toContain('vazio da tela');
  });
});
