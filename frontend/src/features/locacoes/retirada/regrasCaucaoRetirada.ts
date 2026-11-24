import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { cargoPermitido } from '@/routes/autenticacao/usePermissoes';
import { ErroApi } from '@/types/api';
import type { CargoFuncionario } from '@/types/enums';
import type { ContextoRetirada, DadosCaucaoAbertura } from '@/types/retirada';

/**
 * Funções puras da caução na tela 16, sem React e testadas direto (locacao.contract.json v4,
 * caucao_na_retirada_v4.tela_16_v4.regras_puras_do_front; caucao-retirada.contract.json, repeticao_e_token).
 */

/** As três saídas do painel de simulação do cartão. Nenhuma vem marcada. */
export type EscolhaSimulacaoCaucao = 'APROVAR' | 'RECUSAR' | 'ANALISE';

export interface OpcaoSimulacaoCaucao { valor: EscolhaSimulacaoCaucao; rotulo: string; }

/** Ordem do painel: aprovar, recusar, deixar em análise. */
export const OPCOES_SIMULACAO_CAUCAO: readonly OpcaoSimulacaoCaucao[] = [
  { valor: 'APROVAR', rotulo: M.simulacaoAprovar },
  { valor: 'RECUSAR', rotulo: M.simulacaoRecusar },
  { valor: 'ANALISE', rotulo: M.simulacaoAnalise },
];

/** Prefixo do token de "Aprovar"; o resto é aleatório a cada tentativa nova. Não é texto visível. */
export const PREFIXO_TOKEN_SIMULACAO = 'FAKE-';
/** Token que o provedor de simulação recusa (ConstantsCaucao.TokenFakeRecusado). Não é texto visível. */
export const TOKEN_SIMULACAO_RECUSADO = 'FAKE-RECUSADO';
/** Token que o provedor de simulação deixa em análise (ConstantsCaucao.TokenFakeAnalise). Não é texto visível. */
export const TOKEN_SIMULACAO_ANALISE = 'FAKE-ANALISE';
/** Bandeira mandada na simulação, válida para o gateway (2 a 30 de [a-z0-9_]). Não é texto visível. */
export const BANDEIRA_SIMULACAO = 'visa';
/** Bytes do aleatório do token: 16 bytes viram 32 hexadecimais (o token fica com 37 caracteres). */
const BYTES_ALEATORIO_TOKEN = 16;
/**
 * Status sem resposta definitiva do servidor (falha de rede ou tempo esgotado = 0, 408, 502, 503 e 504): a pré-autorização
 * pode ter sido feita no provedor, então a tentativa seguinte com a mesma saída repete o MESMO token.
 */
const STATUS_SEM_RESPOSTA_DEFINITIVA: readonly number[] = [0, 408, 502, 503, 504];

/** A tela pode tentar a abertura com caução: exigida, disponível e com o provedor de simulação liberado. */
export function caucaoPodeSerTentada(contexto: ContextoRetirada): boolean {
  return contexto.caucaoExigida && contexto.caucaoDisponivel && contexto.caucaoModoSimulacao;
}

/** Bloco "Garantia / Caução": só com a caução exigida e sem locação já aberta (aberta, a tela não mostra a caução). */
export function mostrarBlocoCaucao(contexto: ContextoRetirada): boolean {
  return contexto.caucaoExigida && contexto.idLocacaoExistente === null;
}

/**
 * Cargos que iniciam a locação com a caução exigida (decisão 4 do Kaio em 24/11/2026; ajuste A1, "Aviso por cargo").
 * Sem caução, qualquer cargo inicia, como antes.
 */
export const CARGOS_INICIAM_COM_CAUCAO: readonly CargoFuncionario[] = ['ATENDENTE', 'ADMINISTRADOR'];

/**
 * Com a caução exigida, só ATENDENTE e ADMINISTRADOR iniciam a locação; sem cargo (cliente, sessão sem cargo ou sem
 * sessão) ou com um cargo fora da lista, nunca. Espelha a regra do servidor (RegrasCaucaoRetirada.CargoPodeAbrirComCaucao)
 * e só afeta a tela: o servidor confere de novo pelo cargo do banco e responde 403.
 */
export function cargoIniciaComCaucao(cargo: CargoFuncionario | null): boolean {
  return cargoPermitido(cargo, CARGOS_INICIAM_COM_CAUCAO);
}

/** O que a caução pesa no botão principal, no "Pronto para despacho de pátio" e no item da pré-autorização. */
export interface EstadoCaucaoNaTela {
  exigida: boolean;
  podeSerTentada: boolean;
  /** O cargo da sessão inicia a locação com caução (ATENDENTE ou ADMINISTRADOR). Sem caução, não pesa. */
  cargoPodeIniciar: boolean;
  escolha: EscolhaSimulacaoCaucao | null;
}

/** Sem caução: o botão e o "Pronto para despacho" ficam como antes, para qualquer cargo. */
export const SEM_CAUCAO_NA_TELA: EstadoCaucaoNaTela = { exigida: false, podeSerTentada: false, cargoPodeIniciar: false, escolha: null };

export function estadoCaucaoNaTela(
  contexto: ContextoRetirada,
  escolha: EscolhaSimulacaoCaucao | null,
  cargo: CargoFuncionario | null,
): EstadoCaucaoNaTela {
  return {
    exigida: contexto.caucaoExigida,
    podeSerTentada: caucaoPodeSerTentada(contexto),
    cargoPodeIniciar: cargoIniciaComCaucao(cargo),
    escolha,
  };
}

/**
 * Sem caução, nunca segura a abertura (o cargo não pesa). Com a caução exigida, libera só se ela pode ser tentada, o cargo
 * da sessão inicia com caução e há uma saída escolhida.
 */
