import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { ErroApi } from '@/types/api';
import type { ContextoDevolucao, ComparacaoSaida } from '@/types/devolucao';
import { useDevolucao, useRegistrarSaida } from '../hooks/useDevolucao';
import CheckoutDevolucaoPagina from './CheckoutDevolucaoPagina';
vi.mock('../hooks/useDevolucao', () => ({ useDevolucao: vi.fn(), useRegistrarSaida: vi.fn() }));
vi.mock('../cobrancas/useCobrancasExtras', () => ({ useApuracaoCobrancasExtras: () => ({ data: null, isPending: false, isError: false }), useSimularCobrancasExtras: () => ({ isPending: false }), useRegistrarApuracaoCobrancasExtras: () => ({ isPending: false }) }));
type Consulta = ReturnType<typeof useDevolucao>;
type Registro = ReturnType<typeof useRegistrarSaida>;
const entrada: NonNullable<ContextoDevolucao['entrada']> = { idVistoria: 1, idLocacao: 42, idFuncionario: 3, tipo: 'ENTRADA', dataHoraStr: '18/11/2026 09:00', kmRegistrado: 120, nivelCombustivel: 0.5, observacoes: null };
const avaria = { idAvaria: 7, idVistoria: 1, idAvariaEntrada: null, descricao: 'Risco original', gravidade: 'LEVE' as const, valorEstimado: 50, valorEstimadoStr: 'R$ 50,00' };
const contexto: ContextoDevolucao = { locacao: { idLocacao: 42, idReserva: 8, idVeiculo: 3, kmSaida: 100, status: 'EM_ANDAMENTO' }, entrada, avariasEntrada: [avaria], comparacao: null };
const comparacao: ComparacaoSaida = { entrada, saida: { ...entrada, idVistoria: 2, tipo: 'SAIDA' }, comparacaoRealizada: true, avariasPreexistentes: [{ ...avaria, idAvaria: 8, idVistoria: 2, idAvariaEntrada: 7 }], avariasNovas: [{ ...avaria, idAvaria: 9, idVistoria: 2, descricao: 'Amassado novo' }], avariasIndeterminadas: [] };
function renderizar(parcial: Partial<Consulta> = {}, id = '42', registro: Partial<Registro> = {}) {
  vi.mocked(useDevolucao).mockReturnValue({ isPending: false, isError: false, isFetching: false, refetch: vi.fn(), data: contexto, ...parcial } as unknown as Consulta);
  vi.mocked(useRegistrarSaida).mockReturnValue({ isPending: false, isError: false, mutateAsync: vi.fn(), ...registro } as unknown as Registro);
  return renderToStaticMarkup(<AutenticacaoContexto.Provider value={SESSAO_ATENDENTE}><AvisoProvider><MemoryRouter initialEntries={[`/devolucao/${id}`]}><Routes><Route path="/devolucao/:locacaoId" element={<CheckoutDevolucaoPagina />} /></Routes></MemoryRouter></AvisoProvider></AutenticacaoContexto.Provider>);
}
/** Sessão de ATENDENTE: a tela só mostra o simulador (e o botão de apurar) para ATENDENTE e ADMINISTRADOR. */
const SESSAO_ATENDENTE: AutenticacaoContextoValor = {
  usuario: { nome: 'Marina Costa', perfil: 'funcionario', cargo: 'ATENDENTE', empresaNomeFantasia: 'Rota Sul Locadora' },
  autenticado: true, autenticacaoIgnorada: false, entrar: () => undefined, sair: () => undefined, possuiPerfil: () => true,
};
describe('tela RF-22', () => {
  beforeEach(() => vi.clearAllMocks());
  it('mostra entrada antes do formulário, checklist e confirmação acessível', () => {
    const html = renderizar();
    expect(html.indexOf('Vistoria de entrada')).toBeLessThan(html.indexOf('<form'));
    expect(html).toContain('Risco original'); expect(html).toContain('120 km');
    expect(html).toContain('comparacaoConferida'); expect(html).toContain('aria-required="true"');
    expect(html).not.toContain('type="file"'); expect(html).not.toContain('Encerrar');
  });
  it('sem entrada orienta e vincula T1, sem formulário', () => {
    const html = renderizar({ data: { ...contexto, entrada: null } });
    expect(html).not.toContain('<form'); expect(html).toContain('/back-office/locacoes/42/vistoria');
  });
  it('registro existente mostra somente snapshots persistidos', () => {
    const html = renderizar({ data: { ...contexto, avariasEntrada: [{ ...avaria, descricao: 'Alteração posterior' }], comparacao } });
    expect(html).not.toContain('Registrar vistoria de saída'); expect(html).toContain('Simular com política cadastrada'); expect(html).toContain('Já existiam'); expect(html).toContain('Novas');
    expect(html).toContain('Amassado novo'); expect(html).not.toContain('Alteração posterior');
  });
  it('legado não infere novas nem permite novo registro', () => {
    const html = renderizar({ data: { ...contexto, comparacao: { ...comparacao, comparacaoRealizada: false, avariasPreexistentes: [], avariasNovas: [], avariasIndeterminadas: [avaria] } } });
    expect(html).not.toContain('<form'); expect(html).toContain('Comparação não confirmada'); expect(html).not.toContain('Já existiam');
  });
  it.each([0, 401, 403, 404, 500])('erro %s bloqueia formulário mesmo com cache antigo', status => expect(renderizar({ isError: true, error: new ErroApi({ status, mensagem: 'Não foi possível consultar.' }) })).not.toContain('<form'));
  it('loading e ID inválido não abrem formulário', () => {
    expect(renderizar({ isPending: true })).not.toContain('<form');
    expect(renderizar({}, '0')).not.toContain('<form'); expect(useDevolucao).toHaveBeenCalledWith(null);
  });
  it('atualização ou POST pendente desabilitam envio', () => {
    expect(renderizar({ isFetching: true })).toContain('<fieldset disabled=""');
    expect(renderizar({}, '42', { isPending: true })).toContain('<fieldset disabled=""');
  });
  it('locação encerrada bloqueia novo registro', () => expect(renderizar({ data: { ...contexto, locacao: { ...contexto.locacao, status: 'FINALIZADA' } } })).not.toContain('<form'));
});
