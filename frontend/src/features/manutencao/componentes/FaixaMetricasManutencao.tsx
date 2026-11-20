import { Esqueleto, Icone } from '@/components/ui';
import { formatarMoeda, formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_ERRO_RESUMO,
  MENSAGEM_MANUTENCAO_METRICA_CUSTO_MES,
  MENSAGEM_MANUTENCAO_METRICA_OS_ATIVAS,
  MENSAGEM_MANUTENCAO_TENTAR_DE_NOVO,
  mensagemManutencaoOrdensAtivas,
} from '@/lib/mensagens';
import type { ResumoManutencaoRetorno } from '@/types/manutencao';

export interface FaixaMetricasManutencaoProps {
  /** Primeira carga: aparecem os esqueletos. */
  carregando: boolean;
  /** O resumo falhou: a faixa mostra a mensagem no lugar dos valores. */
  erro: boolean;
  /** A nova tentativa está em andamento. */
  tentando: boolean;
  resumo: ResumoManutencaoRetorno | undefined;
  aoTentarNovamente: () => void;
}

/**
 * Faixa de métricas do cabeçalho (desenho 14): "OS Ativas" (ordens abertas, sem período) e "Custo Mês" (custo das ordens
 * encerradas no mês corrente, pela data de saída). Só essas duas: "Aguard. Peças" foi cortado pelo P7.
 */
export function FaixaMetricasManutencao({
  carregando,
  erro,
  tentando,
  resumo,
  aoTentarNovamente,
}: FaixaMetricasManutencaoProps) {
  if (carregando) {
    return (
      <div className="flex items-center gap-2 rounded-cartao bg-branco p-1 shadow-sm" aria-busy="true">
        <Esqueleto className="m-2 h-10 w-40" />
        <Esqueleto className="m-2 h-10 w-40" />
      </div>
    );
  }

  if (erro || resumo === undefined) {
    return (
      <div className="flex flex-wrap items-center gap-2 rounded-cartao bg-branco px-3 py-2 shadow-sm">
        <Icone nome="error" tamanho={18} className="text-status-perigo" />
        <span className="text-apoio text-status-perigo">{MENSAGEM_MANUTENCAO_ERRO_RESUMO}</span>
        <button
          type="button"
          onClick={aoTentarNovamente}
          disabled={tentando}
          className="inline-flex h-8 items-center rounded-sm bg-steel-fundo px-3 text-rotulo font-semibold text-texto-desenho transition-colors hover:bg-steel-alto disabled:cursor-not-allowed disabled:opacity-60"
        >
          {MENSAGEM_MANUTENCAO_TENTAR_DE_NOVO}
        </button>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-4 rounded-cartao bg-branco p-1 shadow-sm">
      <div className="flex items-center gap-2 border-r border-steel-suave px-3 py-1">
        <Icone nome="engineering" tamanho={20} className="text-frota-oficina" />
        <div className="flex flex-col">
          <span className="text-micro leading-none text-texto-apoio">{MENSAGEM_MANUTENCAO_METRICA_OS_ATIVAS}</span>
          <span className="text-corpo-denso font-bold text-texto-desenho">
            {mensagemManutencaoOrdensAtivas(resumo.ordensAbertas, formatarNumero(resumo.ordensAbertas))}
          </span>
        </div>
      </div>
      <div className="flex items-center gap-2 px-3 py-1">
        <Icone nome="payments" tamanho={20} className="text-navy-profundo" />
        <div className="flex flex-col">
          <span className="text-micro leading-none text-texto-apoio">{MENSAGEM_MANUTENCAO_METRICA_CUSTO_MES}</span>
          <span className="text-corpo-denso font-bold text-texto-desenho">{formatarMoeda(resumo.custoPeriodo)}</span>
        </div>
      </div>
    </div>
  );
}
