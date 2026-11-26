import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { MENSAGEM_AREA_NAO_PERMITIDA, MENSAGEM_REGISTRO_NAO_ENCONTRADO } from '@/lib/mensagens';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { ErroApi } from '@/types/api';
import type { ContratoLocacao } from '@/types/contratoLocacao';
import {
  CONTRATO_ASSINADO,
  CONTRATO_PENDENTE,
  DOCUMENTO_SEM_COBRANCAS,
} from '../contrato/contratoLocacao.fixture';
import { useAssinarContratoLocacao, useContratoLocacao } from '../contrato/useContratoLocacao';
import ContratoDigitalPagina from './ContratoDigitalPagina';

vi.mock('../contrato/useContratoLocacao', () => ({
  useContratoLocacao: vi.fn(),
  useAssinarContratoLocacao: vi.fn(),
}));

type Consulta = ReturnType<typeof useContratoLocacao>;
type Assinatura = ReturnType<typeof useAssinarContratoLocacao>;

const RP_06 = 'A locação não tem vistoria de entrada registrada. Registre a vistoria antes de emitir o contrato (RP-06).';

function consultaComSucesso(contrato: ContratoLocacao): Consulta {
  return { isPending: false, isError: false, error: null, data: contrato, refetch: vi.fn() } as unknown as Consulta;
}

function consultaComErro(erro: unknown): Consulta {
  return { isPending: false, isError: true, error: erro, data: undefined, refetch: vi.fn() } as unknown as Consulta;
}

function consultaCarregando(): Consulta {
  return { isPending: true, isError: false, error: null, data: undefined, refetch: vi.fn() } as unknown as Consulta;
}

function assinaturaOcioso(): Assinatura {
  return { isPending: false, mutateAsync: vi.fn() } as unknown as Assinatura;
}

function renderizar(locacaoId: string): string {
  return renderToStaticMarkup(
    <AvisoProvider>
      <MemoryRouter initialEntries={[`/back-office/locacoes/${locacaoId}/contrato`]}>
        <Routes>
          <Route path="/back-office/locacoes/:locacaoId/contrato" element={<ContratoDigitalPagina />} />
        </Routes>
      </MemoryRouter>
    </AvisoProvider>,
  );
}

describe('ContratoDigitalPagina (tela 08)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAssinarContratoLocacao).mockReturnValue(assinaturaOcioso());
  });

  it('mostra o carregamento enquanto o contrato é lido', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaCarregando());
    expect(renderizar('15')).toContain('Carregando...');
  });

  it('mostra o contrato pendente com o formulário, o quadro e o botão de assinar', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaComSucesso(CONTRATO_PENDENTE));
    const html = renderizar('15');
    expect(html).toContain('Emissão de Contrato Digital');
    expect(html).toContain('Minuta do contrato');
    expect(html).toContain('Assinar contrato');
    expect(html).toContain('Li e aceito as condições gerais deste contrato.');
    expect(html).toContain('Li e aceito a política de combustível deste contrato.');
    expect(html).toContain('aria-label="Quadro de assinatura do cliente"');
    expect(html).toContain('touch-none');
    expect(html).toContain('Locação nº 15');
    expect(html).toContain('Etapa 3');
    expect(html).toContain('Em andamento');
    expect(html).not.toContain('Contrato assinado');
    expect(html).not.toContain('Voltar às locações');
    expect(html).not.toContain('Terminal Ativo');
  });

  it('mostra a etapa ativa com o selo "Em andamento" e o quadro com o rótulo de antes de assinar', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaComSucesso(CONTRATO_PENDENTE));
    const html = renderizar('15');
    expect(html).toContain('bg-frota-oficina px-1.5 text-chip-frota font-bold uppercase text-branco">Em andamento</span>');
    expect(html).toContain('Assinatura do cliente');
    expect(html).not.toContain('Assinatura coletada');
  });

  it('mostra só o aceite de condições gerais quando não há configuração de cobranças', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(
      consultaComSucesso({ ...CONTRATO_PENDENTE, documento: DOCUMENTO_SEM_COBRANCAS }),
    );
    const html = renderizar('15');
    expect(html).toContain(M.aceiteCondicoesTexto);
    expect(html).not.toContain(M.aceiteCombustivelTexto);
    expect(html).toContain('Não informada');
  });

  it('mostra o registro não encontrado (404) com a volta às retiradas', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaComErro(new ErroApi({ status: 404, mensagem: 'x' })));
    const html = renderizar('15');
    expect(html).toContain(MENSAGEM_REGISTRO_NAO_ENCONTRADO);
    expect(html).toContain(M.voltarRetiradas);
    expect(html).not.toContain('Assinar contrato');
  });

  it('não mostra termos cortados do desenho em nenhum dos dois estados', () => {
    const termos = [
      'Terminal Ativo',
      'Totem',
      'Token',
      'SMS',
      'WhatsApp',
      'Despachar',
      'Jurídico',
      'SSL',
      'térmica',
      'LOC-',
      'Agente de Entrega',
      'Folha 1 de 2',
      'PDF',
      'Válida via ICP-Brasil',
    ];
    for (const contrato of [CONTRATO_PENDENTE, CONTRATO_ASSINADO]) {
      vi.mocked(useContratoLocacao).mockReturnValue(consultaComSucesso(contrato));
      const html = renderizar('15');
      for (const termo of termos) {
        expect(html).not.toContain(termo);
      }
    }
  });

  it('mostra o contrato assinado sem formulário, com o hash e o link de volta', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaComSucesso(CONTRATO_ASSINADO));
    const html = renderizar('15');
    expect(html).toContain('Contrato assinado');
    expect(html).toContain('Hash SHA-256 do documento');
    expect(html).toContain('Voltar às locações');
    expect(html).toContain('Assinatura coletada');
    expect(html).not.toContain('Assinar contrato');
    expect(html).not.toContain('Limpar Assinatura');
  });

  it('leva à vistoria quando a locação não tem vistoria de entrada (409 RP-06)', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaComErro(new ErroApi({ status: 409, mensagem: RP_06 })));
    const html = renderizar('15');
    expect(html).toContain('Contrato indisponível.');
    expect(html).toContain(RP_06);
    expect(html).toContain('Registrar vistoria de entrada');
    expect(html).toContain('href="/back-office/locacoes/15/vistoria"');
  });

  it('mostra a mensagem de falta de permissão (403) sem dados', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaComErro(new ErroApi({ status: 403, mensagem: 'x' })));
    const html = renderizar('15');
    expect(html).toContain(MENSAGEM_AREA_NAO_PERMITIDA);
    expect(html).not.toContain('Assinar contrato');
  });

  it('mostra o erro de carregamento com a opção de tentar de novo', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaComErro(new ErroApi({ status: 500, mensagem: 'Falha.' })));
    const html = renderizar('15');
    expect(html).toContain('Não foi possível carregar o contrato.');
    expect(html).toContain('Tentar novamente');
  });

  it('recusa um identificador de locação inválido sem consultar o contrato', () => {
    vi.mocked(useContratoLocacao).mockReturnValue(consultaCarregando());
    const html = renderizar('abc');
    expect(html).toContain('A locação informada é inválida.');
    expect(html).not.toContain('Carregando...');
  });
});
