/**
 * Formatação brasileira centralizada. Componentes NUNCA formatam valores por
 * conta própria: sempre chamam uma função daqui.
 *
 * As funções de documento (CPF, placa, telefone, CEP) são progressivas:
 * aceitam o valor parcial enquanto o usuário digita, então servem tanto para
 * exibir quanto como máscara de campo (prop `mascara` do componente Campo).
 */
import {
  MENSAGEM_TEMPO_MENOS_DE_UM_MINUTO,
  MENSAGEM_TEMPO_UMA_HORA,
  MENSAGEM_TEMPO_UM_DIA,
  MENSAGEM_TEMPO_UM_MINUTO,
  mensagemTempoDias,
  mensagemTempoHoras,
  mensagemTempoMinutos,
} from '@/lib/mensagensCadastro';

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

/**
 * Só dígitos, cortados no máximo informado. Serve de máscara para campos de
 * tamanho fixo (ex.: número da CNH com 11 dígitos).
 */
export function limitarDigitos(valor: string, maximo: number): string {
  return somenteDigitos(valor).slice(0, Math.max(0, maximo));
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

/**
 * Máscara de digitação de valor em reais (pt-BR). Só os dígitos digitados contam, e os dois últimos são centavos:
 * "22900" → "229,00"; "10450000" → "104.500,00"; "5" → "0,05"; sem dígito → "" (campo vazio).
 */
export function mascararMoedaDigitada(valor: string): string {
  const digitos = somenteDigitos(valor).replace(/^0+/, '');
  if (digitos.length === 0) return '';
  const comCentavos = digitos.padStart(3, '0');
  const inteiros = comCentavos.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return `${inteiros},${comCentavos.slice(-2)}`;
}

/**
 * Texto de valor em reais (pt-BR) para número: "229,00" → 229; "104.500,00" → 104500; "1234,5" → 1234.5.
 * Aceita só dígitos, ponto de milhar (em grupos de três) e vírgula decimal. Texto vazio ou fora desse formato → nulo.
 */
export function converterMoedaPtBrParaNumero(texto: string): number | null {
  const limpo = texto.trim();
  if (limpo.length === 0) return null;
  if (!/^(\d{1,3}(\.\d{3})+|\d+)(,\d*)?$/.test(limpo)) return null;
  const numero = Number(limpo.replace(/\./g, '').replace(',', '.'));
  return Number.isFinite(numero) ? numero : null;
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

/** Date ou instante em ms → "14:22" (hora local, 24 h). */
export function formatarHora(valor: Date | number | null | undefined): string {
  if (semValor(valor)) return VALOR_AUSENTE;
  const data = valor instanceof Date ? valor : new Date(valor);
  return Number.isNaN(data.getTime()) ? VALOR_AUSENTE : formatadorHora.format(data);
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

/** "11222333000181" → "11.222.333/0001-81" (aceita valor parcial) */
export function formatarCnpj(valor: string | null | undefined): string {
  if (semValor(valor)) return VALOR_AUSENTE;
  const d = somenteDigitos(valor).slice(0, 14);
  if (d.length <= 2) return d;
  if (d.length <= 5) return `${d.slice(0, 2)}.${d.slice(2)}`;
  if (d.length <= 8) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5)}`;
  if (d.length <= 12) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8)}`;
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
}

const BYTES_POR_KB = 1024;
const BYTES_POR_MB = 1024 * 1024;

/** 2516582 → "2,4 MB"; 512000 → "500 KB" (abaixo de 1 MB, em KB inteiros). Decimal com vírgula (pt-BR). */
export function formatarTamanhoArquivo(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) return VALOR_AUSENTE;
  if (bytes < BYTES_POR_MB) return `${Math.max(1, Math.round(bytes / BYTES_POR_KB))} KB`;
  return `${(bytes / BYTES_POR_MB).toFixed(1).replace('.', ',')} MB`;
}

/** Data e hora como o backend manda (dd/MM/yyyy HH:mm, com segundos opcionais), na hora local. */
const REGEX_DATA_HORA_PTBR = /^(\d{2})\/(\d{2})\/(\d{4}) (\d{2}):(\d{2})(?::(\d{2}))?$/;

/**
 * Lê a data e hora que o backend devolve: "19/11/2026 09:58" (formato do projeto, hora de Brasília, sem fuso).
 * Aceita também ISO. Devolve null quando não é uma data real (ex.: 31/02).
 */
export function interpretarDataHoraServidor(valor: string): Date | null {
  const texto = valor.trim();
  const partes = REGEX_DATA_HORA_PTBR.exec(texto);
  if (partes) {
    const [, dia, mes, ano, hora, minuto, segundo = '0'] = partes;
    const data = new Date(Number(ano), Number(mes) - 1, Number(dia), Number(hora), Number(minuto), Number(segundo));
    const conferePeloCalendario =
      data.getFullYear() === Number(ano) && data.getMonth() === Number(mes) - 1 && data.getDate() === Number(dia);
    return conferePeloCalendario && !Number.isNaN(data.getTime()) ? data : null;
  }
  const iso = new Date(texto);
  return Number.isNaN(iso.getTime()) ? null : iso;
}

/**
 * Tempo decorrido entre `data` (hora do servidor) e `agora`, em palavras: "menos de 1 minuto", "2 minutos",
 * "1 hora", "3 dias". Data inválida ou no futuro vira o tempo mínimo.
 */
export function formatarTempoDecorrido(data: string, agora: Date): string {
  const instante = interpretarDataHoraServidor(data);
  const minutos = instante === null ? 0 : Math.floor((agora.getTime() - instante.getTime()) / 60_000);
  if (minutos < 1) return MENSAGEM_TEMPO_MENOS_DE_UM_MINUTO;
  if (minutos < 60) return minutos === 1 ? MENSAGEM_TEMPO_UM_MINUTO : mensagemTempoMinutos(minutos);
  const horas = Math.floor(minutos / 60);
  if (horas < 24) return horas === 1 ? MENSAGEM_TEMPO_UMA_HORA : mensagemTempoHoras(horas);
  const dias = Math.floor(horas / 24);
  return dias === 1 ? MENSAGEM_TEMPO_UM_DIA : mensagemTempoDias(dias);
}
