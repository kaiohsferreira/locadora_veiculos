import { MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';

/**
 * Mensagem amigável de qualquer erro, para exibir em Aviso ou EstadoErro.
 * Erros da API já chegam convertidos (ErroApi); qualquer outro erro vira a
 * mensagem padrão, nunca o texto técnico.
 */
export function obterMensagemErro(erro: unknown): string {
  return erro instanceof ErroApi ? erro.mensagem : MENSAGEM_ERRO_API_PADRAO;
}

/** Erros de validação por campo vindos do backend, ou objeto vazio. */
export function obterErrosDeCampo(erro: unknown): Record<string, string[]> {
  return erro instanceof ErroApi && erro.erros ? erro.erros : {};
}
