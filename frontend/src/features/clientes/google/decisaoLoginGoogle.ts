/**
 * Decide o que a tela faz depois do login com Google (N3, RF-51). Função pura: não navega,
 * não guarda credencial, não mostra aviso. Quem chama executa a ação.
 *
 * Quando a situação é AUTENTICADO, a sessão já foi aberta pelo hook e a guarda da página leva
 * a pessoa ao destino. Nenhuma ação extra é necessária.
 */
import { obterMensagemErro } from '@/lib/erros';
import { GOOGLE_CONTA_AMBIGUA, MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import type { ResultadoLoginGoogle } from '@/types/autenticacao';

/** O que a tela deve fazer depois do login com Google. */
export type AcaoLoginGoogle =
  /** AUTENTICADO: sessão aberta, nada mais a fazer. */
  | { tipo: 'concluido' }
  /** CONFIRMACAO_NECESSARIA: guardar a credencial e ir para /entrar/vincular-google. */
  | { tipo: 'confirmar-vinculo'; nome: string | null; email: string }
  /** CADASTRO_NECESSARIO: ir para o cadastro com nome e e-mail pré-preenchidos. Nada é criado. */
  | { tipo: 'cadastrar'; nome: string | null; email: string }
  /** Mostrar um alerta no bloco do Google (conta ambígua ou erro). */
  | { tipo: 'alerta'; mensagem: string };

/** Decisão para uma resposta de login já convertida em ResultadoLoginGoogle. */
export function decidirAcaoResultadoGoogle(resultado: ResultadoLoginGoogle): AcaoLoginGoogle {
  if (resultado.situacao === 'AUTENTICADO') return { tipo: 'concluido' };
  if (resultado.situacao === 'CONTA_AMBIGUA') return { tipo: 'alerta', mensagem: GOOGLE_CONTA_AMBIGUA };

  // Daqui em diante a situação é CONFIRMACAO_NECESSARIA ou CADASTRO_NECESSARIO, com e-mail obrigatório.
  if (resultado.email === null) return { tipo: 'alerta', mensagem: MENSAGEM_ERRO_API_PADRAO };
  if (resultado.situacao === 'CONFIRMACAO_NECESSARIA') {
    return { tipo: 'confirmar-vinculo', nome: resultado.nome, email: resultado.email };
  }
  return { tipo: 'cadastrar', nome: resultado.nome, email: resultado.email };
}

/**
 * Mensagem para um erro do login com Google. Usa `obterMensagemErro`, como o login por CPF e a
 * recuperação: a API manda o texto (ex.: 429 "Muitas tentativas..."; 401 da prova; 502). Falha de
 * rede chega como ErroApi com a mensagem de conexão. Erro que não é da API vira a mensagem padrão.
 */
export function decidirAcaoErroGoogle(erro: unknown): AcaoLoginGoogle {
  return { tipo: 'alerta', mensagem: obterMensagemErro(erro) };
}
