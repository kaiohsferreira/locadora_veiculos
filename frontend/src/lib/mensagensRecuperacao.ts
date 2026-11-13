/**
 * Mensagens da recuperação de senha (N1, RF-49: telas /recuperar-senha e /redefinir-senha).
 * Reexportadas por `@/lib/mensagens`: o import continua sendo daquele arquivo.
 *
 * As mensagens de validação de CPF e senha vêm de `mensagensAcesso` e `mensagensCadastro`
 * (mesmos textos do login e do cadastro). Os textos de erro da API vêm de `obterMensagemErro`.
 */

// Recuperar senha (/recuperar-senha) ----------------------------------------

export const RECUPERACAO_TITULO = 'Recuperar senha';

export const RECUPERACAO_SUBTITULO =
  'Informe o CPF e o perfil do seu cadastro. As instruções para criar uma nova senha vão para o e-mail cadastrado.';

export const RECUPERACAO_BOTAO_ENVIAR = 'Enviar instruções';

export const RECUPERACAO_LINK_VOLTAR_LOGIN = 'Voltar ao login';

/** Confirmação depois do 202: o mesmo texto em qualquer resposta (nunca diz se a conta existe). */
export const RECUPERACAO_CONFIRMACAO_TITULO = 'Solicitação recebida';

export const RECUPERACAO_CONFIRMACAO_MENSAGEM =
  'Se houver uma conta com esses dados, enviamos instruções para o e-mail cadastrado.';

/** A validade é configuração do servidor: o texto não cita minutos. */
export const RECUPERACAO_CONFIRMACAO_VALIDADE = 'O link vale por tempo limitado e só pode ser usado uma vez.';

export const RECUPERACAO_CONFIRMACAO_BOTAO_VOLTAR = 'Voltar ao login';

// Definir nova senha (/redefinir-senha) -------------------------------------

/** Título da tela e da aba do navegador quando o link é válido. */
export const REDEFINICAO_TITULO = 'Definir nova senha';

export const REDEFINICAO_SUBTITULO = 'Crie uma nova senha para a sua conta AutoVia.';

export const REDEFINICAO_ROTULO_NOVA_SENHA = 'Nova senha';

export const REDEFINICAO_ROTULO_CONFIRMAR_SENHA = 'Confirmar nova senha';

export const REDEFINICAO_BOTAO_SALVAR = 'Salvar nova senha';

export const REDEFINICAO_CONFIRMACAO_OBRIGATORIA = 'Confirme a nova senha.';

// Textos EXATOS de ConstantsMessage.RecuperacaoSenha.cs (validação do RedefinirSenhaVO, 400).

export const REDEFINICAO_SENHA_OBRIGATORIA = 'Informe a nova senha.';

/** Ex.: "A nova senha deve ter entre 8 e 64 caracteres." (mesmo texto do backend, com {0} e {1}). */
export function mensagemRedefinicaoSenhaTamanho(minimo: number, maximo: number): string {
  return `A nova senha deve ter entre ${minimo} e ${maximo} caracteres.`;
}

export const REDEFINICAO_SENHA_FRACA = 'A nova senha deve ter ao menos uma letra e um número.';

export const REDEFINICAO_CONFIRMACAO_DIFERENTE = 'A confirmação não é igual à nova senha.';

/** Resposta 204: a senha foi trocada. Não há login automático. */
export const REDEFINICAO_SUCESSO = 'Senha redefinida. Entre com a nova senha.';

/** Estado de link inválido (409, token ausente ou inválido no fragmento). Mesmo texto para todos os casos. */
export const REDEFINICAO_LINK_INVALIDO_TITULO = 'Link inválido ou expirado';

export const REDEFINICAO_LINK_INVALIDO_MENSAGEM =
  'O link de redefinição é inválido ou expirou. Solicite um novo.';

export const REDEFINICAO_BOTAO_NOVO_LINK = 'Solicitar novo link';
