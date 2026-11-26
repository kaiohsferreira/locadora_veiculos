import { Esqueleto, Icone, ImagemVeiculo } from '@/components/ui';
import { periodoDaLocacao, valorTotalDaLocacao } from '@/features/area-cliente/locacoes/regrasLocacoes';
import { montarCidadeUf } from '@/features/area-cliente/reservas/enderecoLocadora';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import { formatarPlaca } from '@/lib/formatters';
import { MENSAGEM_LOCACOES_VALOR_TOTAL } from '@/lib/mensagensLocacoesCliente';
import { mensagemLocacaoNumero } from '@/lib/mensagensPagamentosCliente';
import { mensagemReservasLocalOuDiarias } from '@/lib/mensagensReservas';
import { ROTULO_STATUS_LOCACAO, type StatusLocacao } from '@/types/enums';
import type { LocacaoCliente } from '@/types/locacoesCliente';

export interface ListaHistoricoLocacoesProps {
  itens: readonly LocacaoCliente[];
  /** Primeira resposta ainda não chegou: mostra o esqueleto. */
  carregando: boolean;
  /** Chamado quando a foto de capa falha. A tela decide se refaz a consulta. */
  aoFalharFotoCapa?: () => void;
}

const LINHAS_ESQUELETO = 3;

/**
 * Histórico (desenho 27 no computador e cartões compactos no celular, como na tela 28). Abaixo de md, a lista
 * compacta; a partir de md, três colunas. As duas são renderizadas e o CSS escolhe qual aparece.
 */
export function ListaHistoricoLocacoes({ itens, carregando, aoFalharFotoCapa }: ListaHistoricoLocacoesProps) {
  if (carregando) {
    return (
      <div aria-busy="true" aria-hidden="true" className="grid gap-4 md:grid-cols-3">
        {Array.from({ length: LINHAS_ESQUELETO }, (_, indice) => (
          <div key={indice} className="flex flex-col gap-3 rounded-xl bg-branco p-4 shadow-sm">
            <Esqueleto className="aspect-video w-full" />
            <Esqueleto className="h-4 w-24" />
            <Esqueleto className="h-4 w-3/4" />
            <Esqueleto className="h-4 w-1/2" />
          </div>
        ))}
      </div>
    );
  }

  return (
    <>
      <ul className="flex flex-col divide-y divide-borda md:hidden">
        {itens.map((locacao) => (
          <li key={locacao.idLocacao} className="py-3">
            <ItemHistoricoCompacto locacao={locacao} aoFalharFotoCapa={aoFalharFotoCapa} />
          </li>
        ))}
      </ul>

      <ul className="hidden gap-4 md:grid md:grid-cols-3">
        {itens.map((locacao) => (
          <li key={locacao.idLocacao} className="flex">
            <CartaoHistoricoLocacao locacao={locacao} aoFalharFotoCapa={aoFalharFotoCapa} />
          </li>
        ))}
      </ul>
    </>
  );
}

