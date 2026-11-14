import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import {
  ContextoCadastroLocadora,
  type ContextoCadastroLocadoraValor,
  type ResultadoCadastroLocadora,
} from '@/features/locadoras/cadastro/ContextoCadastroLocadora';
import type { FormularioEmpresa } from '@/features/locadoras/cadastro/esquemaLocadora';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { CAMINHOS } from '@/routes/caminhos';
import CadastroLocadoraResponsavelPagina from './CadastroLocadoraResponsavelPagina';

// A tela importa as mutações (que carregam o cliente HTTP). Nenhum teste chama a API: o cliente vira dublê.
vi.mock('@/api/client', () => ({
  api: { post: vi.fn() },
}));

const DADOS_EMPRESA: FormularioEmpresa = {
  razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
  nomeFantasia: 'Rota Sul Locadora',
  cnpj: '11222333000181',
  telefone: '5133429000',
  email: 'contato@rotasul.com.br',
  cep: '90200310',
  logradouro: 'Av. Severo Dullius',
  numero: '1240',
  complemento: '',
  bairro: 'São João',
  cidade: 'Porto Alegre',
  uf: 'RS',
};

function autenticacao(autenticado: boolean): AutenticacaoContextoValor {
  return {
    usuario: null,
    autenticado,
    autenticacaoIgnorada: false,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: () => false,
  };
}

function contextoCom(parcial: Partial<ContextoCadastroLocadoraValor>): ContextoCadastroLocadoraValor {
  return {
    dadosEmpresa: DADOS_EMPRESA,
    salvarDadosEmpresa: () => undefined,
    dadosResponsavel: null,
    salvarDadosResponsavel: () => undefined,
    errosServidor: [],
    definirErrosServidor: () => undefined,
    removerErrosServidor: () => undefined,
    conflitos: {},
    registrarConflitos: () => undefined,
    resultado: null,
    salvarResultado: () => undefined,
    ...parcial,
  };
}

/** Renderiza a etapa 2 com o provedor de dados, como a rota-pai faz. */
function renderizar(
  contexto: ContextoCadastroLocadoraValor = contextoCom({}),
  estaAutenticado = false,
): string {
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={autenticacao(estaAutenticado)}>
      <QueryClientProvider client={new QueryClient()}>
        <ContextoCadastroLocadora.Provider value={contexto}>
          <MemoryRouter initialEntries={[CAMINHOS.cadastroLocadoraResponsavel]}>
            <Routes>
              <Route path={CAMINHOS.cadastroLocadoraResponsavel} element={<CadastroLocadoraResponsavelPagina />} />
              <Route path={CAMINHOS.cadastroLocadoraConfirmacao} element={<p>CONFIRMACAO</p>} />
              <Route path={CAMINHOS.inicio} element={<p>INICIO</p>} />
              <Route path={CAMINHOS.cadastroLocadoraEmpresa} element={<p>ETAPA_UM</p>} />
            </Routes>
          </MemoryRouter>
        </ContextoCadastroLocadora.Provider>
      </QueryClientProvider>
    </AutenticacaoContexto.Provider>,
  );
}

