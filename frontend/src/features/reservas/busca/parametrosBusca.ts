import { ehSiglaUf } from '@/features/clientes/cadastro/ufs';
import { formatarNumero } from '@/lib/formatters';
import { isoParaDataBr } from '@/lib/datasBr';
import { obterErroDeCampo } from '@/lib/erros';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import type { CidadeLocadora } from '@/types/empresas';
import type { FiltroVeiculosDisponiveis } from '@/types/veiculos';

const TAMANHO_PAGINA = 12;
const INT_MAXIMO = 2_147_483_647;
const PAGINA_MAXIMA = Math.floor(INT_MAXIMO / TAMANHO_PAGINA) + 1;
const MILISSEGUNDOS_DIA = 86_400_000;
/** Limite inicial do controle deslizante da diária (R$). Cresce quando o mínimo ou o máximo passam dele. */
const LIMITE_INICIAL_CONTROLE = 500;

export interface RascunhoBusca {
  local: string;
  dataRetirada: string;
  dataDevolucao: string;
  idCategoria: string;
  valorDiariaMin: string;
  valorDiariaMax: string;
}

export type ErrosBusca = Partial<Record<keyof RascunhoBusca, string>>;

/** Campos da faixa de diária (os dois controles deslizantes e os campos de número). */
export type CampoDiaria = 'valorDiariaMin' | 'valorDiariaMax';

/** Filtros da lateral, aplicados na hora (categoria e faixa). */
export type FiltrosLateral = Pick<RascunhoBusca, 'idCategoria' | 'valorDiariaMin' | 'valorDiariaMax'>;

/**
 * Resultado de aplicar os filtros da lateral: erro no campo, nada a fazer ou nova consulta. A nova consulta troca a URL
 * sem criar passo no histórico (`substituirHistorico`); o Buscar e a paginação continuam criando passo.
 */
export type DecisaoFiltros =
  | { tipo: 'erro'; erros: ErrosBusca }
  | { tipo: 'igual' }
  | { tipo: 'aplicar'; parametros: URLSearchParams; substituirHistorico: true };

/** Quando um campo muda, estes outros erros também somem: o erro de um depende do valor do outro. */
const ERROS_QUE_DEPENDEM: Partial<Record<keyof RascunhoBusca, (keyof RascunhoBusca)[]>> = {
  dataRetirada: ['dataRetirada', 'dataDevolucao'],
  valorDiariaMin: ['valorDiariaMin', 'valorDiariaMax'],
};

/** Tira do quadro de erros da lateral só os erros que dependem do campo editado; os demais ficam. */
export function limparErrosAoEditar(erros: ErrosBusca, campo: keyof RascunhoBusca): ErrosBusca {
  const restantes: ErrosBusca = { ...erros };
  for (const afetado of ERROS_QUE_DEPENDEM[campo] ?? [campo]) delete restantes[afetado];
  return restantes;
}

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

/**
 * Erros de data da busca já aplicada (vinda da URL), mostrados na abertura da página. Só vale para o campo com data
 * preenchida e que ainda não foi editado: datas vazias ficam para o clique em Buscar, como antes.
 */
