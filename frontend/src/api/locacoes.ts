import { api } from './client';
import type { Pagina } from '@/types/api';
import type { DevolucaoPendente, FiltroDevolucoesPendentes } from '@/types/devolucao';
import type { AbrirLocacao, ContextoRetirada, DadosCaucaoAbertura, LocacaoAberta } from '@/types/retirada';

/**
 * Tempo limite próprio da abertura COM caução (locacao.contract.json v4, caucao_na_retirada_v4.tela_16_v4.chamada): o
 * servidor pode levar cerca de 90 s na pré-autorização (3 tentativas de 30 s), e o padrão do cliente desiste em 30 s.
 * Sem caução, a chamada continua com o padrão do cliente.
 */
export const TEMPO_LIMITE_ABERTURA_COM_CAUCAO_MS = 120_000;

/**
 * Lista de devoluções pendentes da locadora do funcionário (locacao.contract.json, v5 a v7): GET /api/locacoes?status=.
 * Em andamento traz as em andamento e as atrasadas; ATRASADA traz só as atrasadas. Sem filtro de data.
 */
export async function listarDevolucoesPendentes(filtro: FiltroDevolucoesPendentes): Promise<Pagina<DevolucaoPendente>> {
  const { data } = await api.get<Pagina<DevolucaoPendente>>('/api/locacoes', {
    params: { status: filtro.status, pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina },
  });
  return data;
}

export async function buscarContextoRetirada(idReserva: number): Promise<ContextoRetirada> {
  const { data } = await api.get<ContextoRetirada>(`/api/locacoes/retirada/${idReserva}`);
  return data;
}

/** Corpo do POST com caução: a reserva e os quatro campos do cartão, montados um a um (nada a mais vai junto). */
interface CorpoAberturaComCaucao extends DadosCaucaoAbertura { idReserva: number; }

export async function abrirLocacao(dados: AbrirLocacao): Promise<LocacaoAberta> {
  if (!dados.caucao) {
    const { data } = await api.post<LocacaoAberta>('/api/locacoes', { idReserva: dados.idReserva });
    return data;
  }
  const corpo: CorpoAberturaComCaucao = {
    idReserva: dados.idReserva,
    caucaoTokenCartao: dados.caucao.caucaoTokenCartao,
    caucaoMetodoPagamentoId: dados.caucao.caucaoMetodoPagamentoId,
    caucaoEmissorId: dados.caucao.caucaoEmissorId,
    caucaoIdentificadorDispositivo: dados.caucao.caucaoIdentificadorDispositivo,
  };
  const { data } = await api.post<LocacaoAberta>('/api/locacoes', corpo, { timeout: TEMPO_LIMITE_ABERTURA_COM_CAUCAO_MS });
  return data;
}