export function caucaoLiberaAbertura(caucao: EstadoCaucaoNaTela): boolean {
  return !caucao.exigida || (caucao.podeSerTentada && caucao.cargoPodeIniciar && caucao.escolha !== null);
}

/** O que o item da pré-autorização mostra no lugar do painel. São valores internos, não texto visível. */
export type SituacaoPreAutorizacao = 'INDISPONIVEL' | 'SEM_PERMISSAO' | 'PAINEL';

/**
 * Nesta ordem: a caução que não pode ser tentada mostra o texto de indisponível inteiro (vence as outras, para qualquer
 * cargo); a que pode ser tentada, com um cargo que não inicia com caução (inclusive sem cargo), mostra o aviso do cargo no
 * lugar do painel; nos outros casos, o painel de simulação.
 */
export function situacaoDaPreAutorizacao(caucao: EstadoCaucaoNaTela): SituacaoPreAutorizacao {
  if (!caucao.podeSerTentada) return 'INDISPONIVEL';
  if (!caucao.cargoPodeIniciar) return 'SEM_PERMISSAO';
  return 'PAINEL';
}

/**
 * Aleatório do token de "Aprovar": 16 bytes de crypto.getRandomValues em hexadecimal minúsculo (32 caracteres). Não usa
 * crypto.randomUUID, que só existe em contexto seguro (https ou localhost) e quebraria a tela aberta por http://IP-da-rede.
 */
export function gerarAleatorioHex(): string {
  const bytes = new Uint8Array(BYTES_ALEATORIO_TOKEN);
  globalThis.crypto.getRandomValues(bytes);
  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
}

/** Verdadeiro só quando a tentativa anterior terminou sem resposta definitiva do servidor. */
export function repeteToken(status: number): boolean {
  return STATUS_SEM_RESPOSTA_DEFINITIVA.includes(status);
}

/** Tentativa já enviada: a saída escolhida, o token mandado e o status do erro (0 quando não houve resposta). */
export interface TentativaCaucao {
  escolha: EscolhaSimulacaoCaucao;
  token: string;
  status: number;
}

/**
 * Token da tentativa nova. Repete o token da anterior só quando ela terminou sem resposta definitiva e a saída escolhida é
 * a mesma. Senão: "Aprovar" gera "FAKE-" + aleatório (nunca um token fixo), "Recusar" manda "FAKE-RECUSADO" e "Deixar em
 * análise" manda "FAKE-ANALISE". O gerador é injetável para teste.
 */
export function tokenDaTentativa(
  escolha: EscolhaSimulacaoCaucao,
  tentativaAnterior: TentativaCaucao | null,
  gerarAleatorio: () => string = gerarAleatorioHex,
): string {
  if (tentativaAnterior !== null && tentativaAnterior.escolha === escolha && repeteToken(tentativaAnterior.status)) {
    return tentativaAnterior.token;
  }
  if (escolha === 'RECUSAR') return TOKEN_SIMULACAO_RECUSADO;
  if (escolha === 'ANALISE') return TOKEN_SIMULACAO_ANALISE;
  return `${PREFIXO_TOKEN_SIMULACAO}${gerarAleatorio()}`;
}

/** Os quatro campos do cartão para o POST: o token da tentativa, a bandeira da simulação e emissor e dispositivo nulos. */
export function dadosDaCaucaoParaAbrir(token: string): DadosCaucaoAbertura {
  return {
    caucaoTokenCartao: token,
    caucaoMetodoPagamentoId: BANDEIRA_SIMULACAO,
    caucaoEmissorId: null,
    caucaoIdentificadorDispositivo: null,
  };
}

/** Status do erro da abertura. Erro que não veio da API (sem resposta) conta como 0. */
export function statusDoErro(erro: unknown): number {
  return erro instanceof ErroApi ? erro.status : 0;
}

/**
 * Memória da tela 16 que precisa sobreviver ao estado de erro da página: a saída escolhida na simulação e a última
 * tentativa com caução que falhou, da reserva da rota. Fica só na memória do React (nada no localStorage nem no
 * sessionStorage).
 */
export interface MemoriaCaucaoRetirada {
  idReserva: number | null;
  escolha: EscolhaSimulacaoCaucao | null;
  tentativaAnterior: TentativaCaucao | null;
}

/** A mesma memória quando ela é da reserva da rota; senão (outra reserva, sem reserva ou sem memória), tudo volta a nulo. */
export function memoriaDaRetirada(memoria: MemoriaCaucaoRetirada | null, idReserva: number | null): MemoriaCaucaoRetirada {
  if (memoria !== null && memoria.idReserva === idReserva) return memoria;
  return { idReserva, escolha: null, tentativaAnterior: null };
}

/** Grava a saída escolhida. Só muda a memória da mesma reserva; a de outra reserva fica como está. */
export function memoriaComEscolha(
  memoria: MemoriaCaucaoRetirada,
  idReserva: number | null,
  escolha: EscolhaSimulacaoCaucao,
): MemoriaCaucaoRetirada {
  return memoria.idReserva === idReserva ? { ...memoria, escolha } : memoria;
}

/**
 * Grava a tentativa que falhou. Só muda a memória da mesma reserva: a resposta que chega depois de a rota trocar de reserva
 * não mexe na memória da reserva nova.
 */
export function memoriaComTentativa(
  memoria: MemoriaCaucaoRetirada,
  idReserva: number | null,
  tentativa: TentativaCaucao,
): MemoriaCaucaoRetirada {
  return memoria.idReserva === idReserva ? { ...memoria, tentativaAnterior: tentativa } : memoria;
}
