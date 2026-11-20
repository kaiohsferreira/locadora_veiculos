import { Suspense } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';
import { AvisoModoDesenvolvimento } from '@/components/AvisoModoDesenvolvimento';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { Logotipo } from '@/components/Logotipo';
import { Icone } from '@/components/ui/Icone';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { cargoPermitido, usePermissoes } from '@/routes/autenticacao/usePermissoes';
import { CAMINHOS } from '@/routes/caminhos';
import { ROTULO_CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';

interface ModuloMenu {
  rotulo: string;
  para: string;
  /** Ícone Material Symbols. */
  icone: string;
  /** Cargos que veem o item. Sem a lista, todos os funcionários veem. */
  cargos?: readonly CargoFuncionario[];
}

/** Módulos do sistema: visão geral no topo, depois os módulos de operação. */
const MODULOS_SISTEMA: readonly ModuloMenu[] = [
  { rotulo: 'Visão geral', para: CAMINHOS.visaoGeral, icone: 'grid_view' },
  { rotulo: 'Frota', para: CAMINHOS.frota, icone: 'directions_car' },
  { rotulo: 'Clientes', para: CAMINHOS.clientes, icone: 'group', cargos: ['ATENDENTE', 'ADMINISTRADOR'] },
  { rotulo: 'Reservas', para: CAMINHOS.reservas, icone: 'calendar_month' },
  { rotulo: 'Locações', para: CAMINHOS.locacoes, icone: 'key' },
  { rotulo: 'Vendas', para: CAMINHOS.vendas, icone: 'sell' },
  { rotulo: 'Financeiro', para: CAMINHOS.financeiro, icone: 'payments' },
  { rotulo: 'Manutenção', para: CAMINHOS.manutencao, icone: 'build' },
  { rotulo: 'Seguros', para: CAMINHOS.seguros, icone: 'shield' },
];

/**
 * Administração. "Dados da locadora" não entra aqui: fica para depois (RF-73, decisão do dono do produto).
 */
const MODULOS_ADMINISTRACAO: readonly ModuloMenu[] = [
  { rotulo: 'Equipe', para: CAMINHOS.equipe, icone: 'badge', cargos: ['ADMINISTRADOR'] },
  { rotulo: 'Cobranças extras', para: CAMINHOS.configuracoesCobrancasExtras, icone: 'payments', cargos: ['ADMINISTRADOR'] },
];

/**
 * Quem vê o item. Com VITE_IGNORAR_AUTENTICACAO=true (só no npm run dev) o menu mostra tudo, como as rotas liberam tudo.
 * Fora desse modo, o servidor continua sendo a autoridade (o menu só esconde).
 */
function moduloVisivel(modulo: ModuloMenu, cargo: CargoFuncionario | null, autenticacaoIgnorada: boolean): boolean {
  if (autenticacaoIgnorada) return true;
  return modulo.cargos === undefined || cargoPermitido(cargo, modulo.cargos);
}

function classesItemMenu({ isActive }: { isActive: boolean }): string {
  const base =
    'relative flex items-center gap-3 rounded-md px-3 py-2.5 text-corpo-denso font-medium transition-colors';
  return isActive ? `${base} bg-steel-claro text-navy` : `${base} text-branco/80 hover:bg-navy-claro hover:text-branco`;
}

/**
 * Texto do rodapé: "cargo · locadora" quando houver. Sem cargo, "Funcionário" com a locadora quando houver
 * ("Funcionário · <nome fantasia>"), ou só "Funcionário" como antes.
 */
function legendaUsuario(cargo: CargoFuncionario | null, nomeFantasia: string | undefined): string {
  const rotulo = cargo === null ? 'Funcionário' : ROTULO_CARGO_FUNCIONARIO[cargo];
  return nomeFantasia ? `${rotulo} · ${nomeFantasia}` : rotulo;
}

/** Layout do back office: menu lateral fixo em navy + barra superior. Uso em desktop. */
export function LayoutBackOffice() {
  const { usuario, autenticado, autenticacaoIgnorada, sair } = useAutenticacao();
  const { cargo } = usePermissoes();
  const nomeExibido = usuario?.nome ?? (autenticacaoIgnorada ? 'Usuário de desenvolvimento' : 'Funcionário');
  const nomeLocadora = usuario?.empresaNomeFantasia;
  const sistemaVisivel = MODULOS_SISTEMA.filter((modulo) => moduloVisivel(modulo, cargo, autenticacaoIgnorada));
  const administracaoVisivel = MODULOS_ADMINISTRACAO.filter((modulo) => moduloVisivel(modulo, cargo, autenticacaoIgnorada));

  function itensMenu(modulos: readonly ModuloMenu[]) {
    return modulos.map((modulo) => (
      <li key={modulo.para}>
        <NavLink to={modulo.para} className={classesItemMenu}>
          {({ isActive }) => (
            <>
              {isActive && <span className="absolute inset-y-1 left-0 w-1 rounded-r bg-orange" aria-hidden="true" />}
              <Icone nome={modulo.icone} preenchido={isActive} />
              {modulo.rotulo}
            </>
          )}
        </NavLink>
      </li>
    ));
  }

  return (
    <div className="min-h-screen bg-offwhite">
      <a
        href="#conteudo"
        className="sr-only z-50 rounded-md bg-orange px-4 py-2 text-branco focus:not-sr-only focus:fixed focus:left-4 focus:top-4"
      >
        Pular para o conteúdo
      </a>

      <aside className="fixed inset-y-0 left-0 z-30 flex w-64 flex-col bg-navy text-branco">
        <div className="flex h-16 shrink-0 items-center border-b border-branco/10 px-5">
          <Link to={CAMINHOS.visaoGeral} aria-label="AutoVia back office, ir para a visão geral">
            <Logotipo tema="escuro" subtitulo="Back office" />
          </Link>
        </div>

        <div className="flex-1 overflow-y-auto px-3 py-6">
          <nav aria-label="Módulos do sistema">
            <p className="px-3 pb-2 text-legenda font-semibold uppercase tracking-wider text-branco/60">
              Módulos do sistema
            </p>
            <ul className="flex flex-col gap-1">{itensMenu(sistemaVisivel)}</ul>
          </nav>

          {administracaoVisivel.length > 0 && (
            <nav aria-label="Administração" className="mt-6">
              <p className="px-3 pb-2 text-legenda font-semibold uppercase tracking-wider text-branco/60">
                Administração
              </p>
              <ul className="flex flex-col gap-1">{itensMenu(administracaoVisivel)}</ul>
            </nav>
          )}
        </div>

        <div className="shrink-0 border-t border-branco/10 p-4">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-navy-claro text-branco">
              <Icone nome="badge" />
            </span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-corpo-denso font-semibold text-branco">{nomeExibido}</p>
              <p className="truncate text-legenda text-branco/60">{legendaUsuario(cargo, nomeLocadora)}</p>
            </div>
            {autenticado && (
              <button
                type="button"
                onClick={sair}
                aria-label="Sair"
                title="Sair"
                className="inline-flex h-8 w-8 items-center justify-center rounded-md text-branco/80 transition-colors hover:bg-navy-claro hover:text-branco"
              >
                <Icone nome="logout" />
              </button>
            )}
          </div>
        </div>
      </aside>

      <div className="flex min-h-screen flex-col pl-64">
        <AvisoModoDesenvolvimento />
        <header className="sticky top-0 z-20 flex h-16 items-center justify-between gap-4 border-b border-borda bg-branco px-8">
          <p className="text-corpo-denso text-texto-suave">
            <span className="font-semibold text-navy">{nomeLocadora || 'AutoVia'}</span>
            <span aria-hidden="true"> / </span>
            Back office
          </p>
          <Link
            to={CAMINHOS.inicio}
            className="inline-flex items-center gap-1 text-corpo-denso font-medium text-navy hover:underline"
          >
            <Icone nome="open_in_new" tamanho={18} />
            Ver site
          </Link>
        </header>

        <main id="conteudo" tabIndex={-1} className="flex-1 px-8 py-6 focus:outline-none">
          <Suspense fallback={<CarregandoPagina />}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  );
}
