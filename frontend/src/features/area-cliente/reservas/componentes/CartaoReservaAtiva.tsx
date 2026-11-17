import { Icone } from '@/components/ui';
import { BotaoCopiarCodigo } from '@/features/area-cliente/reservas/componentes/BotaoCopiarCodigo';
import { ChipSituacaoReserva } from '@/features/area-cliente/reservas/componentes/ChipSituacaoReserva';
import { FotoVeiculo } from '@/features/area-cliente/reservas/componentes/FotoVeiculo';
import { RoteiroReserva } from '@/features/area-cliente/reservas/componentes/RoteiroReserva';
import { calcularDiarias, podeCancelarReserva, valorPorDiaria } from '@/features/area-cliente/reservas/calculosReserva';
import { formatarMoeda, formatarPlaca, VALOR_AUSENTE } from '@/lib/formatters';
import {
  mensagemReservasAnoModeloCor,
  mensagemReservasCriadaEm,
  mensagemReservasDiariaVezes,
  MENSAGEM_RESERVAS_BOTAO_CANCELAR,
  MENSAGEM_RESERVAS_CODIGO_ROTULO,
  MENSAGEM_RESERVAS_CONDUTOR,
  MENSAGEM_RESERVAS_TITULAR,
  MENSAGEM_RESERVAS_VALOR_ESTIMADO,
} from '@/lib/mensagens';
import type { ReservaCliente } from '@/types/reservas';

export interface CartaoReservaAtivaProps {
  reserva: ReservaCliente;
  /** Nome do cliente titular (da sessão). */
  nomeCondutor: string;
  /** Pede a confirmação do cancelamento desta reserva. */
  aoCancelar: (reserva: ReservaCliente) => void;
}

/**
 * Cartão grande da reserva (desenho 29). Mostra só o que o contrato sustenta: código, criação, veículo, roteiro,
 * condutor titular e valor estimado. A única ação é o cancelamento (com confirmação, no modal).
 */
export function CartaoReservaAtiva({ reserva, nomeCondutor, aoCancelar }: CartaoReservaAtivaProps) {
  const diarias = calcularDiarias(reserva.dataRetirada, reserva.dataDevolucao);
  const valorDiaria = valorPorDiaria(reserva.valorEstimado, diarias);
  const legendaVeiculo = mensagemReservasAnoModeloCor(reserva.anoModelo, reserva.cor);

  return (
    <article
      aria-labelledby={`reserva-${reserva.idReserva}-titulo`}
      className="overflow-hidden rounded-cartao bg-branco shadow-md"
    >
      <header className="flex flex-wrap items-center justify-between gap-4 bg-steel-fundo px-6 py-4">
        <div className="flex flex-wrap items-center gap-4">
          <div className="flex flex-wrap items-center gap-1">
            <span className="text-micro font-semibold uppercase tracking-wider text-texto-suave">
              {MENSAGEM_RESERVAS_CODIGO_ROTULO}
            </span>
            <span className="text-corpo font-bold tabular-nums text-navy" id={`reserva-${reserva.idReserva}-titulo`}>
              {reserva.codigo}
            </span>
            <BotaoCopiarCodigo codigo={reserva.codigo} />
          </div>
          <span aria-hidden="true" className="hidden h-4 w-px bg-borda-forte sm:block" />
          <span className="text-legenda text-status-neutro">{mensagemReservasCriadaEm(reserva.dataReservaStr)}</span>
        </div>
        <ChipSituacaoReserva status={reserva.status} />
      </header>

      <div className="grid grid-cols-1 items-stretch gap-8 p-6 lg:grid-cols-12">
        <div className="flex min-w-0 flex-col justify-between gap-4 lg:col-span-4">
          <div className="flex flex-col">
            <div className="mb-1 flex flex-wrap items-center justify-between gap-1">
              <span className="inline-block rounded-sm bg-steel-suave px-2.5 py-0.5 text-micro font-semibold uppercase tracking-wider text-navy">
                {reserva.categoriaNome}
              </span>
              <span className="inline-flex h-6 items-center gap-1.5 overflow-hidden rounded-sm bg-branco px-2 shadow-sm">
                <span aria-hidden="true" className="inline-block h-2 w-2.5 rounded-sm bg-navy-claro" />
                <span className="text-micro font-bold tracking-widest text-navy">{formatarPlaca(reserva.placa)}</span>
              </span>
            </div>
            <h2 className="break-words text-nome-veiculo leading-tight tracking-tighter text-navy">
              {reserva.marca} {reserva.modelo}
            </h2>
            <p className="text-legenda text-status-neutro">{legendaVeiculo}</p>
          </div>
          <FotoVeiculo fotoCapa={reserva.fotoCapa} descricao={`${reserva.marca} ${reserva.modelo}`} />
        </div>

        <div className="flex min-w-0 flex-col gap-4 lg:col-span-4 lg:border-l lg:border-borda lg:pl-6">
          <RoteiroReserva reserva={reserva} diarias={diarias} />
        </div>

        <div className="flex min-w-0 flex-col justify-between gap-4 lg:col-span-4 lg:border-l lg:border-borda lg:pl-6">
          <div className="flex flex-col gap-1">
            <span className="text-micro font-bold uppercase tracking-wider text-texto-suave">
              {MENSAGEM_RESERVAS_CONDUTOR}
            </span>
            <div className="flex items-center gap-1 rounded bg-steel-fundo p-2">
              <Icone nome="badge" tamanho={20} className="text-navy" />
              <div className="min-w-0">
                <p className="break-words text-corpo-denso font-bold text-navy">{nomeCondutor}</p>
                <p className="text-micro text-texto-suave">{MENSAGEM_RESERVAS_TITULAR}</p>
              </div>
            </div>
          </div>

          <div className="space-y-1 rounded-cartao bg-steel-suave p-4">
            {valorDiaria !== null && (
              <div className="flex items-center justify-between gap-2 text-legenda text-status-neutro">
                <span>{mensagemReservasDiariaVezes(diarias, formatarMoeda(valorDiaria))}</span>
              </div>
            )}
            <div className="flex items-baseline justify-between gap-2">
              <span className="text-micro font-bold uppercase tracking-wider text-texto-suave">
                {MENSAGEM_RESERVAS_VALOR_ESTIMADO}
              </span>
              <span className="text-marca font-bold tabular-nums text-navy">
                {reserva.valorEstimadoStr || VALOR_AUSENTE}
              </span>
            </div>
          </div>
        </div>
      </div>

      {podeCancelarReserva(reserva.status) && (
        <div className="flex flex-wrap items-center justify-between gap-4 border-t border-borda bg-steel-fundo/70 px-6 py-4">
          <button
            type="button"
            onClick={() => aoCancelar(reserva)}
            className="inline-flex h-10 items-center gap-1.5 rounded-sm bg-branco px-4 text-corpo-denso font-semibold text-navy shadow-sm transition-colors hover:bg-steel-suave focus-visible:outline focus-visible:outline-2 focus-visible:outline-steel"
          >
            <Icone nome="event_busy" tamanho={18} />
            <span>{MENSAGEM_RESERVAS_BOTAO_CANCELAR}</span>
          </button>
        </div>
      )}
    </article>
  );
}
