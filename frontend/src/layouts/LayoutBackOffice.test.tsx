import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { CAMINHOS } from '@/routes/caminhos';
import type { UsuarioSessao } from '@/types/autenticacao';
import { LayoutBackOffice } from './LayoutBackOffice';

const ROTULO_EQUIPE = '>Equipe</a>';
const ROTULO_VISAO_GERAL = '>Visão geral</a>';

function contextoCom(usuario: UsuarioSessao | null, autenticacaoIgnorada = false): AutenticacaoContextoValor {
  return {
    usuario,
    autenticado: usuario !== null,
    autenticacaoIgnorada,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: (perfis) => usuario !== null && perfis.includes(usuario.perfil),
  };
}

function renderizarLayout(usuario: UsuarioSessao | null, autenticacaoIgnorada = false): string {
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={contextoCom(usuario, autenticacaoIgnorada)}>
      <MemoryRouter initialEntries={[CAMINHOS.visaoGeral]}>
        <Routes>
          <Route element={<LayoutBackOffice />}>
            <Route path={CAMINHOS.visaoGeral} element={<p>Conteúdo da página</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AutenticacaoContexto.Provider>,
  );
}

const ADMINISTRADOR: UsuarioSessao = {
  nome: 'Marina Costa',
  perfil: 'funcionario',
  cargo: 'ADMINISTRADOR',
  idEmpresa: 4,
  empresaNomeFantasia: 'Rota Sul Locadora',
};

const ATENDENTE: UsuarioSessao = { nome: 'Caio Lima', perfil: 'funcionario', cargo: 'ATENDENTE' };

describe('LayoutBackOffice: logotipo leva à visão geral', () => {
  /** Trecho de abertura do link do logotipo (o que tem o aria-label da marca). */
  function linkDaMarca(marcado: string): string {
    return marcado.match(/<a[^>]*aria-label="AutoVia back office[^"]*"[^>]*>/)?.[0] ?? '';
  }

  it('o link da marca aponta para a visão geral', () => {
    const link = linkDaMarca(renderizarLayout(ADMINISTRADOR));

    expect(link).toContain(`href="${CAMINHOS.visaoGeral}"`);
    expect(link).toContain('aria-label="AutoVia back office, ir para a visão geral"');
  });

  it('o link da marca não aponta mais para a frota', () => {
    const link = linkDaMarca(renderizarLayout(ATENDENTE));

    expect(link).not.toContain(`href="${CAMINHOS.frota}"`);
  });
});

describe('LayoutBackOffice: menu por cargo', () => {
  it('mostra Equipe e a seção Administração para ADMINISTRADOR', () => {
    const marcado = renderizarLayout(ADMINISTRADOR);

    expect(marcado).toContain(ROTULO_EQUIPE);
    expect(marcado).toContain('aria-label="Administração"');
  });

  it('esconde Equipe e a seção Administração para os demais cargos', () => {
    const marcado = renderizarLayout(ATENDENTE);

    expect(marcado).not.toContain(ROTULO_EQUIPE);
    expect(marcado).not.toContain('aria-label="Administração"');
  });

  it('mostra Visão geral para todos os cargos', () => {
    expect(renderizarLayout(ADMINISTRADOR)).toContain(ROTULO_VISAO_GERAL);
    expect(renderizarLayout(ATENDENTE)).toContain(ROTULO_VISAO_GERAL);
  });

  it('mantém a ordem: Visão geral no topo, os 8 módulos atuais e Equipe na seção de administração', () => {
    const marcado = renderizarLayout(ADMINISTRADOR);
    const posicoes = [
      'Visão geral',
      'Frota',
      'Clientes',
      'Reservas',
      'Locações',
      'Vendas',
      'Financeiro',
      'Manutenção',
      'Seguros',
      'Equipe',
    ].map((rotulo) => marcado.indexOf(`>${rotulo}</a>`));

    for (const posicao of posicoes) expect(posicao).toBeGreaterThan(-1);
    expect([...posicoes].sort((a, b) => a - b)).toEqual(posicoes);
  });

  it('não traz Dados da locadora nem as funções escondidas pelo dono do produto', () => {
    const marcado = renderizarLayout(ADMINISTRADOR);

    for (const termo of ['Dados da locadora', 'Nova Locação', 'Exportar', 'Notificações', 'Configurações']) {
      expect(marcado).not.toContain(termo);
    }
  });

  it('o ícone de Visão geral é grid_view, como na tela 39 do desenho', () => {
    const marcado = renderizarLayout(ADMINISTRADOR);

    expect(marcado).toContain('>grid_view</span>');
    expect(marcado).not.toContain('>dashboard</span>');
  });
});

