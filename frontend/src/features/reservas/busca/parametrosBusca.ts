import { ehSiglaUf } from '@/features/clientes/cadastro/ufs';
import { isoParaDataBr } from '@/lib/datasBr';
import { obterErroDeCampo } from '@/lib/erros';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import type { CidadeLocadora } from '@/types/empresas';
import type { FiltroVeiculosDisponiveis } from '@/types/veiculos';

const TAMANHO_PAGINA = 12;
const INT_MAXIMO = 2_147_483_647;
const PAGINA_MAXIMA = Math.floor(INT_MAXIMO / TAMANHO_PAGINA) + 1;
const MILISSEGUNDOS_DIA = 86_400_000;

export interface RascunhoBusca {
  local: string;
  dataRetirada: string;
  dataDevolucao: string;
  idCategoria: string;
  valorDiariaMin: string;
  valorDiariaMax: string;
}

export type ErrosBusca = Partial<Record<keyof RascunhoBusca, string>>;

const CAMPOS_ERRO_API = {
  local: 'cidade',
  dataRetirada: 'dataRetirada',
  dataDevolucao: 'dataDevolucao',
  idCategoria: 'idCategoria',
  valorDiariaMin: 'valorDiariaMin',
  valorDiariaMax: 'valorDiariaMax',
} as const;

export function errosBuscaDaApi(erro: unknown): ErrosBusca {
  const erros: ErrosBusca = {};
  for (const campo of Object.keys(CAMPOS_ERRO_API) as (keyof RascunhoBusca)[]) {
    const mensagem = obterErroDeCampo(erro, CAMPOS_ERRO_API[campo]);
    if (mensagem) erros[campo] = mensagem;
  }
  const erroUf = obterErroDeCampo(erro, 'uf');
  if (erroUf) erros.local ??= erroUf;
  return erros;
}

export const BUSCA_VAZIA: RascunhoBusca = {
  local: '',
  dataRetirada: '',
  dataDevolucao: '',
  idCategoria: '',
  valorDiariaMin: '',
  valorDiariaMax: '',
};

export function hojeLocal(data = new Date()): string {
  return [
    data.getFullYear(),
    String(data.getMonth() + 1).padStart(2, '0'),
    String(data.getDate()).padStart(2, '0'),
  ].join('-');
}

export function avaliarPeriodo(
  { dataRetirada, dataDevolucao }: Pick<RascunhoBusca, 'dataRetirada' | 'dataDevolucao'>,
  hoje: string,
) {
  const retiradaValida = Boolean(isoParaDataBr(dataRetirada));
  const referencia = retiradaValida && dataRetirada >= hoje ? dataRetirada : hoje;
  const amanha = new Date(Date.parse(referencia) + MILISSEGUNDOS_DIA).toISOString().slice(0, 10);
  const minimoDevolucao = isoParaDataBr(amanha) ? amanha : undefined;
  const erros: Pick<ErrosBusca, 'dataRetirada' | 'dataDevolucao'> = {};

  if (!dataRetirada) erros.dataRetirada = texto.erroRetiradaObrigatoria;
  else if (!retiradaValida) erros.dataRetirada = texto.erroRetiradaInvalida;
  else if (dataRetirada < hoje) erros.dataRetirada = texto.erroRetiradaPassada;

  if (!dataDevolucao) erros.dataDevolucao = texto.erroDevolucaoObrigatoria;
  else if (!isoParaDataBr(dataDevolucao)) erros.dataDevolucao = texto.erroDevolucaoInvalida;
  else if (!minimoDevolucao || dataDevolucao < minimoDevolucao) erros.dataDevolucao = texto.erroDevolucaoPeriodo;

  const diarias = Object.keys(erros).length === 0
    ? (Date.parse(dataDevolucao) - Date.parse(dataRetirada)) / MILISSEGUNDOS_DIA
    : 0;
  return { erros, minimoDevolucao, diarias };
}

export function valorLocal({ cidade, uf }: CidadeLocadora): string {
  return `${uf}:${cidade}`;
}

export function lerLocal(valor: string): CidadeLocadora | undefined {
  const uf = valor.slice(0, 2).toUpperCase();
  const cidade = valor.slice(3).trim();
  if (valor[2] !== ':' || !ehSiglaUf(uf) || cidade.length < 1 || cidade.length > 60) return undefined;
  return { cidade, uf };
}

