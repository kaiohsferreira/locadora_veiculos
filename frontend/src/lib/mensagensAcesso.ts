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

export const ACESSO_PARAGRAFO_PAINEL =
  'Reserve online, acompanhe suas locações e pagamentos e conheça os seminovos da frota.';

/** Linha de apoio sob a marca (aparece em caixa alta pelo estilo). */
export const ACESSO_MARCA_PREFIXO = 'Auto';
export const ACESSO_MARCA_SUFIXO = 'Via';
export const ACESSO_LINHA_MARCA = 'Gestão Inteligente de Frotas';

/** Selo em forma de pílula acima do título do painel. */
export const ACESSO_SELO_PAINEL = 'Aluguel & Seminovos';

/** Leitura da marca do painel, que leva ao início. */
export const ACESSO_MARCA_ROTULO_LINK = 'AutoVia, ir para o início';

/** Link "Pular para o conteúdo" do layout da tela de acesso. */
export const ACESSO_PULAR_CONTEUDO = 'Pular para o conteúdo';

// Faixas superior e inferior da coluna do formulário ---------------------------------

export const ACESSO_FAIXA_ROTULO = 'Acesso ao portal';

export const ACESSO_FAIXA_VOLTAR = 'Voltar ao início';

/** Faixa inferior da coluna do formulário. O ano vem de quem chama. */
export function mensagemRodapeAcesso(ano: number): string {
  return `© ${ano} AutoVia · Aluguel e venda de veículos da própria frota.`;
}

export const ACESSO_DESTAQUE_RESERVA_TITULO = 'Reserva online';

export const ACESSO_DESTAQUE_RESERVA_TEXTO = 'Reserve pelo portal';

export const ACESSO_DESTAQUE_CADASTRO_TITULO = 'Cadastro verificado';

export const ACESSO_DESTAQUE_CADASTRO_TEXTO = 'Conferido pela equipe';

export const ACESSO_DESTAQUE_SEMINOVOS_TITULO = 'Seminovos da frota';

export const ACESSO_DESTAQUE_SEMINOVOS_TEXTO = 'Veículos próprios à venda';

// Formulário ----------------------------------------------------------------

export const ACESSO_ROTULO_PERFIL = 'Perfil de acesso';

export const ACESSO_OPCAO_CLIENTE = 'Cliente';

export const ACESSO_OPCAO_EQUIPE = 'Equipe';

export const ACESSO_ROTULO_CPF = 'CPF';

export const ACESSO_ROTULO_SENHA = 'Senha';

/** Rótulos da tela /entrar (as outras telas seguem com ACESSO_ROTULO_CPF e ACESSO_ROTULO_SENHA). */
export const ACESSO_ROTULO_CPF_CADASTRADO = 'CPF cadastrado';

export const ACESSO_PLACEHOLDER_CPF = '000.000.000-00';

export const ACESSO_ROTULO_SENHA_ACESSO = 'Senha de acesso';

/** Link ao lado do rótulo da senha, que leva à recuperação (N1, RF-49). */
export const ACESSO_LINK_ESQUECI_SENHA = 'Esqueci minha senha';

export const ACESSO_BOTAO_ENTRAR = 'Entrar na Conta';

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
