/**
 * Mensagens da tela Minhas reservas (T-FE-03, tela 29). Reexportadas por `@/lib/mensagens`.
 * Textos que vêm do contrato reserva (ErroReserva*) não ficam aqui: a tela mostra o detail do ProblemDetails.
 */

// Página e cabeçalho ---------------------------------------------------------

export const MENSAGEM_RESERVAS_TITULO_PAGINA = 'Minhas reservas';
export const MENSAGEM_RESERVAS_DESCRICAO = 'Acompanhe suas reservas ativas e consulte o histórico.';

/** "Bem-vindo de volta, Marina Costa" (sem nome, só a saudação). */
export function mensagemReservasBoasVindas(nome: string): string {
  const nomeAparado = nome.trim();
  return nomeAparado ? `Bem-vindo de volta, ${nomeAparado}` : 'Bem-vindo de volta';
}

// Abas e atualização ---------------------------------------------------------

export const MENSAGEM_RESERVAS_NAVEGACAO = 'Navegação de reservas';
export const MENSAGEM_RESERVAS_ABA_ATIVAS = 'Ativas e Próximas';
export const MENSAGEM_RESERVAS_ABA_HISTORICO = 'Histórico';

export function mensagemReservasAtualizadoHoje(hora: string): string {
  return `Atualizado hoje às ${hora}`;
}

export function mensagemReservasAtualizadoEm(data: string, hora: string): string {
  return `Atualizado em ${data} às ${hora}`;
}

// Cartão da reserva ativa ----------------------------------------------------

export const MENSAGEM_RESERVAS_CODIGO_ROTULO = 'Código da reserva';
export const MENSAGEM_RESERVAS_COPIAR = 'Copiar código';
export const MENSAGEM_RESERVAS_COPIADO = 'Copiado!';
export const MENSAGEM_RESERVAS_COPIA_FALHOU = 'Não foi possível copiar o código. Selecione e copie manualmente.';

/** "Criada em 16/11/2026 14:22" (a data e a hora já vêm formatadas pelo back). */
export function mensagemReservasCriadaEm(dataHora: string): string {
  return `Criada em ${dataHora}`;
}

export const MENSAGEM_RESERVAS_ROTEIRO = 'Roteiro da reserva';
export const MENSAGEM_RESERVAS_RETIRADA = 'Retirada agendada';
export const MENSAGEM_RESERVAS_DEVOLUCAO = 'Devolução estimada';
export const MENSAGEM_RESERVAS_LOCADORA = 'Locadora';
export const MENSAGEM_RESERVAS_COMO_CHEGAR = 'Como chegar';
export const MENSAGEM_RESERVAS_COMO_CHEGAR_NOVA_ABA = ' (abre o mapa em outra aba)';

/** "1 diária" ou "4 diárias". */
export function mensagemReservasDiarias(quantidade: number): string {
  return quantidade === 1 ? '1 diária' : `${quantidade} diárias`;
}

/** "18/10/2026 a 21/10/2026" (as datas já vêm formatadas). */
export function mensagemReservasPeriodoDatas(inicio: string, fim: string): string {
  return `${inicio} a ${fim}`;
}

/** "4 diárias × R$ 286,54" (o valor já vem formatado). */
export function mensagemReservasDiariaVezes(quantidade: number, valorDiaria: string): string {
  return `${mensagemReservasDiarias(quantidade)} × ${valorDiaria}`;
}

export const MENSAGEM_RESERVAS_CONDUTOR = 'Condutor';
export const MENSAGEM_RESERVAS_TITULAR = 'Titular da reserva';
export const MENSAGEM_RESERVAS_VALOR_ESTIMADO = 'Valor estimado';
export const MENSAGEM_RESERVAS_ANO_MODELO = 'Ano do modelo';

/** "Ano do modelo 2024 · Prata"; sem cor (nula, vazia ou só espaços) devolve só "Ano do modelo 2024". */
export function mensagemReservasAnoModeloCor(ano: number, cor: string | null): string {
  const anoModelo = `${MENSAGEM_RESERVAS_ANO_MODELO} ${ano}`;
  const corAparada = cor?.trim() ?? '';
  return corAparada ? `${anoModelo} · ${corAparada}` : anoModelo;
}

// Cancelamento ---------------------------------------------------------------

export const MENSAGEM_RESERVAS_BOTAO_CANCELAR = 'Cancelar reserva';
export const MENSAGEM_RESERVAS_MANTER = 'Manter reserva';
export const MENSAGEM_RESERVAS_CANCELADA = 'Reserva cancelada.';
export const MENSAGEM_RESERVAS_CANCELAR_DESCRICAO = 'A reserva deixa de valer e não pode ser reativada.';

/** "Cancelar a reserva AV-00042?" */
export function mensagemReservasCancelarTitulo(codigo: string): string {
  return `Cancelar a reserva ${codigo}?`;
}

// Histórico ------------------------------------------------------------------

export const MENSAGEM_RESERVAS_HISTORICO_TITULO = 'Histórico de reservas';
export const MENSAGEM_RESERVAS_HISTORICO_DESCRICAO = 'Reservas concluídas e canceladas.';
export const MENSAGEM_RESERVAS_HISTORICO_BUSCA_FORM = 'Buscar no histórico de reservas';
export const MENSAGEM_RESERVAS_BUSCA_ROTULO = 'Filtrar por código ou modelo';
export const MENSAGEM_RESERVAS_BUSCA_PLACEHOLDER = 'Filtrar por código ou modelo...';
export const MENSAGEM_RESERVAS_BUSCA_BOTAO = 'Filtrar';
export const MENSAGEM_RESERVAS_LIMPAR_BUSCA = 'Limpar busca';
export const MENSAGEM_RESERVAS_TABELA_LEGENDA = 'Histórico de reservas';
export const MENSAGEM_RESERVAS_COLUNA_RESERVA = 'Reserva / data';
export const MENSAGEM_RESERVAS_COLUNA_VEICULO = 'Veículo e categoria';
export const MENSAGEM_RESERVAS_COLUNA_LOCADORA = 'Locadora';
export const MENSAGEM_RESERVAS_COLUNA_PERIODO = 'Período';
export const MENSAGEM_RESERVAS_COLUNA_VALOR = 'Valor estimado';
export const MENSAGEM_RESERVAS_COLUNA_STATUS = 'Status';
export const MENSAGEM_RESERVAS_RODAPE_ITENS = 'reservas';

// Estados --------------------------------------------------------------------

export const MENSAGEM_RESERVAS_VAZIO_ATIVAS_TITULO = 'Você não tem reservas ativas.';
export const MENSAGEM_RESERVAS_VAZIO_ATIVAS_DESCRICAO = 'Quando você reservar um veículo, ele aparece aqui.';
export const MENSAGEM_RESERVAS_BUSCAR_VEICULO = 'Buscar um veículo';
export const MENSAGEM_RESERVAS_VAZIO_HISTORICO_TITULO = 'Nenhuma reserva no histórico.';
export const MENSAGEM_RESERVAS_VAZIO_HISTORICO_DESCRICAO = 'Reservas concluídas e canceladas aparecem aqui.';
export const MENSAGEM_RESERVAS_BUSCA_VAZIA_TITULO = 'Nenhuma reserva encontrada.';
export const MENSAGEM_RESERVAS_ERRO_TITULO = 'Não foi possível carregar suas reservas.';
export const MENSAGEM_RESERVAS_ERRO_ATUALIZACAO =
  'Não foi possível atualizar a lista. Os dados abaixo podem estar desatualizados.';
