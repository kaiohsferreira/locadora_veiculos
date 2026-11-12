/**
 * Mensagens do cadastro do cliente (P2-FE-02, /cadastro/dados-pessoais).
 * Reexportadas por `@/lib/mensagens`: o import continua sendo daquele arquivo.
 *
 * As mensagens de validação são as mesmas do backend (ConstantsMessage.Cliente.cs),
 * para o usuário ver o mesmo texto no front e na API.
 */

// Validação (ClienteVOValidator) ---------------------------------------------

export const MENSAGEM_CADASTRO_NOME_OBRIGATORIO = 'Informe o nome do cliente.';

/** Ex.: "O campo nome deve ter no máximo 120 caracteres." */
export function mensagemCadastroCampoTamanho(rotulo: string, maximo: number): string {
  return `O campo ${rotulo} deve ter no máximo ${maximo} caracteres.`;
}

export const MENSAGEM_CADASTRO_CPF_OBRIGATORIO = 'Informe o CPF.';
export const MENSAGEM_CADASTRO_CPF_INVALIDO = 'O CPF informado é inválido.';

export const MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA = 'Informe a data de nascimento.';
export const MENSAGEM_CADASTRO_DATA_NASCIMENTO_FUTURA = 'A data de nascimento não pode ser futura.';

/** Ex.: "O cliente deve ter pelo menos 18 anos." */
export function mensagemCadastroMenorDeIdade(idadeMinima: number): string {
  return `O cliente deve ter pelo menos ${idadeMinima} anos.`;
}

export const MENSAGEM_CADASTRO_EMAIL_OBRIGATORIO = 'Informe o e-mail.';
export const MENSAGEM_CADASTRO_EMAIL_INVALIDO = 'O e-mail informado é inválido.';

export const MENSAGEM_CADASTRO_TELEFONE_OBRIGATORIO = 'Informe o telefone.';
export const MENSAGEM_CADASTRO_TELEFONE_INVALIDO =
  'O telefone deve ter 10 ou 11 dígitos (DDD e número).';

export const MENSAGEM_CADASTRO_CNH_NUMERO_OBRIGATORIO = 'Informe o número da CNH.';

/** Ex.: "O número da CNH deve ter 11 dígitos." */
export function mensagemCadastroCnhNumeroInvalido(digitos: number): string {
  return `O número da CNH deve ter ${digitos} dígitos.`;
}

export const MENSAGEM_CADASTRO_CNH_CATEGORIA_INVALIDA =
  'Informe uma categoria de CNH válida: A, B, AB, C, D ou E.';
export const MENSAGEM_CADASTRO_CNH_VALIDADE_OBRIGATORIA = 'Informe a validade da CNH.';
export const MENSAGEM_CADASTRO_CNH_VENCIDA = 'A CNH informada está vencida.';

export const MENSAGEM_CADASTRO_UF_INVALIDA = 'A UF deve ter 2 letras.';
export const MENSAGEM_CADASTRO_CEP_INVALIDO = 'O CEP deve ter 8 dígitos.';

export const MENSAGEM_CADASTRO_SENHA_OBRIGATORIA = 'Informe a senha.';

/** Ex.: "A senha deve ter entre 8 e 64 caracteres." */
export function mensagemCadastroSenhaTamanho(minimo: number, maximo: number): string {
  return `A senha deve ter entre ${minimo} e ${maximo} caracteres.`;
}

export const MENSAGEM_CADASTRO_SENHA_FRACA = 'A senha deve ter ao menos uma letra e um número.';

/** Front only: a confirmação não existe no backend. */
export const MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM = 'As senhas não conferem.';

// Textos da tela ------------------------------------------------------------

export const MENSAGEM_CADASTRO_SUBTITULO =
  'Cadastre-se para reservar veículos e acompanhar suas locações.';

export const MENSAGEM_CADASTRO_AVISO_ANALISE =
  'Seu cadastro será analisado pela equipe. Enquanto estiver pendente, ainda não é possível fazer reservas.';

export const MENSAGEM_CADASTRO_DECLARACAO_TITULAR =
  'Ao continuar, você declara ser o titular dos dados informados.';

export const MENSAGEM_CADASTRO_DICA_SENHA = 'De 8 a 64 caracteres, com ao menos uma letra e um número.';

export const MENSAGEM_CADASTRO_DICA_ENDERECO = 'Preencha se quiser informar o endereço agora.';

export const MENSAGEM_CADASTRO_TITULO_PAGINA = 'Cadastro: dados pessoais';
export const MENSAGEM_CADASTRO_TITULO_CARTAO = 'Crie sua conta AutoVia';
export const MENSAGEM_CADASTRO_TRILHA_ROTULO = 'Trilha de navegação';
export const MENSAGEM_CADASTRO_VOLTAR_INICIO = 'Voltar ao início';
export const MENSAGEM_CADASTRO_ABERTURA = 'Abertura de cadastro';
export const MENSAGEM_CADASTRO_ETAPAS_ROTULO = 'Etapas do cadastro';
export const MENSAGEM_CADASTRO_ETAPA_1_TITULO = 'Dados pessoais';
export const MENSAGEM_CADASTRO_ETAPA_1_SITUACAO = 'Em preenchimento';
export const MENSAGEM_CADASTRO_ETAPA_2_TITULO = 'Documentos';
export const MENSAGEM_CADASTRO_ETAPA_2_SITUACAO = 'CNH e comprovante de residência';
export const MENSAGEM_CADASTRO_ETAPA_3_TITULO = 'Análise';
export const MENSAGEM_CADASTRO_ETAPA_3_SITUACAO = 'Verificação pela equipe';
export const MENSAGEM_CADASTRO_LEGENDA_DADOS = 'Dados do cadastro';
export const MENSAGEM_CADASTRO_BOTAO_CONTINUAR = 'Continuar para documentos';
export const MENSAGEM_CADASTRO_JA_POSSUI_CADASTRO = 'Já possui cadastro?';
export const MENSAGEM_CADASTRO_LINK_LOGIN = 'Fazer login';

// Envio e resultado ----------------------------------------------------------

export const MENSAGEM_CADASTRO_SUCESSO_COM_LOGIN = 'Cadastro criado. Agora envie seus documentos.';

export const MENSAGEM_CADASTRO_SUCESSO_SEM_LOGIN =
  'Cadastro criado. Entre com seu CPF e sua senha.';
