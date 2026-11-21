import type { FiltroApolices } from '@/types/seguros';

/** 20 apólices por página (o padrão do servidor). */
export const TAMANHO_PAGINA_SEGUROS = 20;

/** Filtros da tela, como ficam no endereço: o veículo leva a placa junto, para a etiqueta não precisar de outra consulta. */
export interface EstadoFiltrosSeguros {
  pagina: number;
  apenasAVencer: boolean;
  veiculo: { idVeiculo: number; placa: string } | null;
}

/** Inteiro positivo do endereço; qualquer outra coisa é ignorada. */
function inteiroPositivo(texto: string | null): number | null {
  if (texto === null || !/^\d{1,9}$/.test(texto)) return null;
  const numero = Number(texto);
  return numero >= 1 ? numero : null;
}

/** Lê os filtros do endereço (`?veiculo=7&placa=ABC1D23&aVencer=1&pagina=2`). Valor inválido é ignorado. */
export function lerFiltrosSeguros(parametros: URLSearchParams): EstadoFiltrosSeguros {
  const idVeiculo = inteiroPositivo(parametros.get('veiculo'));
  const placa = (parametros.get('placa') ?? '').trim();
  return {
    pagina: inteiroPositivo(parametros.get('pagina')) ?? 1,
    apenasAVencer: parametros.get('aVencer') === '1',
    veiculo: idVeiculo !== null ? { idVeiculo, placa } : null,
  };
}

/** Escreve os filtros no endereço: só o que está ativo, e a página quando não é a primeira. */
export function escreverFiltrosSeguros(estado: EstadoFiltrosSeguros): URLSearchParams {
  const parametros = new URLSearchParams();
  if (estado.veiculo !== null) {
    parametros.set('veiculo', String(estado.veiculo.idVeiculo));
    if (estado.veiculo.placa) parametros.set('placa', estado.veiculo.placa);
  }
  if (estado.apenasAVencer) parametros.set('aVencer', '1');
  if (estado.pagina > 1) parametros.set('pagina', String(estado.pagina));
  return parametros;
}

/** Filtro da API a partir do estado da tela. */
export function montarFiltroApolices(estado: EstadoFiltrosSeguros): FiltroApolices {
  const filtro: FiltroApolices = {
    pagina: estado.pagina,
    tamanhoPagina: TAMANHO_PAGINA_SEGUROS,
    apenasAVencer: estado.apenasAVencer,
  };
  if (estado.veiculo !== null) filtro.idVeiculo = estado.veiculo.idVeiculo;
  return filtro;
}