export function errosDoPeriodoDaUrl(aplicados: RascunhoBusca, rascunho: RascunhoBusca, hoje: string): ErrosBusca {
  const { erros } = avaliarPeriodo(aplicados, hoje);
  const resultado: ErrosBusca = {};
  if (aplicados.dataRetirada && rascunho.dataRetirada === aplicados.dataRetirada && erros.dataRetirada) {
    resultado.dataRetirada = erros.dataRetirada;
  }
  if (aplicados.dataDevolucao && rascunho.dataDevolucao === aplicados.dataDevolucao && erros.dataDevolucao) {
    resultado.dataDevolucao = erros.dataDevolucao;
  }
  return resultado;
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

/** Erros só da faixa de diária: valor inválido, negativo ou máximo menor que o mínimo. */
export function validarFaixaDiaria(rascunho: Pick<RascunhoBusca, CampoDiaria>): ErrosBusca {
  const erros: ErrosBusca = {};
  const minimo = lerDiaria(rascunho.valorDiariaMin);
  const maximo = lerDiaria(rascunho.valorDiariaMax);
  if (rascunho.valorDiariaMin && minimo === undefined) erros.valorDiariaMin = texto.erroDiariaMin;
  if (rascunho.valorDiariaMax && maximo === undefined) erros.valorDiariaMax = texto.erroDiariaMax;
  if (minimo !== undefined && maximo !== undefined && maximo < minimo) erros.valorDiariaMax = texto.erroFaixaDiaria;
  return erros;
}

export function validarBusca(rascunho: RascunhoBusca, hoje: string): ErrosBusca {
  const erros: ErrosBusca = avaliarPeriodo(rascunho, hoje).erros;
  if (rascunho.local && !lerLocal(rascunho.local)) erros.local = texto.erroCidade;
  if (rascunho.idCategoria && lerInteiroPositivo(rascunho.idCategoria) === undefined) erros.idCategoria = texto.erroCategoria;
  return { ...erros, ...validarFaixaDiaria(rascunho) };
}

/**
 * Limite do controle deslizante da diária: R$ 500 por padrão e mais alto quando o mínimo ou o máximo digitado
 * passam dele (o trilho sempre alcança o valor escolhido).
 */
export function calcularLimiteControle(valorDiariaMin: string, valorDiariaMax: string): number {
  const minimo = lerDiaria(valorDiariaMin);
  const maximo = lerDiaria(valorDiariaMax);
  return Math.max(
    LIMITE_INICIAL_CONTROLE,
    minimo === undefined ? 0 : Math.ceil(minimo),
    maximo === undefined ? 0 : Math.ceil(maximo),
  );
}

/** Posição do controle deslizante para o valor do campo. Sem valor: o mínimo começa em 0 e o máximo no fim do trilho. */
export function posicaoDoControle(campo: CampoDiaria, valor: string, limite: number): number {
  const numero = lerDiaria(valor);
  if (numero === undefined) return campo === 'valorDiariaMax' ? limite : 0;
  return Math.min(limite, Math.round(numero));
}

/**
 * Valor do campo para a posição do controle. No fim do trilho, o máximo fica vazio (sem limite), e não o valor do limite.
 */
export function valorDoControle(campo: CampoDiaria, posicao: number, limite: number): string {
  if (campo === 'valorDiariaMax' && posicao >= limite) return '';
  return String(posicao);
}

function valorEmReais(valor: number): string {
  return texto.valorReais(formatarNumero(valor, Number.isInteger(valor) ? 0 : 2));
}

/** Leitura do intervalo ao lado do título "Faixa de diária" (ex.: "R$ 100 – R$ 500+"). Mínimo vazio = R$ 0. */
export function montarLeituraFaixaDiaria(rascunho: Pick<RascunhoBusca, CampoDiaria>): string {
  const minimo = lerDiaria(rascunho.valorDiariaMin) ?? 0;
  const maximo = lerDiaria(rascunho.valorDiariaMax);
  const textoMaximo = maximo === undefined
    ? texto.maximoAberto(valorEmReais(calcularLimiteControle(rascunho.valorDiariaMin, rascunho.valorDiariaMax)))
    : valorEmReais(maximo);
  return texto.leituraFaixaDiaria(valorEmReais(minimo), textoMaximo);
}

/**
 * Erros que impedem aplicar um filtro da lateral. Data vazia não bloqueia: ela fica para o clique em Buscar, como na
 * abertura da página. Data e local digitados são conferidos como no Buscar; a categoria e a faixa também.
 */
export function validarAplicacaoFiltros(proposta: RascunhoBusca, hoje: string): ErrosBusca {
  const erros = validarBusca(proposta, hoje);
  if (!proposta.dataRetirada) delete erros.dataRetirada;
  if (!proposta.dataDevolucao) delete erros.dataDevolucao;
  return erros;
}

/**
 * Decide o que fazer quando um filtro da lateral é aplicado. A proposta é o que está na tela (local, datas e filtros
 * digitados): se algo digitado estiver inválido, devolve os erros e não aplica nada. Se estiver válida e for diferente
 * da busca aplicada, aplica tudo junto e volta para a página 1. Sem mudança, nada acontece em qualquer página.
 */
export function decidirAplicacaoFiltros(aplicados: RascunhoBusca, proposta: RascunhoBusca, hoje: string): DecisaoFiltros {
  const erros = validarAplicacaoFiltros(proposta, hoje);
  if (Object.keys(erros).length > 0) return { tipo: 'erro', erros };
  if (montarParametrosBusca(proposta).toString() === montarParametrosBusca(aplicados).toString()) {
    return { tipo: 'igual' };
  }
  return { tipo: 'aplicar', parametros: montarParametrosBusca(proposta), substituirHistorico: true };
}

/** Campos da busca que vêm da URL (a página fica de fora: ela não entra no rascunho). */
export const CAMPOS_BUSCA: readonly (keyof RascunhoBusca)[] = [
  'local', 'dataRetirada', 'dataDevolucao', 'idCategoria', 'valorDiariaMin', 'valorDiariaMax',
];

/**
 * Campos cujo valor aplicado mudou de uma busca para a outra (ex.: voltar no histórico). Só esses voltam para o
 * rascunho, para que o que foi digitado e não aplicado nos outros campos continue na tela.
 */
export function camposMudadosNaBusca(anterior: RascunhoBusca, atual: RascunhoBusca): Partial<RascunhoBusca> {
  const mudados: Partial<RascunhoBusca> = {};
  for (const campo of CAMPOS_BUSCA) {
    if (anterior[campo] !== atual[campo]) mudados[campo] = atual[campo];
  }
  return mudados;
}

/** Tamanho de uma data ISO (aaaa-mm-dd). Data maior que isso não é guardada. */
const TAMANHO_DATA_URL = 10;

/**
 * Data da URL como veio, quando é válida ou quando é inválida (aí o campo mostra o erro na abertura da página).
 * Texto longo demais é descartado.
 */
function lerDataDaUrl(valor: string): string {
  return valor.length <= TAMANHO_DATA_URL ? valor : '';
}

export function lerBuscaDaUrl(parametros: URLSearchParams) {
  const cidade = parametros.get('cidade')?.trim() ?? '';
  const uf = parametros.get('uf')?.trim() ?? '';
  const local = lerLocal(`${uf}:${cidade}`);
  const categoria = lerInteiroPositivo(parametros.get('idCategoria') ?? '');
  const minimo = lerDiaria(parametros.get('valorDiariaMin') ?? '');
  const maximo = lerDiaria(parametros.get('valorDiariaMax') ?? '');
  const intervaloInvertido = minimo !== undefined && maximo !== undefined && minimo > maximo;
  const rascunho: RascunhoBusca = {
    local: local ? valorLocal(local) : '',
    dataRetirada: lerDataDaUrl(parametros.get('dataRetirada') ?? ''),
    dataDevolucao: lerDataDaUrl(parametros.get('dataDevolucao') ?? ''),
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
