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

/**
 * Primeira mensagem de erro do campo, ou undefined. O nome do campo é comparado
 * sem diferenciar maiúsculas de minúsculas, pois o backend pode mandar "Cpf" ou "cpf".
 */
export function obterErroDeCampo(erro: unknown, campo: string): string | undefined {
  if (!(erro instanceof ErroApi) || !erro.erros) return undefined;
  const nomeProcurado = campo.toLowerCase();
  for (const [nomeCampo, mensagens] of Object.entries(erro.erros)) {
    if (nomeCampo.toLowerCase() === nomeProcurado && mensagens.length > 0) {
      return mensagens[0];
    }
  }
  return undefined;
}

/**
 * Aplica os erros de campo do backend aos campos conhecidos do formulário.
 * Chama `definir` uma vez por campo que veio com erro e devolve true se aplicou algum.
 */
export function aplicarErrosDeCampo<TCampo extends string>(
  erro: unknown,
  campos: readonly TCampo[],
  definir: (campo: TCampo, mensagem: string) => void,
): boolean {
  let aplicou = false;
  for (const campo of campos) {
    const mensagem = obterErroDeCampo(erro, campo);
    if (mensagem !== undefined) {
      definir(campo, mensagem);
      aplicou = true;
    }
  }
  return aplicou;
}
