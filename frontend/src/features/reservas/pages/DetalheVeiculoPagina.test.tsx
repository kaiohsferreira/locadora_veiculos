import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { Route, Routes } from 'react-router-dom';
import { StaticRouter } from 'react-router-dom/server.js';
import { ErroApi } from '@/types/api';
import type { VeiculoLocacao } from '@/types/veiculos';
import { useDetalheVeiculo } from '../hooks/useDetalheVeiculo';
import DetalheVeiculoPagina from './DetalheVeiculoPagina';

vi.mock('../hooks/useDetalheVeiculo', () => ({ useDetalheVeiculo: vi.fn() }));

const CONSULTA = 'cidade=S%C3%A3o+Paulo&uf=SP&idCategoria=3&pagina=2&dataRetirada=2099-06-10&dataDevolucao=2099-06-14';
const VEICULO: VeiculoLocacao = {
  idVeiculo: 7,
  idCategoria: 3,
  categoriaNome: 'SUVs',
  marca: 'Jeep',
  modelo: 'Renegade',
  anoFabricacao: 2024,
  anoModelo: 2025,
  cor: 'Prata',
  valorDiaria: 229,
  valorDiariaStr: 'R$ 229,00',
  locadoraNomeFantasia: 'Locadora Paulista',
  locadoraCidade: 'São Paulo',
  locadoraUf: 'SP',
  disponivelNoPeriodo: true,
  fotos: [
    { url: 'https://fotos.example/principal', urlMiniatura: 'https://fotos.example/thumb', expiraEm: '2099-06-10T10:00:00Z' },
    { url: 'https://fotos.example/lateral', urlMiniatura: null, expiraEm: '2099-06-10T10:00:00Z' },
  ],
  erroFotos: null,
};

function simularConsulta(parcial: Partial<ReturnType<typeof useDetalheVeiculo>>) {
  vi.mocked(useDetalheVeiculo).mockReturnValue({
    isPending: false, isFetching: false, fetchStatus: 'idle', isError: false, refetch: vi.fn(), ...parcial,
  } as ReturnType<typeof useDetalheVeiculo>);
}

function renderizar(consulta = CONSULTA, id = '7') {
  return renderToStaticMarkup(
    <StaticRouter location={`/veiculos/${id}?${consulta}`}>
      <Routes>
        <Route path="/veiculos/:veiculoId" element={<DetalheVeiculoPagina />} />
      </Routes>
    </StaticRouter>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  simularConsulta({ data: VEICULO });
});