describe('LayoutBackOffice: modo de desenvolvimento sem autenticação', () => {
  it('mostra os itens restritos (Equipe e Administração) quando a autenticação está ignorada', () => {
    const marcado = renderizarLayout(null, true);

    expect(marcado).toContain(ROTULO_EQUIPE);
    expect(marcado).toContain('aria-label="Administração"');
  });

  it('mantém o menu restrito quando a autenticação não está ignorada e o cargo não permite', () => {
    const marcado = renderizarLayout(ATENDENTE, false);

    expect(marcado).not.toContain(ROTULO_EQUIPE);
  });
});

describe('LayoutBackOffice: rodapé e cabeçalho com a locadora', () => {
  it('rodapé mostra "cargo · nome fantasia" quando há locadora', () => {
    const marcado = renderizarLayout(ADMINISTRADOR);

    expect(marcado).toContain('Marina Costa');
    expect(marcado).toContain('Administrador · Rota Sul Locadora');
  });

  it('rodapé mostra só o cargo quando não há locadora', () => {
    const marcado = renderizarLayout({ nome: 'Marina Costa', perfil: 'funcionario', cargo: 'ADMINISTRADOR' });

    expect(marcado).toContain('>Administrador</p>');
    expect(marcado).not.toContain('Administrador ·');
  });

  it('rodapé mostra o texto de antes quando não há cargo nem locadora', () => {
    const marcado = renderizarLayout({ nome: 'Dani Reis', perfil: 'funcionario' });

    expect(marcado).toContain('>Funcionário</p>');
  });

  it('cabeçalho mostra o nome fantasia quando há locadora', () => {
    const marcado = renderizarLayout(ADMINISTRADOR);

    expect(marcado).toContain('>Rota Sul Locadora</span>');
  });

  it('cabeçalho mantém "AutoVia" como reserva quando não há locadora', () => {
    const marcado = renderizarLayout(ATENDENTE);

    expect(marcado).toContain('>AutoVia</span>');
  });
});

describe('LayoutBackOffice: legenda do rodapé (cargo e locadora)', () => {
  it('funcionário sem cargo e com locadora mostra "Funcionário · <nome fantasia>"', () => {
    const marcado = renderizarLayout({
      nome: 'Dani Reis',
      perfil: 'funcionario',
      empresaNomeFantasia: 'Rota Sul Locadora',
    });

    expect(marcado).toContain('>Funcionário · Rota Sul Locadora</p>');
  });

  it('com cargo e locadora mostra "cargo · nome fantasia"', () => {
    const marcado = renderizarLayout({
      nome: 'Bia Lima',
      perfil: 'funcionario',
      cargo: 'FINANCEIRO',
      empresaNomeFantasia: 'Rota Sul Locadora',
    });

    expect(marcado).toContain('>Financeiro · Rota Sul Locadora</p>');
  });

  it('com cargo e sem locadora mostra só o cargo', () => {
    const marcado = renderizarLayout(ATENDENTE);

    expect(marcado).toContain('>Atendente</p>');
  });
});
describe('menu da manutenção (P13: só gestor de frota e administrador)', () => {
  const ROTULO_MANUTENCAO = `href="${CAMINHOS.manutencao}"`;
  const GESTOR_FROTA: UsuarioSessao = { nome: 'Lia Rocha', perfil: 'funcionario', cargo: 'GESTOR_FROTA' };
  const FINANCEIRO: UsuarioSessao = { nome: 'Bia Lima', perfil: 'funcionario', cargo: 'FINANCEIRO' };

  it('aparece para o gestor de frota e para o administrador', () => {
    expect(renderizarLayout(GESTOR_FROTA)).toContain(ROTULO_MANUTENCAO);
    expect(renderizarLayout(ADMINISTRADOR)).toContain(ROTULO_MANUTENCAO);
  });

  it('some para os demais cargos (atendente e financeiro)', () => {
    expect(renderizarLayout(ATENDENTE)).not.toContain(ROTULO_MANUTENCAO);
    expect(renderizarLayout(FINANCEIRO)).not.toContain(ROTULO_MANUTENCAO);
  });

  it('o resto do menu segue igual para o atendente (Frota e Visão geral)', () => {
    const marcado = renderizarLayout(ATENDENTE);

    expect(marcado).toContain(`href="${CAMINHOS.frota}"`);
    expect(marcado).toContain(`href="${CAMINHOS.visaoGeral}"`);
  });
});

describe('menu das políticas de cobranças extras', () => {
  it('permite descoberta somente pelo administrador', () => {
    expect(renderizarLayout(ADMINISTRADOR)).toContain(`href="${CAMINHOS.configuracoesCobrancasExtras}"`);
    expect(renderizarLayout(ATENDENTE)).not.toContain(`href="${CAMINHOS.configuracoesCobrancasExtras}"`);
  });
});
