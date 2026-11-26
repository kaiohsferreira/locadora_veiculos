import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { CONTRATO_ASSINADO } from './contratoLocacao.fixture';
import { AreaAssinatura } from './AreaAssinatura';

// O hook de assinatura puxa a instância do Axios (que lê o window). Na marcação estática da área assinada ele não é usado.
vi.mock('./useContratoLocacao', () => ({
  useAssinarContratoLocacao: () => ({ isPending: false, mutateAsync: async () => undefined }),
}));

describe('AreaAssinatura (contrato assinado)', () => {
  const CARIMBO = 'Assinado na tela em 14/11/2026 14:30 • Coletado por Carlos Mendes.';
  const LINHA_REGISTRO = 'Assinado em 14/11/2026 14:30 por João Pereira Silva • coletado por Carlos Mendes';

  it('mostra as duas linhas aprovadas, cada uma no seu bloco: o carimbo sob a imagem e a linha no registro', () => {
    const html = renderToStaticMarkup(
      <MemoryRouter>
        <AreaAssinatura contrato={CONTRATO_ASSINADO} idLocacao={15} />
      </MemoryRouter>,
    );
    const posicaoImagem = html.indexOf('<img');
    const posicaoCarimbo = html.indexOf(CARIMBO);
    const posicaoRegistro = html.indexOf('Registro da assinatura');
    const posicaoLinha = html.indexOf(LINHA_REGISTRO);

    expect(posicaoCarimbo).toBeGreaterThan(posicaoImagem);
    expect(posicaoCarimbo).toBeLessThan(posicaoRegistro);
    expect(posicaoLinha).toBeGreaterThan(posicaoRegistro);
    expect(html).not.toContain('Carimbo de Auditoria');
  });

  it('mostra cada linha uma vez só, com a data, a hora e o funcionário', () => {
    const html = renderToStaticMarkup(
      <MemoryRouter>
        <AreaAssinatura contrato={CONTRATO_ASSINADO} idLocacao={15} />
      </MemoryRouter>,
    );
    expect(html.split(CARIMBO).length - 1).toBe(1);
    expect(html.split(LINHA_REGISTRO).length - 1).toBe(1);
  });
});
