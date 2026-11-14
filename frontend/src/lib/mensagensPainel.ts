/**
 * Textos da visão geral da locadora (P11-FE-01, telas 39 e 40). Reexportados por `@/lib/mensagens`.
 *
 * A mensagem de erro segue ErroConsultarVisaoGeral do contrato painel.contract.json. Os demais textos vêm do desenho
 * (telas 39 e 40) ou foram escritos pelo front e estão marcados como "de interface".
 */
import { formatarNumero } from '@/lib/formatters';
import { pluralizar } from '@/lib/formatadoresPainel';
import type { PeriodoVisaoGeral } from '@/types/painel';

// Página e cabeçalho ---------------------------------------------------------

export const MENSAGEM_PAINEL_TITULO_PAGINA = 'Visão geral';
/** Nome do sistema no rótulo da página quando a sessão não traz a locadora. */
export const MENSAGEM_PAINEL_NOME_SISTEMA = 'AutoVia';
/** Rótulo de leitor de tela do grupo de períodos. */
export const MENSAGEM_PAINEL_PERIODO_GRUPO = 'Período do resumo';
/** De interface: anúncio do período escolhido, lido pelo leitor de tela quando muda. */
export function mensagemPainelPeriodoAnunciado(rotulo: string): string {
  return `Período escolhido: ${rotulo}.`;
}
/** Texto do botão no cabeçalho (desenho da tela 39, com o sinal de mais no próprio texto). */
export const MENSAGEM_PAINEL_BOTAO_CADASTRAR_CABECALHO = '+ Cadastrar veículo';
/** Texto do botão do passo "Cadastre o primeiro veículo" (tela 40). */
export const MENSAGEM_PAINEL_BOTAO_CADASTRAR_PASSO = 'Cadastrar veículo';
/** Data de hoje no subtítulo: "Resumo da operação em 13/11/2026". */
export function mensagemPainelResumoDaOperacao(dataHoje: string): string {
  return `Resumo da operação em ${dataHoje}`;
}

/** Rótulos dos períodos, na ordem do seletor (desenho da tela 39). */
export const ROTULO_PERIODO_VISAO_GERAL: Readonly<Record<PeriodoVisaoGeral, string>> = {
  HOJE: 'Hoje',
  SETE_DIAS: '7 dias',
  TRINTA_DIAS: '30 dias',
  MES_ATUAL: 'Este mês',
};

// Saudação (de interface: depende da hora local) ------------------------------

export const MENSAGEM_SAUDACAO_MANHA = 'Bom dia';
export const MENSAGEM_SAUDACAO_TARDE = 'Boa tarde';
export const MENSAGEM_SAUDACAO_NOITE = 'Boa noite';

// Erros e carregamento -------------------------------------------------------

/** ErroConsultarVisaoGeral do contrato: a mensagem técnica do servidor nunca aparece na tela. */
export const MENSAGEM_PAINEL_ERRO_CARREGAR = 'Não foi possível carregar a visão geral.';
export const MENSAGEM_PAINEL_CARREGANDO_RESUMO = 'Carregando o resumo da locadora';

// Indicadores ----------------------------------------------------------------

export const MENSAGEM_PAINEL_INDICADOR_RECEITA = 'Receita do período';
export const MENSAGEM_PAINEL_INDICADOR_A_RECEBER = 'A receber';
export const MENSAGEM_PAINEL_INDICADOR_EM_ATRASO = 'Em atraso';
export const MENSAGEM_PAINEL_INDICADOR_OCUPACAO = 'Taxa de ocupação';
export const MENSAGEM_PAINEL_INDICADOR_LOCACOES = 'Locações ativas';

/** Texto no lugar do valor em reais quando o cargo não pode ver valores (o corte é do servidor). */
export const MENSAGEM_PAINEL_SEM_PERMISSAO_VALORES = 'Disponível para Administrador e Financeiro';
export const MENSAGEM_PAINEL_SEM_DADOS = 'Sem dados ainda';

/** "+12% em relação ao período anterior". */
export function mensagemPainelVariacao(variacao: string): string {
  return `${variacao} em relação ao período anterior`;
}

/** "14 cobranças em aberto" / "1 cobrança em aberto". */
export function mensagemPainelCobrancasEmAberto(quantidade: number): string {
  return `${pluralizar(quantidade, 'cobrança', 'cobranças')} em aberto`;
}

