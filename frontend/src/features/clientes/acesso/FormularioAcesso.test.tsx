import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { ACESSO_LINK_CRIAR_CONTA } from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import { FormularioAcesso } from './FormularioAcesso';

// Nenhum teste chama a API: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));
// O bloco do Google tem consultas próprias; este teste cuida só do link de criar conta.
vi.mock('@/features/clientes/google/BlocoGoogle', () => ({
  BlocoGoogle: () => null,
}));

function renderizar(): string {
  return renderToStaticMarkup(
    <MemoryRouter>
      <FormularioAcesso perfilInicial="CLIENTE" enviando={false} erroEnvio={null} aoEnviar={() => undefined} />
    </MemoryRouter>,
  );
}

describe('FormularioAcesso: link "Criar conta" (RF-52)', () => {
  it('leva à escolha do tipo de conta (tela 35), não direto ao cadastro do cliente', () => {
    const marcado = renderizar();

    expect(marcado).toContain(ACESSO_LINK_CRIAR_CONTA);
    expect(marcado).toContain(`href="${CAMINHOS.cadastro}"`);
    expect(marcado).not.toContain(`href="${CAMINHOS.cadastroDadosPessoais}"`);
  });
});
