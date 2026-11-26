import { converterMoedaPtBrParaNumero, formatarNumero } from '@/lib/formatters';
import { CAMPOS_SIMULACAO_INICIAIS, type CamposSimulacao } from './camposSimulacao';
import { NUMERO_PARCELAS_MAXIMO, NUMERO_PARCELAS_MINIMO, temAteDuasCasas } from './regrasSimulacao';

/** Nomes dos parâmetros do endereço que guardam a simulação na ida para o login. */
const PARAMETRO_ENTRADA = 'entrada';
const PARAMETRO_PARCELAS = 'parcelas';

/**
 * Endereço de volta do login: o caminho da proposta com a entrada (em reais, com ponto decimal) e as parcelas digitadas,
 * para a pessoa voltar com os mesmos valores. Campos inválidos não entram (a volta usa os valores iniciais).
 */
export function montarRetornoLogin(caminho: string, campos: CamposSimulacao): string {
  const parametros = new URLSearchParams();
  const entrada = converterMoedaPtBrParaNumero(campos.entrada);
  if (entrada !== null && entrada >= 0 && temAteDuasCasas(entrada)) {
    parametros.set(PARAMETRO_ENTRADA, entrada.toFixed(2));
  }
  if (/^\d+$/.test(campos.parcelas.trim())) {
    parametros.set(PARAMETRO_PARCELAS, String(Number(campos.parcelas.trim())));
  }
  const busca = parametros.toString();
  return busca.length > 0 ? `${caminho}?${busca}` : caminho;
}

/**
 * Campos iniciais da tela pelo endereço (a volta do login). Cada valor fora do formato ou do intervalo é ignorado e fica
 * o inicial: entrada R$ 0,00 e 12 parcelas.
 */
export function lerCamposDoEndereco(busca: string): CamposSimulacao {
  const parametros = new URLSearchParams(busca);
  const campos: CamposSimulacao = { ...CAMPOS_SIMULACAO_INICIAIS };

  const entradaTexto = parametros.get(PARAMETRO_ENTRADA);
  if (entradaTexto !== null && /^\d{1,10}(\.\d{1,2})?$/.test(entradaTexto)) {
    campos.entrada = formatarNumero(Number(entradaTexto), 2);
  }

  const parcelasTexto = parametros.get(PARAMETRO_PARCELAS);
  if (parcelasTexto !== null && /^\d{1,2}$/.test(parcelasTexto)) {
    const parcelas = Number(parcelasTexto);
    if (parcelas >= NUMERO_PARCELAS_MINIMO && parcelas <= NUMERO_PARCELAS_MAXIMO) campos.parcelas = String(parcelas);
  }

  return campos;
}
