/**
 * Chamadas das cobranças do cliente (contrato pagamento: GET /api/pagamentos/meus e GET /api/pagamentos/meus/resumo).
 * As telas acessam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import type {
  CobrancaPix,
  FiltroMeusPagamentos,
  MeuPagamento,
  PeriodoPagamentosCliente,
  RespostaMeusPagamentos,
  ResumoMeusPagamentos,
} from '@/types/pagamentos';

const CAMINHO_MEUS_PAGAMENTOS = '/api/pagamentos/meus';
const CAMINHO_RESUMO_MEUS_PAGAMENTOS = '/api/pagamentos/meus/resumo';

/** Caminho de POST /api/pagamentos/meus/{id}/pix (gerar ou reabrir o PIX de uma cobrança do cliente). */
export function caminhoGerarPixMeuPagamento(idPagamento: number): string {
  return `${CAMINHO_MEUS_PAGAMENTOS}/${idPagamento}/pix`;
}

/**
 * Gera ou reabre o PIX de uma cobrança do cliente (200). A chamada não tem corpo: valor, pagador e locadora vêm do
 * servidor. Fechar a gaveta não cancela nada; reabrir chama a mesma rota. Erros chegam como ErroApi e propagam.
 */
export async function gerarPixMeuPagamento(idPagamento: number): Promise<CobrancaPix> {
  const resposta = await api.post<CobrancaPix>(caminhoGerarPixMeuPagamento(idPagamento));
  return resposta.data;
}

/** Parâmetros da query de GET /api/pagamentos/meus. */
export type ParametrosMeusPagamentos = Record<string, string | number>;

/** Monta a query da lista: aba, periodo, pagina e tamanhoPagina (sempre enviados). */
export function montarParametrosMeusPagamentos(filtro: FiltroMeusPagamentos): ParametrosMeusPagamentos {
  return {
    aba: filtro.aba,
    periodo: filtro.periodo,
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
  };
}

/** Monta a query do resumo: só o período (ele afeta as contagens das abas). */
export function montarParametrosResumoMeusPagamentos(periodo: PeriodoPagamentosCliente): ParametrosMeusPagamentos {
  return { periodo };
}

/** Resposta vazia ou sem lista vira página vazia, para a tela não quebrar. */
function normalizarListagem(corpo: unknown, filtro: FiltroMeusPagamentos): RespostaMeusPagamentos {
  const objeto = corpo as Partial<RespostaMeusPagamentos> | null | undefined;
  if (objeto === null || typeof objeto !== 'object' || !Array.isArray(objeto.itens)) {
    return { itens: [], total: 0, pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina };
  }
  const itens: MeuPagamento[] = objeto.itens;
  return {
    itens,
    total: typeof objeto.total === 'number' ? objeto.total : itens.length,
    pagina: typeof objeto.pagina === 'number' ? objeto.pagina : filtro.pagina,
    tamanhoPagina: typeof objeto.tamanhoPagina === 'number' ? objeto.tamanhoPagina : filtro.tamanhoPagina,
  };
}

/** Lista paginada das cobranças do próprio cliente (200). Erros chegam como ErroApi e propagam. */
export async function listarMeusPagamentos(filtro: FiltroMeusPagamentos): Promise<RespostaMeusPagamentos> {
  const resposta = await api.get<RespostaMeusPagamentos>(CAMINHO_MEUS_PAGAMENTOS, {
    params: montarParametrosMeusPagamentos(filtro),
  });
  return normalizarListagem(resposta.data, filtro);
}

/** Totais e contagens das abas do cliente (200). */
export async function obterResumoMeusPagamentos(periodo: PeriodoPagamentosCliente): Promise<ResumoMeusPagamentos> {
  const resposta = await api.get<ResumoMeusPagamentos>(CAMINHO_RESUMO_MEUS_PAGAMENTOS, {
    params: montarParametrosResumoMeusPagamentos(periodo),
  });
  return resposta.data;
}
