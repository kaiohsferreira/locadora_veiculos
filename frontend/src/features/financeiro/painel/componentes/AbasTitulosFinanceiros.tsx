import { formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_FINANCEIRO_ABA_EM_ATRASO,
  MENSAGEM_FINANCEIRO_ABA_PAGOS,
  MENSAGEM_FINANCEIRO_ABA_TODOS,
  MENSAGEM_FINANCEIRO_ABA_VENCE_HOJE,
  mensagemFinanceiroAbaComContador,
} from '@/lib/mensagens';
import type { AbaTitulosFinanceiros } from '@/types/painelFinanceiro';

/** Contadores das abas, vindos da mesma resposta da tabela. Ausentes enquanto a tabela carrega. */
export interface ContadoresAbasFinanceiras {
  quantidadeTodos: number;
  quantidadeEmAtraso: number;
  quantidadeVenceHoje: number;
}

export interface AbasTitulosFinanceirosProps {
  abaAtual: AbaTitulosFinanceiros;
  contadores: ContadoresAbasFinanceiras | undefined;
  aoMudarAba: (aba: AbaTitulosFinanceiros) => void;
}

interface AbaConfig {
  aba: AbaTitulosFinanceiros;
  rotulo: string;
  /** Mostra o contador entre parênteses (o desenho e o contrato só o dão às três primeiras abas). */
  contador?: (contadores: ContadoresAbasFinanceiras) => number;
  /** Aba de atraso: rótulo e ponto em vermelho, como no desenho. */
  alerta?: boolean;
}

const ABAS: readonly AbaConfig[] = [
  { aba: 'TODOS', rotulo: MENSAGEM_FINANCEIRO_ABA_TODOS, contador: (c) => c.quantidadeTodos },
  { aba: 'EM_ATRASO', rotulo: MENSAGEM_FINANCEIRO_ABA_EM_ATRASO, contador: (c) => c.quantidadeEmAtraso, alerta: true },
  { aba: 'VENCE_HOJE', rotulo: MENSAGEM_FINANCEIRO_ABA_VENCE_HOJE, contador: (c) => c.quantidadeVenceHoje },
  { aba: 'PAGOS_RECENTES', rotulo: MENSAGEM_FINANCEIRO_ABA_PAGOS },
];

/**
 * Abas da tabela (Todos os Títulos, Em Atraso, A Vencer Hoje e Pagos Recentemente), no bloco de cinza-azulado do desenho.
 * A aba selecionada fica em branco, como no desenho; "Em Atraso" leva o ponto vermelho.
 */
export function AbasTitulosFinanceiras({ abaAtual, contadores, aoMudarAba }: AbasTitulosFinanceirosProps) {
  return (
    <div className="flex flex-wrap items-center gap-1 rounded bg-steel-fundo p-1">
      {ABAS.map((config) => {
        const ativa = config.aba === abaAtual;
        const quantidade =
          config.contador !== undefined && contadores !== undefined
            ? formatarNumero(config.contador(contadores))
            : undefined;
        return (
          <button
            key={config.aba}
            type="button"
            aria-pressed={ativa}
            onClick={() => aoMudarAba(config.aba)}
            className={`inline-flex items-center gap-1 rounded-md px-3 py-1.5 text-rotulo font-semibold transition-colors ${
              ativa ? 'bg-branco shadow-sm' : 'enabled:hover:bg-branco enabled:hover:text-texto-desenho'
            } ${
              config.alerta
                ? `text-manutencao-alerta ${ativa ? '' : 'enabled:hover:text-manutencao-alerta'}`
                : ativa
                  ? 'text-navy-profundo'
                  : 'text-texto-apoio'
            }`}
          >
            {config.alerta && <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-manutencao-alerta" />}
            {mensagemFinanceiroAbaComContador(config.rotulo, quantidade)}
          </button>
        );
      })}
    </div>
  );
}
