/**
 * Aviso da troca de status do veículo. Sem React.
 *
 * - 404 e 409: a frase do servidor (veículo não encontrado, reserva ou locação em aberto, status só por fluxo).
 * - 403: o texto padrão de acesso negado, como o cliente HTTP já usa.
 * - Os demais erros: a mensagem pronta do cliente (rede, tempo esgotado, 500, sessão). Nunca a técnica.
 */
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGEM_ACESSO_NEGADO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';

export function mensagemErroTrocaStatus(erro: unknown): string {
  if (!(erro instanceof ErroApi)) return obterMensagemErro(erro);
  if (erro.status === 404 || erro.status === 409) return erro.mensagem;
  if (erro.status === 403) return MENSAGEM_ACESSO_NEGADO;
  return erro.mensagem;
}
