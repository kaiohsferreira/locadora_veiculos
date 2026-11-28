/**
 * Regras da verificação de cadastros (tela 18), sem React e sem API: como o estado da tela vira o filtro da fila, quando
 * a página volta para a primeira, quais botões aparecem, quando Recusar habilita e qual aviso de erro mostrar.
 * A chamada HTTP fica em `@/api/verificacaoCadastros`.
 */
import { formatarCep, formatarData, formatarDataHora, VALOR_AUSENTE } from '@/lib/formatters';
import { ehImagemDocumento } from '@/lib/documentosCliente';
import {
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_VERIFICACAO_DOC_COMPROVANTE,
  MENSAGEM_VERIFICACAO_DOC_CNH,
  MENSAGEM_VERIFICACAO_SUCESSO_APROVADO,
  MENSAGEM_VERIFICACAO_SUCESSO_RECUSADO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import { ROTULO_STATUS_CADASTRO_CLIENTE, STATUS_CADASTRO_CLIENTE, type StatusCadastroCliente } from '@/types/enums';
import { TIPOS_DOCUMENTO_CLIENTE, type TipoDocumentoCliente } from '@/types/documentosCliente';
import type {
  ClienteVerificacao,
  DecisaoVerificacaoCliente,
  DocumentoVerificacao,
  FiltroFilaVerificacao,
  RequisicaoDecisaoVerificacao,
} from '@/types/verificacaoCadastros';

/** Linhas por página da fila. */
export const TAMANHO_PAGINA_FILA = 10;

/** Espera depois da última tecla antes de consultar a busca (uma consulta por pausa, não por tecla). */
export const ATRASO_BUSCA_FILA_MS = 400;

/** Teto da busca no contrato (60 caracteres depois de Trim). */
export const TAMANHO_MAXIMO_BUSCA_FILA = 60;

/** Teto da justificativa no contrato (VerificacaoCadastroVO: máximo 500 caracteres). */
export const TAMANHO_MAXIMO_JUSTIFICATIVA = 500;

/** Filtro de status escolhido: vazio = todos. */
export type SituacaoFiltroFila = StatusCadastroCliente | '';

export interface OpcaoFiltroFila {
  valor: StatusCadastroCliente;
  rotulo: string;
}

/** Status que o seletor oferece (o "Todos" é a opção vazia do próprio seletor). */
export const OPCOES_STATUS_FILA: readonly OpcaoFiltroFila[] = STATUS_CADASTRO_CLIENTE.map((valor) => ({
  valor,
  rotulo: ROTULO_STATUS_CADASTRO_CLIENTE[valor],
}));

/** Página e filtros da fila, como a tela guarda. `busca` é o termo que já foi aplicado (aparado). */
export interface EstadoListagemFila {
  pagina: number;
  status: SituacaoFiltroFila;
  busca: string;
}

export const ESTADO_LISTAGEM_FILA_INICIAL: EstadoListagemFila = {
  pagina: 1,
  status: '',
  busca: '',
};

/** O que mudou na tela: o status, o termo de busca já aplicado ou a página. */
export type MudancaListagemFila =
  | { tipo: 'status'; status: SituacaoFiltroFila }
  | { tipo: 'busca'; busca: string }
  | { tipo: 'pagina'; pagina: number };

/** Valor do seletor de status como filtro: só os status conhecidos passam; qualquer outro texto vira "todos". */
export function situacaoDoSeletor(valor: string): SituacaoFiltroFila {
  const encontrado = STATUS_CADASTRO_CLIENTE.find((status) => status === valor);
  return encontrado ?? '';
}

/** Termo da busca como a API recebe: aparado e limitado ao teto. Vazio = sem busca. */
export function termoBuscaFila(busca: string): string {
  return busca.trim().slice(0, TAMANHO_MAXIMO_BUSCA_FILA).trim();
}

/**
 * Aplica uma mudança. Trocar o status volta para a página 1 (se mudou). Aplicar uma busca diferente volta para a
 * página 1; espaços a mais não contam como mudança. Trocar de página só muda a página. Não altera o estado recebido.
 */
export function alterarListagemFila(estado: EstadoListagemFila, mudanca: MudancaListagemFila): EstadoListagemFila {
  if (mudanca.tipo === 'pagina') return { ...estado, pagina: mudanca.pagina };
  if (mudanca.tipo === 'status') {
    if (mudanca.status === estado.status) return estado;
    return { ...estado, status: mudanca.status, pagina: 1 };
  }
  const novaBusca = termoBuscaFila(mudanca.busca);
  if (novaBusca === estado.busca) return estado;
  return { ...estado, busca: novaBusca, pagina: 1 };
}

/** Filtro pronto para a API: status e busca só entram quando escolhidos. */
export function montarFiltroFila(estado: EstadoListagemFila): FiltroFilaVerificacao {
  const filtro: FiltroFilaVerificacao = { pagina: estado.pagina, tamanhoPagina: TAMANHO_PAGINA_FILA };
  if (estado.status !== '') filtro.status = estado.status;
  if (estado.busca !== '') filtro.busca = estado.busca;
  return filtro;
}

/**
 * Página que a tela deve mostrar depois da resposta da fila: a pedida, ou a última existente quando a pedida passa do total
 * (ex.: a última linha saiu do filtro). Só vale com a resposta da consulta atual (`respostaAtual`): a anterior, que o
 * TanStack Query mantém enquanto a nova carrega, não corrige nada. Sem resposta (total indefinido), devolve a pedida.
 */
export function paginaAposRespostaFila(pagina: number, total: number | undefined, respostaAtual: boolean): number {
  if (total === undefined || !respostaAtual) return pagina;
  const ultimaPagina = Math.max(1, Math.ceil(total / TAMANHO_PAGINA_FILA));
  return pagina > ultimaPagina ? ultimaPagina : pagina;
}

/** Há busca ou status aplicado (escolhe a mensagem de lista vazia e o botão de limpar). */
export function temFiltroFila(estado: EstadoListagemFila): boolean {
  return estado.busca !== '' || estado.status !== '';
}

/** Filtro da contagem de pendentes do cartão: só o status, sem busca e na primeira página. */
export function filtroPendentes(): FiltroFilaVerificacao {
  return { pagina: 1, tamanhoPagina: 1, status: 'PENDENTE' };
}

/** Os botões de decisão aparecem para pendente e recusado. Aprovado não recebe decisão (o servidor responderia 409). */
export function mostrarDecisao(status: StatusCadastroCliente): boolean {
  return status !== 'APROVADO';
}

/** Recusar só envia com justificativa (depois de aparar) e fora do envio. */
export function recusaHabilitada(justificativa: string, enviando: boolean): boolean {
  return !enviando && justificativa.trim() !== '';
}

/** Corpo do PATCH: aprovação sem justificativa; recusa com a justificativa aparada. */
export function requisicaoDecisao(
  decisao: DecisaoVerificacaoCliente,
  justificativa: string,
): RequisicaoDecisaoVerificacao {
  if (decisao === 'APROVADO') return { decisao };
  return { decisao, justificativa: justificativa.trim() };
}

/** Frase do aviso de sucesso da decisão. */
export function mensagemSucessoDecisao(decisao: DecisaoVerificacaoCliente): string {
  return decisao === 'APROVADO' ? MENSAGEM_VERIFICACAO_SUCESSO_APROVADO : MENSAGEM_VERIFICACAO_SUCESSO_RECUSADO;
}

/**
 * Mensagem de erro da tela (aviso da decisão e erro da fila ou do painel). 409 e 404 mostram a frase que o servidor mandou;
 * os demais erros, inclusive 500, usam o texto padrão do projeto.
 */
export function mensagemErroVerificacao(erro: unknown): string {
  if (erro instanceof ErroApi && (erro.status === 409 || erro.status === 404)) return erro.mensagem;
  return MENSAGEM_ERRO_API_PADRAO;
}

/** Iniciais do avatar: as duas primeiras palavras do nome ("Mariana Ribeiro de Souza" → "MR"). */
export function iniciaisCliente(nome: string): string {
  const palavras = nome.trim().split(/\s+/).filter((palavra) => palavra !== '');
  return palavras
    .slice(0, 2)
    .map((palavra) => palavra.charAt(0))
    .join('')
    .toUpperCase();
}

/**
 * Texto no formato que o servidor manda ("dd/MM/yyyy" ou "dd/MM/yyyy HH:mm"). Esse texto já vem pronto: a tela mostra como
 * veio, sem passar pelo `new Date`, que leria "05/11/2026" como 11 de maio e não leria "14/08/1992".
 */
const REGEX_DATA_PTBR_SERVIDOR = /^\d{2}\/\d{2}\/\d{4}( \d{2}:\d{2}(:\d{2})?)?$/;

/** Data e hora como a tela mostra. Aceita o formato do servidor (dd/MM/yyyy HH:mm) e ISO; outro texto volta como veio. */
export function dataHoraExibicao(valor: string | null): string {
  if (valor === null || valor === '') return VALOR_AUSENTE;
  if (REGEX_DATA_PTBR_SERVIDOR.test(valor)) return valor;
  const formatada = formatarDataHora(valor);
  return formatada === VALOR_AUSENTE ? valor : formatada;
}

/** Data como a tela mostra. Aceita o formato do servidor (dd/MM/yyyy) e ISO (AAAA-MM-DD). */
export function dataExibicao(valor: string | null): string {
  if (valor !== null && REGEX_DATA_PTBR_SERVIDOR.test(valor)) return valor;
  return formatarData(valor);
}

/**
 * Linhas do endereço do painel: rua e número (com complemento e bairro) numa linha, cidade/UF e CEP na outra. Campos
 * vazios saem. Sem nenhum campo, a lista vem vazia (o painel mostra "Endereço não informado.").
 */
export function linhasEndereco(cliente: ClienteVerificacao): string[] {
  const ruaENumero = [cliente.logradouro, cliente.numero, cliente.complemento, cliente.bairro]
    .map((parte) => parte?.trim() ?? '')
    .filter((parte) => parte !== '')
    .join(', ');
  const cidadeUf = [cliente.cidade, cliente.uf]
    .map((parte) => parte?.trim() ?? '')
    .filter((parte) => parte !== '')
    .join(' / ');
  const cep = cliente.cep ? formatarCep(cliente.cep) : '';
  const linhaCep = [cidadeUf, cep !== VALOR_AUSENTE ? cep : ''].filter((parte) => parte !== '').join(' · ');
  return [ruaENumero, linhaCep].filter((linha) => linha !== '');
}

/** Tipos de documento na ordem da tela. */
export const ORDEM_DOCUMENTOS_VERIFICACAO: readonly TipoDocumentoCliente[] = TIPOS_DOCUMENTO_CLIENTE;

/** Nome do tipo na tela. */
export function rotuloTipoDocumentoVerificacao(tipo: TipoDocumentoCliente): string {
  return tipo === 'CNH' ? MENSAGEM_VERIFICACAO_DOC_CNH : MENSAGEM_VERIFICACAO_DOC_COMPROVANTE;
}

/** Documento do tipo (no máximo um por tipo), ou undefined quando não foi enviado. */
export function documentoDoTipo(
  documentos: readonly DocumentoVerificacao[],
  tipo: TipoDocumentoCliente,
): DocumentoVerificacao | undefined {
  return documentos.find((documento) => documento.tipo === tipo);
}

/**
 * Como o documento aparece: 'ausente' (não enviado), 'sem-link' (item sem link temporário: sem imagem e sem link),
 * 'imagem' (JPG ou PNG, com o link) ou 'pdf' (link para baixar).
 */
export type SituacaoVisualDocumento = 'ausente' | 'sem-link' | 'imagem' | 'pdf';

/** Só link http ou https vai para a tela; javascript:, data: e texto que não é URL contam como sem link. */
export function ehLinkDocumentoSeguro(url: string): boolean {
  try {
    const protocolo = new URL(url).protocol;
    return protocolo === 'https:' || protocolo === 'http:';
  } catch {
    return false;
  }
}

export function situacaoVisualDocumento(documento: DocumentoVerificacao | undefined): SituacaoVisualDocumento {
  if (documento === undefined) return 'ausente';
  if (documento.url === null || !ehLinkDocumentoSeguro(documento.url)) return 'sem-link';
  return ehImagemDocumento(documento.tipoConteudo) ? 'imagem' : 'pdf';
}
