import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { CadastroContexto, type CadastroContextoValor } from '@/features/clientes/cadastro/ContextoCadastro';
import type { DadosPessoaisCadastro } from '@/features/clientes/cadastro/esquemaCadastro';
import { CHAVE_DOCUMENTOS_CLIENTE } from '@/features/clientes/hooks/useDocumentosCliente';
import CadastroDocumentosPagina from '@/features/clientes/pages/CadastroDocumentosPagina';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import type { DocumentoClienteGravado } from '@/types/documentosCliente';
import type { UsuarioSessao } from '@/types/autenticacao';

// O cliente HTTP lê o window ao carregar: a página é testada sem rede e sem navegador.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

const DADOS: DadosPessoaisCadastro = {
  nome: 'Mariana Carvalho Souza',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: 'mariana@email.com',
  telefone: '(11) 98765-4321',
};

/** Termos que não podem aparecer na tela (contrato cliente-documento, frontend_tela21.testes_obrigatorios). */
const TERMOS_PROIBIDOS = [
  'TLS',
  'Biométrica',
  'inteligência artificial',
  'OCR',
  'AES-256',
  'CONTRAN',
  'Protocolo',
  'Validado com Sucesso',
];

/** Renderiza a página com os contextos que ela usa (sessão, dados do cadastro e a fila de consultas). */
function renderizarPagina(opcoes: {
  usuario: UsuarioSessao | null;
  dados: DadosPessoaisCadastro | null;
  /** Lista do servidor já carregada (cache da consulta do cliente); sem ela a página mostra "Carregando...". */
  documentosNoServidor?: DocumentoClienteGravado[];
}): string {
  const autenticado = opcoes.usuario !== null;
  const clienteQuery = new QueryClient();
  if (opcoes.usuario !== null && opcoes.documentosNoServidor !== undefined) {
    clienteQuery.setQueryData([CHAVE_DOCUMENTOS_CLIENTE, opcoes.usuario.idUsuario], opcoes.documentosNoServidor);
  }
  const contextoAutenticacao: AutenticacaoContextoValor = {
    usuario: opcoes.usuario,
    autenticado,
    autenticacaoIgnorada: false,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: () => false,
  };
  const contextoCadastro: CadastroContextoValor = {
    dadosPessoais: opcoes.dados,
    documentos: null,
    veioDoGoogle: false,
    arquivosDocumentos: {},
    errosServidor: [],
    salvarDadosPessoais: () => undefined,
    salvarDocumentos: () => undefined,
    salvarArquivosDocumentos: () => undefined,
    definirErrosServidor: () => undefined,
    removerErrosServidor: () => undefined,
  };
  return renderToStaticMarkup(
    <QueryClientProvider client={clienteQuery}>
      <MemoryRouter>
        <AvisoProvider>
          <AutenticacaoContexto.Provider value={contextoAutenticacao}>
            <CadastroContexto.Provider value={contextoCadastro}>
              <CadastroDocumentosPagina />
            </CadastroContexto.Provider>
          </AutenticacaoContexto.Provider>
        </AvisoProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CadastroDocumentosPagina, modo cadastro (F1)', () => {
  const html = renderizarPagina({ usuario: null, dados: DADOS });

  it('mostra os dois cartões, a CNH digitada, endereço e senha, e o botão que cria a conta', () => {
    expect(html).toContain('Foto da CNH (frente e verso)');
    expect(html).toContain('Comprovante de residência');
    expect(html).toContain('Dados da CNH');
    expect(html).toContain('Informe os dados como estão na sua CNH.');
    expect(html).toContain('Endereço (opcional)');
    expect(html).toContain('Senha de acesso');
    expect(html).toContain('Voltar aos Dados Pessoais');
    expect(html).toContain('Confirmar Dados e Finalizar Cadastro');
  });

  it('mostra o CPF mascarado, não o CPF inteiro', () => {
    expect(html).toContain('CPF Vinculado: ***.982.247-**');
    expect(html).not.toContain('529.982.247-25');
    expect(html).not.toContain('52998224725');
  });

  it('o CPF vem depois do bloco da CNH, como no desenho (as informações complementares ficam no fim)', () => {
    expect(html.indexOf('Dados da CNH')).toBeGreaterThan(-1);
    expect(html.indexOf('Dados da CNH')).toBeLessThan(html.indexOf('CPF Vinculado'));
    expect(html.indexOf('CPF Vinculado')).toBeLessThan(html.indexOf('Endereço (opcional)'));
  });

  it('os botões dos cartões e o botão final têm altura de toque de 44 px', () => {
    expect(html.match(/<button[^>]*>/g)?.some((botao) => botao.includes('!h-11'))).toBe(true);
    expect(html).toContain('flex h-11 w-full items-center justify-center gap-1');
  });

  it('o cabeçalho e a trilha são os do desenho; o título é o de 30/38', () => {
    expect(html).toContain('Documentos do cadastro');
    expect(html).toContain('Envio e Validação de Documentos');
    expect(html).toContain('text-titulo-documentos');
    expect(html).toContain('Validação Cadastral');
    expect(html).toContain('Envio de Documentos');
  });

  it('não mostra o modo só enviar', () => {
    expect(html).not.toContain('Seu cadastro já foi criado');
    expect(html).not.toContain('>Enviar documentos<');
  });

  it('não contém nenhum dos termos que o desenho promete e o sistema não faz', () => {
    for (const termo of TERMOS_PROIBIDOS) {
      expect(html).not.toContain(termo);
    }
  });
});

