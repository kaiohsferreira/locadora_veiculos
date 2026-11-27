import type { ReactNode } from 'react';
import { Botao, Esqueleto, Icone, Tabela, type ColunaTabela } from '@/components/ui';
import { descreverPrazo, iniciaisSeguradora } from '@/features/seguros/regras/vencimento';
import {
  SEGUROS_BOTAO_EDITAR,
  SEGUROS_COLUNA_ACOES,
  SEGUROS_COLUNA_APOLICE,
  SEGUROS_COLUNA_PREMIO,
  SEGUROS_COLUNA_SEGURADORA,
  SEGUROS_COLUNA_STATUS,
  SEGUROS_COLUNA_VEICULO,
  SEGUROS_COLUNA_VIGENCIA_PRAZOS,
  SEGUROS_FILTRAR_POR_VEICULO,
  SEGUROS_LEGENDA_TABELA,
} from '@/lib/mensagensSeguros';
import type { Apolice } from '@/types/seguros';
import { EtiquetaVencimento } from './EtiquetaVencimento';

export interface TabelaApolicesProps {
  itens: readonly Apolice[];
  carregando: boolean;
  /** Conteúdo quando a lista vem vazia. */
  vazio: ReactNode;
  aoEditar: (apolice: Apolice, gatilho: HTMLElement) => void;
  /** Clicar na placa filtra as apólices daquele veículo. */
  aoFiltrarVeiculo: (apolice: Apolice) => void;
}

/** Placa como botão de filtro. */
function BotaoPlaca({ apolice, aoFiltrarVeiculo }: { apolice: Apolice; aoFiltrarVeiculo: (apolice: Apolice) => void }) {
  return (
    <button
      type="button"
      onClick={() => aoFiltrarVeiculo(apolice)}
      title={SEGUROS_FILTRAR_POR_VEICULO}
      className="font-bold tracking-wider text-navy underline-offset-2 hover:underline"
    >
      {apolice.placaVeiculo}
    </button>
  );
}

/** Seguradora com o círculo das iniciais. */
function Seguradora({ apolice }: { apolice: Apolice }) {
  return (
    <span className="flex items-center gap-2">
      <span
        aria-hidden="true"
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-navy text-legenda font-bold text-branco"
        data-testid="iniciais-seguradora"
      >
        {iniciaisSeguradora(apolice.seguradora)}
      </span>
      <span className="font-medium text-texto">{apolice.seguradora}</span>
    </span>
  );
}

/** Número da apólice com a cobertura embaixo (cortada, com o texto inteiro no title). */
function ApoliceCobertura({ apolice }: { apolice: Apolice }) {
  return (
    <span className="flex flex-col">
      <span className="font-medium text-texto tabular-nums">{apolice.numeroApolice}</span>
      {apolice.cobertura && (
        <span className="max-w-[12rem] truncate text-legenda text-texto-suave" title={apolice.cobertura}>
          {apolice.cobertura}
        </span>
      )}
    </span>
  );
}

/** Vigência "início → fim" com o prazo embaixo ("Faltam N dias"), em destaque quando vence em até 60 dias. */
function VigenciaPrazo({ apolice }: { apolice: Apolice }) {
  const prazo = descreverPrazo(apolice);
  return (
    <span className="flex flex-col">
      <span className="whitespace-nowrap text-texto tabular-nums">
        {apolice.dataInicioStr} → {apolice.dataFimStr}
      </span>
      <span
        className={`inline-flex items-center gap-1 text-legenda ${prazo.destaque ? 'font-semibold text-status-pendente' : 'text-texto-suave'}`}
        data-testid="prazo-apolice"
      >
        {prazo.destaque && <Icone nome="alarm" tamanho={14} />}
        {prazo.texto}
      </span>
    </span>
  );
}

