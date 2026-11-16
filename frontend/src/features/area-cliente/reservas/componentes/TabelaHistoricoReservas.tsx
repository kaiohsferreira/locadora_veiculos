import { Esqueleto, Icone } from '@/components/ui';
import { ChipSituacaoReserva } from '@/features/area-cliente/reservas/componentes/ChipSituacaoReserva';
import { montarCidadeUf } from '@/features/area-cliente/reservas/enderecoLocadora';
import { calcularDiarias } from '@/features/area-cliente/reservas/calculosReserva';
import { VALOR_AUSENTE } from '@/lib/formatters';
import {
  mensagemReservasDiarias,
  mensagemReservasPeriodoDatas,
  MENSAGEM_RESERVAS_COLUNA_LOCADORA,
  MENSAGEM_RESERVAS_COLUNA_PERIODO,
  MENSAGEM_RESERVAS_COLUNA_RESERVA,
  MENSAGEM_RESERVAS_COLUNA_STATUS,
  MENSAGEM_RESERVAS_COLUNA_VALOR,
  MENSAGEM_RESERVAS_COLUNA_VEICULO,
  MENSAGEM_RESERVAS_TABELA_LEGENDA,
} from '@/lib/mensagens';
import type { ReservaCliente } from '@/types/reservas';

export interface TabelaHistoricoReservasProps {
  itens: readonly ReservaCliente[];
  /** Mostra linhas de esqueleto no lugar dos dados. */
  carregando: boolean;
}

interface ColunaHistorico {
  titulo: string;
  /** Alinhamento do cabeçalho e das células da coluna (como no desenho 29). */
  alinhamento: string;
}

const COLUNAS: readonly ColunaHistorico[] = [
  { titulo: MENSAGEM_RESERVAS_COLUNA_RESERVA, alinhamento: 'text-left' },
  { titulo: MENSAGEM_RESERVAS_COLUNA_VEICULO, alinhamento: 'text-left' },
  { titulo: MENSAGEM_RESERVAS_COLUNA_LOCADORA, alinhamento: 'text-left' },
  { titulo: MENSAGEM_RESERVAS_COLUNA_PERIODO, alinhamento: 'text-left' },
  { titulo: MENSAGEM_RESERVAS_COLUNA_VALOR, alinhamento: 'text-right' },
  { titulo: MENSAGEM_RESERVAS_COLUNA_STATUS, alinhamento: 'text-center' },
];

const LINHAS_ESQUELETO = 5;

/**
 * Tabela do histórico com 6 colunas e sem coluna de ações (sem NF-e nem "Alugar novamente", que não têm dado ou
 * destino). A tabela rola dentro do cartão em telas estreitas, nunca a página.
 */
export function TabelaHistoricoReservas({ itens, carregando }: TabelaHistoricoReservasProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[44rem] text-left">
        <caption className="sr-only">{MENSAGEM_RESERVAS_TABELA_LEGENDA}</caption>
        <thead className="bg-steel-fundo">
          <tr>
            {COLUNAS.map((coluna) => (
              <th
                key={coluna.titulo}
                scope="col"
                className={`px-4 py-2 text-micro font-semibold uppercase tracking-wider text-texto-suave ${coluna.alinhamento}`}
              >
                {coluna.titulo}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-borda">
          {carregando
            ? Array.from({ length: LINHAS_ESQUELETO }, (_, indice) => (
                <tr key={`esqueleto-${indice}`} aria-hidden="true">
                  {COLUNAS.map((coluna) => (
                    <td key={coluna.titulo} className="px-4 py-4">
                      <Esqueleto className="h-4 w-full max-w-32" />
                    </td>
                  ))}
                </tr>
              ))
            : itens.map((reserva) => <LinhaHistoricoReserva key={reserva.idReserva} reserva={reserva} />)}
        </tbody>
      </table>
    </div>
  );
}

function LinhaHistoricoReserva({ reserva }: { reserva: ReservaCliente }) {
  const diarias = calcularDiarias(reserva.dataRetirada, reserva.dataDevolucao);
  const cidadeUf = montarCidadeUf(reserva);
  return (
    <tr className="hover:bg-steel-fundo/40">
      <td className="px-4 py-4 align-middle">
        <span className="block text-corpo-denso font-bold tabular-nums text-navy">{reserva.codigo}</span>
        <span className="block text-legenda text-texto-suave">
          {mensagemReservasPeriodoDatas(reserva.dataRetiradaStr, reserva.dataDevolucaoStr)}
        </span>
      </td>
      <td className="px-4 py-4 align-middle">
        <div className="flex items-center gap-2">
          <div className="flex h-7 w-10 shrink-0 items-center justify-center overflow-hidden rounded-sm bg-steel-fundo">
            <Icone nome="directions_car" tamanho={20} className="text-texto-suave" />
          </div>
          <div className="min-w-0">
            <p className="break-words text-corpo-denso font-semibold text-navy">
              {reserva.marca} {reserva.modelo}
            </p>
            <p className="text-micro text-texto-suave">{reserva.categoriaNome}</p>
          </div>
        </div>
      </td>
      <td className="px-4 py-4 align-middle">
        <p className="break-words text-corpo-denso text-texto">{reserva.locadoraNomeFantasia}</p>
        {cidadeUf && <p className="text-micro text-texto-suave">{cidadeUf}</p>}
      </td>
      <td className="px-4 py-4 align-middle text-corpo-denso font-medium tabular-nums text-navy">
        {mensagemReservasDiarias(diarias)}
      </td>
      <td className="px-4 py-4 align-middle text-right text-corpo font-bold tabular-nums text-navy">
        {reserva.valorEstimadoStr || VALOR_AUSENTE}
      </td>
      <td className="px-4 py-4 align-middle text-center">
        <ChipSituacaoReserva status={reserva.status} variante="tabela" />
      </td>
    </tr>
  );
}
