import { MENSAGENS_VISTORIA } from './mensagensVistoria';

/**
 * Mensagens da tela "Minhas locações" do cliente (tela 27, T-FE-02). Textos aprovados pelo Kaio em 26/11/2026
 * ("Aprovo como esta", aprovacao-eduardo-finalizar-26-11.md e aprovacao-eduardo-telas-26-11.md, seção Tela 27).
 * Reexportadas por `@/lib/mensagens`. Os textos reaproveitados de outras telas ficam nos arquivos de origem.
 */

// Cabeçalho e navegação ------------------------------------------------------

export const MENSAGEM_LOCACOES_TITULO_PAGINA = 'Minhas locações';
export const MENSAGEM_LOCACOES_DESCRICAO =
  'Acompanhe suas locações em andamento, o prazo de devolução e consulte o histórico.';
export const MENSAGEM_LOCACOES_NOVA_RESERVA = 'Nova Reserva';
export const MENSAGEM_LOCACOES_NAVEGACAO = 'Navegação de locações';
export const MENSAGEM_LOCACOES_ABA_ANDAMENTO = 'Em andamento';
export const MENSAGEM_LOCACOES_ABA_HISTORICO = 'Histórico';
export const MENSAGEM_LOCACOES_MIGALHA_ATUAL = 'Minhas locações';

/** "Locação em andamento" (uma) ou "Locações em andamento" (mais de uma). */
export function mensagemLocacoesCabecalhoAtivas(quantidade: number): string {
  return quantidade === 1 ? 'Locação em andamento' : 'Locações em andamento';
}

// Estados da lista -----------------------------------------------------------

export const MENSAGEM_LOCACOES_VAZIO_ANDAMENTO_TITULO = 'Você não tem locações em andamento.';
export const MENSAGEM_LOCACOES_VAZIO_ANDAMENTO_DESCRICAO = 'Quando você retirar um veículo, a locação aparece aqui.';
export const MENSAGEM_LOCACOES_VAZIO_HISTORICO_TITULO = 'Nenhuma locação no histórico.';
export const MENSAGEM_LOCACOES_VAZIO_HISTORICO_DESCRICAO = 'Locações encerradas e canceladas aparecem aqui.';
export const MENSAGEM_LOCACOES_VER_RESERVAS = 'Ver minhas reservas';
export const MENSAGEM_LOCACOES_ERRO_TITULO = 'Não foi possível carregar suas locações.';
/** Texto do erro da lista quando não há resposta do servidor ou ele falhou (500): aprovado em 26/11/2026 (ERRO-1). */
export const MENSAGEM_LOCACOES_ERRO_LISTA = 'Não foi possível carregar suas locações agora. Tente novamente em instantes.';
/** Rodapé da paginação, com o total: "{n} locações" (aprovado em 26/11/2026). */
export const MENSAGEM_LOCACOES_RODAPE_ITENS = 'locações';

// Últimas locações (dentro da aba Em andamento) ------------------------------

export const MENSAGEM_LOCACOES_ULTIMAS_TITULO = 'Últimas locações do histórico';
export const MENSAGEM_LOCACOES_ULTIMAS_DESCRICAO = 'Suas últimas locações encerradas ou canceladas.';

/** Link para a aba Histórico, só quando há mais locações do que os cartões mostrados. */
export function mensagemLocacoesVerTodas(total: number): string {
  return `Ver todas as ${total} locações`;
}

// Cartão da locação em andamento ---------------------------------------------

export const MENSAGEM_LOCACOES_SITUACAO_ATIVA = 'Locação Ativa';
export const MENSAGEM_LOCACOES_SITUACAO_ATRASADA = 'Locação Atrasada';

/** "Locação nº 12" e "Reserva AV-00042": o código da reserva de origem, sem texto novo além de "Reserva". */
export function mensagemLocacoesCodigoReserva(codigo: string): string {
  return `Reserva ${codigo}`;
}

/** "Locadora X · Rua Y, 100 - Centro" (nome da locadora e o endereço; sem endereço, só o nome). */
export function mensagemLocacoesLocalEndereco(nomeLocadora: string, endereco: string): string {
  return endereco ? `${nomeLocadora} · ${endereco}` : nomeLocadora;
}

/** "Retirado em Locadora X" (no lugar de "Retirado no Aeroporto de Congonhas (CGH)"). */
export function mensagemLocacoesRetiradoEm(nomeLocadora: string): string {
  return `Retirado em ${nomeLocadora}`;
}

export const MENSAGEM_LOCACOES_RETIRADA_EFETUADA = 'Retirada Efetuada';
export const MENSAGEM_LOCACOES_DEVOLUCAO_PREVISTA = 'Devolução Prevista';
export const MENSAGEM_LOCACOES_VALOR_TOTAL = 'Valor total';
export const MENSAGEM_LOCACOES_KM_RETIRADA = MENSAGENS_VISTORIA.kmSaida;
export const MENSAGEM_LOCACOES_COMBUSTIVEL_RETIRADA = 'Combustível na retirada';
export const MENSAGEM_LOCACOES_COMBUSTIVEL_NAO_INFORMADO = MENSAGENS_VISTORIA.semCombustivel;

/** "98%" e, só quando for 100%, "98% (cheio)" conforme o nível. */
export function mensagemLocacoesCombustivel(percentual: string, cheio: boolean): string {
  return cheio ? `${percentual} (cheio)` : percentual;
}

/** "Progresso da locação: 65% percorrido" (texto para leitor de tela da barra). */
export function mensagemLocacoesProgresso(percentual: number): string {
  return `Progresso da locação: ${percentual}% percorrido`;
}

// Cronômetro -----------------------------------------------------------------

export const MENSAGEM_LOCACOES_CRONOMETRO_TITULO = 'Tempo Restante para Devolução';
export const MENSAGEM_LOCACOES_PRAZO_VENCIDO = 'Prazo de devolução vencido. O atraso pode gerar cobrança extra.';

/** "Dentro do prazo • Tolerância: 59 min"; sem tolerância da categoria, só "Dentro do prazo". */
export function mensagemLocacoesDentroDoPrazo(toleranciaMinutos: number | null): string {
  // == null: chave ausente na resposta (undefined) também é "sem tolerância", e não "undefined min".
  if (toleranciaMinutos == null) return 'Dentro do prazo';
  return `Dentro do prazo • Tolerância: ${toleranciaMinutos} min`;
}

/** Texto para leitor de tela do tempo restante (a contagem em segundos não é anunciada a cada segundo). */
export function mensagemLocacoesFaltam(dias: number, horas: number, minutos: number): string {
  return `Faltam ${dias} dia(s), ${horas} hora(s) e ${minutos} minuto(s) para a devolução.`;
}

/** Rótulos dos quatro blocos do cronômetro: o nome completo, e o curto só no celular (com o nome completo para leitor). */
export const MENSAGEM_LOCACOES_UNIDADES_CRONOMETRO = {
  dias: { longo: 'Dias', curto: 'Dias' },
  horas: { longo: 'Horas', curto: 'Hrs' },
  minutos: { longo: 'Minutos', curto: 'Min' },
  segundos: { longo: 'Segundos', curto: 'Seg' },
} as const;

// Histórico ------------------------------------------------------------------

/** Título da aba Histórico (lista completa, com paginação). */
export const MENSAGEM_LOCACOES_HISTORICO_TITULO = 'Histórico';
export const MENSAGEM_LOCACOES_HISTORICO_DESCRICAO = 'Suas últimas locações encerradas ou canceladas.';
