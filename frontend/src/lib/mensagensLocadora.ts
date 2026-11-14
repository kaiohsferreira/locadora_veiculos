/**
 * Mensagens do cadastro da locadora (P10-FE-01, /cadastro e /cadastro/locadora/empresa).
 * Reexportadas por `@/lib/mensagens`: o import continua sendo daquele arquivo.
 *
 * As mensagens de validação são as do backend (ConstantsMessage.Locadora.cs, contrato empresa v1),
 * para o usuário ver o mesmo texto no front e na API. Textos genéricos (tamanho, e-mail, telefone,
 * CEP, faixa da senha) vêm de `mensagensCadastro` e não são repetidos aqui.
 */

// Tela 35: tipo de conta (/cadastro) -------------------------------------------

export const MENSAGEM_LOCADORA_TITULO_TIPO_CONTA = 'Cadastro: tipo de conta';
export const MENSAGEM_LOCADORA_SELO_AMBIENTE = 'Ambiente seguro AutoVia';
export const MENSAGEM_LOCADORA_TITULO_TIPO_CONTA_PAGINA = 'Como você quer usar a AutoVia?';
export const MENSAGEM_LOCADORA_SUBTITULO_TIPO_CONTA =
  'Escolha o tipo de conta. Você pode alugar e comprar com qualquer uma delas.';

export const MENSAGEM_LOCADORA_ETIQUETA_PESSOA_FISICA = 'Pessoa física';
export const MENSAGEM_LOCADORA_ETIQUETA_PESSOA_JURIDICA = 'Pessoa jurídica';

export const MENSAGEM_LOCADORA_PF_TITULO = 'Quero alugar ou comprar';
export const MENSAGEM_LOCADORA_PF_TEXTO =
  'Acesso a veículos individuais e a ofertas de seminovos, com cadastro pelo CPF.';
export const MENSAGEM_LOCADORA_PF_VANTAGEM_1 = 'Reserve e alugue veículos de várias locadoras';
export const MENSAGEM_LOCADORA_PF_VANTAGEM_2 = 'Compre seminovos da frota da AutoVia';
export const MENSAGEM_LOCADORA_PF_VANTAGEM_3 = 'Acompanhe locações e pagamentos no portal';
export const MENSAGEM_LOCADORA_PF_BOTAO = 'Criar conta de cliente';

export const MENSAGEM_LOCADORA_PJ_TITULO = 'Sou uma locadora';
export const MENSAGEM_LOCADORA_PJ_TEXTO =
  'Anuncie sua frota no catálogo da AutoVia e gerencie reservas, locações e equipe pelo painel da locadora.';
export const MENSAGEM_LOCADORA_PJ_VANTAGEM_1 = 'Anuncie a sua frota no catálogo integrado';
export const MENSAGEM_LOCADORA_PJ_VANTAGEM_2 = 'Receba reservas e gerencie contratos digitais';
export const MENSAGEM_LOCADORA_PJ_VANTAGEM_3 = 'Acompanhe tudo no painel analítico da locadora';
export const MENSAGEM_LOCADORA_PJ_BOTAO = 'Cadastrar minha locadora';

export const MENSAGEM_LOCADORA_JA_POSSUI_CADASTRO_TIPO = 'Já possui cadastro na plataforma?';

// Tela 36: dados da empresa (/cadastro/locadora/empresa) -----------------------

export const MENSAGEM_LOCADORA_TITULO_PAGINA_EMPRESA = 'Cadastro de locadora: dados da empresa';
export const MENSAGEM_LOCADORA_TITULO_EMPRESA = 'Cadastre a sua locadora';
export const MENSAGEM_LOCADORA_SUBTITULO_EMPRESA =
  'Informe os dados da empresa. Eles aparecem para os clientes no catálogo.';

export const MENSAGEM_LOCADORA_ETAPA_1_TITULO = 'Dados da empresa';
export const MENSAGEM_LOCADORA_ETAPA_2_TITULO = 'Responsável e acesso';
export const MENSAGEM_LOCADORA_ETAPA_3_TITULO = 'Confirmação';
export const MENSAGEM_LOCADORA_SITUACAO_EM_PREENCHIMENTO = 'Em preenchimento';
export const MENSAGEM_LOCADORA_SITUACAO_CONCLUIDA = 'Concluída';
export const MENSAGEM_LOCADORA_SITUACAO_AGUARDANDO = 'Aguardando';

