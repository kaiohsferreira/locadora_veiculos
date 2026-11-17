import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AvisoProvider } from '@/components/ui';
import { valoresIniciaisCadastro } from '@/lib/conversaoVeiculo';
import type { ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import { useCadastrarVeiculo } from '@/features/frota/hooks/useCadastrarVeiculo';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { useEditarVeiculo } from '@/features/frota/hooks/useEditarVeiculo';
import { useRascunhoVeiculo } from '@/features/frota/hooks/useRascunhoVeiculo';
import { useVeiculo } from '@/features/frota/hooks/useVeiculo';
import CadastroVeiculoPagina from './CadastroVeiculoPagina';

// Nenhum teste chama a API: os hooks de dados viram dublês. O bloco de fotos tem a própria cobertura.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));
vi.mock('@/features/frota/hooks/useCategorias', () => ({ useCategorias: vi.fn() }));
vi.mock('@/features/frota/hooks/useVeiculo', () => ({ useVeiculo: vi.fn() }));
vi.mock('@/features/frota/hooks/useCadastrarVeiculo', () => ({ useCadastrarVeiculo: vi.fn() }));
vi.mock('@/features/frota/hooks/useEditarVeiculo', () => ({ useEditarVeiculo: vi.fn() }));
vi.mock('@/features/frota/hooks/useRascunhoVeiculo', () => ({ useRascunhoVeiculo: vi.fn() }));
vi.mock('@/features/frota/componentes/BlocoFotosVeiculo', () => ({ BlocoFotosVeiculo: () => null }));

type Categorias = ReturnType<typeof useCategorias>;
type Veiculo = ReturnType<typeof useVeiculo>;
type Cadastro = ReturnType<typeof useCadastrarVeiculo>;
type Edicao = ReturnType<typeof useEditarVeiculo>;
type Rascunho = ReturnType<typeof useRascunhoVeiculo>;

function categoriasPadrao(): Categorias {
  return {
    data: {
      itens: [{ idCategoria: 1, nome: 'Grupo B', descricao: 'Econômico', valorDiariaBase: 90, valorDiariaBaseStr: 'R$ 90,00' }],
      total: 1,
      pagina: 1,
      tamanhoPagina: 100,
    },
    isPending: false,
    isError: false,
    refetch: vi.fn(),
  } as unknown as Categorias;
}

function veiculoSemDados(): Veiculo {
  return { data: undefined, isPending: true, isError: false, error: null, refetch: vi.fn() } as unknown as Veiculo;
}

function cadastroParado(): Cadastro {
  return { mutate: vi.fn(), isPending: false } as unknown as Cadastro;
}

function edicaoParada(): Edicao {
  return { mutate: vi.fn(), isPending: false } as unknown as Edicao;
}

function rascunhoSem(): Rascunho {
  return { rascunhoInicial: null, salvar: vi.fn(() => true), apagar: vi.fn() };
}

const RASCUNHO: ValoresFormularioVeiculo = { ...valoresIniciaisCadastro(), placa: 'BRA2E19', marca: 'Fiat' };

/** Renderiza a página na rota dada, com os mesmos provedores que a aplicação usa nas telas. */
function renderizar(caminho: string): string {
  const cliente = new QueryClient();
  return renderToStaticMarkup(
    <QueryClientProvider client={cliente}>
      <AvisoProvider>
        <MemoryRouter initialEntries={[caminho]}>
          <Routes>
            <Route path={CAMINHOS.frotaEditarVeiculo} element={<CadastroVeiculoPagina />} />
            <Route path={CAMINHOS.frotaNovoVeiculo} element={<CadastroVeiculoPagina />} />
          </Routes>
        </MemoryRouter>
      </AvisoProvider>
    </QueryClientProvider>,
  );
}

describe('CadastroVeiculoPagina: cadastro', () => {
  beforeEach(() => {
    vi.mocked(useCategorias).mockReturnValue(categoriasPadrao());
    vi.mocked(useVeiculo).mockReturnValue(veiculoSemDados());
    vi.mocked(useCadastrarVeiculo).mockReturnValue(cadastroParado());
    vi.mocked(useEditarVeiculo).mockReturnValue(edicaoParada());
    vi.mocked(useRascunhoVeiculo).mockReturnValue(rascunhoSem());
  });

  it('nasce vazia: um único título, todos os rótulos e os botões do desenho', () => {
    const html = renderizar(CAMINHOS.frotaNovoVeiculo);
    expect(html.match(/<h1/g)).toHaveLength(1);
    expect(html).toContain('Cadastrar Novo Veículo');
    for (const rotulo of [
      'Placa (Mercosul)',
      'Chassi (VIN - 17 Dígitos)',
      'Renavam (11 Dígitos)',
      'Marca Fabricante',
      'Modelo e Versão Completa',
      'Ano Fabricação',
      'Ano Modelo',
      'Cor Externa / Acabamento',
      'Grupo Operacional / Categoria',
      'Valor Base da Diária (Locação)',
      'Valor Referência Seminovos (Desmobilização)',
      'Data da Compra',
      'Odômetro de Entrega',
    ]) {
      expect(html).toContain(rotulo);
    }
    expect(html).toContain('Salvar e Ativar Veículo');
    expect(html).toContain('Salvar como Rascunho');
    expect(html).toContain('Cancelar / Voltar');
  });

  it('não marca cor nem seleciona marca ou categoria de exemplo', () => {
    const html = renderizar(CAMINHOS.frotaNovoVeiculo);
    expect(html).not.toContain('checked=""');
    // "Selecione" (valor vazio) é a opção marcada; Jeep (marca) e o Grupo G (categoria) do desenho não aparecem marcados.
    expect(html).not.toMatch(/<option value="(Jeep|1)"[^>]*selected/);
  });

  it('com rascunho deste navegador: avisa e oferece descartar', () => {
    vi.mocked(useRascunhoVeiculo).mockReturnValue({ ...rascunhoSem(), rascunhoInicial: RASCUNHO });
    const html = renderizar(CAMINHOS.frotaNovoVeiculo);
    expect(html).toContain('Encontramos um rascunho salvo neste navegador');
    expect(html).toContain('Descartar rascunho');
    expect(html).toContain('value="BRA2E19"');
  });

  it('categorias carregando: aviso no seletor e seletor travado', () => {
    vi.mocked(useCategorias).mockReturnValue({
      data: undefined,
      isPending: true,
      isError: false,
      refetch: vi.fn(),
    } as unknown as Categorias);
    const html = renderizar(CAMINHOS.frotaNovoVeiculo);
    expect(html).toContain('Carregando categorias...');
  });

  it('categorias com erro: mensagem própria e botão "Tentar de novo"', () => {
    vi.mocked(useCategorias).mockReturnValue({
      data: undefined,
      isPending: false,
      isError: true,
      refetch: vi.fn(),
    } as unknown as Categorias);
    const html = renderizar(CAMINHOS.frotaNovoVeiculo);
    expect(html).toContain('Não foi possível carregar as categorias.');
    expect(html).toContain('Tentar de novo');
  });

  it('categorias vazias: texto próprio, não um seletor em branco', () => {
    vi.mocked(useCategorias).mockReturnValue({
      data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 100 },
      isPending: false,
      isError: false,
      refetch: vi.fn(),
    } as unknown as Categorias);
    const html = renderizar(CAMINHOS.frotaNovoVeiculo);
    expect(html).toContain('Nenhuma categoria ativa cadastrada.');
  });
});

