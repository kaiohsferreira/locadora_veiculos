import { MENSAGEM_CARREGANDO } from '@/lib/mensagens';
import { Esqueleto } from './ui/Esqueleto';

/** Esqueleto exibido enquanto o código de uma página é carregado. */
export function CarregandoPagina() {
  return (
    <div className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6" aria-busy="true">
      <output className="sr-only">{MENSAGEM_CARREGANDO}</output>
      <Esqueleto className="h-4 w-32" />
      <Esqueleto className="mt-3 h-8 w-72 max-w-full" />
      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        <Esqueleto className="h-28" />
        <Esqueleto className="h-28" />
        <Esqueleto className="h-28" />
      </div>
      <Esqueleto className="mt-6 h-64" />
    </div>
  );
}
