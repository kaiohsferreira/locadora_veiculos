/**
 * Mensagens fixas exibidas ao usuário. Centralize aqui toda mensagem de erro,
 * aviso ou estado da interface: nunca escreva a mesma frase em dois lugares e
 * nunca mostre a mensagem técnica crua do backend.
 */

// Erros de API ---------------------------------------------------------------

/** Mensagem genérica usada quando o backend não devolve uma mensagem aproveitável. */
export const MENSAGEM_ERRO_API_PADRAO =
  'Não foi possível concluir a operação. Tente novamente em instantes.';

export const MENSAGEM_FALHA_CONEXAO =
  'Não foi possível conectar ao servidor. Verifique sua conexão com a internet e tente novamente.';

export const MENSAGEM_TEMPO_ESGOTADO =
  'O servidor demorou demais para responder. Tente novamente em instantes.';

export const MENSAGEM_ERRO_SERVIDOR =
  'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.';

export const MENSAGEM_OPERACAO_CANCELADA = 'A operação foi cancelada.';

export const MENSAGEM_DADOS_INVALIDOS = 'Alguns dados estão inválidos. Confira os campos destacados.';

export const MENSAGEM_SESSAO_EXPIRADA = 'Sua sessão expirou. Entre novamente para continuar.';

export const MENSAGEM_ACESSO_NEGADO = 'Você não tem permissão para realizar esta ação.';

export const MENSAGEM_REGISTRO_NAO_ENCONTRADO = 'O registro solicitado não foi encontrado.';

export const MENSAGEM_CONFLITO =
  'Não foi possível concluir porque os dados mudaram. Atualize a página e tente novamente.';

// Estados de tela ------------------------------------------------------------

export const MENSAGEM_LISTA_VAZIA = 'Nenhum registro encontrado.';

export const MENSAGEM_ERRO_CARREGAMENTO = 'Não foi possível carregar as informações.';

export const MENSAGEM_CARREGANDO = 'Carregando...';

export const MENSAGEM_TELA_EM_CONSTRUCAO =
  'Esta tela ainda está em construção. A versão final será implementada a partir do protótipo.';

// Navegação e acesso ---------------------------------------------------------

export const MENSAGEM_PAGINA_NAO_ENCONTRADA =
  'A página que você procurou não existe ou mudou de endereço.';

export const MENSAGEM_AREA_NAO_PERMITIDA =
  'Sua conta não tem permissão para acessar esta área.';

export const MENSAGEM_ERRO_INESPERADO =
  'Algo deu errado ao exibir esta página. Recarregue a página ou volte para o início.';

export const MENSAGEM_AUTENTICACAO_IGNORADA =
  'Modo de desenvolvimento: a verificação de login está desativada (VITE_IGNORAR_AUTENTICACAO=true).';

// Mensagens por módulo ficam em mensagens<Modulo>.ts e são reexportadas aqui.
export * from './mensagensAcesso';
export * from './mensagensCadastro';