function lerInteiroPositivo(valor: string, maximo = INT_MAXIMO): number | undefined {
  const numero = Number(valor);
  return /^\d+$/.test(valor) && numero > 0 && numero <= maximo ? numero : undefined;
}

function lerDiaria(valor: string): number | undefined {
  const numero = Number(valor);
  // Usa ponto decimal e limita a magnitude aos valores seguros para o decimal da API.
  return /^\d+(\.\d+)?$/.test(valor) && numero < 1e28 ? numero : undefined;
}

export function validarBusca(rascunho: RascunhoBusca, hoje: string): ErrosBusca {
  const erros: ErrosBusca = avaliarPeriodo(rascunho, hoje).erros;
  if (rascunho.local && !lerLocal(rascunho.local)) erros.local = texto.erroCidade;
  if (rascunho.idCategoria && lerInteiroPositivo(rascunho.idCategoria) === undefined) erros.idCategoria = texto.erroCategoria;
  const minimo = lerDiaria(rascunho.valorDiariaMin);
  const maximo = lerDiaria(rascunho.valorDiariaMax);
  if (rascunho.valorDiariaMin && minimo === undefined) erros.valorDiariaMin = texto.erroDiariaMin;
  if (rascunho.valorDiariaMax && maximo === undefined) erros.valorDiariaMax = texto.erroDiariaMax;
  if (minimo !== undefined && maximo !== undefined && maximo < minimo) erros.valorDiariaMax = texto.erroFaixaDiaria;
  return erros;
}

export function lerBuscaDaUrl(parametros: URLSearchParams) {
  const cidade = parametros.get('cidade')?.trim() ?? '';
  const uf = parametros.get('uf')?.trim() ?? '';
  const local = lerLocal(`${uf}:${cidade}`);
  const categoria = lerInteiroPositivo(parametros.get('idCategoria') ?? '');
  const minimo = lerDiaria(parametros.get('valorDiariaMin') ?? '');
  const maximo = lerDiaria(parametros.get('valorDiariaMax') ?? '');
  const intervaloInvertido = minimo !== undefined && maximo !== undefined && minimo > maximo;
  const retirada = parametros.get('dataRetirada') ?? '';
  const devolucao = parametros.get('dataDevolucao') ?? '';
  const rascunho: RascunhoBusca = {
    local: local ? valorLocal(local) : '',
    dataRetirada: isoParaDataBr(retirada) ? retirada : '',
    dataDevolucao: isoParaDataBr(devolucao) ? devolucao : '',
    idCategoria: categoria?.toString() ?? '',
    valorDiariaMin: intervaloInvertido ? '' : minimo === undefined ? '' : (parametros.get('valorDiariaMin') ?? ''),
    valorDiariaMax: intervaloInvertido ? '' : maximo === undefined ? '' : (parametros.get('valorDiariaMax') ?? ''),
  };
  return { rascunho, pagina: lerInteiroPositivo(parametros.get('pagina') ?? '', PAGINA_MAXIMA) ?? 1 };
}

export function montarFiltroBusca(rascunho: RascunhoBusca, pagina = 1): FiltroVeiculosDisponiveis {
  return {
    dataRetirada: rascunho.dataRetirada,
    dataDevolucao: rascunho.dataDevolucao,
    ...lerLocal(rascunho.local),
    idCategoria: lerInteiroPositivo(rascunho.idCategoria),
    valorDiariaMin: lerDiaria(rascunho.valorDiariaMin),
    valorDiariaMax: lerDiaria(rascunho.valorDiariaMax),
    pagina,
    tamanhoPagina: TAMANHO_PAGINA,
  };
}

export function montarParametrosBusca(rascunho: RascunhoBusca, pagina = 1): URLSearchParams {
  const parametros = new URLSearchParams();
  const local = lerLocal(rascunho.local);
  if (local) {
    parametros.set('cidade', local.cidade);
    parametros.set('uf', local.uf);
  }
  if (rascunho.dataRetirada) parametros.set('dataRetirada', rascunho.dataRetirada);
  if (rascunho.dataDevolucao) parametros.set('dataDevolucao', rascunho.dataDevolucao);
  if (rascunho.idCategoria) parametros.set('idCategoria', rascunho.idCategoria);
  if (rascunho.valorDiariaMin) parametros.set('valorDiariaMin', rascunho.valorDiariaMin);
  if (rascunho.valorDiariaMax) parametros.set('valorDiariaMax', rascunho.valorDiariaMax);
  if (pagina > 1) parametros.set('pagina', String(pagina));
  return parametros;
}
