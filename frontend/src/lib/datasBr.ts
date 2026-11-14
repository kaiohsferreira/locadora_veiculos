/**
 * Datas no formato brasileiro DD/MM/AAAA, para campos digitados. Funções puras, sem React.
 *
 * O backend trabalha com "AAAA-MM-DD" (DateOnly). Estas funções fazem a ponte: mascaram o que a pessoa digita
 * e convertem para o formato da API. Para exibir datas use `formatarData` de `@/lib/formatters`.
 */

const REGEX_DATA_BR = /^(\d{2})\/(\d{2})\/(\d{4})$/;
const REGEX_DATA_ISO = /^(\d{4})-(\d{2})-(\d{2})$/;

/** Dias de cada mês; fevereiro depende do ano bissexto. */
function diasNoMes(ano: number, mes: number): number {
  const bissexto = (ano % 4 === 0 && ano % 100 !== 0) || ano % 400 === 0;
  if (mes === 2) return bissexto ? 29 : 28;
  return [4, 6, 9, 11].includes(mes) ? 30 : 31;
}

/**
 * Máscara de digitação DD/MM/AAAA (aceita valor parcial). Só dígitos, até 8.
 * Ex.: "23022025" → "23/02/2025"; "2302" → "23/02".
 */
export function mascararDataBr(valor: string): string {
  const digitos = valor.replace(/\D/g, '').slice(0, 8);
  if (digitos.length <= 2) return digitos;
  if (digitos.length <= 4) return `${digitos.slice(0, 2)}/${digitos.slice(2)}`;
  return `${digitos.slice(0, 2)}/${digitos.slice(2, 4)}/${digitos.slice(4)}`;
}

/**
 * DD/MM/AAAA completo e existente no calendário → "AAAA-MM-DD". Devolve null quando o texto
 * está incompleto ou não é uma data real (ex.: 31/02/2025).
 */
export function dataBrParaIso(valor: string): string | null {
  const partes = REGEX_DATA_BR.exec(valor);
  if (!partes) return null;
  const dia = Number(partes[1]);
  const mes = Number(partes[2]);
  const ano = Number(partes[3]);
  if (ano < 1 || mes < 1 || mes > 12 || dia < 1 || dia > diasNoMes(ano, mes)) return null;
  return `${String(ano).padStart(4, '0')}-${partes[2]}-${partes[1]}`;
}

/** "AAAA-MM-DD" → "DD/MM/AAAA". Devolve string vazia quando a entrada não é uma data ISO válida. */
export function isoParaDataBr(valor: string): string {
  const partes = REGEX_DATA_ISO.exec(valor);
  if (!partes) return '';
  const ano = Number(partes[1]);
  const mes = Number(partes[2]);
  const dia = Number(partes[3]);
  if (ano < 1 || mes < 1 || mes > 12 || dia < 1 || dia > diasNoMes(ano, mes)) return '';
  return `${partes[3]}/${partes[2]}/${partes[1]}`;
}
