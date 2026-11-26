/**
 * Chaves do cache da devolução. Fica sem imports de API para que os hooks possam usar a chave sem carregar o cliente HTTP
 * (os testes dos hooks rodam sem janela).
 */
export const chaveDevolucoesPendentes = ['devolucoes-pendentes'] as const;

/** Prefixo da memória de cálculo do encerramento (a chave completa acrescenta a versão da vistoria de saída). */
export const chaveSimulacaoEncerramento = (id: number) => ['simulacao-encerramento', id] as const;
