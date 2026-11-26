/**
 * Chamadas das avarias da vistoria (contrato avaria, v6): listar (GET), registrar (POST, multipart) e repetir o envio
 * da foto (PUT). As telas acessam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import { MENSAGEM_AVARIA_ERRO_LISTA } from '@/lib/mensagensVistoriaAvarias';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import { ErroApi } from '@/types/api';

/** Nome do campo do arquivo no formulário (contrato avaria: "foto"). */
export const CAMPO_FOTO_AVARIA = 'foto';

/** Tempo limite próprio do envio: 10 MB em rede lenta não cabem nos 30 s padrão do cliente. */
export const TEMPO_LIMITE_ENVIO_FOTO_AVARIA_MS = 120_000;

function caminhoAvarias(idVistoria: number): string {
  return `/api/vistorias/${idVistoria}/avarias`;
}

/**
 * Avarias ativas da vistoria, em ordem de id, com a foto quando o arquivo existe. O contrato manda array sem envelope:
 * outra forma de resposta vira erro (a tela mostra o aviso de lista), e não uma lista vazia que pareça "sem avarias".
 */
export async function listarAvarias(idVistoria: number): Promise<AvariaRegistrada[]> {
  const resposta = await api.get<unknown>(caminhoAvarias(idVistoria));
  if (!Array.isArray(resposta.data)) {
    throw new ErroApi({ status: 0, mensagem: MENSAGEM_AVARIA_ERRO_LISTA });
  }
  return resposta.data as AvariaRegistrada[];
}

/**
 * Registra uma avaria com a foto (201). Não definir o Content-Type na mão: o navegador põe o boundary.
 * Quando a foto falha depois de a avaria ser gravada, o servidor responde 502 (ver `desfechoFalhaDoPost` em sequenciaEnvioAvaria.ts).
 */
export async function registrarAvaria(idVistoria: number, dados: DadosNovaAvaria): Promise<AvariaRegistrada> {
  const formulario = new FormData();
  formulario.append('descricao', dados.descricao);
  formulario.append('gravidade', dados.gravidade);
  if (dados.valorEstimado !== null) formulario.append('valorEstimado', dados.valorEstimado);
  if (dados.angulo !== null) formulario.append('angulo', dados.angulo);
  if (dados.posX !== null) formulario.append('posX', dados.posX);
  if (dados.posY !== null) formulario.append('posY', dados.posY);
  formulario.append(CAMPO_FOTO_AVARIA, dados.foto, dados.foto.name);

  const resposta = await api.post<AvariaRegistrada>(caminhoAvarias(idVistoria), formulario, {
    timeout: TEMPO_LIMITE_ENVIO_FOTO_AVARIA_MS,
  });
  return resposta.data;
}

/**
 * Remove só o arquivo da foto (204). A avaria continua; a rota é idempotente. A tela NÃO chama esta função na troca de foto
 * (a troca é só o PUT). Uso só com confirmação do usuário (contrato avaria: "nao ha como desfazer").
 */
export async function removerFotoAvaria(idVistoria: number, idAvaria: number): Promise<void> {
  await api.delete(`${caminhoAvarias(idVistoria)}/${idAvaria}/foto`);
}

/** Repete o envio da foto de uma avaria já gravada (200). Serve para repetir o envio que falhou e para trocar a foto. */
export async function enviarFotoAvaria(idVistoria: number, idAvaria: number, arquivo: File): Promise<AvariaRegistrada> {
  const formulario = new FormData();
  formulario.append(CAMPO_FOTO_AVARIA, arquivo, arquivo.name);

  const resposta = await api.put<AvariaRegistrada>(`${caminhoAvarias(idVistoria)}/${idAvaria}/foto`, formulario, {
    timeout: TEMPO_LIMITE_ENVIO_FOTO_AVARIA_MS,
  });
  return resposta.data;
}
