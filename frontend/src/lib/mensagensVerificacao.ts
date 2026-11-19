/**
 * Textos da tela de Verificação de Cadastros (P2-FE-04, tela 18). Reexportados por `@/lib/mensagens`.
 *
 * Os textos vêm do desenho D:/Estudo/TI/telas/18_back_office_verifica_o_de_cadastros.html, com as trocas do contrato
 * cliente-fila-verificacao v1 (sem o selo da fila, sem o histórico do turno, sem score, OCR, Serasa, protocolo e
 * reenvio). Os textos marcados como "novo" não estão no desenho e precisam de aprovação do Kaio antes do commit.
 */

// Cabeçalho ------------------------------------------------------------------

export const MENSAGEM_VERIFICACAO_TITULO_PAGINA = 'Verificação de Cadastros';
export const MENSAGEM_VERIFICACAO_MIGALHA_RAIZ = 'AutoVia Back Office';
export const MENSAGEM_VERIFICACAO_MIGALHA_MODULO = 'Clientes';
export const MENSAGEM_VERIFICACAO_MIGALHA_LEGENDA = 'Caminho da página';
/** Novo: subtítulo com a regra do RF-10 (no desenho não há subtítulo). */
export const MENSAGEM_VERIFICACAO_DESCRICAO =
  'Cadastros que pediram acesso à sua locadora. Só cadastro aprovado permite reservar.';
/** Desenho: "Sincronizar Fila (Auto)". Sem o "(Auto)": a fila não atualiza sozinha. */
export const MENSAGEM_VERIFICACAO_BOTAO_SINCRONIZAR = 'Sincronizar Fila';

// Cartões --------------------------------------------------------------------

export const MENSAGEM_VERIFICACAO_CARTAO_PENDENTES = 'Cadastros Pendentes';
export const MENSAGEM_VERIFICACAO_CARTAO_UNIDADE = 'solicitações';

// Barra de filtros -----------------------------------------------------------

/** Novo: rótulo para leitor de tela (o campo mostra só o ícone e o exemplo). */
export const MENSAGEM_VERIFICACAO_BUSCA_ROTULO = 'Buscar cadastro';
/** Desenho: "Buscar por nome, CPF ou CNH...". Trocado: a busca vale para nome, CPF ou e-mail (decisão 8). */
export const MENSAGEM_VERIFICACAO_BUSCA_PLACEHOLDER = 'Buscar por nome, CPF ou e-mail...';
/** Novo: rótulo para leitor de tela do seletor de status. */
export const MENSAGEM_VERIFICACAO_STATUS_ROTULO = 'Filtrar por status';
/** Desenho: "Todos os Status". */
export const MENSAGEM_VERIFICACAO_STATUS_TODOS = 'Todos os Status';

// Tabela ---------------------------------------------------------------------

export const MENSAGEM_VERIFICACAO_TABELA_LEGENDA = 'Cadastros para verificação';
export const MENSAGEM_VERIFICACAO_COLUNA_CLIENTE = 'Cliente';
export const MENSAGEM_VERIFICACAO_COLUNA_CPF = 'CPF';
/** Desenho: "Data Cadastro". */
export const MENSAGEM_VERIFICACAO_COLUNA_DATA = 'Data do cadastro';
export const MENSAGEM_VERIFICACAO_COLUNA_STATUS = 'Status';
export const MENSAGEM_VERIFICACAO_COLUNA_ACAO = 'Ação';
/** Novo: nome do botão de abrir a linha, para leitor de tela (o desenho só tem o ícone). */
export function mensagemVerificacaoAbrirCadastro(nome: string): string {
  return `Abrir cadastro de ${nome}`;
}
/** Novo: rodapé. O desenho dizia "cadastros pendentes" mesmo com outro filtro. */
export const MENSAGEM_VERIFICACAO_RODAPE_ITENS = 'cadastros';

// Estados da fila (novos) ----------------------------------------------------

export const MENSAGEM_VERIFICACAO_VAZIA_TITULO = 'Nenhum cadastro na fila.';
export const MENSAGEM_VERIFICACAO_VAZIA_DESCRICAO = 'Os pedidos de cadastro da sua locadora aparecem aqui.';
export const MENSAGEM_VERIFICACAO_VAZIA_FILTRO_TITULO = 'Nenhum cadastro encontrado.';
export const MENSAGEM_VERIFICACAO_VAZIA_FILTRO_DESCRICAO = 'Ajuste a busca ou o status e tente de novo.';
export const MENSAGEM_VERIFICACAO_LIMPAR_FILTROS = 'Limpar filtros';

// Painel do cliente ---------------------------------------------------------

/** Desenho: "Auditoria Cadastral". */
export const MENSAGEM_VERIFICACAO_PAINEL_TITULO = 'Auditoria Cadastral';
/** Desenho: título do botão de fechar. */
export const MENSAGEM_VERIFICACAO_PAINEL_FECHAR = 'Fechar painel';
export const MENSAGEM_VERIFICACAO_PAINEL_CARREGANDO = 'Carregando o cadastro';
export const MENSAGEM_VERIFICACAO_PAINEL_ERRO_TITULO = 'Não foi possível carregar o cadastro.';

