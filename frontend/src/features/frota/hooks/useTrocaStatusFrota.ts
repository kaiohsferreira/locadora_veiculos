import { useEffect, useRef, useState, type RefObject } from 'react';
import { useAviso } from '@/components/ui';
import { useAlterarStatusVeiculo } from '@/features/frota/hooks/useAlterarStatusVeiculo';
import { mensagemErroTrocaStatus } from '@/features/frota/status/mapeamentoErroTrocaStatus';
import {
  itemPedeConfirmacao,
  mensagemSucessoTrocaStatus,
  type ItemMenuOperacional,
} from '@/features/frota/status/menuOperacional';
import type { DestinoManualStatusVeiculo, VeiculoRetorno } from '@/types/veiculos';

/** O que as telas 11 e 12 usam da troca de status pelo menu de três pontos. */
export interface TrocaStatusFrotaValor {
  /** Veículo com troca em andamento: a linha dele fica bloqueada. Nulo sem troca. */
  idEmAndamento: number | null;
  /** Há uma troca em andamento (todos os menus ficam bloqueados). */
  pendente: boolean;
  /** Veículo cuja baixa espera a confirmação (o Modal fica aberto até a resposta). */
  paraBaixa: VeiculoRetorno | null;
  /** Escolha no menu: a baixa abre a confirmação; as outras trocas vão direto. */
  aoEscolherItem: (veiculo: VeiculoRetorno, item: ItemMenuOperacional) => void;
  /** Fecha a confirmação da baixa sem chamar a API. */
  cancelarBaixa: () => void;
  /** Confirma a baixa (a API recebe BAIXADO). */
  confirmarBaixa: () => void;
}

/**
 * Troca de status do veículo pela tela (menu de três pontos): chamada, linha em andamento, confirmação da baixa, avisos
 * de sucesso e de erro, e a devolução do foco depois da troca. Usada pelas duas telas da frota (11 e 12).
 */
export function useTrocaStatusFrota(refAreaTabela: RefObject<HTMLElement>): TrocaStatusFrotaValor {
  const aviso = useAviso();
  const trocaStatus = useAlterarStatusVeiculo();
  /** Veículo cuja baixa espera a confirmação (o Modal fica aberto até a resposta). */
  const [paraBaixa, setParaBaixa] = useState<VeiculoRetorno | null>(null);
  /** Linha com troca em andamento: o botão dela fica bloqueado até a lista recarregar. */
  const idEmAndamento = trocaStatus.isPending ? (trocaStatus.variables?.idVeiculo ?? null) : null;
  const trocaPendente = trocaStatus.isPending;
  const trocaPendenteAntes = useRef(false);

  // Terminou a troca e o foco ficou no body (a linha ou o botão saiu da lista, ou o Modal não achou o botão): o foco vai
  // para a área da tabela. O efeito roda depois da limpeza do Modal, que já tentou devolver o foco ao botão.
  useEffect(() => {
    const terminou = trocaPendenteAntes.current && !trocaPendente;
    trocaPendenteAntes.current = trocaPendente;
    if (!terminou) return;
    const foco = document.activeElement;
    if (foco === null || foco === document.body) refAreaTabela.current?.focus();
  }, [trocaPendente, refAreaTabela]);

  function trocarStatus(idVeiculo: number, status: DestinoManualStatusVeiculo) {
    trocaStatus.mutate(
      { idVeiculo, status },
      {
        onSuccess: () => aviso.sucesso(mensagemSucessoTrocaStatus(status)),
        onError: (erro) => aviso.erro(mensagemErroTrocaStatus(erro)),
      },
    );
  }

  /** Escolha no menu: a baixa abre a confirmação; as outras trocas vão direto. */
  function aoEscolherItem(veiculo: VeiculoRetorno, item: ItemMenuOperacional) {
    if (itemPedeConfirmacao(item)) {
      setParaBaixa(veiculo);
      return;
    }
    trocarStatus(veiculo.idVeiculo, item.destino);
  }

  function confirmarBaixa() {
    if (paraBaixa === null) return;
    trocaStatus.mutate(
      { idVeiculo: paraBaixa.idVeiculo, status: 'BAIXADO' },
      {
        onSuccess: () => {
          aviso.sucesso(mensagemSucessoTrocaStatus('BAIXADO'));
          setParaBaixa(null);
        },
        onError: (erro) => {
          setParaBaixa(null);
          aviso.erro(mensagemErroTrocaStatus(erro));
        },
      },
    );
  }

  return {
    idEmAndamento,
    pendente: trocaPendente,
    paraBaixa,
    aoEscolherItem,
    cancelarBaixa: () => setParaBaixa(null),
    confirmarBaixa,
  };
}
