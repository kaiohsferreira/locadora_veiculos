import { Campo, Icone, Select } from '@/components/ui';
import {
  MENSAGEM_VERIFICACAO_BUSCA_PLACEHOLDER,
  MENSAGEM_VERIFICACAO_BUSCA_ROTULO,
  MENSAGEM_VERIFICACAO_STATUS_ROTULO,
  MENSAGEM_VERIFICACAO_STATUS_TODOS,
} from '@/lib/mensagens';
import {
  OPCOES_STATUS_FILA,
  situacaoDoSeletor,
  TAMANHO_MAXIMO_BUSCA_FILA,
  type SituacaoFiltroFila,
} from './regras';

export interface FiltrosFilaVerificacaoProps {
  /** Texto digitado na busca (a tela decide quando ele vai para a consulta). */
  busca: string;
  aoMudarBusca: (busca: string) => void;
  /** Status escolhido ("" = todos). */
  status: SituacaoFiltroFila;
  aoMudarStatus: (status: SituacaoFiltroFila) => void;
}

/**
 * Barra da fila (tela 18): busca por nome, CPF ou e-mail e filtro de status. Sem filtro de data, sem "Auto" e sem
 * filtro avançado. Campos no visual de filtro da frota (tela 11), que é o mesmo do desenho.
 */
export function FiltrosFilaVerificacao({ busca, aoMudarBusca, status, aoMudarStatus }: FiltrosFilaVerificacaoProps) {
  return (
    <div className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-cartao sm:flex-row sm:items-center sm:justify-between">
      <div className="w-full sm:w-80">
        <Campo
          variante="frota"
          type="search"
          rotulo={MENSAGEM_VERIFICACAO_BUSCA_ROTULO}
          ocultarRotulo
          prefixo={<Icone nome="search" tamanho={18} className="text-contorno" />}
          placeholder={MENSAGEM_VERIFICACAO_BUSCA_PLACEHOLDER}
          maxLength={TAMANHO_MAXIMO_BUSCA_FILA}
          autoComplete="off"
          value={busca}
          onChange={(evento) => aoMudarBusca(evento.target.value)}
        />
      </div>
      <div className="w-full sm:w-64">
        <Select
          variante="frota"
          rotulo={MENSAGEM_VERIFICACAO_STATUS_ROTULO}
          ocultarRotulo
          opcaoVazia={MENSAGEM_VERIFICACAO_STATUS_TODOS}
          opcoes={OPCOES_STATUS_FILA.map((opcao) => ({ valor: opcao.valor, rotulo: opcao.rotulo }))}
          value={status}
          onChange={(evento) => aoMudarStatus(situacaoDoSeletor(evento.target.value))}
        />
      </div>
    </div>
  );
}