/** Chip de situação do histórico no celular: a cancelada em vermelho com ícone, como na tela 28. */
function ChipSituacaoLocacao({ status }: { status: StatusLocacao }) {
  const cancelada = status === 'CANCELADA';
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-micro font-semibold ${
        cancelada ? 'bg-manutencao-alerta-trilho text-manutencao-alerta-escuro' : 'bg-steel-suave text-status-neutro'
      }`}
    >
      {cancelada ? <Icone nome="cancel" tamanho={12} /> : <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-status-sucesso" />}
      {ROTULO_STATUS_LOCACAO[status]}
    </span>
  );
}

/** Item do histórico no celular: situação, número e valor na primeira linha; foto miniatura, nome, período e local. */
function ItemHistoricoCompacto({ locacao, aoFalharFotoCapa }: { locacao: LocacaoCliente; aoFalharFotoCapa?: () => void }) {
  const cancelada = locacao.status === 'CANCELADA';
  const cidadeUf = montarCidadeUf(locacao);
  return (
    <div className={`flex flex-col gap-2 ${cancelada ? 'opacity-85' : ''}`}>
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
        <ChipSituacaoLocacao status={locacao.status} />
        <span className="text-micro text-texto-apoio">{mensagemLocacaoNumero(locacao.idLocacao)}</span>
        <span
          className={
            cancelada
              ? 'ml-auto text-micro font-medium tabular-nums text-texto-apoio line-through'
              : 'ml-auto text-corpo-denso font-bold tabular-nums text-navy-profundo'
          }
        >
          {valorTotalDaLocacao(locacao)}
        </span>
      </div>
      <div className="flex items-center gap-3">
        <ImagemVeiculo
          url={locacao.fotoCapa ? urlDaImagem(locacao.fotoCapa) : null}
          alt=""
          ajuste="cover"
          className="h-12 w-16 shrink-0 rounded-sm"
          tamanhoIcone={24}
          aoFalhar={aoFalharFotoCapa}
        />
        <div className="min-w-0">
          <p className="text-corpo-denso font-semibold text-navy-profundo">
            {locacao.marca} {locacao.modelo}
          </p>
          <p className="text-legenda text-texto-apoio">{periodoDaLocacao(locacao)}</p>
          <p className="text-micro text-texto-apoio">
            {mensagemReservasLocalOuDiarias(cidadeUf, formatarPlaca(locacao.placa), locacao.diarias)}
          </p>
        </div>
      </div>
    </div>
  );
}

/** Cartão do histórico no computador (desenho 27): foto 16:9 com a situação e a placa, categoria, período e local. */
function CartaoHistoricoLocacao({ locacao, aoFalharFotoCapa }: { locacao: LocacaoCliente; aoFalharFotoCapa?: () => void }) {
  const cancelada = locacao.status === 'CANCELADA';
  return (
    <article className="flex w-full flex-col gap-4 rounded-xl bg-branco p-4 shadow-sm transition-shadow hover:shadow-md">
      <div className="relative aspect-video w-full overflow-hidden rounded-lg bg-steel-fundo">
        <ImagemVeiculo
          url={locacao.fotoCapa ? urlDaImagem(locacao.fotoCapa) : null}
          alt={`${locacao.marca} ${locacao.modelo}`}
          ajuste="cover"
          className="h-full w-full"
          tamanhoIcone={48}
          aoFalhar={aoFalharFotoCapa}
        />
        <span className="absolute left-2 top-2 rounded bg-steel-alto/90 px-2 py-0.5 text-legenda font-bold text-navy-profundo">
          {ROTULO_STATUS_LOCACAO[locacao.status]}
        </span>
        <span className="absolute bottom-2 right-2 rounded bg-branco px-1.5 py-0.5 text-micro font-bold tracking-wider text-navy-profundo shadow-sm">
          {formatarPlaca(locacao.placa)}
        </span>
      </div>

      <div className="flex flex-col gap-1">
        <span className="text-legenda font-semibold uppercase text-texto-apoio">{locacao.categoriaNome}</span>
        <h3 className="break-words text-corpo font-bold text-navy-profundo">
          {locacao.marca} {locacao.modelo}
        </h3>
      </div>

      <div className="flex flex-col gap-1 rounded-lg bg-steel-fundo p-2.5 text-legenda text-texto-apoio">
        <span className="flex items-center gap-2">
          <Icone nome="calendar_month" tamanho={16} className="text-texto-suave" />
          <span>{periodoDaLocacao(locacao)}</span>
        </span>
        <span className="flex min-w-0 items-center gap-2">
          <Icone nome="pin_drop" tamanho={16} className="text-texto-suave" />
          <span className="truncate">{locacao.locadoraNomeFantasia}</span>
        </span>
      </div>

      <div className="mt-auto flex items-end justify-between gap-2 pt-2">
        <div className="flex flex-col">
          <span className="text-legenda text-texto-apoio">{mensagemLocacaoNumero(locacao.idLocacao)}</span>
          <span className="text-legenda text-texto-apoio">{MENSAGEM_LOCACOES_VALOR_TOTAL}</span>
        </div>
        <span
          className={
            cancelada
              ? 'text-corpo font-medium text-texto-apoio line-through'
              : 'text-marca font-bold tabular-nums text-navy-profundo'
          }
        >
          {valorTotalDaLocacao(locacao)}
        </span>
      </div>
    </article>
  );
}
