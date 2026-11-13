/**
 * Mensagens do login com Google (N3, RF-51: botão em /entrar e tela /entrar/vincular-google).
 * Reexportadas por `@/lib/mensagens`: o import continua sendo daquele arquivo.
 */

// Bloco do Google na tela de acesso -----------------------------------------------

/** Separador entre o botão de CPF e a opção do Google. */
export const GOOGLE_SEPARADOR = 'ou continue com';

/** Leitura para quem usa leitor de tela enquanto o botão do Google ainda carrega. */
export const GOOGLE_CARREGANDO = 'Carregando o login com o Google...';

/** Falha ao carregar o script do Google (bloqueador de anúncios, rede). O formulário de CPF segue. */
export const GOOGLE_FALHA_SCRIPT =
  'Não foi possível carregar o login com o Google. Entre com CPF e senha.';

/** Situação CONTA_AMBIGUA: mais de um cliente ativo com o mesmo e-mail. */
export const GOOGLE_CONTA_AMBIGUA =
  'Encontramos mais de um cadastro com este e-mail. Entre com CPF e senha.';

/** Texto ao bloco do Google enquanto o login com Google está em andamento (leitores de tela e visual). */
export const GOOGLE_ENTRANDO = 'Entrando com o Google...';

/** Botão de nova tentativa quando o script do Google não carrega. */
export const GOOGLE_TENTAR_NOVAMENTE = 'Tentar novamente';

/** Situação CADASTRO_NECESSARIO: faixa informativa no topo da tela de cadastro (só quando veio do Google). */
export const GOOGLE_CADASTRO_NECESSARIO =
  'Não encontramos cadastro com este e-mail. Complete o cadastro para continuar.';

// Modo de teste (provedor FAKE, só em desenvolvimento) -----------------------------

export const GOOGLE_TESTE_TITULO = 'Continuar com o Google (teste)';

export const GOOGLE_TESTE_DICA =
  'Modo de teste: a conta Google é simulada pelo servidor de desenvolvimento.';

export const GOOGLE_TESTE_ROTULO_EMAIL = 'E-mail da conta de teste';

export const GOOGLE_TESTE_BOTAO = 'Continuar';

export const GOOGLE_TESTE_ERRO_EMAIL_OBRIGATORIO = 'Informe o e-mail da conta de teste.';

export const GOOGLE_TESTE_ERRO_EMAIL_INVALIDO = 'Informe um e-mail válido.';

// Tela /entrar/vincular-google -----------------------------------------------------

export const GOOGLE_VINCULAR_TITULO_PAGINA = 'Vincular conta Google';

export const GOOGLE_VINCULAR_TITULO = 'Vincular conta Google';

/** Texto da tela com o e-mail vindo do Google. O e-mail é exibido como texto, nunca como HTML. */
export function mensagemVincularEmail(email: string): string {
  return `Encontramos o e-mail ${email} na sua conta Google. Para vincular, informe o CPF e a senha da sua conta AutoVia.`;
}

export const GOOGLE_VINCULAR_BOTAO = 'Vincular e entrar';

export const GOOGLE_VINCULAR_VOLTAR = 'Voltar ao login';

/** Mesmo texto que a API devolve no 401 de CPF ou senha (ErroCredenciaisInvalidas). Usado para distinguir do 401 da prova do Google. */
export const MENSAGEM_CREDENCIAIS_INVALIDAS = 'CPF ou senha inválidos.';

/**
 * Prova do Google recusada ou expirada no vínculo (401 da prova, 400 apontando a credencial). Texto do front:
 * reenviar a mesma prova não adianta, então não diz "tente novamente".
 */
export const GOOGLE_VINCULAR_PROVA_EXPIRADA =
  'A confirmação com o Google expirou. Volte ao login e escolha Continuar com o Google de novo.';
