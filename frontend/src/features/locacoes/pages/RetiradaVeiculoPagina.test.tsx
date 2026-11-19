import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { ErroApi } from '@/types/api';
import { useAbrirLocacao, useRetirada } from '../hooks/useRetirada';
import { contextoRetiradaTeste as contexto } from '../retirada/contextoRetirada.fixture';
import RetiradaVeiculoPagina from './RetiradaVeiculoPagina';
vi.mock('../hooks/useRetirada', () => ({ useRetirada: vi.fn(), useAbrirLocacao: vi.fn() }));
type Consulta = ReturnType<typeof useRetirada>;
type Registro = ReturnType<typeof useAbrirLocacao>;
function renderizar(parcial: Partial<Consulta> = {}, id = '8', registro: Partial<Registro> = {}): string {
  vi.mocked(useRetirada).mockReturnValue({ isPending: false, isError: false, isFetching: false, refetch: vi.fn(), data: contexto, ...parcial } as unknown as Consulta);
  vi.mocked(useAbrirLocacao).mockReturnValue({ isPending: false, isError: false, mutateAsync: vi.fn(), ...registro } as unknown as Registro);
  return renderToStaticMarkup(<AvisoProvider><MemoryRouter initialEntries={[`/retirada/${id}`]}><Routes><Route path="/retirada/:reservaId" element={<RetiradaVeiculoPagina />} /></Routes></MemoryRouter></AvisoProvider>);
}
describe('tela de retirada', () => {
  beforeEach(() => vi.clearAllMocks());
  it('mostra dados reais com máscara, status local e conferência acessível', () => {
    const html = renderizar();
    for (const texto of ['Cliente de teste', '***.***.***-09', '*******1234', 'Cadastro nesta locadora', '12.000 km', 'R$ 240,00', 'aria-required="true"', 'Confira os documentos originais apresentados pelo cliente.']) expect(html).toContain(texto);
    expect(html.match(/type="checkbox"/g)).toHaveLength(2);
    expect(html).not.toContain('type="file"');
    expect(html).toContain('disabled=""');
  });
  it('loading não expõe formulário', () => { const html = renderizar({ isPending: true }); expect(html).toContain('aria-busy="true"'); expect(html).not.toContain('<form'); });
  it.each([401, 403, 404, 500])('erro %s não expõe formulário nem dados antigos', (status) => {
    const html = renderizar({ isError: true, error: new ErroApi({ status, mensagem: 'Não foi possível consultar.' }) });
    expect(html).not.toContain('<form'); expect(html).not.toContain('Cliente de teste'); expect(html).toContain('Tentar novamente');
  });
  it('id inválido desabilita consulta', () => { expect(renderizar({}, '0')).not.toContain('<form'); expect(useRetirada).toHaveBeenCalledWith(null); });
  it('retomada mostra vistoria e não oferece nova abertura', () => {
    const html = renderizar({ data: { ...contexto, idLocacaoExistente: 42, statusVeiculo: 'LOCADO' } });
    expect(html).toContain('Continuar para vistoria'); expect(html).toContain('/back-office/locacoes/42/vistoria'); expect(html).not.toContain('<form');
  });
  it('bloqueia durante atualização e envio', () => {
    expect(renderizar({ isFetching: true })).toContain('<fieldset disabled=""');
    expect(renderizar({}, '8', { isPending: true })).toContain('<fieldset disabled=""');
  });
  it('orienta estado indisponível', () => expect(renderizar({ data: { ...contexto, statusVeiculo: 'DISPONIVEL' } })).toContain('A retirada exige uma reserva ativa'));
  it('escapa dados e mantém status local ausente explícito', () => {
    const html = renderizar({ data: { ...contexto, clienteNome: '<script>alert(1)</script>', statusCadastroLocadora: null } });
    expect(html).toContain('&lt;script&gt;'); expect(html).toContain('Sem cadastro ativo nesta locadora');
  });
});
