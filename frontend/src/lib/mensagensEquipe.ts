/**
 * Mensagens da equipe da locadora (P10-FE-03, tela 41). Reexportadas por `@/lib/mensagens`.
 *
 * Os textos de validação e de erro do servidor (ErroEquipe* do contrato equipe.contract.json) estão aqui com a mesma
 * grafia, para o usuário ver o mesmo texto no front e na API. Textos que não estão no contrato estão marcados
 * como "de interface" e foram escritos pelo front.
 */

// Página e cabeçalho ---------------------------------------------------------

export const MENSAGEM_EQUIPE_TITULO_PAGINA = 'Equipe';
export const MENSAGEM_EQUIPE_ADMINISTRACAO = 'Administração';
export const MENSAGEM_EQUIPE_DESCRICAO = 'Pessoas que acessam o painel da locadora e o que cada uma pode fazer.';
export const MENSAGEM_EQUIPE_BOTAO_ADICIONAR = 'Adicionar funcionário';

// Indicadores ----------------------------------------------------------------

export const MENSAGEM_EQUIPE_INDICADOR_ATIVOS = 'Funcionários ativos';
export const MENSAGEM_EQUIPE_INDICADOR_ATIVOS_SELO = 'Em operação';
export const MENSAGEM_EQUIPE_INDICADOR_INATIVOS = 'Inativos';
export const MENSAGEM_EQUIPE_INDICADOR_INATIVOS_SELO = 'Desligados';
export const MENSAGEM_EQUIPE_INDICADOR_INATIVOS_DETALHE = 'acesso revogado';
export const MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES = 'Administradores';
export const MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES_SELO = 'Acesso Total';
export const MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES_DETALHE = 'gestor master';

/** "de 11 cadastrados" (ativos sobre o total de cadastrados). */
export function mensagemEquipeDeCadastrados(total: number): string {
  return `de ${total} cadastrados`;
}

// Filtros --------------------------------------------------------------------

export const MENSAGEM_EQUIPE_BUSCA_ROTULO = 'Buscar por nome, CPF ou matrícula';
export const MENSAGEM_EQUIPE_BUSCA_PLACEHOLDER = 'Buscar por nome, CPF ou matrícula...';
export const MENSAGEM_EQUIPE_CARGO_ROTULO = 'Cargo';
export const MENSAGEM_EQUIPE_CARGO_TODOS = 'Todos os cargos';
export const MENSAGEM_EQUIPE_SITUACAO_GRUPO = 'Situação dos funcionários';
export const MENSAGEM_EQUIPE_SITUACAO_TODOS = 'Todos';
export const MENSAGEM_EQUIPE_SITUACAO_ATIVOS = 'Ativos';
export const MENSAGEM_EQUIPE_SITUACAO_INATIVOS = 'Inativos';
export const MENSAGEM_EQUIPE_LIMPAR_FILTROS = 'Limpar filtros';

// Tabela ---------------------------------------------------------------------

export const MENSAGEM_EQUIPE_TABELA_LEGENDA = 'Funcionários da locadora';
export const MENSAGEM_EQUIPE_COLUNA_FUNCIONARIO = 'Funcionário';
export const MENSAGEM_EQUIPE_COLUNA_CPF = 'CPF';
export const MENSAGEM_EQUIPE_COLUNA_MATRICULA = 'Matrícula';
export const MENSAGEM_EQUIPE_COLUNA_CARGO = 'Cargo';
export const MENSAGEM_EQUIPE_COLUNA_ADMISSAO = 'Admissão';
export const MENSAGEM_EQUIPE_COLUNA_SITUACAO = 'Situação';
export const MENSAGEM_EQUIPE_COLUNA_ACOES = 'Ações';
export const MENSAGEM_EQUIPE_SELO_VOCE = 'Você';
export const MENSAGEM_EQUIPE_SITUACAO_ATIVO = 'Ativo';
export const MENSAGEM_EQUIPE_SITUACAO_INATIVO = 'Inativo';
export const MENSAGEM_EQUIPE_ACAO_EDITAR = 'Editar';
export const MENSAGEM_EQUIPE_ACAO_DESATIVAR = 'Desativar';
export const MENSAGEM_EQUIPE_ACAO_REATIVAR = 'Reativar acesso';

