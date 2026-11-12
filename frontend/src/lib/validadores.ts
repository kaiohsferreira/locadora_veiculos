/**
 * Validações puras de campos de formulário. Servem para validar antes de chamar a API
 * (o backend valida de novo e é a palavra final). Datas são "AAAA-MM-DD" (DateOnly).
 */
import { somenteDigitos } from '@/lib/formatters';

const REGEX_DATA_ISO = /^(\d{4})-(\d{2})-(\d{2})$/;
const REGEX_CPF_REPETIDO = /^(\d)\1{10}$/;
const TAMANHO_CPF = 11;

/**
 * Data local à meia-noite. `setFullYear` evita o mapeamento do Date para anos 0 a 99
 * (o construtor `new Date(ano, ...)` trataria "0050" como 1950).
 */
function criarDataLocal(ano: number, mes: number, dia: number): Date {
  const data = new Date(2000, 0, 1);
  data.setFullYear(ano, mes - 1, dia);
  data.setHours(0, 0, 0, 0);
  return data;
}

/** Data ISO convertida para meia-noite local, ou null quando não existe no calendário. */
function lerDataIso(valor: string): Date | null {
  const partes = REGEX_DATA_ISO.exec(valor);
  if (!partes) return null;
  const ano = Number(partes[1]);
  const mes = Number(partes[2]);
  const dia = Number(partes[3]);
  const data = criarDataLocal(ano, mes, dia);
  const existe =
    data.getFullYear() === ano && data.getMonth() === mes - 1 && data.getDate() === dia;
  return existe ? data : null;
}

/** Meia-noite local do dia de referência (só a data importa nas comparações). */
function inicioDoDia(hoje: Date): Date {
  return criarDataLocal(hoje.getFullYear(), hoje.getMonth() + 1, hoje.getDate());
}

/** Dígito verificador do CPF com `tamanho` dígitos base (9 para o primeiro, 10 para o segundo). */
function calcularDigitoCpf(digitos: string, tamanho: number): number {
  let soma = 0;
  for (let indice = 0; indice < tamanho; indice++) {
    soma += Number(digitos[indice]) * (tamanho + 1 - indice);
  }
  const resto = soma % 11;
  return resto < 2 ? 0 : 11 - resto;
}

/**
 * CPF com ou sem máscara. Exige 11 dígitos, rejeita sequências repetidas
 * (111.111.111-11) e confere os dois dígitos verificadores, como o backend (StaticMethods.IsCpfValido).
 */
export function ehCpfValido(valor: string): boolean {
  const digitos = somenteDigitos(valor);
  if (digitos.length !== TAMANHO_CPF) return false;
  if (REGEX_CPF_REPETIDO.test(digitos)) return false;

  const primeiroVerificador = calcularDigitoCpf(digitos, 9);
  const segundoVerificador = calcularDigitoCpf(digitos, 10);
  return (
    primeiroVerificador === Number(digitos[9]) && segundoVerificador === Number(digitos[10])
  );
}

/** "AAAA-MM-DD" que existe no calendário (ex.: 2024-02-29 vale; 2025-02-29 não). */
export function ehDataIsoValida(valor: string): boolean {
  return lerDataIso(valor) !== null;
}

/**
 * Anos completos na data de referência (padrão: agora, fuso local).
 * Devolve null quando a data é inválida ou está no futuro.
 */
export function calcularIdade(dataNascimentoIso: string, hoje: Date = new Date()): number | null {
  const nascimento = lerDataIso(dataNascimentoIso);
  if (nascimento === null) return null;

  const referencia = inicioDoDia(hoje);
  if (nascimento.getTime() > referencia.getTime()) return null;

  let idade = referencia.getFullYear() - nascimento.getFullYear();
  const aniversarioAindaNaoChegou =
    referencia.getMonth() < nascimento.getMonth() ||
    (referencia.getMonth() === nascimento.getMonth() &&
      referencia.getDate() < nascimento.getDate());
  if (aniversarioAindaNaoChegou) idade -= 1;
  return idade;
}

/** Data ISO válida posterior a hoje. Hoje não é futura. */
export function ehDataIsoFutura(valor: string, hoje: Date = new Date()): boolean {
  const data = lerDataIso(valor);
  return data !== null && data.getTime() > inicioDoDia(hoje).getTime();
}

/** Data ISO válida anterior a hoje. Hoje não é passada. */
export function ehDataIsoPassada(valor: string, hoje: Date = new Date()): boolean {
  const data = lerDataIso(valor);
  return data !== null && data.getTime() < inicioDoDia(hoje).getTime();
}
