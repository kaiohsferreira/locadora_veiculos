/**
 * Decisões do fluxo da tela 05 (cadastro e edição), como funções puras, sem React. A página só aplica o resultado.
 *
 * - Rota: sem id é cadastro; com id válido é edição; id que não é inteiro positivo não existe (nunca vira consulta).
 * - Envio: um envio por vez; depois de criado, o formulário não envia de novo (clique duplo ou nova tentativa).
 * - URL: depois do cadastro a tela segue no cadastro enquanto o lote de fotos roda. Só troca para a rota de edição quando
 *   nenhuma foto está aguardando, enviando nem com erro: o lote terminou sem falha, ou as que falharam foram reenviadas
 *   com sucesso ou removidas. Trocar antes cancela o envio em curso (a fila cancela ao desmontar).
 * - Rascunho: o aviso de rascunho recuperado só existe no cadastro, antes da criação do veículo.
 * - Cancelar / Voltar: volta à tela anterior quando a pessoa chegou navegando dentro do sistema; aberta direto, vai para a frota.
 */
import type { SituacaoItemFila } from '@/types/fotosVeiculo';
import { caminhoEditarVeiculo } from '@/routes/caminhos';

/** O que a rota da tela representa. */
export type IdentificacaoRotaVeiculo =
  | { tipo: 'cadastro' }
  | { tipo: 'edicao'; idVeiculo: number }
  | { tipo: 'invalido' };

/** Parâmetro `idVeiculo` da rota: ausente é cadastro; número inteiro positivo é edição; o resto é inválido. */
export function identificarRotaVeiculo(parametro: string | undefined): IdentificacaoRotaVeiculo {
  if (parametro === undefined) return { tipo: 'cadastro' };
  if (!/^[1-9]\d*$/.test(parametro)) return { tipo: 'invalido' };
  const idVeiculo = Number(parametro);
  if (!Number.isSafeInteger(idVeiculo)) return { tipo: 'invalido' };
  return { tipo: 'edicao', idVeiculo };
}

/** Pode enviar o cadastro? Não enquanto um envio está em curso e não depois que o veículo já foi criado. */
export function podeEnviarCadastro(estado: { enviando: boolean; criado: boolean }): boolean {
  return !estado.enviando && !estado.criado;
}

/** Situações que ainda não terminaram: enquanto alguma existe, a tela fica no cadastro. */
const SITUACOES_PENDENTES: readonly SituacaoItemFila[] = ['aguardando', 'enviando', 'erro'];

/** A fila não tem foto aguardando, enviando nem com erro. Só então o cadastro pode seguir para a edição. */
export function filaSemPendencias(situacoes: readonly SituacaoItemFila[]): boolean {
  return !situacoes.some((situacao) => SITUACOES_PENDENTES.includes(situacao));
}

/**
 * Caminho da edição a que a tela deve ir depois do cadastro, ou nulo quando ainda não deve trocar: sem veículo criado,
 * ou com alguma foto do lote com falha (a tela fica para o "Tentar de novo"). `temFalha` falso com o veículo criado
 * significa que não há mais nada pendente na fila.
 */
export function caminhoEdicaoAposLote(idCriado: number | null, temFalha: boolean): string | null {
  if (idCriado === null || temFalha) return null;
  return caminhoEditarVeiculo(idCriado);
}

/** O aviso "rascunho recuperado" aparece só no cadastro, enquanto o veículo ainda não foi criado. */
export function avisoRascunhoVisivel(estado: { edicao: boolean; rascunhoAberto: boolean; criado: boolean }): boolean {
  return !estado.edicao && estado.rascunhoAberto && !estado.criado;
}

/** Estado de origem que a tela leva pela navegação: se a pessoa chegou por dentro do sistema. */
export interface LocalizacaoVeiculo {
  key: string;
  state: unknown;
}

/**
 * A pessoa chegou a esta tela navegando dentro do sistema? A navegação que a própria tela faz (cadastro para edição)
 * grava a resposta no estado da rota. Sem isso, a chave da entrada diz: 'default' é a primeira entrada (aberta direto).
 */
export function origemDaTelaVeiculo(localizacao: LocalizacaoVeiculo): boolean {
  const estado = localizacao.state;
  if (typeof estado === 'object' && estado !== null && 'veioDoSistema' in estado) {
    if (typeof estado.veioDoSistema === 'boolean') return estado.veioDoSistema;
  }
  return localizacao.key !== 'default';
}

/** Destino do "Cancelar / Voltar": a tela anterior (dentro do sistema) ou a frota (aberta direto). */
export type DestinoCancelamentoVeiculo = 'voltar' | 'frota';

export function destinoCancelamentoVeiculo(veioDoSistema: boolean): DestinoCancelamentoVeiculo {
  return veioDoSistema ? 'voltar' : 'frota';
}
