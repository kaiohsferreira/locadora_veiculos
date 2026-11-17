/**
 * Chamadas das fotos do veículo (contrato veiculo-foto): listar (GET), enviar (POST, multipart com o campo
 * "foto") e remover (DELETE). As telas acessam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import { PERCENTUAL_MAXIMO } from '@/lib/fotosVeiculo';
import type { FotoDoVeiculo } from '@/types/fotosVeiculo';

/** Nome do campo do arquivo no formulário (contrato: EnviarFotoVeiculoRequest.foto). */
export const CAMPO_FOTO_VEICULO = 'foto';

/** Tempo limite próprio do envio: 10 MB em rede lenta não cabem nos 30 s padrão do cliente. */
export const TEMPO_LIMITE_ENVIO_FOTO_MS = 120_000;

/** Opções do envio: progresso de 0 a 100 e cancelamento. */
export interface OpcoesEnvioFoto {
  aoProgredir?: (percentual: number) => void;
  sinal?: AbortSignal;
}

function caminhoFotosVeiculo(idVeiculo: number): string {
  return `/api/veiculos/${idVeiculo}/fotos`;
}

/** Fotos ativas do veículo, em ordem. Resposta que não é lista vira lista vazia. */
export async function listarFotosVeiculo(idVeiculo: number): Promise<FotoDoVeiculo[]> {
  const resposta = await api.get<unknown>(caminhoFotosVeiculo(idVeiculo));
  return Array.isArray(resposta.data) ? (resposta.data as FotoDoVeiculo[]) : [];
}

/** Envia uma foto (201 com a foto gravada). Não definir o Content-Type na mão: o navegador põe o boundary. */
export async function enviarFotoVeiculo(
  idVeiculo: number,
  arquivo: File,
  opcoes: OpcoesEnvioFoto = {},
): Promise<FotoDoVeiculo> {
  const formulario = new FormData();
  formulario.append(CAMPO_FOTO_VEICULO, arquivo, arquivo.name);

  const resposta = await api.post<FotoDoVeiculo>(caminhoFotosVeiculo(idVeiculo), formulario, {
    timeout: TEMPO_LIMITE_ENVIO_FOTO_MS,
    signal: opcoes.sinal,
    onUploadProgress: (evento) => {
      const total = evento.total ?? 0;
      if (total > 0 && opcoes.aoProgredir) {
        opcoes.aoProgredir(Math.round((evento.loaded * PERCENTUAL_MAXIMO) / total));
      }
    },
  });
  return resposta.data;
}

/** Remove a foto: apaga o arquivo no armazenamento e desativa a linha (204). */
export async function removerFotoVeiculo(idVeiculo: number, idVeiculoFoto: number): Promise<void> {
  await api.delete(`${caminhoFotosVeiculo(idVeiculo)}/${idVeiculoFoto}`);
}