describe('CadastroLocadoraResponsavelPagina (tela 37): conteúdo', () => {
  it('mostra o título, o subtítulo e a etapa 2 do indicador', () => {
    const marcado = renderizar();

    expect(marcado).toContain('Quem vai administrar a conta?');
    expect(marcado).toContain('Esta pessoa será a administradora da locadora na AutoVia.');
    expect(marcado).toContain('aria-current="step"');
    expect(marcado).toContain('Em andamento');
    expect(marcado).toContain('ETAPA 2 DE 3');
  });

  it('tem os campos do contrato com os rótulos do desenho', () => {
    const marcado = renderizar();

    for (const rotulo of [
      'Nome completo do responsável',
      'CPF',
      'E-mail de acesso',
      'Senha',
      'Confirmar senha',
      'Requisitos de segurança da credencial',
      'Mínimo de 8 caracteres',
      'Uma letra maiúscula',
      'Um número',
    ]) {
      expect(marcado).toContain(rotulo);
    }
  });

  it('a ajuda do e-mail diz que o acesso é por CPF (não diz que o e-mail serve para entrar)', () => {
    const marcado = renderizar();

    expect(marcado).toContain('Usado para recuperar a senha. O acesso é feito com o CPF.');
    expect(marcado).not.toContain('usado para entrar');
    expect(marcado).not.toContain('Será usado para entrar');
  });

  it('tem o aceite dos termos, sem link (não há página de termos) e com a caixa desmarcada', () => {
    const marcado = renderizar();

    expect(marcado).toContain('Li e aceito os');
    expect(marcado).toContain('Termos de Uso');
    expect(marcado).toContain('Política de Privacidade');
    expect(marcado).toContain('type="checkbox"');
    expect(marcado).not.toMatch(/<a [^>]*>Termos de Uso/);
    expect(marcado).not.toMatch(/checked=""/);
  });

  it('os dois campos de senha são do tipo senha e não trazem valor', () => {
    const marcado = renderizar();

    expect(marcado.match(/type="password"/g)).toHaveLength(2);
    expect(marcado).not.toMatch(/type="password"[^>]*value=/);
    expect(marcado).not.toMatch(/value="[^"]*"[^>]*type="password"/);
  });

  it('o medidor começa sem nível (senha ainda não digitada) e a regra da senha é a do desenho', () => {
    const marcado = renderizar();

    expect(marcado).not.toContain('Senha forte');
    expect(marcado).not.toContain('Nível 0 de 4');
    expect(marcado).toContain('(pendente)');
  });

  it('tem o botão de criar a conta e o de voltar para a etapa 1', () => {
    const marcado = renderizar();

    expect(marcado).toContain('Criar conta da locadora');
    expect(marcado).toContain('Voltar');
    expect(marcado).toContain(`href="${CAMINHOS.cadastroLocadoraEmpresa}"`);
  });

  it('não traz os textos omitidos: cargo, celular, plano, e-mail de confirmação ou promessa de criptografia', () => {
    const marcado = renderizar();

    expect(marcado).not.toContain('Cargo na empresa');
    expect(marcado).not.toContain('Celular');
    expect(marcado).not.toContain('Plano Inicial');
    expect(marcado).not.toContain('Empresarial AutoVia');
    expect(marcado).not.toContain('Enviamos um e-mail');
    expect(marcado).not.toContain('criptografia de ponta a ponta');
  });
});

describe('CadastroLocadoraResponsavelPagina (tela 37): guardas de entrada', () => {
  it('sem os dados da etapa 1 em memória, não mostra a etapa 2 (volta para a etapa 1)', () => {
    const marcado = renderizar(contextoCom({ dadosEmpresa: null }));

    expect(marcado).not.toContain('Quem vai administrar a conta?');
  });

  it('com a conta já criada, não mostra a etapa 2 (vai para a confirmação)', () => {
    const resultado: ResultadoCadastroLocadora = {
      empresa: {
        idEmpresa: 7,
        razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
        nomeFantasia: 'Rota Sul Locadora',
        cnpj: '11222333000181',
        cnpjStr: '11.222.333/0001-81',
        telefone: '5133429000',
        email: 'contato@rotasul.com.br',
        cep: '90200310',
        logradouro: 'Av. Severo Dullius',
        numero: '1240',
        complemento: null,
        bairro: 'São João',
        cidade: 'Porto Alegre',
        uf: 'RS',
        ativo: true,
        idFuncionarioResponsavel: 21,
        responsavelNome: 'Marina Costa',
        matriculaResponsavel: 'MAT-0001',
        dataCadastroStr: '13/11/2026 10:00',
      },
      emailAcesso: 'marina@rotasul.com.br',
      loginAutomatico: true,
    };

    expect(renderizar(contextoCom({ resultado }))).not.toContain('Quem vai administrar a conta?');
  });

  it('quem já tem sessão não mostra a etapa 2 (vai para o início)', () => {
    expect(renderizar(contextoCom({}), true)).not.toContain('Quem vai administrar a conta?');
  });
});
