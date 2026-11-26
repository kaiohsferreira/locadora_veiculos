import type { ReactNode } from 'react';
import { Esqueleto, Icone, ImagemVeiculo } from '@/components/ui';
import { ChipSituacaoReserva } from '@/features/area-cliente/reservas/componentes/ChipSituacaoReserva';
import { calcularDiarias } from '@/features/area-cliente/reservas/calculosReserva';
import { montarCidadeUf } from '@/features/area-cliente/reservas/enderecoLocadora';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import { formatarPlaca, VALOR_AUSENTE } from '@/lib/formatters';
import {
  mensagemReservasDiarias,
  mensagemReservasLocalOuDiarias,
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
  aoFalharFotoCapa?: () => void;
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

/**
 * Histórico da tela Minhas reservas (desenho 29 na tabela, desenho 28 em cartões). Abaixo de md, cartões; a partir
 * de md, a tabela de 6 colunas. Os dois são renderizados e o CSS escolhe qual aparece.
 */
export function ListaHistoricoReservas({ itens, carregando, aoFalharFotoCapa }: ListaHistoricoReservasProps) {
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
          : itens.map((reserva) => (
              <li key={reserva.idReserva} className="p-4">
                <CartaoHistoricoReserva reserva={reserva} aoFalharFotoCapa={aoFalharFotoCapa} />
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

/**
 * Cartão do histórico no celular (desenho 28): situação, código (cinza, pequeno) e valor na primeira linha; foto
 * miniatura com o nome, o período e a última linha "cidade • Placa XXX" (sem cidade: "Placa XXX"; sem placa: "N
 * diárias"). Reserva cancelada: 85% de opacidade e valor riscado, pequeno e cinza (11 px, texto-apoio), como no
 * desenho 28. Locadora e diárias ficam de fora do celular, como no desenho.
 */
function CartaoHistoricoReserva({
  reserva,
  aoFalharFotoCapa,
}: {
  reserva: ReservaCliente;
  aoFalharFotoCapa?: () => void;
}) {
  const cidadeUf = montarCidadeUf(reserva);
  const placa = reserva.placa?.trim() ?? '';
  const diarias = calcularDiarias(reserva.dataRetirada, reserva.dataDevolucao);
  const localOuDiarias = mensagemReservasLocalOuDiarias(cidadeUf, placa ? formatarPlaca(placa) : '', diarias);
  const cancelada = reserva.status === 'CANCELADA';

  return (
    <div className={cancelada ? 'space-y-2 opacity-85' : 'space-y-2'}>
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
        <ChipSituacaoReserva status={reserva.status} variante="celular" />
        <span className="text-micro text-texto-apoio">{reserva.codigo}</span>
        <span
          className={
            cancelada
              ? 'ml-auto text-micro font-medium tabular-nums text-texto-apoio line-through'
              : 'ml-auto text-corpo-denso font-bold tabular-nums text-navy'
          }
        >
          {reserva.valorEstimadoStr || VALOR_AUSENTE}
        </span>
      </div>
      <div className="flex items-center gap-3">
        <ImagemVeiculo
          url={reserva.fotoCapa ? urlDaImagem(reserva.fotoCapa) : null}
          alt=""
          ajuste="cover"
          className="h-12 w-16 shrink-0 rounded-sm"
          tamanhoIcone={24}
          aoFalhar={aoFalharFotoCapa}
        />
        <div className="min-w-0">
          <p className="text-corpo-denso font-semibold text-navy">{reserva.marca} {reserva.modelo}</p>
          <p className="text-legenda text-texto-suave">
            {mensagemReservasPeriodoDatas(reserva.dataRetiradaStr, reserva.dataDevolucaoStr)}
          </p>
          <p className="text-micro text-texto-suave">{localOuDiarias}</p>
        </div>
      </div>
    </div>
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