describe('DetalheVeiculoPagina', () => {
  it('consulta as datas da URL, exibe ficha e galeria e encaminha o período confirmado', () => {
    const html = renderizar();
    expect(useDetalheVeiculo).toHaveBeenCalledWith(7, { dataRetirada: '2099-06-10', dataDevolucao: '2099-06-14' });
    expect(html).toContain('<h1');
    expect(html).toContain('Jeep Renegade');
    expect(html).toContain('Ficha do veículo');
    expect(html).toContain('Ano de fabricação');
    expect(html).toContain('Locadora Paulista');
    expect(html).toContain('src="https://fotos.example/principal"');
    expect(html).toContain('src="https://fotos.example/thumb"');
    expect(html).toContain('src="https://fotos.example/lateral"');
    expect(html).toContain('aria-label="Exibir foto 2" aria-pressed="false"');
    expect(html).toContain('Disponível no período selecionado.');
    expect(html).toContain('4 diárias');
    expect(html).toMatch(/R\$\s916,00/);
    expect(html).toContain('href="/reservas/checkout/7?dataRetirada=2099-06-10&amp;dataDevolucao=2099-06-14"');
    expect(html).toContain(`href="/veiculos?${CONSULTA.replaceAll('&', '&amp;')}"`);
  });

  it('diferencia períodos da URL e calcula o total usando a diária numérica em centavos', () => {
    simularConsulta({ data: { ...VEICULO, valorDiaria: 149.90, valorDiariaStr: 'R$ 149,90' } });
    const html = renderizar('dataRetirada=2099-07-01&dataDevolucao=2099-07-05');
    expect(useDetalheVeiculo).toHaveBeenCalledWith(7, { dataRetirada: '2099-07-01', dataDevolucao: '2099-07-05' });
    expect(html).toMatch(/R\$\s599,60/);
  });

  it('mostra diária e solicita as datas em acesso direto', () => {
    simularConsulta({ data: { ...VEICULO, disponivelNoPeriodo: null } });
    const html = renderizar('');
    expect(useDetalheVeiculo).toHaveBeenCalledWith(7, undefined);
    expect(html).toContain('R$ 229,00');
    expect(html).toContain('Informe as datas para consultar a disponibilidade.');
    expect(html).not.toContain('Total estimado');
    expect(html).not.toContain('/reservas/checkout');
  });

  it.each([
    'dataRetirada=2099-06-10',
    'dataRetirada=2099-06-14&dataDevolucao=2099-06-10',
    'dataRetirada=2099-06-31&dataDevolucao=2099-07-05',
    'dataRetirada=2000-06-10&dataDevolucao=2000-06-14',
  ])('ignora datas inválidas na consulta e exibe erro de campo (%s)', (consulta) => {
    simularConsulta({ data: { ...VEICULO, disponivelNoPeriodo: null } });
    const html = renderizar(consulta);
    expect(useDetalheVeiculo).toHaveBeenCalledWith(7, undefined);
    expect(html).toContain('aria-invalid="true"');
    expect(html).not.toContain('/reservas/checkout');
  });

  it('preserva ficha e fotos em conflito e orienta ajustar as datas', () => {
    simularConsulta({ data: { ...VEICULO, disponivelNoPeriodo: false } });
    const html = renderizar();
    expect(html).toContain('Ficha do veículo');
    expect(html).toContain('https://fotos.example/principal');
    expect(html).toContain('Veículo indisponível nesse período. Consulte outras datas.');
    expect(html).not.toContain('/reservas/checkout');
  });

  it('mantém ficha e galeria em reconsulta, sem exibir preço ou disponibilidade enquanto busca', () => {
    simularConsulta({ data: VEICULO, isFetching: true, fetchStatus: 'fetching' });
    const html = renderizar();
    expect(html).toContain('Ficha do veículo');
    expect(html).toContain('https://fotos.example/principal');
    expect(html).toContain('Consultando disponibilidade');
    expect(html).not.toContain('R$ 229,00');
    expect(html).not.toContain('/reservas/checkout');
  });

  it('mantém a ficha em cache e aguarda a conexão sem liberar checkout durante consulta pausada', () => {
    simularConsulta({ data: VEICULO, fetchStatus: 'paused', isPaused: true });
    const html = renderizar();
    expect(html).toContain('Aguardando conexão para concluir a consulta.');
    expect(html).toContain('Ficha do veículo');
    expect(html).not.toContain('Disponível no período selecionado.');
    expect(html).not.toContain('Total estimado');
    expect(html).not.toContain('/reservas/checkout');
  });

  it('preserva os centavos quando o total excede o limite inteiro seguro de Number', () => {
    simularConsulta({ data: { ...VEICULO, valorDiaria: 99_999_999.99, valorDiariaStr: 'R$ 99.999.999,99' } });
    const html = renderizar('dataRetirada=2099-06-10&dataDevolucao=9999-12-31');
    expect(html).toMatch(/R\$\s288\.561\.899\.971\.143,81/);
  });

  it('mostra ausência de fotos e mantém a ficha útil; avisa sobre falha parcial sem ocultar foto disponível', () => {
    simularConsulta({ data: { ...VEICULO, cor: null, fotos: [] } });
    const semFotos = renderizar();
    expect(semFotos).toContain('Nenhuma foto disponível.');
    expect(semFotos).toContain('directions_car');
    expect(semFotos).toContain('Ficha do veículo');
    expect(semFotos).not.toContain('>Cor</dt>');
    expect(semFotos).not.toContain('Exibir foto');

    simularConsulta({ data: { ...VEICULO, erroFotos: 'Algumas fotos não puderam ser carregadas.' } });
    const parcial = renderizar();
    expect(parcial).toContain('Algumas fotos não puderam ser carregadas.');
    expect(parcial).toContain('https://fotos.example/principal');
  });

  it('exibe carregamento e permite repetir a consulta em caso de falha', () => {
    simularConsulta({ isPending: true, isFetching: true });
    expect(renderizar()).toContain('Carregando detalhes do veículo');

    simularConsulta({ isError: true, error: new ErroApi({ status: 500, mensagem: 'Tente em instantes.' }) });
    const html = renderizar();
    expect(html).toContain('Não foi possível carregar os detalhes do veículo.');
    expect(html).toContain('Tentar novamente');
  });

  it('orienta voltar à busca para 404 e evita consultar identificadores inválidos', () => {
    simularConsulta({ isError: true, error: new ErroApi({ status: 404, mensagem: 'Falha.' }) });
    const html = renderizar();
    expect(html).toContain('Veículo indisponível para locação');
    expect(html).toContain('Voltar à busca');
    expect(html).not.toContain('Tentar novamente');

    vi.mocked(useDetalheVeiculo).mockClear();
    expect(renderizar('', '2147483648')).toContain('Veículo indisponível para locação');
    expect(useDetalheVeiculo).not.toHaveBeenCalled();
  });
});
