/**
 * Mensagens do cadastro do cliente (P2-FE-02, /cadastro/dados-pessoais e /cadastro/documentos).
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

// Textos comuns às duas etapas ------------------------------------------------

export const MENSAGEM_CADASTRO_TRILHA_ROTULO = 'Trilha de navegação';
export const MENSAGEM_CADASTRO_ETAPAS_ROTULO = 'Etapas do cadastro';
export const MENSAGEM_CADASTRO_ETAPA_1_TITULO = 'Dados pessoais';
export const MENSAGEM_CADASTRO_ETAPA_1_SITUACAO = 'Em preenchimento';
export const MENSAGEM_CADASTRO_ETAPA_2_TITULO = 'Documentos';
export const MENSAGEM_CADASTRO_ETAPA_2_SITUACAO = 'Dados da CNH';
export const MENSAGEM_CADASTRO_ETAPA_3_TITULO = 'Confirmação';
export const MENSAGEM_CADASTRO_ETAPA_3_SITUACAO = 'Conferência pela equipe';

/** Situação das etapas no indicador da segunda tela. */
export const MENSAGEM_CADASTRO_SITUACAO_CONCLUIDO = 'Concluído';
export const MENSAGEM_CADASTRO_SITUACAO_ATUAL = 'Etapa atual';
export const MENSAGEM_CADASTRO_SITUACAO_PENDENTE = 'Pendente';

export const MENSAGEM_CADASTRO_DICA_SENHA = 'De 8 a 64 caracteres, com ao menos uma letra e um número.';

// Etapa 1: dados pessoais (/cadastro/dados-pessoais) ---------------------------

export const MENSAGEM_CADASTRO_TITULO_PAGINA = 'Cadastro: dados pessoais';
export const MENSAGEM_CADASTRO_VOLTAR_INICIO = 'Voltar ao início';
export const MENSAGEM_CADASTRO_ABERTURA = 'Abertura de cadastro';
export const MENSAGEM_CADASTRO_ETIQUETA_PESSOA_FISICA = 'Pessoa física';

export const MENSAGEM_CADASTRO_SELO = 'Cadastro gratuito';
export const MENSAGEM_CADASTRO_TITULO_CARTAO = 'Crie sua conta AutoVia';
export const MENSAGEM_CADASTRO_SUBTITULO =
  'Cadastre-se para reservar veículos e acompanhar suas locações pelo portal.';

export const MENSAGEM_CADASTRO_VERIFICADO_TITULO = 'Cadastro verificado';
export const MENSAGEM_CADASTRO_VERIFICADO_TEXTO =
  'A equipe confere seus documentos antes da primeira reserva.';

export const MENSAGEM_CADASTRO_ROTULO_NOME = 'Nome completo';
export const MENSAGEM_CADASTRO_APOIO_NOME = 'Conforme documento oficial';
export const MENSAGEM_CADASTRO_PLACEHOLDER_NOME = 'Seu nome completo';
export const MENSAGEM_CADASTRO_ROTULO_CPF = 'CPF';
export const MENSAGEM_CADASTRO_ROTULO_NASCIMENTO = 'Data de nascimento';
export const MENSAGEM_CADASTRO_APOIO_NASCIMENTO = 'Mínimo 18 anos';
export const MENSAGEM_CADASTRO_ROTULO_EMAIL = 'E-mail pessoal ou comercial';
export const MENSAGEM_CADASTRO_DICA_EMAIL = 'Usado para recuperar sua senha.';
export const MENSAGEM_CADASTRO_ROTULO_TELEFONE = 'Telefone celular';
export const MENSAGEM_CADASTRO_DICA_TELEFONE = 'Com DDD.';

export const MENSAGEM_CADASTRO_DECLARACAO_TITULAR =
  'Ao continuar, você declara ser o titular dos dados informados.';

export const MENSAGEM_CADASTRO_JA_POSSUI_CADASTRO = 'Já possui cadastro?';
export const MENSAGEM_CADASTRO_LINK_LOGIN = 'Fazer login';
export const MENSAGEM_CADASTRO_BOTAO_CONTINUAR = 'Continuar para Documentos';

export const MENSAGEM_CADASTRO_FAIXA_SENHA = 'A senha é criada na próxima etapa.';
export const MENSAGEM_CADASTRO_FAIXA_ETAPA_1 = 'ETAPA 1 DE 3';