export const MENSAGEM_LOCADORA_ROTULO_RAZAO_SOCIAL = 'Razão social';
export const MENSAGEM_LOCADORA_ROTULO_NOME_FANTASIA = 'Nome fantasia';
export const MENSAGEM_LOCADORA_DICA_NOME_FANTASIA = 'Nome exibido no catálogo';
export const MENSAGEM_LOCADORA_ROTULO_CNPJ = 'CNPJ';
export const MENSAGEM_LOCADORA_ROTULO_TELEFONE_COMERCIAL = 'Telefone comercial';
export const MENSAGEM_LOCADORA_ROTULO_EMAIL_COMERCIAL = 'E-mail comercial';

export const MENSAGEM_LOCADORA_TITULO_ENDERECO = 'Endereço da sede operacional';
export const MENSAGEM_LOCADORA_ROTULO_CEP = 'CEP';
export const MENSAGEM_LOCADORA_ROTULO_LOGRADOURO = 'Endereço';
export const MENSAGEM_LOCADORA_ROTULO_NUMERO = 'Número';
export const MENSAGEM_LOCADORA_ROTULO_COMPLEMENTO = 'Complemento';
export const MENSAGEM_LOCADORA_ROTULO_BAIRRO = 'Bairro';
export const MENSAGEM_LOCADORA_ROTULO_CIDADE = 'Cidade';
export const MENSAGEM_LOCADORA_ROTULO_UF = 'UF';

export const MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO = 'Este CNPJ já está em uso.';
export const MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO = 'Este e-mail já está em uso.';

export const MENSAGEM_LOCADORA_BOTAO_CONTINUAR = 'Continuar para Responsável';
export const MENSAGEM_LOCADORA_LINK_JA_POSSUI_CADASTRO = 'Já possui cadastro?';
export const MENSAGEM_LOCADORA_FAIXA_ETAPA_1 = 'ETAPA 1 DE 3';

/** Três cartões de apoio do desenho, com o texto ajustado ao que o sistema faz (ver relatório). */
export const MENSAGEM_LOCADORA_PILAR_ETAPAS_TITULO = 'Cadastro em três etapas';
export const MENSAGEM_LOCADORA_PILAR_ETAPAS_TEXTO =
  'Dados da empresa, responsável e confirmação, em um fluxo curto.';
export const MENSAGEM_LOCADORA_PILAR_PAINEL_TITULO = 'Painel de gestão';
export const MENSAGEM_LOCADORA_PILAR_PAINEL_TEXTO =
  'Acompanhe a frota, as reservas e a equipe da locadora em um só lugar.';
export const MENSAGEM_LOCADORA_PILAR_RESERVAS_TITULO = 'Reservas online';
export const MENSAGEM_LOCADORA_PILAR_RESERVAS_TEXTO =
  'Os clientes reservam os veículos da sua locadora pelo portal AutoVia.';

// Validação (ConstantsMessage.Locadora.cs) ---------------------------------------

export const MENSAGEM_LOCADORA_RAZAO_SOCIAL_OBRIGATORIA = 'Informe a razão social da locadora.';
export const MENSAGEM_LOCADORA_NOME_FANTASIA_OBRIGATORIO = 'Informe o nome fantasia da locadora.';
export const MENSAGEM_LOCADORA_CNPJ_OBRIGATORIO = 'Informe o CNPJ da locadora.';
export const MENSAGEM_LOCADORA_CNPJ_INVALIDO = 'O CNPJ informado é inválido.';
export const MENSAGEM_LOCADORA_TELEFONE_OBRIGATORIO = 'Informe o telefone da locadora.';
export const MENSAGEM_LOCADORA_EMAIL_OBRIGATORIO = 'Informe o e-mail da locadora.';
export const MENSAGEM_LOCADORA_CEP_OBRIGATORIO = 'Informe o CEP da sede.';
export const MENSAGEM_LOCADORA_LOGRADOURO_OBRIGATORIO = 'Informe o logradouro da sede.';
export const MENSAGEM_LOCADORA_NUMERO_OBRIGATORIO = 'Informe o número do endereço da sede.';
export const MENSAGEM_LOCADORA_BAIRRO_OBRIGATORIO = 'Informe o bairro da sede.';
export const MENSAGEM_LOCADORA_CIDADE_OBRIGATORIA = 'Informe a cidade da sede.';
export const MENSAGEM_LOCADORA_UF_OBRIGATORIA = 'Informe a UF da sede.';
export const MENSAGEM_LOCADORA_UF_INVALIDA = 'A UF deve ter 2 letras.';

