import { Icone } from '@/components/ui';
import { montarEnderecoLocadora, montarLinkMapa } from '@/features/area-cliente/reservas/enderecoLocadora';
import {
  mensagemReservasDiarias,
  MENSAGEM_RESERVAS_COMO_CHEGAR,
  MENSAGEM_RESERVAS_COMO_CHEGAR_NOVA_ABA,
  MENSAGEM_RESERVAS_DEVOLUCAO,
  MENSAGEM_RESERVAS_RETIRADA,
  MENSAGEM_RESERVAS_ROTEIRO,
} from '@/lib/mensagens';
import type { ReservaCliente } from '@/types/reservas';

export interface RoteiroReservaProps {
  reserva: ReservaCliente;
  diarias: number;
}

/**
 * Retirada e devolução com a data (a reserva guarda só o dia, sem hora). Como no desenho 29, o nome da locadora e o
 * endereço ficam dentro de cada etapa: na retirada, com o link para o mapa; na devolução, sem link.
 */
export function RoteiroReserva({ reserva, diarias }: RoteiroReservaProps) {
  const endereco = montarEnderecoLocadora(reserva);
  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="text-micro font-bold uppercase tracking-wider text-texto-suave">
          {MENSAGEM_RESERVAS_ROTEIRO}
        </span>
        <span className="rounded-sm bg-steel-suave px-2 py-0.5 text-micro font-semibold text-navy">
          {mensagemReservasDiarias(diarias)}
        </span>
      </div>

      <ol className="relative space-y-4 before:absolute before:bottom-4 before:left-3 before:top-4 before:w-0.5 before:bg-borda-forte">
        <li className="relative pl-8">
          <span
            aria-hidden="true"
            className="absolute left-1 top-1 flex h-4 w-4 items-center justify-center rounded-full bg-orange ring-4 ring-branco"
          >
            <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-branco" />
          </span>
          <div className="flex flex-col gap-0.5">
            <p className="text-micro font-bold uppercase text-orange-escuro">{MENSAGEM_RESERVAS_RETIRADA}</p>
            <p className="text-corpo font-bold tabular-nums text-navy">{reserva.dataRetiradaStr}</p>
            <p className="min-w-0 break-words text-corpo-denso font-semibold text-navy">
              {reserva.locadoraNomeFantasia}
            </p>
            {endereco && <p className="break-words text-legenda text-status-neutro">{endereco}</p>}
            {endereco && <LinkMapaReserva reserva={reserva} className="hidden pt-1 md:inline-flex" />}
          </div>
        </li>
        <li className="relative pl-8">
          <span
            aria-hidden="true"
            className="absolute left-1 top-1 flex h-4 w-4 items-center justify-center rounded-full bg-navy ring-4 ring-branco"
          >
            <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-branco" />
          </span>
          <div className="flex flex-col gap-0.5">
            <p className="text-micro font-bold uppercase text-navy">{MENSAGEM_RESERVAS_DEVOLUCAO}</p>
            <p className="text-corpo font-bold tabular-nums text-navy">{reserva.dataDevolucaoStr}</p>
            <p className="min-w-0 break-words text-corpo-denso font-semibold text-navy">
              {reserva.locadoraNomeFantasia}
            </p>
            {endereco && <p className="break-words text-legenda text-status-neutro">{endereco}</p>}
          </div>
        </li>
      </ol>
    </>
  );
}

export function LinkMapaReserva({ reserva, className }: { reserva: ReservaCliente; className: string }) {
  return (
    <a
      href={montarLinkMapa(reserva)}
      target="_blank"
      rel="noreferrer"
      className={`items-center gap-1 text-rotulo font-semibold text-navy transition-colors hover:text-orange-escuro focus-visible:outline focus-visible:outline-2 focus-visible:outline-steel ${className}`}
    >
      <Icone nome="near_me" tamanho={14} />
      <span>{MENSAGEM_RESERVAS_COMO_CHEGAR}</span>
      <span className="sr-only">{MENSAGEM_RESERVAS_COMO_CHEGAR_NOVA_ABA}</span>
    </a>
  );
}