describe('CadastroDocumentosPagina, modo só enviar (F4)', () => {
  const html = renderizarPagina({
    usuario: { nome: 'Mariana', perfil: 'cliente', idUsuario: 7 },
    dados: null,
  });

  it('mostra o aviso de cadastro criado, o botão de enviar e a lista que está carregando', () => {
    expect(html).toContain('Seu cadastro já foi criado. Envie os documentos que faltam.');
    expect(html).toContain('Enviar documentos');
    expect(html).toContain('Carregando...');
  });

  it('esconde CNH, endereço, senha e a volta aos dados pessoais', () => {
    expect(html).not.toContain('Senha de acesso');
    expect(html).not.toContain('Endereço (opcional)');
    expect(html).not.toContain('Voltar aos Dados Pessoais');
    expect(html).not.toContain('CPF Vinculado');
  });

  it('os dois cartões continuam na tela', () => {
    expect(html).toContain('Foto da CNH (frente e verso)');
    expect(html).toContain('Comprovante de residência');
  });

  it('não contém nenhum dos termos proibidos', () => {
    for (const termo of TERMOS_PROIBIDOS) {
      expect(html).not.toContain(termo);
    }
  });
});

const CNH_GRAVADA: DocumentoClienteGravado = {
  idClienteDocumento: 1,
  tipo: 'CNH',
  nomeArquivo: 'cnh.pdf',
  tipoConteudo: 'application/pdf',
  tamanhoBytes: 2048,
  dataEnvio: '19/11/2026 09:00',
};

const COMPROVANTE_GRAVADO: DocumentoClienteGravado = {
  idClienteDocumento: 2,
  tipo: 'COMPROVANTE_RESIDENCIA',
  nomeArquivo: 'conta.png',
  tipoConteudo: 'image/png',
  tamanhoBytes: 4096,
  dataEnvio: '19/11/2026 09:01',
};

describe('CadastroDocumentosPagina, só enviar com os dois documentos já enviados (F4)', () => {
  const usuario: UsuarioSessao = { nome: 'Mariana', perfil: 'cliente', idUsuario: 7 };

  it('mostra o aviso de já enviados e esconde o botão de enviar quando nada foi escolhido', () => {
    const html = renderizarPagina({ usuario, dados: null, documentosNoServidor: [CNH_GRAVADA, COMPROVANTE_GRAVADO] });
    expect(html).toContain('Seus documentos já foram enviados. Se precisar, substitua um arquivo.');
    expect(html).not.toContain('Seu cadastro já foi criado');
    expect(html).not.toContain('>Enviar documentos<');
  });

  it('com um documento faltando, mantém o aviso e o botão de enviar', () => {
    const html = renderizarPagina({ usuario, dados: null, documentosNoServidor: [CNH_GRAVADA] });
    expect(html).toContain('Seu cadastro já foi criado. Envie os documentos que faltam.');
    expect(html).toContain('Enviar documentos');
    expect(html).not.toContain('Seus documentos já foram enviados');
  });
});