// Tela 37: responsável e acesso (/cadastro/locadora/responsavel) ----------------

export const MENSAGEM_LOCADORA_TITULO_PAGINA_RESPONSAVEL = 'Cadastro de locadora: responsável e acesso';
export const MENSAGEM_LOCADORA_TITULO_RESPONSAVEL = 'Quem vai administrar a conta?';
export const MENSAGEM_LOCADORA_SUBTITULO_RESPONSAVEL = 'Esta pessoa será a administradora da locadora na AutoVia.';
export const MENSAGEM_LOCADORA_ROTULO_NOME_RESPONSAVEL = 'Nome completo do responsável';
export const MENSAGEM_LOCADORA_PLACEHOLDER_NOME_RESPONSAVEL = 'Nome do gestor ou procurador';
export const MENSAGEM_LOCADORA_PLACEHOLDER_CPF = '000.000.000-00';
export const MENSAGEM_LOCADORA_PLACEHOLDER_EMAIL_RESPONSAVEL = 'email.corporativo@locadora.com.br';
export const MENSAGEM_LOCADORA_ROTULO_CPF_RESPONSAVEL = 'CPF';
export const MENSAGEM_LOCADORA_ROTULO_EMAIL_ACESSO = 'E-mail de acesso';
/** Texto de ajuda do desenho 37, corrigido pela decisão do dono do produto: o acesso é por CPF. */
export const MENSAGEM_LOCADORA_DICA_EMAIL_ACESSO = 'Usado para recuperar a senha. O acesso é feito com o CPF.';

export const MENSAGEM_LOCADORA_REQUISITOS_TITULO = 'Requisitos de segurança da credencial';
export const MENSAGEM_LOCADORA_REQUISITO_TAMANHO = 'Mínimo de 8 caracteres';
export const MENSAGEM_LOCADORA_REQUISITO_MAIUSCULA = 'Uma letra maiúscula';
export const MENSAGEM_LOCADORA_REQUISITO_NUMERO = 'Um número';
export const MENSAGEM_LOCADORA_REQUISITO_ATENDIDO = '(atendido)';
export const MENSAGEM_LOCADORA_REQUISITO_PENDENTE = '(pendente)';

/** Rótulo do nível da senha (medidor de quatro barras, com três requisitos). */
export function mensagemLocadoraNivelSenha(nivel: number): string {
  if (nivel >= 3) return 'Senha forte';
  if (nivel === 2) return 'Senha média';
  if (nivel === 1) return 'Senha fraca';
  return 'Senha muito fraca';
}

/** Ex.: "Nível 3 de 4" (o desenho mostra quatro barras). */
export function mensagemLocadoraNumeroNivelSenha(nivel: number, total: number): string {
  return `Nível ${nivel} de ${total}`;
}

export const MENSAGEM_LOCADORA_SENHAS_COINCIDEM = 'Senhas coincidem perfeitamente.';

export const MENSAGEM_LOCADORA_ACEITE_PREFIXO = 'Li e aceito os';
export const MENSAGEM_LOCADORA_ACEITE_TERMOS = 'Termos de Uso';
export const MENSAGEM_LOCADORA_ACEITE_CONJUNCAO = 'e a';
export const MENSAGEM_LOCADORA_ACEITE_PRIVACIDADE = 'Política de Privacidade';
export const MENSAGEM_LOCADORA_ACEITE_SUFIXO = 'da AutoVia.';

export const MENSAGEM_LOCADORA_BOTAO_VOLTAR_EMPRESA = 'Voltar';
export const MENSAGEM_LOCADORA_BOTAO_CRIAR_CONTA = 'Criar conta da locadora';
export const MENSAGEM_LOCADORA_FAIXA_SEGURANCA_RESPONSAVEL =
  'A senha é criada nesta etapa e não é guardada no navegador.';
