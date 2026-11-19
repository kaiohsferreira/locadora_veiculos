/**
 * Cartões e pílulas da tela 11 (Gestão de Frota). Funções puras, sem React.
 *
 * Os percentuais saem do mesmo cálculo da tela 39 (`montarLegendaFrota` em `@/features/visao-geral/frota`), sobre a
 * frota em operação (LOCADO, DISPONIVEL, RESERVADO e MANUTENCAO). A tela 39 não muda: aqui só se reaproveita o cálculo.
 */
import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';
import { formatarPercentual } from '@/lib/formatadoresPainel';
import {
  MENSAGEM_FROTA_ATIVOS,
  MENSAGEM_FROTA_CARTAO_DISPONIVEIS,
  MENSAGEM_FROTA_CARTAO_LOCADOS,
  MENSAGEM_FROTA_CARTAO_OFICINA,
  MENSAGEM_FROTA_CARTAO_RESERVADOS,
  MENSAGEM_FROTA_CARTAO_TOTAL,
  MENSAGEM_FROTA_CHIP_DISPONIVEL,
  MENSAGEM_FROTA_CHIP_LOCADO,
  MENSAGEM_FROTA_CHIP_OFICINA,
  MENSAGEM_FROTA_CHIP_RESERVADO,
  MENSAGEM_FROTA_EM_USO,
  MENSAGEM_FROTA_UNIDADES,
  MENSAGEM_FROTA_VEICULOS,
  mensagemFrotaDisponibilidade,
  mensagemFrotaEmReserva,
  mensagemFrotaEmServico,
  mensagemFrotaTaxaOcupacao,
} from '@/lib/mensagens';
import { montarLegendaFrota } from '@/features/visao-geral/frota';
import type { StatusVeiculo } from '@/types/enums';
import type { FrotaPorSituacao, RespostaVisaoGeral } from '@/types/painel';

/** Cartão da tela 11: total, disponíveis, locados, oficina/revisão e reservados (no lugar de bloqueado/sinistro). */
export type ChaveCartaoFrota = 'total' | 'disponiveis' | 'locados' | 'oficina' | 'reservados';

export interface CartaoFrota {
  chave: ChaveCartaoFrota;
  titulo: string;
  /** Ícone Material Symbols do círculo do cartão. */
  icone: string;
  /** Número formatado, ou o valor ausente enquanto não há dado. */
  valor: string;
  unidade: string;
  /** Linha de percentual (sobre a frota em operação). Nula no cartão total. */
  apoio: string | null;
  /** Etiqueta do cartão (chip). Nula no cartão total. */
  chip: string | null;
  /** Veículos novos no mês. Nulo quando o servidor não manda o campo (servidor antigo). Só no cartão total. */
  novosNoMes: number | null;
}

/** Percentual de uma linha: "63,6%" com uma casa decimal; zero sai como "0%". Sem dado, o valor ausente. */
export function formatarPercentualFrota(valor: number | undefined): string {
  if (valor === undefined) return VALOR_AUSENTE;
  return valor === 0 ? formatarPercentual(0) : formatarPercentual(valor, 1);
}

/**
 * Cinco cartões da tela 11. Sem resposta (carregando ou erro), o número sai como valor ausente e nenhum percentual é
 * inventado. Total zero: todos os percentuais saem "0%".
 */
export function montarCartoesFrota(resposta: RespostaVisaoGeral | undefined): CartaoFrota[] {
  const legenda = resposta ? montarLegendaFrota(resposta.frotaPorSituacao) : undefined;
  const linha = (situacao: StatusVeiculo) => legenda?.find((item) => item.situacao === situacao);
  const percentual = (situacao: StatusVeiculo) => {
    const item = linha(situacao);
    return item === undefined ? undefined : item.percentual;
  };
  const quantidade = (situacao: StatusVeiculo) => {
    const item = linha(situacao);
    return item === undefined ? VALOR_AUSENTE : formatarNumero(item.quantidade);
  };
  const apoioDe = (situacao: StatusVeiculo, mensagem: (texto: string) => string): string | null => {
    const valor = percentual(situacao);
    return valor === undefined ? null : mensagem(formatarPercentualFrota(valor));
  };

  return [
    {
      chave: 'total',
      titulo: MENSAGEM_FROTA_CARTAO_TOTAL,
      icone: 'garage',
      valor: resposta ? formatarNumero(resposta.totalVeiculos) : VALOR_AUSENTE,
      unidade: MENSAGEM_FROTA_UNIDADES,
      apoio: null,
      chip: null,
      novosNoMes: resposta?.veiculosNovosNoMes ?? null,
    },
    {
      chave: 'disponiveis',
      titulo: MENSAGEM_FROTA_CARTAO_DISPONIVEIS,
      icone: 'check_circle',
      valor: quantidade('DISPONIVEL'),
      unidade: MENSAGEM_FROTA_VEICULOS,
      apoio: apoioDe('DISPONIVEL', mensagemFrotaDisponibilidade),
      chip: MENSAGEM_FROTA_CHIP_DISPONIVEL,
      novosNoMes: null,
    },
    {
      chave: 'locados',
      titulo: MENSAGEM_FROTA_CARTAO_LOCADOS,
      icone: 'key',
      valor: quantidade('LOCADO'),
      unidade: MENSAGEM_FROTA_EM_USO,
      apoio: apoioDe('LOCADO', mensagemFrotaTaxaOcupacao),
      chip: MENSAGEM_FROTA_CHIP_LOCADO,
      novosNoMes: null,
    },
    {
      chave: 'oficina',
      titulo: MENSAGEM_FROTA_CARTAO_OFICINA,
      icone: 'build',
      valor: quantidade('MANUTENCAO'),
      unidade: MENSAGEM_FROTA_ATIVOS,
      apoio: apoioDe('MANUTENCAO', mensagemFrotaEmServico),
      chip: MENSAGEM_FROTA_CHIP_OFICINA,
      novosNoMes: null,
    },
    {
      chave: 'reservados',
      titulo: MENSAGEM_FROTA_CARTAO_RESERVADOS,
      icone: 'bookmark',
      valor: quantidade('RESERVADO'),
      unidade: MENSAGEM_FROTA_VEICULOS,
      apoio: apoioDe('RESERVADO', mensagemFrotaEmReserva),
      chip: MENSAGEM_FROTA_CHIP_RESERVADO,
      novosNoMes: null,
    },
  ];
}

/**
 * Contagem de cada pílula: a chave vazia é "Todos" (soma dos status de `frotaPorSituacao`); as outras são o status.
 * Sem a lista (carregando ou erro), devolve nulo e a tela mostra o valor ausente.
 */
export function contarPilulasFrota(
  frotaPorSituacao: readonly FrotaPorSituacao[] | undefined,
): Record<StatusVeiculo | '', number> | null {
  if (frotaPorSituacao === undefined) return null;
  const contagem: Record<StatusVeiculo | '', number> = {
    '': 0,
    DISPONIVEL: 0,
    RESERVADO: 0,
    LOCADO: 0,
    MANUTENCAO: 0,
    VENDIDO: 0,
    BAIXADO: 0,
  };
  for (const item of frotaPorSituacao) {
    contagem[item.situacao] += item.quantidade;
    contagem[''] += item.quantidade;
  }
  return contagem;
}