/** Rótulo de leitor de tela da ação da linha, com o nome do funcionário (ex.: "Editar Marina Costa"). */
export function mensagemEquipeAcaoDaLinha(acao: string, nome: string): string {
  return `${acao} ${nome}`;
}

export const MENSAGEM_EQUIPE_RODAPE_ITENS = 'funcionários';

// Estados da lista -----------------------------------------------------------

export const MENSAGEM_EQUIPE_ERRO_LISTA = 'Não foi possível listar a equipe.';
export const MENSAGEM_EQUIPE_VAZIA_COM_FILTRO = 'Nenhum funcionário encontrado.';
export const MENSAGEM_EQUIPE_VAZIA_COM_FILTRO_DESCRICAO =
  'Ajuste a busca ou os filtros e tente de novo.';

// Painel (gaveta) de adicionar e editar ---------------------------------------

export const MENSAGEM_EQUIPE_PAINEL_ADICIONAR = 'Adicionar funcionário';
export const MENSAGEM_EQUIPE_PAINEL_EDITAR = 'Editar funcionário';
export const MENSAGEM_EQUIPE_FECHAR = 'Fechar';
export const MENSAGEM_EQUIPE_CANCELAR = 'Cancelar';
export const MENSAGEM_EQUIPE_SALVAR_EDICAO = 'Salvar alterações';
export const MENSAGEM_EQUIPE_CARREGANDO_FUNCIONARIO = 'Carregando o funcionário...';
export const MENSAGEM_EQUIPE_ERRO_CONSULTA = 'Não foi possível consultar o funcionário.';

export const MENSAGEM_EQUIPE_ROTULO_NOME = 'Nome completo';
export const MENSAGEM_EQUIPE_ROTULO_CPF = 'CPF';
export const MENSAGEM_EQUIPE_ROTULO_MATRICULA = 'Matrícula';
export const MENSAGEM_EQUIPE_ROTULO_EMAIL = 'E-mail institucional';
export const MENSAGEM_EQUIPE_ROTULO_ADMISSAO = 'Data de admissão';
export const MENSAGEM_EQUIPE_ROTULO_CARGO = 'Cargo atribuído';
export const MENSAGEM_EQUIPE_ROTULO_SENHA = 'Senha inicial';
export const MENSAGEM_EQUIPE_ROTULO_CONFIRMAR_SENHA = 'Confirmar senha';

export const MENSAGEM_EQUIPE_PLACEHOLDER_NOME = 'Ex: Roberto Alencar dos Santos';
export const MENSAGEM_EQUIPE_PLACEHOLDER_CPF = '000.000.000-00';
export const MENSAGEM_EQUIPE_PLACEHOLDER_MATRICULA = 'MAT-0108';
export const MENSAGEM_EQUIPE_PLACEHOLDER_EMAIL = 'nome@rotasul.com.br';
export const MENSAGEM_EQUIPE_PLACEHOLDER_ADMISSAO = 'DD/MM/AAAA';

/** Dica de interface: a regra da senha inicial aparece antes do erro. */
export const MENSAGEM_EQUIPE_DICA_SENHA = 'Entre 8 e 64 caracteres, com uma letra maiúscula e um número.';
/** Aviso que ocupa, no desenho, o lugar do aviso de envio por e-mail: a senha é informada pessoalmente e não volta a aparecer. */
export const MENSAGEM_EQUIPE_AVISO_SENHA = 'Informe a senha ao funcionário. Ela não fica visível depois.';
/** Dica de interface na edição: o CPF é só leitura. */
export const MENSAGEM_EQUIPE_DICA_CPF_SOMENTE_LEITURA = 'O CPF não pode ser alterado.';
/** Dica de interface: o cargo do próprio usuário não é editável. */
export const MENSAGEM_EQUIPE_DICA_CARGO_PROPRIO = 'Você não pode alterar o próprio cargo.';

export const MENSAGEM_EQUIPE_CAIXA_CARGO = 'O que este cargo pode fazer:';

// Confirmação de desativação e reativação ------------------------------------

/** Título da confirmação (de interface): "Desativar acesso de Carlos Eduardo Silveira?". */
export function mensagemEquipeConfirmarDesativacaoTitulo(nome: string): string {
  return `Desativar acesso de ${nome}?`;
}

export const MENSAGEM_EQUIPE_CONFIRMAR_DESATIVACAO_TEXTO =
  'A pessoa deixa de entrar no painel da locadora. O CPF e a matrícula continuam reservados, e o acesso pode ser reativado depois.';