export const MENSAGEM_LOCADORA_FAIXA_ETAPA_2 = 'ETAPA 2 DE 3';

/** Validação da etapa 2 (ConstantsMessage.Locadora.cs, contrato empresa v1). */
export const MENSAGEM_LOCADORA_RESPONSAVEL_NOME_OBRIGATORIO = 'Informe o nome do responsável.';
export const MENSAGEM_LOCADORA_RESPONSAVEL_CPF_OBRIGATORIO = 'Informe o CPF do responsável.';
export const MENSAGEM_LOCADORA_RESPONSAVEL_CPF_INVALIDO = 'O CPF do responsável é inválido.';
export const MENSAGEM_LOCADORA_RESPONSAVEL_EMAIL_OBRIGATORIO = 'Informe o e-mail do responsável.';
export const MENSAGEM_LOCADORA_SENHA_FRACA = 'A senha deve ter ao menos uma letra maiúscula e um número.';
export const MENSAGEM_LOCADORA_ACEITE_TERMOS_OBRIGATORIO =
  'É preciso aceitar os termos de uso e a política de privacidade.';

/** 409 do CPF do responsável (o texto do backend, ErroLocadoraResponsavelCpfDuplicado). */
export const MENSAGEM_LOCADORA_RESPONSAVEL_CPF_JA_CADASTRADO = 'Este CPF já está em uso.';

// Envio e avisos gerais (404, 429, rede) -----------------------------------------

export const MENSAGEM_LOCADORA_CADASTRO_INDISPONIVEL =
  'O cadastro de locadoras não está disponível no momento. Tente novamente mais tarde.';
export const MENSAGEM_LOCADORA_LIMITE_TENTATIVAS =
  'Muitas tentativas de cadastro em pouco tempo. Aguarde um pouco e tente novamente.';
export const MENSAGEM_LOCADORA_FALHA_ENVIO =
  'Não foi possível enviar o cadastro. Verifique sua conexão e tente novamente.';

// Tela 38: confirmação (/cadastro/locadora/confirmacao) -------------------------

export const MENSAGEM_LOCADORA_TITULO_PAGINA_CONFIRMACAO = 'Cadastro de locadora: confirmação';
export const MENSAGEM_LOCADORA_TITULO_CONFIRMACAO = 'Locadora cadastrada';
export const MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_INICIO = 'A conta da';
export const MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_COM_SESSAO =
  'está ativa. Você já pode acessar o painel de gestão da sua frota.';
export const MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_SEM_SESSAO =
  'está ativa. Entre com o CPF do responsável para acessar o painel de gestão da sua frota.';
export const MENSAGEM_LOCADORA_AVISO_SEM_LOGIN =
  'A conta foi criada, mas não foi possível entrar automaticamente. Entre com o CPF do responsável e a senha.';
export const MENSAGEM_LOCADORA_RESUMO_TITULO = 'Resumo do cadastro';
export const MENSAGEM_LOCADORA_RESUMO_ATIVO = 'Ativo no Sistema';
export const MENSAGEM_LOCADORA_RESUMO_EMPRESA = 'Dados da empresa';
export const MENSAGEM_LOCADORA_RESUMO_ACESSO = 'Dados de acesso';
export const MENSAGEM_LOCADORA_RESUMO_CIDADE_UF = 'Cidade e UF';
export const MENSAGEM_LOCADORA_RESUMO_RESPONSAVEL = 'Responsável';
export const MENSAGEM_LOCADORA_BOTAO_PAINEL = 'Ir para o meu painel';
export const MENSAGEM_LOCADORA_BOTAO_PRIMEIRO_VEICULO = 'Cadastrar o primeiro veículo';
export const MENSAGEM_LOCADORA_FAIXA_SEGURANCA_CONFIRMACAO = 'Nenhuma senha foi guardada neste navegador.';
export const MENSAGEM_LOCADORA_FAIXA_ETAPA_3 = 'ETAPA 3 DE 3 · FINALIZADO';

// Indicador de etapas dos desenhos 37 e 38 ------------------------------------

export const MENSAGEM_LOCADORA_PASSO = 'Passo';
export const MENSAGEM_LOCADORA_SITUACAO_EM_ANDAMENTO = 'Em andamento';
