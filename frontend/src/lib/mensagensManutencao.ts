/**
 * Textos da tela Manutenção da Frota (P8-FE-01, tela 14, desenho 14_back_office_manuten_o_da_frota). Reexportados por
 * `@/lib/mensagens`.
 *
 * Os textos aprovados pelo Kaio em 20/11/2026 ("Aprovo como está") vêm de docs/textos-novos-semana4-20-11.md e do
 * contrato manutencao.contract.json v2. Os marcados como "pendente" não estavam na lista aprovada e aguardam o Kaio.
 * Os números (quantidades, km) entram já formatados pelo chamador, com `@/lib/formatters`.
 */

/** Substitui {0}, {1}... pelos valores, na ordem. */
function preencher(modelo: string, ...valores: string[]): string {
  return modelo.replace(/\{(\d+)\}/g, (marca, indice: string) => valores[Number(indice)] ?? marca);
}

// Cabeçalho ------------------------------------------------------------------

export const MENSAGEM_MANUTENCAO_TITULO = 'Manutenção da Frota';
/** Pendente (pergunta 7 do contrato): a trilha fica com estes dois itens; o último ("Oficinas Credenciadas") foi cortado. */
export const MENSAGEM_MANUTENCAO_TRILHA_OPERACOES = 'Operações';
/** Pendente (pergunta 7 do contrato): ver a trilha acima. */
export const MENSAGEM_MANUTENCAO_TRILHA_CONTROLE = 'Controle Técnico de Ativos';
export const MENSAGEM_MANUTENCAO_DESCRICAO =
  'Alertas de revisão por quilometragem e controle das ordens de manutenção da frota.';
/** Botão do cabeçalho e do cartão (P7: "Agendar manutenção" e "Agendar Revisão" viram "Abrir ordem"). */
export const MENSAGEM_MANUTENCAO_BOTAO_ABRIR_ORDEM = 'Abrir ordem';

// Alertas de revisão ---------------------------------------------------------

export const MENSAGEM_MANUTENCAO_ALERTAS_TITULO = 'Alertas de Veículos Próximos da Revisão';
export const MENSAGEM_MANUTENCAO_ALERTA_UM = '1 Requer Atenção';
export const MENSAGEM_MANUTENCAO_ALERTA_VARIOS = '{0} Requerem Atenção';
export const MENSAGEM_MANUTENCAO_INTERVALO_PADRAO = 'Intervalo Padrão: {0} km';
export const MENSAGEM_MANUTENCAO_CATEGORIA = 'Categoria {0}';
export const MENSAGEM_MANUTENCAO_PROXIMA_ROTULO = 'Próxima:';
export const MENSAGEM_MANUTENCAO_FALTAM = 'Faltam {0} km';
export const MENSAGEM_MANUTENCAO_FALTAM_APENAS = 'Faltam apenas {0} km!';
export const MENSAGEM_MANUTENCAO_PASSOU_LIMITE = 'Passou {0} km do limite';
export const MENSAGEM_MANUTENCAO_CICLO_ATINGIDO = '{0}% do ciclo atingido';
export const MENSAGEM_MANUTENCAO_CICLO_LIMITE = '{0}% do ciclo limite';
export const MENSAGEM_MANUTENCAO_REVISAO_IMINENTE = 'Revisão Iminente';
export const MENSAGEM_MANUTENCAO_REVISAO_VENCIDA = 'Revisão vencida';
export const MENSAGEM_MANUTENCAO_CRITICO = 'Crítico';
export const MENSAGEM_MANUTENCAO_BLOQUEAR_OFICINA = 'Bloquear p/ Oficina';
export const MENSAGEM_MANUTENCAO_VER_HISTORICO = 'Ver Histórico';
export const MENSAGEM_MANUTENCAO_NENHUM_ALERTA = 'Nenhum veículo próximo da revisão.';
export const MENSAGEM_MANUTENCAO_ERRO_ALERTAS = 'Não foi possível carregar os alertas de revisão.';
export const MENSAGEM_MANUTENCAO_TENTAR_DE_NOVO = 'Tentar de novo';

// Metricas e resumo ----------------------------------------------------------

export const MENSAGEM_MANUTENCAO_METRICA_OS_ATIVAS = 'OS Ativas';
export const MENSAGEM_MANUTENCAO_METRICA_CUSTO_MES = 'Custo Mês';
export const MENSAGEM_MANUTENCAO_METRICA_UMA_ORDEM = '1 ordem';
export const MENSAGEM_MANUTENCAO_METRICA_VARIAS_ORDENS = '{0} ordens';
export const MENSAGEM_MANUTENCAO_ERRO_RESUMO = 'Não foi possível carregar o resumo da manutenção.';

