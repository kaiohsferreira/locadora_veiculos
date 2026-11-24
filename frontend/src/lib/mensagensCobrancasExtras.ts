export const MENSAGENS_COBRANCAS_EXTRAS = {
  titulo: 'Simular cobranças extras',
  aviso: 'A prévia usa a política cadastrada pela locadora para a categoria do veículo. O lançamento exige confirmação. As avarias usam valores estimados da vistoria; a locação não será encerrada.',
  inteiro: 'Informe um inteiro entre 0 e 2.147.483.647.',
  valor: 'Informe um valor maior que zero, até 99.999.999,99, com no máximo duas casas decimais.',
  modo: 'Selecione uma opção.',
  invalido: 'Confira os campos obrigatórios da política de cobranças extras.',
  semComparacao: 'Registre a comparação de saída antes de simular cobranças extras.',
  legado: 'Comparação não confirmada: o registro legado não permite simular cobranças extras.',
  semEntrada: 'A simulação exige vistorias de entrada e saída com combustível registrado.',
  semEstimativa: 'Informe estimativas válidas para todas as novas avarias antes de simular. As estimativas continuam provisórias.',
  erro400: 'Confira os dados da solicitação. Existem valores ausentes ou inválidos.',
  erro404: 'A locação não está disponível para esta simulação.',
  erro409: 'Confira a política cadastrada, as vistorias e as estimativas. A política pode ter mudado; faça uma nova simulação antes de confirmar.',
  configConflito: 'Não foi possível salvar. A versão pode ter mudado ou já existe política para esta categoria. Suas alterações foram preservadas. Recarregue os dados para conferir antes de salvar novamente.',
  salvo: 'Política de cobranças extras salva.',
  desativado: 'Política de cobranças extras desativada.',
  confirmado: 'Apuração de cobranças extras registrada.',
  /** Caixa de confirmação antes de registrar. Proposta de contrato cobrancas-extras v2 (textos_novos): aprovado pelo Kaio em 24/11/2026. */
  confirmacao: 'Conferi a política, a versão e as quatro memórias de cálculo. Confirmo o registro das cobranças automáticas, incluindo as estimativas das novas avarias, e o lançamento do pagamento para o cliente.',
  /**
   * Caixa de confirmação com total zero: o servidor não lança pagamento de valor zero, então a frase não fala do lançamento.
   * É o texto que já existia antes (HEAD 5a551c5). Acerto D2, aprovado pelo Kaio em 24/11/2026.
   */
  confirmacaoSemPagamento: 'Conferi a política, a versão e as quatro memórias de cálculo. Confirmo o registro das cobranças automáticas, incluindo as estimativas das novas avarias.',
  /** Linha no cartão da apuração registrada, quando houve pagamento lançado. Proposta de contrato: aprovado pelo Kaio em 24/11/2026. */
  pagamentoLancado: (idPagamento: number) => `Pagamento nº ${idPagamento} lançado para o cliente.`,
} as const;

/**
 * Texto da caixa de confirmação conforme o total da simulação: com total maior que zero o servidor lança o pagamento
 * (confirmacao); com total zero não lança nada (confirmacaoSemPagamento). Mesma regra do servidor (total > 0).
 */
export function mensagemConfirmacaoApuracao(total: number): string {
  return total > 0 ? MENSAGENS_COBRANCAS_EXTRAS.confirmacao : MENSAGENS_COBRANCAS_EXTRAS.confirmacaoSemPagamento;
}
