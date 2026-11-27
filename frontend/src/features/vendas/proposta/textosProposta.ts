/** Funções puras da tela de proposta: textos do cartão do carro e leitura do id da rota. */
import { formatarNumero } from '@/lib/formatters';

/**
 * Ano de fabricação e do modelo, sempre os dois (desenhos 24 e 31): "2023/2023", "2022/2023". O separador muda só o
 * espaçamento ("2023 / 2024" na ficha técnica da proposta).
 */
export function textoAnos(anoFabricacao: number, anoModelo: number, separador = '/'): string {
  return `${anoFabricacao}${separador}${anoModelo}`;
}

/** Cidade e UF da locadora, sem barra solta quando um dos dois falta. */
export function textoCidadeUf(cidade: string, uf: string): string {
  return [cidade, uf].filter((parte) => parte.trim().length > 0).join('/');
}

/** Id do veículo pela rota: só inteiro positivo; qualquer outra coisa vira nulo (a tela mostra "não está à venda"). */
export function lerIdVeiculo(parametro: string | undefined): number | null {
  if (parametro === undefined || !/^\d+$/.test(parametro)) return null;
  const id = Number(parametro);
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}

/**
 * Separa os centavos do valor em reais que o servidor manda, para o preço grande do desenho ("R$ 104.500" e ",00" menor).
 * Sem vírgula, o texto inteiro fica na primeira parte.
 */
export function separarCentavos(valorStr: string): { inteiro: string; centavos: string } {
  const virgula = valorStr.lastIndexOf(',');
  if (virgula < 0) return { inteiro: valorStr, centavos: '' };
  return { inteiro: valorStr.slice(0, virgula), centavos: valorStr.slice(virgula) };
}

/** Valor em reais sem centavos, para os atalhos e a barra da entrada ("R$ 10.450"). */
export function textoReaisInteiros(valor: number): string {
  return `R$ ${formatarNumero(Math.round(valor))}`;
}
