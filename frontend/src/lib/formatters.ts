/**
 * Formatação brasileira centralizada. Componentes NUNCA formatam valores por
 * conta própria: sempre chamam uma função daqui.
 *
 * As funções de documento (CPF, placa, telefone, CEP) são progressivas:
 * aceitam o valor parcial enquanto o usuário digita, então servem tanto para
 * exibir quanto como máscara de campo (prop `mascara` do componente Campo).
 */

/** Exibido quando não há valor (null, undefined ou data inválida). */
export const VALOR_AUSENTE = '—';

const formatadorMoeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const formatadorNumero = new Intl.NumberFormat('pt-BR');
const formatadorData = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
});
const formatadorHora = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' });

/** Data sem hora no formato ISO (ex.: "2026-11-04"), como o backend envia DateOnly. */
const REGEX_DATA_ISO = /^(\d{4})-(\d{2})-(\d{2})$/;

function semValor(valor: unknown): valor is null | undefined {
  return valor === null || valor === undefined;
}

/** Remove tudo que não for dígito. Use antes de enviar CPF, telefone e CEP ao backend. */
export function somenteDigitos(valor: string): string {
  return valor.replace(/\D/g, '');
}

// Números --------------------------------------------------------------------

/** 1234.5 → "R$ 1.234,50" */
export function formatarMoeda(valor: number | null | undefined): string {
  if (semValor(valor) || !Number.isFinite(valor)) return VALOR_AUSENTE;
  return formatadorMoeda.format(valor);
}

/** 1480 → "1.480"; com casas decimais fixas: (63.6, 1) → "63,6" */
export function formatarNumero(valor: number | null | undefined, casasDecimais?: number): string {
  if (semValor(valor) || !Number.isFinite(valor)) return VALOR_AUSENTE;
  if (casasDecimais === undefined) return formatadorNumero.format(valor);
  return new Intl.NumberFormat('pt-BR', {
    minimumFractionDigits: casasDecimais,
    maximumFractionDigits: casasDecimais,
  }).format(valor);
}

/** 24850 → "24.850 km" */
export function formatarQuilometragem(valor: number | null | undefined): string {
  if (semValor(valor) || !Number.isFinite(valor)) return VALOR_AUSENTE;
  return `${formatadorNumero.format(valor)} km`;
}

// Datas ----------------------------------------------------------------------

/**
 * Converte o valor do backend em Date. Datas sem hora ("2026-11-04") são
 * montadas no fuso local, para não "voltar um dia" no horário de Brasília.
 */
function converterParaData(valor: string | Date): Date | null {
  if (valor instanceof Date) return Number.isNaN(valor.getTime()) ? null : valor;

  const apenasData = REGEX_DATA_ISO.exec(valor);
  if (apenasData) {
    const [, ano, mes, dia] = apenasData;
    return new Date(Number(ano), Number(mes) - 1, Number(dia));
  }

  const data = new Date(valor);
  return Number.isNaN(data.getTime()) ? null : data;
}

/** "2026-11-04" ou Date → "04/11/2026" */
export function formatarData(valor: string | Date | null | undefined): string {
  if (semValor(valor) || valor === '') return VALOR_AUSENTE;
  const data = converterParaData(valor);
  return data ? formatadorData.format(data) : VALOR_AUSENTE;
}

/** "2026-11-04T14:30:00" ou Date → "04/11/2026 14:30" */
export function formatarDataHora(valor: string | Date | null | undefined): string {
  if (semValor(valor) || valor === '') return VALOR_AUSENTE;
  const data = converterParaData(valor);
  return data ? `${formatadorData.format(data)} ${formatadorHora.format(data)}` : VALOR_AUSENTE;
}

// Documentos e contato -------------------------------------------------------

/** "12345678909" → "123.456.789-09" (aceita valor parcial) */
export function formatarCpf(valor: string | null | undefined): string {
  if (semValor(valor)) return VALOR_AUSENTE;
  const d = somenteDigitos(valor).slice(0, 11);
  if (d.length <= 3) return d;
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
}

/**
 * Placa em maiúsculas, nos dois padrões (aceita valor parcial):
 * - Mercosul (AAA9A99), sem hífen: "bra2e19" → "BRA2E19";
 * - antigo (AAA9999), com hífen: "abc1234" → "ABC-1234".
 * O 5º caractere decide o padrão: letra = Mercosul, dígito = antigo.
 */
export function formatarPlaca(valor: string | null | undefined): string {
  if (semValor(valor)) return VALOR_AUSENTE;
  const placa = valor.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 7);
  if (placa.length <= 4) return placa;
  const padraoAntigo = /\d/.test(placa.charAt(4));
  return padraoAntigo ? `${placa.slice(0, 3)}-${placa.slice(3)}` : placa;
}

/**
 * Telefone fixo ou celular com DDD (aceita valor parcial):
 * "1134567890" → "(11) 3456-7890"; "11987654321" → "(11) 98765-4321".
 */
export function formatarTelefone(valor: string | null | undefined): string {
  if (semValor(valor)) return VALOR_AUSENTE;
  const d = somenteDigitos(valor).slice(0, 11);
  if (d.length === 0) return '';
  if (d.length <= 2) return `(${d}`;
  if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

/** "01310100" → "01310-100" (aceita valor parcial) */
export function formatarCep(valor: string | null | undefined): string {
  if (semValor(valor)) return VALOR_AUSENTE;
  const d = somenteDigitos(valor).slice(0, 8);
  if (d.length <= 5) return d;
  return `${d.slice(0, 5)}-${d.slice(5)}`;
}
