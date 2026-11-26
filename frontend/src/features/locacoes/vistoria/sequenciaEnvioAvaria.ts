/**
 * Sequência do envio das avarias da tela 19 (contrato avaria, N8 e N11 de 26/11/2026). Funções puras, sem React e sem API:
 * o hook só executa o passo que estas funções devolvem.
 *
 * Regra central: a avaria só é criada (POST) uma vez. Depois que o servidor a gravou, qualquer repetição é só da foto (PUT).
 */
import { ErroApi } from '@/types/api';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';

/** O que fazer com um item local: criar a avaria (POST) ou só enviar a foto de uma avaria já gravada (PUT). */
export type PassoEnvioAvaria = { tipo: 'criar_avaria' } | { tipo: 'enviar_foto'; idAvaria: number };

/** Sem id da avaria, o item ainda precisa do POST. Com id, o POST nunca se repete. */
export function passoDoEnvio(idAvaria: number | null): PassoEnvioAvaria {
  return idAvaria === null ? { tipo: 'criar_avaria' } : { tipo: 'enviar_foto', idAvaria };
}

/**
 * Desfecho de uma falha. `foto_pendente`: a avaria existe no servidor e falta a foto (repetir só o PUT).
 * `sem_id_no_502`: o 502 veio sem idAvaria no corpo; o hook procura a avaria pela lista antes de decidir.
 * `erro`: nada foi gravado (ou a falha não tem relação com a foto) e o POST pode ser repetido pelo usuário.
 */
export type DesfechoFalhaEnvio =
  | { tipo: 'foto_pendente'; idAvaria: number }
  | { tipo: 'sem_id_no_502' }
  | { tipo: 'erro' };

/** Lê o 502 do POST: a avaria foi gravada e a foto não. O idAvaria vem do corpo do ProblemDetails. */
export function desfechoFalhaDoPost(erro: unknown): DesfechoFalhaEnvio {
  if (!(erro instanceof ErroApi) || erro.status !== 502) return { tipo: 'erro' };
  const idAvaria = erro.problema?.idAvaria;
  if (idAvaria === undefined) return { tipo: 'sem_id_no_502' };
  return { tipo: 'foto_pendente', idAvaria };
}

/** A avaria da lista tem os mesmos dados do envio: descrição, gravidade, ângulo, posição e valor (texto convertido para número). */
export function avariaEhDoEnvio(avaria: AvariaRegistrada, dados: DadosNovaAvaria): boolean {
  return (
    avaria.descricao === dados.descricao &&
    avaria.gravidade === dados.gravidade &&
    avaria.angulo === dados.angulo &&
    avaria.posX === (dados.posX === null ? null : Number(dados.posX)) &&
    avaria.posY === (dados.posY === null ? null : Number(dados.posY)) &&
    avaria.valorEstimado === (dados.valorEstimado === null ? null : Number(dados.valorEstimado))
  );
}

/**
 * Depois de uma falha sem resposta (tempo esgotado ou queda de rede) o POST pode ter sido gravado. Conta como gravada pelo
 * envio só a avaria NOVA na lista (id que não existia antes do POST) com os mesmos dados. Devolve a de maior id, ou nulo.
 */
export function localizarAvariaDoEnvio(
  idsAntesDoPost: readonly number[],
  listaDepois: readonly AvariaRegistrada[],
  dados: DadosNovaAvaria,
): AvariaRegistrada | null {
  const candidatas = listaDepois.filter((avaria) => !idsAntesDoPost.includes(avaria.idAvaria) && avariaEhDoEnvio(avaria, dados));
  if (candidatas.length === 0) return null;
  return candidatas.reduce((maior, avaria) => (avaria.idAvaria > maior.idAvaria ? avaria : maior));
}

/** Status que indicam recusa definitiva do POST: o texto do servidor vai para o item (repetir com os mesmos dados não resolve o 409). */
export const STATUS_RECUSA_DO_POST: readonly number[] = [400, 404, 409];

/**
 * Recusa do POST que repetir com os mesmos dados não resolve (400 de dados ou de foto, 409 de regra): o item mostra a
 * mensagem do servidor e não oferece "Tentar de novo" (N8 e tela 19, 26/11/2026). O 404 segue repetível.
 */
export const STATUS_SEM_REPETICAO_DO_POST: readonly number[] = [400, 409];

/**
 * Desfecho de qualquer falha do envio de um item. Item já gravado (tem id) falhou na foto: continua pendente da foto.
 * Item novo falhou no POST: vale o 502 com id, ou o caso sem id, ou erro.
 */
export function desfechoFalhaDoEnvio(idAvariaDoItem: number | null, erro: unknown): DesfechoFalhaEnvio {
  if (idAvariaDoItem !== null) return { tipo: 'foto_pendente', idAvaria: idAvariaDoItem };
  return desfechoFalhaDoPost(erro);
}
