import { Suspense } from 'react';
import { Outlet } from 'react-router-dom';
import { AvisoModoDesenvolvimento } from '@/components/AvisoModoDesenvolvimento';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { ACESSO_PULAR_CONTEUDO } from '@/lib/mensagens';

/** Layout da tela de acesso (/entrar): tela cheia, sem menu do topo e sem rodapé do site. */
export function LayoutAcesso() {
  return (
    <div className="min-h-screen bg-offwhite">
      <a
        href="#conteudo"
        className="sr-only z-50 rounded-md bg-navy px-4 py-2 text-branco focus:not-sr-only focus:fixed focus:left-4 focus:top-4"
      >
        {ACESSO_PULAR_CONTEUDO}
      </a>
      <AvisoModoDesenvolvimento />

      <main id="conteudo" tabIndex={-1} className="focus:outline-none">
        <Suspense fallback={<CarregandoPagina />}>
          <Outlet />
        </Suspense>
      </main>
    </div>
  );
}
