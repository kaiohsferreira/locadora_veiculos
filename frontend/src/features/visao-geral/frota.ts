/**
 * Situação da frota (tela 39): legenda com os status reais do veículo e geometria da rosca em SVG.
 * Função pura. A rosca usa circunferência de 100 unidades (raio 15,9155), como no desenho.
 */
import { ROTULO_STATUS_VEICULO, type StatusVeiculo } from '@/types/enums';
import type { FrotaPorSituacao } from '@/types/painel';

/**
 * Ordem da legenda e da rosca: a frota em operação, com locado primeiro (como no desenho). Vendido e baixado não entram:
 * o centro da rosca é o `totalVeiculos` do servidor, que também os deixa de fora (a conta bate com o cartão de ocupação).
 */
export const ORDEM_SITUACOES_FROTA: readonly StatusVeiculo[] = ['LOCADO', 'DISPONIVEL', 'RESERVADO', 'MANUTENCAO'];

/** Uma linha da legenda. Situação sem veículo sai com quantidade zero, para a legenda não mudar de tamanho. */
export interface LinhaSituacaoFrota {
  situacao: StatusVeiculo;
  rotulo: string;
  quantidade: number;
  /** Percentual do total (0 quando o total é zero). */
  percentual: number;
}

/** Um arco da rosca: comprimento e deslocamento na circunferência de 100 unidades. */
export interface SegmentoRosca {
  situacao: StatusVeiculo;
  comprimento: number;
  /** Valor do stroke-dashoffset (negativo, como no desenho). */
  deslocamento: number;
}

/**
 * Legenda da frota em operação, na ordem fixa. Soma as quantidades de uma mesma situação, se o servidor mandar repetida.
 * Vendido e baixado não entram nem no total nem nas linhas.
 */
export function montarLegendaFrota(frota: readonly FrotaPorSituacao[]): LinhaSituacaoFrota[] {
  const quantidades = new Map<StatusVeiculo, number>();
  for (const item of frota) {
    quantidades.set(item.situacao, (quantidades.get(item.situacao) ?? 0) + item.quantidade);
  }
  const total = ORDEM_SITUACOES_FROTA.reduce((soma, situacao) => soma + (quantidades.get(situacao) ?? 0), 0);

  return ORDEM_SITUACOES_FROTA.map((situacao) => {
    const quantidade = quantidades.get(situacao) ?? 0;
    return {
      situacao,
      rotulo: ROTULO_STATUS_VEICULO[situacao],
      quantidade,
      percentual: total > 0 ? (quantidade * 100) / total : 0,
    };
  });
}

/**
 * Arcos da rosca em sequência a partir do topo. Situação com zero veículo não gera arco; total zero não gera nenhum
 * (a rosca fica só com a trilha de fundo).
 */
export function calcularSegmentosRosca(linhas: readonly LinhaSituacaoFrota[]): SegmentoRosca[] {
  const segmentos: SegmentoRosca[] = [];
  let acumulado = 0;
  for (const linha of linhas) {
    if (linha.quantidade <= 0) continue;
    segmentos.push({
      situacao: linha.situacao,
      comprimento: linha.percentual,
      deslocamento: 0 - acumulado,
    });
    acumulado += linha.percentual;
  }
  return segmentos;
}
