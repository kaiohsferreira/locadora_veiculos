/**
 * Mensagens da tela de acesso ao portal (P2-FE-01, /entrar).
 * Reexportadas por `@/lib/mensagens`: o import continua sendo daquele arquivo.
 */

// Textos da tela ------------------------------------------------------------

/** Título da aba do navegador e da tela (useTituloDocumento). */
export const ACESSO_TITULO_PAGINA = 'Acesso ao portal';

export const ACESSO_TITULO = 'Acesse sua conta AutoVia';

export const ACESSO_SUBTITULO = 'Entre com o CPF e a senha do seu cadastro.';

/** Descrição do painel da marca (leitores de tela). */
export const ACESSO_DESCRICAO_PAINEL = 'Apresentação da AutoVia';

export const ACESSO_FRASE_PAINEL = 'Alugue ou compre veículos da própria frota da AutoVia.';

export const ACESSO_DESTAQUE_RESERVA_TITULO = 'Reserva online';

export const ACESSO_DESTAQUE_RESERVA_TEXTO = 'Escolha o veículo e reserve pelo portal.';

export const ACESSO_DESTAQUE_CADASTRO_TITULO = 'Cadastro verificado';

export const ACESSO_DESTAQUE_CADASTRO_TEXTO =
  'A equipe confere seus documentos antes da primeira reserva.';

export const ACESSO_DESTAQUE_SEMINOVOS_TITULO = 'Seminovos da frota';

export const ACESSO_DESTAQUE_SEMINOVOS_TEXTO = 'Veículos da própria frota à venda.';

// Formulário ----------------------------------------------------------------

export const ACESSO_ROTULO_PERFIL = 'Perfil de acesso';

export const ACESSO_OPCAO_CLIENTE = 'Cliente';

export const ACESSO_OPCAO_EQUIPE = 'Equipe';

export const ACESSO_ROTULO_CPF = 'CPF';

export const ACESSO_ROTULO_SENHA = 'Senha';

/** Link ao lado do rótulo da senha, que leva à recuperação (N1, RF-49). */
export const ACESSO_LINK_ESQUECI_SENHA = 'Esqueci minha senha';

export const ACESSO_BOTAO_ENTRAR = 'Entrar na conta';

export const ACESSO_PERGUNTA_SEM_CONTA = 'Não tem uma conta AutoVia?';

export const ACESSO_LINK_CRIAR_CONTA = 'Criar conta';

export const ACESSO_EQUIPE_SEM_CADASTRO = 'O acesso da equipe é criado pelo administrador.';

// Validação (antes de chamar a API) -----------------------------------------

export const ACESSO_ERRO_PERFIL_OBRIGATORIO = 'Escolha o perfil de acesso: Cliente ou Equipe.';

export const ACESSO_ERRO_CPF_OBRIGATORIO = 'Informe o CPF.';

export const ACESSO_ERRO_CPF_DIGITOS = 'O CPF deve ter 11 dígitos.';

export const ACESSO_ERRO_SENHA_OBRIGATORIA = 'Informe a senha.';

/** Tamanho máximo da senha no login (contrato auth.contract.json). Usado pelo esquema e pela mensagem. */
export const ACESSO_TAMANHO_MAXIMO_SENHA = 128;

export const ACESSO_ERRO_SENHA_TAMANHO = `A senha deve ter no máximo ${ACESSO_TAMANHO_MAXIMO_SENHA} caracteres.`;

export const ACESSO_ERRO_LEMBRAR_INVALIDO = 'Não foi possível entender a opção de lembrar de mim.';

// Lembrar de mim (N2, RF-50) ----------------------------------------------------

export const ACESSO_LEMBRAR_ROTULO = 'Lembrar de mim por 30 dias';

export const ACESSO_LEMBRAR_DICA = 'Use apenas em computador seu.';

/** Login deu certo, mas o vencimento do token já passou (relógio do dispositivo atrasado ou token vencido). */
export const ACESSO_ERRO_SESSAO_NAO_ABERTA =
  'Não foi possível abrir a sessão. Confira a data e a hora deste dispositivo e tente de novo.';
