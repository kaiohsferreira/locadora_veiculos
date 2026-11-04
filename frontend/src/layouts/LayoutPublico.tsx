import { Suspense, useId, useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';
import { AvisoModoDesenvolvimento } from '@/components/AvisoModoDesenvolvimento';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { Logotipo } from '@/components/Logotipo';
import { Botao, BotaoLink } from '@/components/ui/Botao';
import { Icone } from '@/components/ui/Icone';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';

interface LinkNavegacao {
  rotulo: string;
  para: string;
  /** Ativo só no caminho exato (usado no "Início"). */
  exato?: boolean;
}

const LINKS_NAVEGACAO: readonly LinkNavegacao[] = [
  { rotulo: 'Início', para: CAMINHOS.inicio, exato: true },
  { rotulo: 'Seminovos', para: CAMINHOS.seminovos },
  { rotulo: 'Minhas reservas', para: CAMINHOS.minhasReservas },
];

/** Ano exibido no rodapé (calculado uma vez, ao carregar a aplicação). */
const ANO_ATUAL = new Date().getFullYear();

interface EstadoLink {
  isActive: boolean;
}

function classesLinkDesktop({ isActive }: EstadoLink): string {
  const base =
    'relative flex h-16 items-center text-corpo-denso font-medium transition-colors ' +
    'after:absolute after:inset-x-0 after:bottom-0 after:h-0.5';
  return isActive ? `${base} text-navy after:bg-orange` : `${base} text-texto-suave hover:text-navy`;
}

function classesLinkMobile({ isActive }: EstadoLink): string {
  const base = 'flex items-center rounded-md border-l-4 px-3 py-3 text-corpo font-medium';
  return isActive
    ? `${base} border-orange bg-steel-claro text-navy`
    : `${base} border-transparent text-texto hover:bg-offwhite`;
}

/** Layout da área pública e do cliente: barra de navegação responsiva + rodapé. */
export function LayoutPublico() {
  const [menuAberto, setMenuAberto] = useState(false);
  const idMenuMobile = useId();
  const { usuario, autenticado, sair } = useAutenticacao();

  function fecharMenu() {
    setMenuAberto(false);
  }

  function aoSair() {
    fecharMenu();
    sair();
  }

  return (
    <div className="flex min-h-screen flex-col bg-offwhite">
      <a
        href="#conteudo"
        className="sr-only z-50 rounded-md bg-navy px-4 py-2 text-branco focus:not-sr-only focus:fixed focus:left-4 focus:top-4"
      >
        Pular para o conteúdo
      </a>
      <AvisoModoDesenvolvimento />

      <header className="sticky top-0 z-40 border-b border-borda bg-branco">
        <div className="mx-auto flex h-16 w-full max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
          <Link to={CAMINHOS.inicio} onClick={fecharMenu} aria-label="AutoVia, ir para o início">
            <Logotipo />
          </Link>

          <nav aria-label="Navegação principal" className="hidden md:block">
            <ul className="flex items-center gap-8">
              {LINKS_NAVEGACAO.map((link) => (
                <li key={link.para}>
                  <NavLink to={link.para} end={link.exato} className={classesLinkDesktop}>
                    {link.rotulo}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>

          <div className="hidden items-center gap-3 md:flex">
            {autenticado && usuario ? (
              <>
                {usuario.perfil === 'funcionario' && (
                  <BotaoLink to={CAMINHOS.backOffice} variante="fantasma" tamanho="pequeno" icone="dashboard">
                    Back office
                  </BotaoLink>
                )}
                <span className="text-corpo-denso text-texto-suave">
                  Olá, <span className="font-semibold text-navy">{usuario.nome}</span>
                </span>
                <Botao variante="contorno" tamanho="pequeno" icone="logout" onClick={aoSair}>
                  Sair
                </Botao>
              </>
            ) : (
              <BotaoLink to={CAMINHOS.entrar} variante="secundario" tamanho="pequeno" icone="login">
                Entrar
              </BotaoLink>
            )}
          </div>

          <button
            type="button"
            className="inline-flex h-10 w-10 items-center justify-center rounded-md text-navy hover:bg-offwhite md:hidden"
            aria-expanded={menuAberto}
            aria-controls={idMenuMobile}
            aria-label={menuAberto ? 'Fechar menu' : 'Abrir menu'}
            onClick={() => setMenuAberto((aberto) => !aberto)}
          >
            <Icone nome={menuAberto ? 'close' : 'menu'} tamanho={24} />
          </button>
        </div>

        <nav
          id={idMenuMobile}
          aria-label="Navegação principal (celular)"
          className={`${menuAberto ? 'block' : 'hidden'} border-t border-borda bg-branco px-4 pb-4 pt-2 md:hidden`}
        >
          <ul className="flex flex-col gap-1">
            {LINKS_NAVEGACAO.map((link) => (
              <li key={link.para}>
                <NavLink to={link.para} end={link.exato} className={classesLinkMobile} onClick={fecharMenu}>
                  {link.rotulo}
                </NavLink>
              </li>
            ))}
          </ul>
          <div className="mt-3 flex flex-col gap-2 border-t border-borda pt-3">
            {autenticado && usuario ? (
              <>
                <p className="px-3 text-corpo-denso text-texto-suave">
                  Olá, <span className="font-semibold text-navy">{usuario.nome}</span>
                </p>
                {usuario.perfil === 'funcionario' && (
                  <BotaoLink to={CAMINHOS.backOffice} variante="contorno" larguraTotal icone="dashboard" onClick={fecharMenu}>
                    Back office
                  </BotaoLink>
                )}
                <Botao variante="contorno" larguraTotal icone="logout" onClick={aoSair}>
                  Sair
                </Botao>
              </>
            ) : (
              <BotaoLink to={CAMINHOS.entrar} variante="secundario" larguraTotal icone="login" onClick={fecharMenu}>
                Entrar
              </BotaoLink>
            )}
          </div>
        </nav>
      </header>

      <main id="conteudo" tabIndex={-1} className="flex-1 focus:outline-none">
        <Suspense fallback={<CarregandoPagina />}>
          <Outlet />
        </Suspense>
      </main>

      <footer className="border-t border-borda bg-branco">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-2 px-4 py-6 text-corpo-denso text-texto-suave sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
          <p>AutoVia · Aluguel e venda de veículos da própria frota.</p>
          <p>© {ANO_ATUAL} AutoVia</p>
        </div>
      </footer>
    </div>
  );
}
