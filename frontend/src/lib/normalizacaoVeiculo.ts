/**
 * Normalização da digitação dos campos do veículo, enquanto a pessoa digita. Só troca o que o servidor também troca
 * (StaticMethods.NormalizePlaca e o Trim do validador); o que é inválido continua entrando e a validação aponta.
 */
import { LIMITES_VEICULO } from '@/lib/limitesVeiculo';

/** Caractere nulo (U+0000). O PostgreSQL recusa texto com ele; o servidor responde 400. */
export const CARACTERE_NULO = String.fromCharCode(0);

/** Só letras e números, em maiúsculas, cortados no máximo. Usado para placa e chassi. */
function alfanumericoMaiusculo(valor: string, maximo: number): string {
  return valor.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, maximo);
}

/** Placa: maiúsculas, sem hífen nem espaço, até 7 caracteres ("bra-2e19" → "BRA2E19"). */
export function normalizarPlacaDigitada(valor: string): string {
  return alfanumericoMaiusculo(valor, LIMITES_VEICULO.placa);
}

/** Chassi: maiúsculas, só letras e números, até 17 caracteres. A letra I, O ou Q entra e a validação recusa. */
export function normalizarChassiDigitado(valor: string): string {
  return alfanumericoMaiusculo(valor, LIMITES_VEICULO.chassi);
}

/** Placa como o servidor a recebe e confere (StaticMethods.NormalizePlaca): sem hífen nem espaço, em maiúsculas. */
export function normalizarPlacaParaServidor(valor: string): string {
  return valor.replace(/[-\s]/g, '').toUpperCase();
}

/** Renavam: só dígitos, até 11 (os zeros à esquerda ficam). */
export function normalizarRenavamDigitado(valor: string): string {
  return valor.replace(/\D/g, '').slice(0, LIMITES_VEICULO.renavam);
}

/** Ano digitado: só dígitos, até quatro. */
export function normalizarAnoDigitado(valor: string): string {
  return valor.replace(/\D/g, '').slice(0, LIMITES_VEICULO.digitosAno);
}

/** Quilometragem digitada: só dígitos (sem sinal, então não entra valor negativo). */
export function normalizarKmDigitado(valor: string): string {
  return valor.replace(/\D/g, '');
}

/** Texto livre (marca, modelo, cor): tira o caractere nulo, que o banco recusaria. Os espaços ficam para o validador. */
export function normalizarTextoDigitado(valor: string): string {
  return valor.split(CARACTERE_NULO).join('');
}
