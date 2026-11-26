import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { ErroApi } from '@/types/api';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import type { ContextoVistoriaEntrada, VistoriaEntrada } from '@/types/vistorias';
import { useRegistrarVistoriaEntrada, useVistoriaEntrada } from '../hooks/useVistoriaEntrada';
import type { ItemAvariaLocal } from '../vistoria/avariasVistoria';
import { useAvariasVistoria, useEnvioAvarias, useVeiculoDaVistoria } from '../vistoria/useAvariasVistoria';
import VistoriaAvariasPagina from './VistoriaAvariasPagina';

vi.mock('../hooks/useVistoriaEntrada', () => ({
  useVistoriaEntrada: vi.fn(),
  useRegistrarVistoriaEntrada: vi.fn(),
}));
vi.mock('../vistoria/useAvariasVistoria', () => ({
  useAvariasVistoria: vi.fn(),
  useVeiculoDaVistoria: vi.fn(),
  useEnvioAvarias: vi.fn(),
}));
vi.mock('@/routes/autenticacao/useAutenticacao', () => ({
  useAutenticacao: () => ({ usuario: { nome: 'Carlos Mendes', perfil: 'ATENDENTE' } }),
}));

type Consulta = ReturnType<typeof useVistoriaEntrada>;
type Registro = ReturnType<typeof useRegistrarVistoriaEntrada>;
type Avarias = ReturnType<typeof useAvariasVistoria>;
type Veiculo = ReturnType<typeof useVeiculoDaVistoria>;
type Envio = ReturnType<typeof useEnvioAvarias>;

const contexto: ContextoVistoriaEntrada = {
  locacao: {
    idLocacao: 42, idReserva: 8, idCliente: 9, idVeiculo: 3, status: 'EM_ANDAMENTO',
    dataRetiradaStr: '17/11/2026 09:00', dataDevolucaoPrevistaStr: '19/11/2026 09:00', kmSaida: 12000,
  },
  vistoria: null,
};

const AVARIA_DO_SERVIDOR: AvariaRegistrada = {
  idAvaria: 5, idVistoria: 2, descricao: 'Risco superficial', gravidade: 'LEVE', valorEstimado: 180,
  angulo: 'LATERAL_DIREITA', posX: 80, posY: 40, foto: null, erroFoto: 'texto do servidor',
};

const DADOS_LOCAIS: DadosNovaAvaria = {
  descricao: 'Risco na porta', gravidade: 'LEVE', valorEstimado: '120.00', angulo: 'LATERAL_DIREITA', posX: '80.00', posY: '40.00',
  foto: new File(['x'], 'foto.jpg', { type: 'image/jpeg' }),
};

function envioVazio(parcial: Partial<Envio> = {}): Envio {
  return {
    locais: [], enviando: false, progresso: null, falhou: false,
    adicionar: vi.fn(), redefinir: vi.fn(), marcarNaoEnviadas: vi.fn(), enviarPendentes: vi.fn(), tentarDeNovo: vi.fn(), substituirFotoGravada: vi.fn(),
    ...parcial,
  };
}

function renderizar(parcial: Partial<Consulta>, id = '42', avarias: Partial<Avarias> = {}, envio: Partial<Envio> = {}): string {
  vi.mocked(useVistoriaEntrada).mockReturnValue({
    isPending: false, isError: false, isFetching: false, refetch: vi.fn(), data: contexto,
    ...parcial,
  } as unknown as Consulta);
  vi.mocked(useRegistrarVistoriaEntrada).mockReturnValue({
    isPending: false, isError: false, mutateAsync: vi.fn(),
  } as unknown as Registro);
  vi.mocked(useAvariasVistoria).mockReturnValue({
    data: [], isPending: false, isError: false, isFetching: false, refetch: vi.fn(), ...avarias,
  } as unknown as Avarias);
  vi.mocked(useVeiculoDaVistoria).mockReturnValue({
    data: { marca: 'Jeep', modelo: 'Renegade', placa: 'BRA2E19' }, isPending: false, isError: false,
  } as unknown as Veiculo);
  vi.mocked(useEnvioAvarias).mockReturnValue(envioVazio(envio));
  return renderToStaticMarkup(
    <AvisoProvider><MemoryRouter initialEntries={[`/vistoria/${id}`]}>
      <Routes><Route path="/vistoria/:locacaoId" element={<VistoriaAvariasPagina />} /></Routes>
    </MemoryRouter></AvisoProvider>,
  );
}

