import type { ReactNode } from 'react';
import { Botao, Esqueleto, Tabela, type ColunaTabela } from '@/components/ui';
import { formatarMoeda } from '@/lib/formatters';
import {
  SEGUROS_BOTAO_EDITAR,
  SEGUROS_COLUNA_ACOES,
  SEGUROS_COLUNA_NUMERO,
  SEGUROS_COLUNA_PREMIO,
  SEGUROS_COLUNA_SEGURADORA,
  SEGUROS_COLUNA_VEICULO,
  SEGUROS_COLUNA_VENCIMENTO,
  SEGUROS_COLUNA_VIGENCIA,
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

/** Vigência "01/01/2026 – 30/12/2026". */
function vigencia(apolice: Apolice): string {
  return `${apolice.dataInicioStr} – ${apolice.dataFimStr}`;
}

/** Placa como botão de filtro. */
function BotaoPlaca({ apolice, aoFiltrarVeiculo }: { apolice: Apolice; aoFiltrarVeiculo: (apolice: Apolice) => void }) {
  return (
    <button
      type="button"
      onClick={() => aoFiltrarVeiculo(apolice)}
      title={SEGUROS_FILTRAR_POR_VEICULO}
      className="font-semibold text-navy underline-offset-2 hover:underline"
    >
      {apolice.placaVeiculo}
    </button>
  );
}

/**
 * Apólices da página: tabela no computador (Tabela do sistema) e uma lista de cartões no celular, com as mesmas
 * informações. A coluna de vencimento sempre tem texto (EtiquetaVencimento).
 */
export function TabelaApolices({ itens, carregando, vazio, aoEditar, aoFiltrarVeiculo }: TabelaApolicesProps) {
  const colunas: readonly ColunaTabela<Apolice>[] = [
    { chave: 'veiculo', titulo: SEGUROS_COLUNA_VEICULO, renderizar: (apolice) => <BotaoPlaca apolice={apolice} aoFiltrarVeiculo={aoFiltrarVeiculo} /> },
    { chave: 'seguradora', titulo: SEGUROS_COLUNA_SEGURADORA, renderizar: (apolice) => apolice.seguradora },
    { chave: 'numero', titulo: SEGUROS_COLUNA_NUMERO, renderizar: (apolice) => apolice.numeroApolice },
    { chave: 'vigencia', titulo: SEGUROS_COLUNA_VIGENCIA, renderizar: vigencia, classe: 'whitespace-nowrap' },
    {
      chave: 'premio',
      titulo: SEGUROS_COLUNA_PREMIO,
      renderizar: (apolice) => formatarMoeda(apolice.valorPremio),
      alinhamento: 'direita',
      classe: 'whitespace-nowrap tabular-nums',
    },
    { chave: 'vencimento', titulo: SEGUROS_COLUNA_VENCIMENTO, renderizar: (apolice) => <EtiquetaVencimento apolice={apolice} /> },
    {
      chave: 'acoes',
      titulo: SEGUROS_COLUNA_ACOES,
      ocultarTitulo: true,
      alinhamento: 'direita',
      // "relative": o título invisível (sr-only, posição absoluta) fica preso à célula e não alarga a página quando a
      // tabela rola de lado dentro da caixa.
      classe: 'relative',
      renderizar: (apolice) => (
        <Botao
          variante="contorno"
          tamanho="pequeno"
          icone="edit"
          onClick={(evento) => aoEditar(apolice, evento.currentTarget)}
          aria-label={`${SEGUROS_BOTAO_EDITAR} ${apolice.numeroApolice}`}
        >
          {SEGUROS_BOTAO_EDITAR}
        </Botao>
      ),
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
                <p className="text-corpo-denso text-navy">
                  {apolice.seguradora} · <span className="text-texto-suave">{apolice.numeroApolice}</span>
                </p>
                <p className="text-corpo-denso text-texto-suave">{vigencia(apolice)}</p>
                <div className="flex items-center justify-between gap-2">
                  <span className="font-semibold text-navy tabular-nums">{formatarMoeda(apolice.valorPremio)}</span>
                  <Botao
                    variante="contorno"
                    tamanho="pequeno"
                    icone="edit"
                    onClick={(evento) => aoEditar(apolice, evento.currentTarget)}
                    aria-label={`${SEGUROS_BOTAO_EDITAR} ${apolice.numeroApolice}`}
                  >
                    {SEGUROS_BOTAO_EDITAR}
                  </Botao>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </>
  );
}