// Histórico, filtros e tabela ------------------------------------------------

export const MENSAGEM_MANUTENCAO_HISTORICO_TITULO = 'Histórico e Ordens de Manutenção';
export const MENSAGEM_MANUTENCAO_HISTORICO_DESCRICAO = 'Ordens de manutenção abertas e encerradas da frota.';
export const MENSAGEM_MANUTENCAO_BUSCA = 'Buscar por placa, modelo ou código OS...';
export const MENSAGEM_MANUTENCAO_FILTRO_TIPO = 'Tipo:';
export const MENSAGEM_MANUTENCAO_TIPO_TODAS = 'Todas';
export const MENSAGEM_MANUTENCAO_FILTRO_STATUS = 'Status:';
export const MENSAGEM_MANUTENCAO_STATUS_TODOS = 'Todos os Status';
export const MENSAGEM_MANUTENCAO_STATUS_EXECUCAO = 'Em Execução';
export const MENSAGEM_MANUTENCAO_STATUS_CONCLUIDA = 'Concluída';
export const MENSAGEM_MANUTENCAO_ATUALIZAR = 'Atualizar';
export const MENSAGEM_MANUTENCAO_LIMPAR_FILTROS = 'Limpar filtros';
export const MENSAGEM_MANUTENCAO_COLUNA_VEICULO = 'Veículo & Placa';
export const MENSAGEM_MANUTENCAO_COLUNA_TIPO = 'Tipo';
export const MENSAGEM_MANUTENCAO_COLUNA_ENTRADA = 'Entrada';
export const MENSAGEM_MANUTENCAO_COLUNA_SAIDA = 'Saída';
export const MENSAGEM_MANUTENCAO_COLUNA_OFICINA = 'Oficina';
export const MENSAGEM_MANUTENCAO_COLUNA_CUSTO = 'Custo Total';
export const MENSAGEM_MANUTENCAO_COLUNA_STATUS = 'Status';
export const MENSAGEM_MANUTENCAO_COLUNA_ACOES = 'Ações';
export const MENSAGEM_MANUTENCAO_EM_ABERTO = 'Em aberto';
export const MENSAGEM_MANUTENCAO_ENCERRAR_OS = 'Encerrar OS';
export const MENSAGEM_MANUTENCAO_LISTA_VAZIA = 'Nenhuma ordem de manutenção encontrada.';
export const MENSAGEM_MANUTENCAO_ERRO_LISTA = 'Não foi possível carregar as ordens de manutenção.';
export const MENSAGEM_MANUTENCAO_LINHAS_POR_PAGINA = 'Linhas por página:';
export const MENSAGEM_MANUTENCAO_RODAPE_ITENS = 'ordens de serviço';

// Modal de abrir ordem -------------------------------------------------------

export const MENSAGEM_MANUTENCAO_MODAL_ABRIR_TITULO = 'Abrir ordem de manutenção';
export const MENSAGEM_MANUTENCAO_CAMPO_VEICULO = 'Veículo';
/** Mesmo rótulo do cabeçalho da coluna "Tipo" (texto aprovado). */
export const MENSAGEM_MANUTENCAO_CAMPO_TIPO = 'Tipo';
export const MENSAGEM_MANUTENCAO_CAMPO_OFICINA = 'Oficina';
export const MENSAGEM_MANUTENCAO_CAMPO_DESCRICAO = 'Descrição';
export const MENSAGEM_MANUTENCAO_CAMPO_DATA_ENTRADA = 'Data de entrada';
export const MENSAGEM_MANUTENCAO_BOTAO_ABRIR_CONFIRMAR = 'Abrir ordem';
export const MENSAGEM_MANUTENCAO_BOTAO_CANCELAR = 'Cancelar';
export const MENSAGEM_MANUTENCAO_SUCESSO_ABRIR = 'Ordem de manutenção aberta. O veículo saiu da busca.';

// Modal de encerrar ordem ----------------------------------------------------

export const MENSAGEM_MANUTENCAO_CAMPO_KM_SAIDA = 'Quilometragem na saída';
export const MENSAGEM_MANUTENCAO_CAMPO_DATA_SAIDA = 'Data de saída';
export const MENSAGEM_MANUTENCAO_CAMPO_CUSTO = 'Custo (R$)';
export const MENSAGEM_MANUTENCAO_BOTAO_ENCERRAR_ORDEM = 'Encerrar ordem';
export const MENSAGEM_MANUTENCAO_SUCESSO_ENCERRAR = 'Ordem encerrada.';

