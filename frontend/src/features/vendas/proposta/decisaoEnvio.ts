import { ErroApi } from '@/types/api';

/** HTTP 401: a sessão acabou durante o envio. */
const STATUS_NAO_AUTENTICADO = 401;

/** O que a tela faz quando a pessoa clica em enviar, antes de chamar a API. */
export type DecisaoAntesDoEnvio = 'irParaLogin' | 'avisarSoCliente' | 'enviar';

/** O que a tela faz com a resposta do envio. */
export type DecisaoDepoisDoEnvio = 'irParaLogin' | 'mostrarErro' | 'mostrarConfirmacao';

/**
 * Antes do envio: sem login, a pessoa vai entrar e voltar (com os valores guardados); com login de quem não é cliente,
 * a tela avisa que só cliente envia; com login de cliente, envia.
 */
export function decidirAntesDoEnvio(autenticado: boolean, ehCliente: boolean): DecisaoAntesDoEnvio {
  if (!autenticado) return 'irParaLogin';
  if (!ehCliente) return 'avisarSoCliente';
  return 'enviar';
}

/**
 * Depois do envio: sem erro, mostra a confirmação; 401 (a sessão acabou) leva ao login, como antes do envio; qualquer
 * outro erro aparece na tela com a mensagem do servidor.
 */
export function decidirDepoisDoEnvio(erro: unknown): DecisaoDepoisDoEnvio {
  if (erro === null || erro === undefined) return 'mostrarConfirmacao';
  if (erro instanceof ErroApi && erro.status === STATUS_NAO_AUTENTICADO) return 'irParaLogin';
  return 'mostrarErro';
}