export const MENSAGEM_VERIFICACAO_SECAO_DADOS = 'Dados pessoais';
export const MENSAGEM_VERIFICACAO_ROTULO_CPF = 'CPF';
export const MENSAGEM_VERIFICACAO_ROTULO_NASCIMENTO = 'Nascimento';
export const MENSAGEM_VERIFICACAO_ROTULO_EMAIL = 'E-mail';
export const MENSAGEM_VERIFICACAO_ROTULO_TELEFONE = 'Telefone';

/** Novo: CNH como o cliente informou (sem leitura automática). */
export const MENSAGEM_VERIFICACAO_SECAO_CNH = 'CNH informada';
export const MENSAGEM_VERIFICACAO_ROTULO_CNH_NUMERO = 'Número';
export const MENSAGEM_VERIFICACAO_ROTULO_CNH_CATEGORIA = 'Categoria';
export const MENSAGEM_VERIFICACAO_ROTULO_CNH_VALIDADE = 'Validade';

export const MENSAGEM_VERIFICACAO_SECAO_ENDERECO = 'Endereço';
export const MENSAGEM_VERIFICACAO_ENDERECO_VAZIO = 'Endereço não informado.';

/** Novo: bloco do pedido na sua locadora. */
export const MENSAGEM_VERIFICACAO_SECAO_PEDIDO = 'Pedido na sua locadora';
export function mensagemVerificacaoPedidoEm(data: string): string {
  return `Pedido em ${data}`;
}
export function mensagemVerificacaoUltimaDecisao(data: string): string {
  return `Última decisão em ${data}`;
}
export const MENSAGEM_VERIFICACAO_ROTULO_MOTIVO_RECUSA = 'Motivo da recusa';

// Documentos -----------------------------------------------------------------

export const MENSAGEM_VERIFICACAO_SECAO_DOCUMENTOS = 'Documentos';
export const MENSAGEM_VERIFICACAO_DOC_CNH = 'CNH';
export const MENSAGEM_VERIFICACAO_DOC_COMPROVANTE = 'Comprovante de residência';
/** Novo: documento do tipo que o cliente ainda não enviou. */
export const MENSAGEM_VERIFICACAO_DOC_SEM_ENVIO = 'Sem documentos enviados.';
/** Novo: documento com link indisponível (o item continua na lista, sem imagem e sem link). */
export const MENSAGEM_VERIFICACAO_DOC_SEM_LINK = 'Arquivo indisponível para visualização no momento.';
export const MENSAGEM_VERIFICACAO_DOC_BAIXAR_PDF = 'Baixar PDF';
export function mensagemVerificacaoAbrirDocumento(rotulo: string): string {
  return `Abrir documento (${rotulo}) em nova aba`;
}
export function mensagemVerificacaoImagemDocumento(rotulo: string): string {
  return `Imagem enviada pelo cliente: ${rotulo}`;
}
export function mensagemVerificacaoEnviadoEm(data: string): string {
  return `Enviado em ${data}`;
}
export const MENSAGEM_VERIFICACAO_DOCS_ERRO = 'Não foi possível carregar os documentos.';
export const MENSAGEM_VERIFICACAO_TENTAR_NOVAMENTE = 'Tentar novamente';

// Decisão --------------------------------------------------------------------

export const MENSAGEM_VERIFICACAO_SECAO_DECISAO = 'Decisão';
/** Desenho: "Justificativa ou Observações Internas". Trocado: a justificativa vale só na recusa (decisão 7). */
export const MENSAGEM_VERIFICACAO_JUSTIFICATIVA_ROTULO = 'Justificativa da recusa';
/** Desenho: "Informe o motivo da recusa ou anotações para o time de balcão...". Sai a parte das anotações. */
export const MENSAGEM_VERIFICACAO_JUSTIFICATIVA_PLACEHOLDER = 'Informe o motivo da recusa...';
/** Novo: texto de apoio verdadeiro (decisão 7). */
export const MENSAGEM_VERIFICACAO_JUSTIFICATIVA_APOIO =
  'A justificativa só é usada na recusa. Para recusar, informe o motivo: ele fica registrado.';
export const MENSAGEM_VERIFICACAO_BOTAO_APROVAR = 'Aprovar Cadastro';
export const MENSAGEM_VERIFICACAO_BOTAO_RECUSAR = 'Recusar';
/** Novo: no lugar dos botões, quando o cadastro já está aprovado. */
export const MENSAGEM_VERIFICACAO_JA_APROVADO = 'Este cadastro já está aprovado.';

// Avisos ---------------------------------------------------------------------

export const MENSAGEM_VERIFICACAO_SUCESSO_APROVADO = 'Cadastro aprovado.';
export const MENSAGEM_VERIFICACAO_SUCESSO_RECUSADO = 'Cadastro recusado.';