export const MENSAGEM_CADASTRO_PILAR_RESERVA_TITULO = 'Reserva online';
export const MENSAGEM_CADASTRO_PILAR_RESERVA_TEXTO = 'Escolha o veículo e reserve pelo portal.';
export const MENSAGEM_CADASTRO_PILAR_GRATIS_TITULO = 'Sem taxa de cadastro';
export const MENSAGEM_CADASTRO_PILAR_GRATIS_TEXTO = 'Criar a conta é gratuito.';
export const MENSAGEM_CADASTRO_PILAR_VERIFICADO_TITULO = 'Cadastro verificado';
export const MENSAGEM_CADASTRO_PILAR_VERIFICADO_TEXTO =
  'A equipe confere seus documentos antes da primeira reserva.';

export const MENSAGEM_CADASTRO_FAIXA_ESCURA_TITULO = 'Já escolheu seu modelo?';
export const MENSAGEM_CADASTRO_FAIXA_ESCURA_TEXTO = 'Conheça os seminovos da frota da AutoVia.';
export const MENSAGEM_CADASTRO_FAIXA_ESCURA_BOTAO = 'Explorar catálogo';

// Etapa 2: documentos e acesso (/cadastro/documentos) --------------------------

export const MENSAGEM_CADASTRO_TITULO_PAGINA_DOCUMENTOS = 'Cadastro: documentos e acesso';
export const MENSAGEM_CADASTRO_TRILHA_CADASTRO = 'Cadastro AutoVia';
export const MENSAGEM_CADASTRO_TRILHA_DADOS_PESSOAIS = 'Dados pessoais';
export const MENSAGEM_CADASTRO_TRILHA_DOCUMENTOS = 'Documentos';

export const MENSAGEM_CADASTRO_SELO_DOCUMENTOS = 'Dados da habilitação';
export const MENSAGEM_CADASTRO_TITULO_DOCUMENTOS = 'Documentos e acesso';
export const MENSAGEM_CADASTRO_SUBTITULO_DOCUMENTOS =
  'Informe os dados da sua CNH e crie a senha de acesso. A equipe confere o cadastro antes da primeira reserva.';

export const MENSAGEM_CADASTRO_BLOCO_CNH_TITULO = 'Dados da CNH';
export const MENSAGEM_CADASTRO_BLOCO_CNH_TEXTO =
  'Digite os dados como aparecem na sua carteira de habilitação.';
export const MENSAGEM_CADASTRO_ROTULO_CNH_NUMERO = 'Número do registro CNH';
export const MENSAGEM_CADASTRO_ROTULO_CNH_CATEGORIA = 'Categoria da habilitação';
export const MENSAGEM_CADASTRO_ROTULO_CNH_VALIDADE = 'Data de validade da CNH';

export const MENSAGEM_CADASTRO_BLOCO_ENDERECO_TITULO = 'Endereço (opcional)';
export const MENSAGEM_CADASTRO_BLOCO_ENDERECO_TEXTO = 'Você pode informar agora ou depois.';
export const MENSAGEM_CADASTRO_ROTULO_CEP = 'CEP';
export const MENSAGEM_CADASTRO_ROTULO_LOGRADOURO = 'Logradouro';
export const MENSAGEM_CADASTRO_ROTULO_NUMERO = 'Número';
export const MENSAGEM_CADASTRO_ROTULO_COMPLEMENTO = 'Complemento';
export const MENSAGEM_CADASTRO_ROTULO_BAIRRO = 'Bairro';
export const MENSAGEM_CADASTRO_ROTULO_CIDADE = 'Cidade';
export const MENSAGEM_CADASTRO_ROTULO_UF = 'UF';

export const MENSAGEM_CADASTRO_BLOCO_SENHA_TITULO = 'Senha de acesso';
export const MENSAGEM_CADASTRO_ROTULO_SENHA = 'Senha';
export const MENSAGEM_CADASTRO_ROTULO_CONFIRMAR_SENHA = 'Confirmar senha';

export const MENSAGEM_CADASTRO_VOLTAR_DADOS_PESSOAIS = 'Voltar aos Dados Pessoais';
export const MENSAGEM_CADASTRO_BOTAO_FINALIZAR = 'Confirmar Dados e Finalizar Cadastro';
export const MENSAGEM_CADASTRO_FAIXA_CONFERENCIA =
  'A equipe confere seu cadastro antes da primeira reserva.';

// Envio e resultado ----------------------------------------------------------

export const MENSAGEM_CADASTRO_SUCESSO_COM_LOGIN =
  'Cadastro criado. A equipe confere seus dados antes da primeira reserva.';

export const MENSAGEM_CADASTRO_SUCESSO_SEM_LOGIN =
  'Cadastro criado. Entre com seu CPF e sua senha.';