/** Botão de editar a apólice. */
function BotaoEditar({ apolice, aoEditar }: { apolice: Apolice; aoEditar: TabelaApolicesProps['aoEditar'] }) {
  return (
    <Botao
      variante="contorno"
      tamanho="pequeno"
      icone="edit"
      onClick={(evento) => aoEditar(apolice, evento.currentTarget)}
      aria-label={`${SEGUROS_BOTAO_EDITAR} ${apolice.numeroApolice}`}
    >
      {SEGUROS_BOTAO_EDITAR}
    </Botao>
  );
}

/**
 * Apólices da página (desenho 13): tabela no computador e lista de cartões no celular, com as mesmas informações. O
 * prêmio é o texto do servidor (valorPremioStr) e o status sempre tem texto (EtiquetaVencimento).
 *
 * Fora do desenho por não existirem no sistema: modelo, ano, chassi e foto do veículo, convênio da seguradora e a
 * parcela mensal do prêmio.
 */
export function TabelaApolices({ itens, carregando, vazio, aoEditar, aoFiltrarVeiculo }: TabelaApolicesProps) {
  const colunas: readonly ColunaTabela<Apolice>[] = [
    { chave: 'veiculo', titulo: SEGUROS_COLUNA_VEICULO, renderizar: (apolice) => <BotaoPlaca apolice={apolice} aoFiltrarVeiculo={aoFiltrarVeiculo} /> },
    { chave: 'seguradora', titulo: SEGUROS_COLUNA_SEGURADORA, renderizar: (apolice) => <Seguradora apolice={apolice} /> },
    { chave: 'apolice', titulo: SEGUROS_COLUNA_APOLICE, renderizar: (apolice) => <ApoliceCobertura apolice={apolice} /> },
    { chave: 'vigencia', titulo: SEGUROS_COLUNA_VIGENCIA_PRAZOS, renderizar: (apolice) => <VigenciaPrazo apolice={apolice} /> },
    {
      chave: 'premio',
      titulo: SEGUROS_COLUNA_PREMIO,
      renderizar: (apolice) => apolice.valorPremioStr,
      alinhamento: 'direita',
      classe: 'whitespace-nowrap font-semibold tabular-nums',
    },
    { chave: 'status', titulo: SEGUROS_COLUNA_STATUS, renderizar: (apolice) => <EtiquetaVencimento apolice={apolice} /> },
    {
      chave: 'acoes',
      titulo: SEGUROS_COLUNA_ACOES,
      ocultarTitulo: true,
      alinhamento: 'direita',
      // "relative": o título invisível (sr-only, posição absoluta) fica preso à célula e não alarga a página quando a
      // tabela rola de lado dentro da caixa.
      classe: 'relative',
      renderizar: (apolice) => <BotaoEditar apolice={apolice} aoEditar={aoEditar} />,
    },
  ];

  return (
    <>
      <div className="hidden md:block">
        <Tabela<Apolice>
          colunas={colunas}
          dados={itens}
          obterChave={(apolice) => apolice.idSeguro}
          legenda={SEGUROS_LEGENDA_TABELA}
          carregando={carregando}
          vazio={vazio}
        />
      </div>

      <div className="md:hidden">
        {carregando ? (
          <div className="flex flex-col gap-3 p-4" aria-busy="true">
            <Esqueleto className="h-28 w-full" />
            <Esqueleto className="h-28 w-full" />
          </div>
        ) : itens.length === 0 ? (
          vazio
        ) : (
          <ul className="divide-y divide-borda" aria-label={SEGUROS_LEGENDA_TABELA}>
            {itens.map((apolice) => (
              <li key={apolice.idSeguro} className="flex flex-col gap-2 p-4">
                <div className="flex items-start justify-between gap-2">
                  <BotaoPlaca apolice={apolice} aoFiltrarVeiculo={aoFiltrarVeiculo} />
                  <EtiquetaVencimento apolice={apolice} />
                </div>
                <Seguradora apolice={apolice} />
                <ApoliceCobertura apolice={apolice} />
                <VigenciaPrazo apolice={apolice} />
                <div className="flex items-center justify-between gap-2">
                  <span className="font-semibold text-navy tabular-nums">{apolice.valorPremioStr}</span>
                  <BotaoEditar apolice={apolice} aoEditar={aoEditar} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </>
  );
}