describe('tela 19 de vistoria de entrada com avarias', () => {
  beforeEach(() => vi.clearAllMocks());

  it('sem vistoria: formulário de odômetro com os textos aprovados e o título da tela', () => {
    const html = renderizar({});
    expect(html).toContain('Vistoria e Inspeção do Veículo');
    expect(html).toContain('<form');
    expect(html).toContain('Quilometragem Atual (Odômetro)');
    expect(html).toContain('aria-required="true"');
    expect(html).toContain('0,5 para meio tanque');
    expect(html).toContain('Observações (opcional)');
    expect(html).toContain('Registrar vistoria de entrada');
    expect(html).toContain('Odômetro e Combustível');
    expect(html).not.toContain('type="file"');
  });

  it('mostra o km de saída da locação e a faixa do veículo, sem a placa de outra locação', () => {
    const html = renderizar({});
    expect(html).toContain('Quilometragem na retirada');
    expect(html).toContain('12.000 km');
    expect(html).toContain('Jeep Renegade');
    expect(html).toContain('BRA2E19');
    expect(html).toContain('nº 42');
  });

  it('trilha com a vistoria em andamento e o vistoriador logado antes do registro', () => {
    const html = renderizar({});
    expect(html).toContain('Etapa 2 • Em Andamento');
    expect(html).toContain('Vistoriador Responsável');
    expect(html).toContain('Carlos Mendes');
  });

  it('mapa sem vistoria: quatro quadrantes com botão de marcar e o botão Adicionar Avaria', () => {
    const html = renderizar({});
    expect(html).toContain('1. Vista Frontal (Frente)');
    expect(html).toContain('4. Lateral Direita (Passageiro)');
    expect(html).toContain('Marcar avaria na Frente');
    expect(html).toContain('Adicionar Avaria');
    expect(html).toContain('Sem Avarias');
  });

  it('carregamento não abre formulário nem mapa editável', () => {
    const html = renderizar({ isPending: true, data: undefined });
    expect(html).not.toContain('<form');
    expect(html).not.toContain('Marcar avaria na');
  });

  it.each([404, 403, 500])('erro de consulta %s não abre formulário, mesmo com dados antigos', (status) => {
    const html = renderizar({ isError: true, error: new ErroApi({ status, mensagem: 'Não foi possível carregar.' }) });
    expect(html).not.toContain('<form');
    expect(html).toContain('Tentar novamente');
  });

  it('id inválido não abre formulário e desabilita consulta pelo id nulo', () => {
    expect(renderizar({}, '0')).not.toContain('<form');
    expect(useVistoriaEntrada).toHaveBeenCalledWith(null);
  });

  it('vistoria já registrada: leitura, mapa sem marcação, avarias do servidor e botão de avançar para o contrato', () => {
    const html = renderizar(
      { data: { ...contexto, vistoria: {
        idVistoria: 2, idLocacao: 42, idFuncionario: 7, tipo: 'ENTRADA', dataHoraStr: '17/11/2026 10:00',
        kmRegistrado: 12000, nivelCombustivel: null, observacoes: '<script>teste</script>',
      } } },
      '42',
      { data: [AVARIA_DO_SERVIDOR] },
    );
    expect(html).not.toContain('<form');
    expect(html).toContain('<output');
    expect(html).toContain('12.000 km');
    expect(html).toContain('Não informado');
    expect(html).toContain('&lt;script&gt;teste&lt;/script&gt;');
    expect(html).toContain('Etapa 2 • Concluído');
    expect(html).toContain('Avançar para Emissão de Contrato');
    expect(html).not.toContain('Marcar avaria na');
    expect(html).not.toContain('Adicionar Avaria');
    expect(html).toContain('Lateral Direita');
    expect(html).toContain('Custo Estimado Reparo:');
    expect(html).toContain('180,00');
    expect(html).toContain('Não foi possível carregar esta foto. Se o problema continuar, remova a foto e envie de novo.');
    expect(html).toContain('1 Avaria Mapeada');
    // Avaria gravada sem arquivo: o campo de nova foto aparece na leitura (troca só por PUT).
    expect(html).toContain('type="file"');
  });

  it('vistoria registrada com avaria local que falhou no envio: Tentar de novo e o aviso de envio parcial', () => {
    const item: ItemAvariaLocal = {
      chave: 1, dados: DADOS_LOCAIS, previa: 'blob:previa-1', situacao: 'foto_pendente', idAvaria: 9,
    };
    const html = renderizar(
      { data: { ...contexto, vistoria: {
        idVistoria: 2, idLocacao: 42, idFuncionario: 7, tipo: 'ENTRADA', dataHoraStr: '17/11/2026 10:00',
        kmRegistrado: 12000, nivelCombustivel: 0.5, observacoes: null,
      } } },
      '42',
      { data: [] },
      { locais: [item], falhou: true },
    );
    expect(html).not.toContain('<form');
    expect(html).not.toContain('Marcar avaria na');
    expect(html).toContain('Tentar de novo');
    expect(html).toContain('A avaria foi registrada, mas a foto não foi enviada. Envie a foto de novo.');
    expect(html).toContain('Algumas avarias não foram enviadas. Tente de novo.');
  });

  it('erro na lista sem dados carregados: só o erro, sem lista', () => {
    const html = renderizar(
      { data: { ...contexto, vistoria: {
        idVistoria: 2, idLocacao: 42, idFuncionario: 7, tipo: 'ENTRADA', dataHoraStr: '17/11/2026 10:00',
        kmRegistrado: 12000, nivelCombustivel: 0.5, observacoes: null,
      } } },
      '42',
      { isError: true, data: undefined, error: new ErroApi({ status: 500, mensagem: 'x' }) },
    );
    expect(html).toContain('Não foi possível carregar as avarias.');
    expect(html).not.toContain('Nenhuma avaria registrada nesta vistoria.');
  });

  it('erro ao carregar as avarias da vistoria registrada mostra o erro da lista, sem derrubar a leitura', () => {
    const html = renderizar(
      { data: { ...contexto, vistoria: {
        idVistoria: 2, idLocacao: 42, idFuncionario: 7, tipo: 'ENTRADA', dataHoraStr: '17/11/2026 10:00',
        kmRegistrado: 12000, nivelCombustivel: 0.5, observacoes: null,
      } } },
      '42',
      { isError: true, error: new ErroApi({ status: 500, mensagem: 'x' }) },
    );
    expect(html).toContain('Não foi possível carregar as avarias.');
    expect(html).toContain('<output');
    expect(html).toContain('Odômetro e Combustível');
  });

  it('progresso do envio das avarias locais aparece com a frase aprovada', () => {
    const html = renderizar({}, '42', {}, { progresso: { enviadas: 1, total: 3 }, falhou: false });
    expect(html).toContain('Vistoria registrada. 1 de 3 avarias enviadas.');
  });

  /** Contexto com a entrada já registrada; o número do funcionário é distinto para o teste achar se ele vazar. */
  function comEntradaRegistrada(parcial: Partial<VistoriaEntrada> = {}): ContextoVistoriaEntrada {
    return {
      ...contexto,
      vistoria: {
        idVistoria: 2, idLocacao: 42, idFuncionario: 987654, tipo: 'ENTRADA', dataHoraStr: '17/11/2026 10:00',
        kmRegistrado: 12000, nivelCombustivel: 0.5, observacoes: null, ...parcial,
      },
    };
  }

  it('entrada registrada com operadorStr: caixa do vistoriador e Funcionário responsável com o nome, nunca o número', () => {
    const html = renderizar({ data: comEntradaRegistrada({ operadorStr: 'Rogério V.' }) }, '42', { data: [] });
    expect(html).toContain('Vistoriador Responsável');
    expect(html).toContain('Rogério V.');
    expect(html).toContain('Funcionário responsável');
    // O usuário logado não aparece na entrada já registrada: quem aparece é quem registrou.
    expect(html).not.toContain('Carlos Mendes');
    expect(html).not.toContain('987.654');
    expect(html).not.toContain('987654');
  });

  it('entrada registrada sem operadorStr: sem caixa do vistoriador e sem a linha do funcionário, nem com o número', () => {
    const html = renderizar({ data: comEntradaRegistrada({ operadorStr: null }) }, '42', { data: [] });
    expect(html).not.toContain('Vistoriador Responsável');
    expect(html).not.toContain('Funcionário responsável');
    expect(html).not.toContain('Carlos Mendes');
    expect(html).not.toContain('987.654');
    expect(html).not.toContain('987654');
  });

  it('entrada registrada com combustível: o medidor mostra o nível e a linha crua com o número não aparece', () => {
    const html = renderizar({ data: comEntradaRegistrada({ operadorStr: 'Rogério V.', nivelCombustivel: 0.5 }) }, '42', { data: [] });
    expect(html).toContain('Nível de Combustível (Tanque)');
    expect(html).not.toContain('Nível de combustível (0 a 1)');
    expect(html).not.toContain('Não informado');
  });
});
