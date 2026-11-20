import type { ReactNode } from 'react';
import { Esqueleto, Icone } from '@/components/ui';
import { ChipSituacaoReserva } from '@/features/area-cliente/reservas/componentes/ChipSituacaoReserva';
import { calcularDiarias } from '@/features/area-cliente/reservas/calculosReserva';
import { montarCidadeUf } from '@/features/area-cliente/reservas/enderecoLocadora';
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

export interface ListaHistoricoReservasProps {
  itens: readonly ReservaCliente[];
  carregando: boolean;
}

type CampoHistorico = 'reserva' | 'veiculo' | 'locadora' | 'periodo' | 'valor' | 'status';

interface ColunaHistorico {
  campo: CampoHistorico;
  titulo: string;
  alinhamento: string;
}

const COLUNAS: readonly ColunaHistorico[] = [
  { campo: 'reserva', titulo: MENSAGEM_RESERVAS_COLUNA_RESERVA, alinhamento: 'text-left' },
  { campo: 'veiculo', titulo: MENSAGEM_RESERVAS_COLUNA_VEICULO, alinhamento: 'text-left' },
  { campo: 'locadora', titulo: MENSAGEM_RESERVAS_COLUNA_LOCADORA, alinhamento: 'text-left' },
  { campo: 'periodo', titulo: MENSAGEM_RESERVAS_COLUNA_PERIODO, alinhamento: 'text-left' },
  { campo: 'valor', titulo: MENSAGEM_RESERVAS_COLUNA_VALOR, alinhamento: 'text-right' },
  { campo: 'status', titulo: MENSAGEM_RESERVAS_COLUNA_STATUS, alinhamento: 'text-center' },
];

const LINHAS_ESQUELETO = 5;

/** Cartões em telas estreitas e tabela em telas largas, com os mesmos campos. */
export function ListaHistoricoReservas({ itens, carregando }: ListaHistoricoReservasProps) {
  const registros = itens.map((reserva) => ({
    id: reserva.idReserva,
    campos: apresentarCamposHistorico(reserva),
  }));

  return (
    <>
      <ul
        aria-label={MENSAGEM_RESERVAS_TABELA_LEGENDA}
        aria-hidden={carregando || undefined}
        className="list-disc divide-y divide-borda [overflow-wrap:anywhere] marker:text-transparent md:hidden"
      >
        {carregando
          ? Array.from({ length: LINHAS_ESQUELETO }, (_, indice) => (
              <li key={indice} className="space-y-3 p-4">
                <Esqueleto className="h-4 w-24" />
                <Esqueleto className="h-4 w-3/4" />
                <Esqueleto className="h-4 w-1/2" />
              </li>
            ))
          : registros.map(({ id, campos }) => (
              <li key={id} className="p-4">
                <dl className="grid grid-cols-2 gap-x-3 gap-y-4">
                  {COLUNAS.map(({ campo, titulo }) => (
                    <div key={campo} className={campo === 'periodo' || campo === 'valor' ? '' : 'col-span-2'}>
                      <dt className="mb-1 text-micro font-semibold uppercase tracking-wider text-texto-suave">
                        {titulo}
                      </dt>
                      <dd>{campos[campo]}</dd>
                    </div>
                  ))}
                </dl>
              </li>
            ))}
      </ul>

      <table className="hidden w-full text-left [overflow-wrap:anywhere] md:table">
        <caption className="sr-only">{MENSAGEM_RESERVAS_TABELA_LEGENDA}</caption>
        <thead className="bg-steel-fundo">
          <tr>
            {COLUNAS.map(({ campo, titulo, alinhamento }) => (
              <th
                key={campo}
                scope="col"
                className={`px-4 py-2 text-micro font-semibold uppercase tracking-wider text-texto-suave ${alinhamento}`}
              >
                {titulo}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-borda">
          {carregando
            ? Array.from({ length: LINHAS_ESQUELETO }, (_, indice) => (
                <tr key={indice} aria-hidden="true">
                  {COLUNAS.map(({ campo }) => (
                    <td key={campo} className="px-4 py-4">
                      <Esqueleto className="h-4 w-full max-w-32" />
                    </td>
                  ))}
                </tr>
              ))
            : registros.map(({ id, campos }) => (
                <tr key={id} className="hover:bg-steel-fundo/40">
                  {COLUNAS.map(({ campo, alinhamento }) => (
                    <td key={campo} className={`px-4 py-4 align-middle ${alinhamento}`}>
                      {campos[campo]}
                    </td>
                  ))}
                </tr>
              ))}
        </tbody>
      </table>
    </>
  );
}

function apresentarCamposHistorico(reserva: ReservaCliente): Record<CampoHistorico, ReactNode> {
  const diarias = calcularDiarias(reserva.dataRetirada, reserva.dataDevolucao);
  const cidadeUf = montarCidadeUf(reserva);
  return {
    reserva: (
      <>
        <span className="block text-corpo-denso font-bold tabular-nums text-navy">{reserva.codigo}</span>
        <span className="block text-legenda text-texto-suave">
          {mensagemReservasPeriodoDatas(reserva.dataRetiradaStr, reserva.dataDevolucaoStr)}
        </span>
      </>
    ),
    veiculo: (
      <div className="flex items-center gap-2">
        <div className="flex h-7 w-10 shrink-0 items-center justify-center overflow-hidden rounded-sm bg-steel-fundo">
          <Icone nome="directions_car" tamanho={20} className="text-texto-suave" />
        </div>
        <div className="min-w-0">
          <p className="text-corpo-denso font-semibold text-navy">
            {reserva.marca} {reserva.modelo}
          </p>
          <p className="text-micro text-texto-suave">{reserva.categoriaNome}</p>
        </div>
      </div>
    ),
    locadora: (
      <>
        <p className="text-corpo-denso text-texto">{reserva.locadoraNomeFantasia}</p>
        {cidadeUf && <p className="text-micro text-texto-suave">{cidadeUf}</p>}
      </>
    ),
    periodo: (
      <span className="text-corpo-denso font-medium tabular-nums text-navy">
        {mensagemReservasDiarias(diarias)}
      </span>
    ),
    valor: (
      <span className="text-corpo font-bold tabular-nums text-navy">
        {reserva.valorEstimadoStr || VALOR_AUSENTE}
      </span>
    ),
    status: <ChipSituacaoReserva status={reserva.status} variante="tabela" />,
  };
}
