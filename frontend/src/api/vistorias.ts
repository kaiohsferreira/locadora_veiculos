import { api } from './client';
import { ErroApi } from '@/types/api';
import type { ContextoVistoriaEntrada, LocacaoDaVistoria, RegistrarVistoriaEntrada, VistoriaEntrada } from '@/types/vistorias';

const caminhoEntrada = (idLocacao: number): string => `/api/locacoes/${idLocacao}/vistorias/entrada`;

/** Só interpreta 404 como ausência de vistoria depois de validar acesso à locação. */
export async function buscarContextoVistoriaEntrada(idLocacao: number): Promise<ContextoVistoriaEntrada> {
  const locacao = await api.get<LocacaoDaVistoria>(`/api/locacoes/${idLocacao}`);
  try {
    const vistoria = await api.get<VistoriaEntrada>(caminhoEntrada(idLocacao));
    return { locacao: locacao.data, vistoria: vistoria.data };
  } catch (erro: unknown) {
    if (erro instanceof ErroApi && erro.status === 404) {
      return { locacao: locacao.data, vistoria: null };
    }
    throw erro;
  }
}

export async function registrarVistoriaEntrada(idLocacao: number, dados: RegistrarVistoriaEntrada): Promise<VistoriaEntrada> {
  const resposta = await api.post<VistoriaEntrada>(caminhoEntrada(idLocacao), dados);
  return resposta.data;
}
