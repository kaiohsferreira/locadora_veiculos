import { Esqueleto } from '@/components/ui';

/** Cartão-esqueleto de 3 colunas, enquanto a primeira resposta da aba Ativas não chega. */
export function EsqueletoReservaAtiva() {
  return (
    <div aria-hidden="true" className="overflow-hidden rounded-cartao bg-branco shadow-md">
      <div className="flex flex-wrap items-center justify-between gap-4 bg-steel-fundo px-6 py-4">
        <Esqueleto className="h-4 w-64" />
        <Esqueleto className="h-6 w-24" />
      </div>
      <div className="grid grid-cols-1 gap-8 p-6 lg:grid-cols-12">
        <div className="flex flex-col gap-4 lg:col-span-4">
          <Esqueleto className="h-5 w-40" />
          <Esqueleto className="h-6 w-56" />
          <Esqueleto className="aspect-[16/10] w-full" />
        </div>
        <div className="flex flex-col gap-4 lg:col-span-4 lg:border-l lg:border-borda lg:pl-6">
          <Esqueleto className="h-4 w-48" />
          <Esqueleto className="h-5 w-40" />
          <Esqueleto className="h-5 w-44" />
          <Esqueleto className="h-4 w-56" />
        </div>
        <div className="flex flex-col gap-4 lg:col-span-4 lg:border-l lg:border-borda lg:pl-6">
          <Esqueleto className="h-14 w-full" />
          <Esqueleto className="h-24 w-full" />
        </div>
      </div>
    </div>
  );
}