export const MENSAGEM_EQUIPE_CONFIRMAR_DESATIVAR = 'Desativar acesso';

// Sucessos -------------------------------------------------------------------

export const MENSAGEM_EQUIPE_SUCESSO_CADASTRO = 'Funcionário adicionado à equipe.';
export const MENSAGEM_EQUIPE_SUCESSO_EDICAO = 'Dados do funcionário atualizados.';
export const MENSAGEM_EQUIPE_SUCESSO_DESATIVADO = 'Acesso desativado. O funcionário não consegue mais entrar no painel.';
export const MENSAGEM_EQUIPE_SUCESSO_REATIVADO = 'Acesso reativado. O funcionário pode entrar no painel de novo.';

// Validação do formulário (ErroEquipe* do contrato) ---------------------------

export const MENSAGEM_EQUIPE_NOME_OBRIGATORIO = 'Informe o nome do funcionário.';
export const MENSAGEM_EQUIPE_CPF_OBRIGATORIO = 'Informe o CPF do funcionário.';
export const MENSAGEM_EQUIPE_CPF_INVALIDO = 'O CPF informado é inválido.';
export const MENSAGEM_EQUIPE_MATRICULA_OBRIGATORIA = 'Informe a matrícula do funcionário.';
export const MENSAGEM_EQUIPE_EMAIL_OBRIGATORIO = 'Informe o e-mail do funcionário.';
export const MENSAGEM_EQUIPE_EMAIL_INVALIDO = 'O e-mail informado é inválido.';
export const MENSAGEM_EQUIPE_ADMISSAO_OBRIGATORIA = 'Informe a data de admissão.';
/** De interface: a data digitada precisa existir no calendário no formato DD/MM/AAAA. */
export const MENSAGEM_EQUIPE_ADMISSAO_FORMATO = 'Informe uma data válida no formato DD/MM/AAAA.';
/** De interface: a admissão não pode ser futura (regra da decisão de 14/11). */
export const MENSAGEM_EQUIPE_ADMISSAO_FUTURA = 'A data de admissão não pode ser futura.';
export const MENSAGEM_EQUIPE_CARGO_OBRIGATORIO = 'Informe o cargo do funcionário.';
export const MENSAGEM_EQUIPE_SENHA_OBRIGATORIA = 'Informe a senha inicial do funcionário.';
export const MENSAGEM_EQUIPE_SENHA_TAMANHO = 'A senha inicial deve ter entre 8 e 64 caracteres.';
export const MENSAGEM_EQUIPE_SENHA_FRACA = 'A senha inicial deve ter ao menos uma letra maiúscula e um número.';
export const MENSAGEM_EQUIPE_CONFIRMACAO_SENHA = 'As senhas não conferem.';

/** "O campo nome deve ter no máximo 120 caracteres." (ErroEquipeCampoTamanho). */
export function mensagemEquipeCampoTamanho(campo: string, maximo: number): string {
  return `O campo ${campo} deve ter no máximo ${maximo} caracteres.`;
}

// Erros de API (ErroEquipe* do contrato, com o que a tela mostra) -------------

export const MENSAGEM_EQUIPE_CPF_JA_CADASTRADO = 'Já existe um funcionário cadastrado com este CPF.';
export const MENSAGEM_EQUIPE_MATRICULA_JA_CADASTRADA = 'Já existe um funcionário com esta matrícula nesta locadora.';
export const MENSAGEM_EQUIPE_ULTIMO_ADMINISTRADOR = 'A locadora precisa manter pelo menos um administrador ativo.';
export const MENSAGEM_EQUIPE_FUNCIONARIO_NAO_ENCONTRADO = 'O funcionário informado não foi encontrado.';
export const MENSAGEM_EQUIPE_ACESSO_NEGADO = 'Somente administradores podem gerenciar a equipe da locadora.';
/**
 * De interface: o contrato manda 409 "mensagem própria" para alterar o próprio acesso, mas não fixa o texto.
 * Usada quando o conflito é do próprio usuário e não é CPF, matrícula nem último administrador. Ver entrega.
 */
export const MENSAGEM_EQUIPE_PROPRIO_ACESSO = 'Você não pode alterar o próprio acesso. Peça a outro administrador.';
