import { Icone } from '@/components/ui';
import {
  MENSAGEM_PAGAMENTOS_BOTAO_EM_BREVE,
  mensagemBotaoPagarFatura,
} from '@/lib/mensagens';
import { textoFaixaPendencias } from '@/features/financeiro/cliente/regrasPagamentosCliente';
import { BotaoPagarDesligado } from './BotaoPagarDesligado';

export interface FaixaPendenciaPagamentosProps {
  quantidadePendentes: number;
  /** Total pendente como veio do servidor (ex.: "R$ 395,60"). */
  totalPendenteStr: string;
}

/** Faixa do topo (desenho) com as cobranças pendentes. Sem pendente, a faixa não aparece. */
export function FaixaPendenciaPagamentos({ quantidadePendentes, totalPendenteStr }: FaixaPendenciaPagamentosProps) {
  const texto = textoFaixaPendencias(quantidadePendentes);
  if (texto === null) return null;

  return (
    <div className="w-full bg-frota-oficina text-branco shadow-sm">
      <div className="mx-auto flex w-full max-w-7xl flex-wrap items-center justify-between gap-2 px-4 py-2.5 sm:px-6 lg:px-8">
        <div className="flex items-center gap-1">
          <Icone nome="priority_high" tamanho={18} />
          <p className="text-apoio font-medium">{texto}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-apoio">{MENSAGEM_PAGAMENTOS_BOTAO_EM_BREVE}</span>
          <BotaoPagarDesligado
            variante="faixa"
            rotulo={mensagemBotaoPagarFatura(totalPendenteStr)}
            iconeDireita="arrow_forward"
          />
        </div>
      </div>
    </div>
  );
}
