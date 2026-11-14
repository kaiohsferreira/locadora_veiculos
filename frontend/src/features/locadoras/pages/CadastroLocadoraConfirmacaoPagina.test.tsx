import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import {
  ContextoCadastroLocadora,
  type ContextoCadastroLocadoraValor,
  type ResultadoCadastroLocadora,
} from '@/features/locadoras/cadastro/ContextoCadastroLocadora';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { CAMINHOS } from '@/routes/caminhos';
import CadastroLocadoraConfirmacaoPagina from './CadastroLocadoraConfirmacaoPagina';

vi.mock('@/api/client', () => ({
  api: { post: vi.fn() },
}));

const RESULTADO: ResultadoCadastroLocadora = {
  empresa: {
    idEmpresa: 7,
    razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
    nomeFantasia: 'Rota Sul Locadora',
    cnpj: '14829401000192',
    cnpjStr: '14.829.401/0001-92',
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

function contextoCom(resultado: ResultadoCadastroLocadora | null): ContextoCadastroLocadoraValor {
  return {
    dadosEmpresa: null,
    salvarDadosEmpresa: () => undefined,
    dadosResponsavel: null,
    salvarDadosResponsavel: () => undefined,
    errosServidor: [],
    definirErrosServidor: () => undefined,
    removerErrosServidor: () => undefined,
    conflitos: {},
    registrarConflitos: () => undefined,
    resultado,
    salvarResultado: () => undefined,
  };
}

function renderizar(resultado: ResultadoCadastroLocadora | null, estaAutenticado: boolean): string {
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={autenticacao(estaAutenticado)}>
      <QueryClientProvider client={new QueryClient()}>
        <ContextoCadastroLocadora.Provider value={contextoCom(resultado)}>
          <MemoryRouter initialEntries={[CAMINHOS.cadastroLocadoraConfirmacao]}>
            <Routes>
              <Route path={CAMINHOS.cadastroLocadoraConfirmacao} element={<CadastroLocadoraConfirmacaoPagina />} />
              <Route path={CAMINHOS.cadastroLocadoraEmpresa} element={<p>ETAPA_UM</p>} />
            </Routes>
          </MemoryRouter>
        </ContextoCadastroLocadora.Provider>
      </QueryClientProvider>
    </AutenticacaoContexto.Provider>,
  );
}

describe('CadastroLocadoraConfirmacaoPagina (tela 38): resumo com os dados devolvidos', () => {
  it('mostra o título e o nome fantasia da conta criada', () => {
    const marcado = renderizar(RESULTADO, true);

    expect(marcado).toContain('Locadora cadastrada');
    expect(marcado).toContain('Rota Sul Locadora');
    expect(marcado).toContain('ETAPA 3 DE 3 · FINALIZADO');
  });

  it('o resumo traz o CNPJ formatado, a cidade e a UF, o responsável e o e-mail de acesso digitado', () => {
    const marcado = renderizar(RESULTADO, true);

    expect(marcado).toContain('14.829.401/0001-92');
    expect(marcado).toContain('Porto Alegre - RS');
    expect(marcado).toContain('Marina Costa');
    expect(marcado).toContain('marina@rotasul.com.br');
    expect(marcado).toContain('Resumo do cadastro');
  });

  it('mostra "Ativo no Sistema" só quando a empresa está ativa', () => {
    expect(renderizar(RESULTADO, true)).toContain('Ativo no Sistema');
    expect(renderizar({ ...RESULTADO, empresa: { ...RESULTADO.empresa, ativo: false } }, true)).not.toContain(
      'Ativo no Sistema',
    );
  });

  it('o e-mail de acesso é o devolvido pela API (responsavelEmail), não o digitado', () => {
    const marcado = renderizar(
      { ...RESULTADO, emailAcesso: 'digitado@rotasul.com.br', empresa: { ...RESULTADO.empresa, responsavelEmail: 'api@rotasul.com.br' } },
      true,
    );

    expect(marcado).toContain('api@rotasul.com.br');
    expect(marcado).not.toContain('digitado@rotasul.com.br');
  });

  it('sem responsavelEmail na resposta, usa o e-mail digitado como reserva', () => {
    const marcado = renderizar({ ...RESULTADO, emailAcesso: 'digitado@rotasul.com.br' }, true);

    expect(marcado).toContain('digitado@rotasul.com.br');
  });

  it('não traz os textos omitidos: plano, e-mail de confirmação, celular, promessa de criptografia', () => {
    const marcado = renderizar(RESULTADO, true);

    expect(marcado).not.toContain('Plano Inicial');
    expect(marcado).not.toContain('Empresarial AutoVia');
    expect(marcado).not.toContain('Enviamos um e-mail');
    expect(marcado).not.toContain('Celular');
    expect(marcado).not.toContain('criptografia de ponta a ponta');
  });
});

describe('CadastroLocadoraConfirmacaoPagina (tela 38): botões com e sem sessão', () => {
  it('com sessão: "Ir para o meu painel" leva ao back office e "Cadastrar o primeiro veículo" ao cadastro de veículo', () => {
    const marcado = renderizar(RESULTADO, true);

    expect(marcado).toContain('Ir para o meu painel');
    expect(marcado).toContain(`href="${CAMINHOS.backOffice}"`);
    expect(marcado).toContain('Cadastrar o primeiro veículo');
    expect(marcado).toContain(`href="${CAMINHOS.frotaNovoVeiculo}"`);
    expect(marcado).not.toContain('Fazer login');
  });

  it('sem sessão (login automático falhou): oferece "Fazer login" no lugar do painel e avisa que a conta foi criada', () => {
    const marcado = renderizar({ ...RESULTADO, loginAutomatico: false }, false);

    expect(marcado).toContain('Fazer login');
    expect(marcado).toContain(`href="${CAMINHOS.entrar}"`);
    expect(marcado).not.toContain('Ir para o meu painel');
    expect(marcado).toContain('A conta foi criada, mas não foi possível entrar automaticamente.');
    expect(marcado).toContain('Entre com o CPF do responsável para acessar o painel de gestão da sua frota.');
  });

  it('sem a conta criada em memória (recarregou a página), não mostra a confirmação', () => {
    expect(renderizar(null, false)).not.toContain('Locadora cadastrada');
  });
});