// Validações do formulário (mesmos textos do servidor) -----------------------

export const MENSAGEM_MANUTENCAO_ERRO_VEICULO = 'Informe o veículo.';
export const MENSAGEM_MANUTENCAO_ERRO_TIPO = 'Informe o tipo da manutenção.';
export const MENSAGEM_MANUTENCAO_ERRO_DATA_ENTRADA_FUTURA = 'A data de entrada não pode ser futura.';
export const MENSAGEM_MANUTENCAO_ERRO_DATA_SAIDA_FUTURA = 'A data de saída não pode ser futura.';
export const MENSAGEM_MANUTENCAO_ERRO_DATA_SAIDA_ANTERIOR =
  'A data de saída não pode ser anterior à data de entrada.';
export const MENSAGEM_MANUTENCAO_ERRO_KM = 'Informe a quilometragem do veículo na saída.';
/** Pendente: a quilometragem tem de caber num inteiro de 32 bits (o servidor guarda int). Texto igual ao da vistoria, uso novo aqui. */
export const MENSAGEM_MANUTENCAO_ERRO_KM_LIMITE = 'Informe a quilometragem inteira entre 0 e 2.147.483.647 km.';
/** Pendente: o texto de custo do servidor (ErroManutencaoCustoPrecisao) não estava na lista aprovada. */
export const MENSAGEM_MANUTENCAO_ERRO_CUSTO_PRECISAO =
  'O custo deve ter no máximo 8 dígitos inteiros e 2 casas decimais.';

/** Texto do alerta de revisão, quando há um só ou mais de um. `quantidade` decide o singular. */
export function mensagemManutencaoSeloAlertas(quantidade: number, quantidadeTexto: string): string {
  return quantidade === 1 ? MENSAGEM_MANUTENCAO_ALERTA_UM : preencher(MENSAGEM_MANUTENCAO_ALERTA_VARIOS, quantidadeTexto);
}

export function mensagemManutencaoIntervalo(kmTexto: string): string {
  return preencher(MENSAGEM_MANUTENCAO_INTERVALO_PADRAO, kmTexto);
}

export function mensagemManutencaoCategoria(nome: string): string {
  return preencher(MENSAGEM_MANUTENCAO_CATEGORIA, nome);
}

export function mensagemManutencaoFaltam(kmTexto: string): string {
  return preencher(MENSAGEM_MANUTENCAO_FALTAM, kmTexto);
}

export function mensagemManutencaoFaltamApenas(kmTexto: string): string {
  return preencher(MENSAGEM_MANUTENCAO_FALTAM_APENAS, kmTexto);
}

export function mensagemManutencaoPassouLimite(kmTexto: string): string {
  return preencher(MENSAGEM_MANUTENCAO_PASSOU_LIMITE, kmTexto);
}

export function mensagemManutencaoCicloAtingido(percentualTexto: string): string {
  return preencher(MENSAGEM_MANUTENCAO_CICLO_ATINGIDO, percentualTexto);
}

export function mensagemManutencaoCicloLimite(percentualTexto: string): string {
  return preencher(MENSAGEM_MANUTENCAO_CICLO_LIMITE, percentualTexto);
}

/** "1 ordem" ou "18 ordens": o singular só para exatamente uma. */
export function mensagemManutencaoOrdensAtivas(quantidade: number, quantidadeTexto: string): string {
  return quantidade === 1
    ? MENSAGEM_MANUTENCAO_METRICA_UMA_ORDEM
    : preencher(MENSAGEM_MANUTENCAO_METRICA_VARIAS_ORDENS, quantidadeTexto);
}

/** Código da OS na tabela e no cartão (o número é o id da ordem). */
export function mensagemManutencaoOs(idManutencao: number): string {
  return `OS #${idManutencao}`;
}

export function mensagemManutencaoEncerrarTitulo(idManutencao: number): string {
  return preencher('Encerrar ordem #{0}', String(idManutencao));
}

export function mensagemManutencaoOficinaTamanho(maximo: number): string {
  return preencher('A oficina deve ter no máximo {0} caracteres.', String(maximo));
}

export function mensagemManutencaoDescricaoTamanho(maximo: number): string {
  return preencher('A descrição deve ter no máximo {0} caracteres.', String(maximo));
}
