import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { ErroApi } from '@/types/api';
import type { ContextoVistoriaEntrada } from '@/types/vistorias';
import { useRegistrarVistoriaEntrada, useVistoriaEntrada } from '../hooks/useVistoriaEntrada';
import VistoriaAvariasPagina from './VistoriaAvariasPagina';

vi.mock('../hooks/useVistoriaEntrada', () => ({
  useVistoriaEntrada: vi.fn(),
  useRegistrarVistoriaEntrada: vi.fn(),
}));
type Consulta = ReturnType<typeof useVistoriaEntrada>;
type Registro = ReturnType<typeof useRegistrarVistoriaEntrada>;
const contexto: ContextoVistoriaEntrada = {
  locacao: {
    idLocacao: 42, idReserva: 8, idCliente: 9, idVeiculo: 3, status: 'EM_ANDAMENTO',
    dataRetiradaStr: '17/11/2026 09:00', dataDevolucaoPrevistaStr: '19/11/2026 09:00',
  },
  vistoria: null,
};

function renderizar(parcial: Partial<Consulta>, id = '42', registro: Partial<Registro> = {}): string {
  vi.mocked(useVistoriaEntrada).mockReturnValue({
    isPending: false, isError: false, isFetching: false, refetch: vi.fn(), data: contexto,
    ...parcial,
  } as unknown as Consulta);
  vi.mocked(useRegistrarVistoriaEntrada).mockReturnValue({
    isPending: false, isError: false, mutateAsync: vi.fn(), ...registro,
  } as unknown as Registro);
  return renderToStaticMarkup(
    <AvisoProvider><MemoryRouter initialEntries={[`/vistoria/${id}`]}>
      <Routes><Route path="/vistoria/:locacaoId" element={<VistoriaAvariasPagina />} /></Routes>
    </MemoryRouter></AvisoProvider>,
  );
}

describe('tela de vistoria de entrada', () => {
  beforeEach(() => vi.clearAllMocks());

  it('exibe formulário com rótulos, obrigatório e dica de combustível após contexto validado', () => {
    const html = renderizar({});
    expect(html).toContain('<form');
    expect(html).toContain('Quilometragem registrada (km)');
    expect(html).toContain('aria-required="true"');
    expect(html).toContain('0,5 para meio tanque');
    expect(html).toContain('17/11/2026 09:00');
    expect(html).toContain('Observações (opcional)');
    expect(html).not.toContain('type="file"');
  });

  it('carregamento não abre formulário', () => {
    const html = renderizar({ isPending: true, data: undefined });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('<form');
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

  it('bloqueia campos e envio enquanto consulta atualiza ou POST está pendente', () => {
    expect(renderizar({ isFetching: true })).toContain('<fieldset disabled=""');
    expect(renderizar({}, '42', { isPending: true })).toContain('<fieldset disabled=""');
  });

  it('reabertura mostra registro existente sem novo formulário e trata combustível legado nulo', () => {
    const html = renderizar({ data: { ...contexto, vistoria: {
      idVistoria: 2, idLocacao: 42, idFuncionario: 7, tipo: 'ENTRADA', dataHoraStr: '17/11/2026 10:00',
      kmRegistrado: 12000, nivelCombustivel: null, observacoes: '<script>teste</script>',
    } } });
    expect(html).not.toContain('<form');
    expect(html).toContain('<output');
    expect(html).toContain('12.000 km');
    expect(html).toContain('Não informado');
    expect(html).toContain('&lt;script&gt;teste&lt;/script&gt;');
  });
});