/** "3 cobranças vencidas" / "1 cobrança vencida". */
export function mensagemPainelCobrancasVencidas(quantidade: number): string {
  return pluralizar(quantidade, 'cobrança vencida', 'cobranças vencidas');
}

/**
 * "34 de 50 veículos locados", "1 de 1 veículo locado", "0 de 1 veículo locado" (de interface: a contagem vem do
 * servidor). O adjetivo concorda com o substantivo (o total), não com a quantidade locada.
 */
export function mensagemPainelOcupacao(locados: number, total: number): string {
  const substantivo = pluralizar(total, 'veículo', 'veículos');
  const adjetivo = total === 1 ? 'locado' : 'locados';
  return `${formatarNumero(locados)} de ${substantivo} ${adjetivo}`;
}

/** "5 devoluções previstas para hoje" / "1 devolução prevista para hoje". */
export function mensagemPainelDevolucoesPrevistas(quantidade: number): string {
  return pluralizar(quantidade, 'devolução prevista para hoje', 'devoluções previstas para hoje');
}

// Situação da frota ----------------------------------------------------------

export const MENSAGEM_PAINEL_FROTA_TITULO = 'Situação da frota';
export const MENSAGEM_PAINEL_FROTA_INVENTARIO = 'Inventário';
export const MENSAGEM_PAINEL_FROTA_CENTRO = 'veículos';
export const MENSAGEM_PAINEL_FROTA_LEGENDA = 'Quantidade de veículos por situação';

/** "50 veículos" ou "nenhum veículo cadastrado" (resumo para leitor de tela). */
export function mensagemPainelFrotaResumo(total: number): string {
  return total === 0 ? 'nenhum veículo cadastrado' : pluralizar(total, 'veículo', 'veículos');
}

/** Rótulo do gráfico para leitores de tela (a legenda ao lado traz os números). */
export function mensagemPainelFrotaGrafico(resumo: string): string {
  return `Gráfico da situação da frota: ${resumo}`;
}

// Variante de primeiro acesso (tela 40) --------------------------------------

export const MENSAGEM_PAINEL_CONFIGURACAO_INICIAL = 'Configuração inicial';

/** "Bem-vinda à AutoVia, Rota Sul Locadora" (desenho da tela 40). Texto mantido como no desenho: sem precedente de forma neutra. */
export function mensagemPainelBoasVindas(nomeLocadora: string | undefined): string {
  return nomeLocadora ? `Bem-vinda à AutoVia, ${nomeLocadora}` : 'Bem-vinda à AutoVia';
}

/**
 * De interface: o desenho diz "para a sua frota aparecer no catálogo", mas a publicação no catálogo não entra nesta fatia
 * (sem passo correspondente), então a frase fica neutra.
 */
export function mensagemPainelFaltamPassos(restantes: number): string {
  return restantes === 1
    ? 'Falta 1 passo para começar a operar.'
    : `Faltam ${restantes} passos para começar a operar.`;
}

export const MENSAGEM_PAINEL_PROGRESSO_ATIVACAO = 'Progresso da ativação';

/** "1 de 2 concluído" (de interface, calculado pelos passos). */
export function mensagemPainelProgresso(concluidos: number, total: number): string {
  return `${concluidos} de ${total} concluído${concluidos === 1 ? '' : 's'}`;
}

export const MENSAGEM_PAINEL_PRIMEIROS_PASSOS = 'Primeiros passos';
export const MENSAGEM_PAINEL_PRIMEIROS_PASSOS_DESCRICAO =
  'Siga o roteiro operacional para começar a emitir contratos e receber reservas';

/** "Etapa 2 em andamento" (selo do cartão de passos). */
export function mensagemPainelEtapaEmAndamento(numero: number): string {
  return `Etapa ${numero} em andamento`;
}

export const MENSAGEM_PAINEL_PASSO_CONCLUIDO = 'Concluído';
export const MENSAGEM_PAINEL_PASSO_PROXIMO = 'Próximo passo';
export const MENSAGEM_PAINEL_PASSO_CONTA_TITULO = 'Conta criada';
export const MENSAGEM_PAINEL_PASSO_CONTA_DESCRICAO =
  'Cadastro inicial e dados da administradora confirmados com sucesso.';
export const MENSAGEM_PAINEL_PASSO_VEICULO_TITULO = 'Cadastre o primeiro veículo';
export const MENSAGEM_PAINEL_PASSO_VEICULO_DESCRICAO =
  'Informe placa Mercosul, categoria, quilometragem, fotos e diária.';
