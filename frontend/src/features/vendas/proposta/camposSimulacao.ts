import { converterMoedaPtBrParaNumero, limitarDigitos } from '@/lib/formatters';
import {
  PROPOSTA_ERRO_ENTRADA_INVALIDA,
  PROPOSTA_ERRO_ENTRADA_MAIOR,
  PROPOSTA_ERRO_ENTRADA_VAZIA,
  PROPOSTA_ERRO_PARCELAS,
} from '@/lib/mensagensProposta';
import { NUMERO_PARCELAS_MAXIMO, NUMERO_PARCELAS_MINIMO } from './regrasSimulacao';

/** Texto digitado nos dois campos da simulação (a entrada já com a máscara de reais). */
export interface CamposSimulacao {
  entrada: string;
  parcelas: string;
}

/** Erro de cada campo; ausente quando o campo está certo. */
export interface ErrosSimulacao {
  entrada?: string;
  parcelas?: string;
}

/** Valores convertidos dos campos; nulos quando o texto não é um número. */
export interface ValoresSimulacao {
  valorEntrada: number | null;
  numeroParcelas: number | null;
}

/** Valores iniciais da tela: entrada R$ 0,00 e 12 parcelas. */
export const CAMPOS_SIMULACAO_INICIAIS: CamposSimulacao = { entrada: '0,00', parcelas: '12' };

/** Máximo de dígitos do campo de parcelas (60 tem dois). */
const DIGITOS_PARCELAS = 2;

/** Mantém só os dígitos do campo de parcelas, no máximo dois. */
export function mascararParcelas(valor: string): string {
  return limitarDigitos(valor, DIGITOS_PARCELAS);
}

/** Converte o texto dos campos em números. Campo vazio ou fora do formato vira nulo. */
export function converterCampos(campos: CamposSimulacao): ValoresSimulacao {
  const valorEntrada = converterMoedaPtBrParaNumero(campos.entrada);
  const parcelasTexto = campos.parcelas.trim();
  const numeroParcelas = /^\d+$/.test(parcelasTexto) ? Number(parcelasTexto) : null;
  return { valorEntrada, numeroParcelas };
}

/**
 * Regras dos campos, as mesmas da API (para a pessoa ver o erro no campo, sem esperar um 400): entrada preenchida, em
 * reais e menor que o valor de venda; parcelas de 1 a 60.
 */
export function validarCampos(campos: CamposSimulacao, valorVenda: number): ErrosSimulacao {
  const erros: ErrosSimulacao = {};
  const { valorEntrada, numeroParcelas } = converterCampos(campos);

  if (campos.entrada.trim().length === 0) erros.entrada = PROPOSTA_ERRO_ENTRADA_VAZIA;
  else if (valorEntrada === null) erros.entrada = PROPOSTA_ERRO_ENTRADA_INVALIDA;
  else if (valorEntrada >= valorVenda) erros.entrada = PROPOSTA_ERRO_ENTRADA_MAIOR;

  if (numeroParcelas === null || numeroParcelas < NUMERO_PARCELAS_MINIMO || numeroParcelas > NUMERO_PARCELAS_MAXIMO) {
    erros.parcelas = PROPOSTA_ERRO_PARCELAS;
  }

  return erros;
}

/** Os campos não têm erro. */
export function camposValidos(erros: ErrosSimulacao): boolean {
  return erros.entrada === undefined && erros.parcelas === undefined;
}
