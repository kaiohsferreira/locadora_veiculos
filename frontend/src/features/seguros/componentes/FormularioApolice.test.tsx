import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { valoresIniciaisCadastro, valoresIniciaisEdicao } from '@/features/seguros/regras/formularioApolice';
import type { Apolice } from '@/types/seguros';
import { FormularioApolice, type FormularioApoliceProps } from './FormularioApolice';

// Nenhum teste chama a API: o cliente HTTP vira dublê e a busca por placa não consulta.
vi.mock('@/api/client', () => ({ api: { get: vi.fn() } }));
vi.mock('@/features/seguros/hooks/useBuscaVeiculoPlaca', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/features/seguros/hooks/useBuscaVeiculoPlaca')>()),
  useBuscaVeiculoPlaca: () => ({ data: undefined, isError: false, error: null, isFetching: false }),
}));

const APOLICE: Apolice = {
  idSeguro: 3,
  idVeiculo: 7,
  placaVeiculo: 'ABC1D23',
  seguradora: 'Porto Seguro',
  numeroApolice: '123.456-7',
  dataInicio: '2026-01-01',
  dataInicioStr: '01/01/2026',
  dataFim: '2026-12-30',
  dataFimStr: '30/12/2026',
  valorPremio: 3450.5,
  valorPremioStr: 'R$ 3.450,50',
  cobertura: 'Compreensiva',
  diasParaVencer: 30,
  venceEm60Dias: true,
};

function renderizar(parcial: Partial<FormularioApoliceProps> = {}): string {
  const props: FormularioApoliceProps = {
    valoresIniciais: valoresIniciaisCadastro(),
    enviando: false,
    erroGeral: null,
    errosCampos: {},
    aoEnviar: vi.fn(),
    aoCancelar: vi.fn(),
    ...parcial,
  };
  return renderToStaticMarkup(<FormularioApolice {...props} />);
}

describe('FormularioApolice', () => {
  it('cadastro: mostra todos os campos e os botões, sem alerta', () => {
    const html = renderizar();

    for (const rotulo of [
      'Veículo',
      'Seguradora',
      'Número da apólice',
      'Início da vigência',
      'Fim da vigência',
      'Valor do prêmio (R$)',
      'Cobertura',
    ]) {
      expect(html).toContain(rotulo);
    }
    expect(html).toContain('type="date"');
    expect(html).toContain('Salvar apólice');
    expect(html).toContain('Cancelar');
    expect(html).not.toContain('role="alert"');
  });

  it('edição: abre com o veículo da apólice já escolhido e o botão de trocar', () => {
    // Os demais campos são preenchidos pelo react-hook-form depois de montar (valoresIniciaisEdicao tem teste próprio).
    const html = renderizar({ valoresIniciais: valoresIniciaisEdicao(APOLICE) });

    expect(html).toContain('ABC1D23');
    expect(html).toContain('Trocar veículo');
  });

  it('erro geral do servidor aparece num alerta no topo', () => {
    const html = renderizar({ erroGeral: 'Não foi possível salvar a apólice.' });

    expect(html).toContain('role="alert"');
    expect(html).toContain('Não foi possível salvar a apólice.');
  });

  it('enquanto envia, desliga os campos e marca o formulário como ocupado', () => {
    const html = renderizar({ enviando: true });

    expect(html).toContain('aria-busy="true"');
    expect(html).toMatch(/<fieldset disabled=""/);
  });

  it('os campos de texto respeitam os limites da API', () => {
    const html = renderizar();

    expect(html).toContain('maxLength="100"');
    expect(html).toContain('maxLength="30"');
    expect(html).toContain('maxLength="200"');
  });
});