describe('CadastroVeiculoPagina: edição', () => {
  beforeEach(() => {
    vi.mocked(useCategorias).mockReturnValue(categoriasPadrao());
    vi.mocked(useCadastrarVeiculo).mockReturnValue(cadastroParado());
    vi.mocked(useEditarVeiculo).mockReturnValue(edicaoParada());
    vi.mocked(useRascunhoVeiculo).mockReturnValue(rascunhoSem());
  });

  it('carregando o veículo: título de edição e aviso de carregamento, sem formulário', () => {
    vi.mocked(useVeiculo).mockReturnValue(veiculoSemDados());
    const html = renderizar(CAMINHOS.frotaEditarVeiculo.replace(':idVeiculo', '12'));
    expect(html).toContain('Editar Veículo');
    expect(html).toContain('Atualize os dados técnicos, comerciais e de aquisição do veículo.');
    expect(html).toContain('Carregando...');
    expect(html).not.toContain('Salvar Alterações');
    expect(html).not.toContain('Placa (Mercosul)');
  });

  it('veículo que não existe (404): mensagem de não encontrado, sem texto de apoio e sem formulário', () => {
    vi.mocked(useVeiculo).mockReturnValue({
      data: undefined,
      isPending: false,
      isError: true,
      error: new ErroApi({ status: 404, mensagem: 'O veículo informado não foi encontrado.' }),
      refetch: vi.fn(),
    } as unknown as Veiculo);
    const html = renderizar(CAMINHOS.frotaEditarVeiculo.replace(':idVeiculo', '12'));
    expect(html).toContain('Veículo não encontrado.');
    expect(html).not.toContain('Atualize os dados técnicos');
    expect(html).not.toContain('Placa (Mercosul)');
  });

  it('id que não é número válido: não existe, sem consulta e sem texto de apoio', () => {
    vi.mocked(useVeiculo).mockReturnValue(veiculoSemDados());
    const html = renderizar(CAMINHOS.frotaEditarVeiculo.replace(':idVeiculo', 'abc'));
    expect(html).toContain('Veículo não encontrado.');
    expect(html).not.toContain('Atualize os dados técnicos');
    expect(html).not.toContain('Placa (Mercosul)');
  });
});
