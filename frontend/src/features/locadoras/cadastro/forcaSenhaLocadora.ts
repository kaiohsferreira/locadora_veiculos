/**
 * Requisitos e medidor de força da senha do responsável (tela 37). Sem React: só lê a senha digitada.
 *
 * Os três requisitos são os do desenho (mínimo de 8 caracteres, uma letra maiúscula, um número). O medidor
 * tem quatro barras, como no desenho, e cada requisito atendido acende uma barra. Com os três atendidos o
 * medidor mostra três barras de quatro; a quarta fica apagada até o dono do produto definir a regra
 * (decisão em aberto, listada na entrega).
 */

/** Quantidade de barras do medidor (o desenho mostra quatro). */
export const TOTAL_BARRAS_FORCA_SENHA = 4;

/** Mínimo de caracteres exibido como requisito (o mesmo do backend). */
export const TAMANHO_MINIMO_REQUISITO_SENHA = 8;

/** Quais requisitos a senha atende agora. */
export interface RequisitosSenhaLocadora {
  tamanho: boolean;
  maiuscula: boolean;
  numero: boolean;
}

/** Avalia os três requisitos sobre a senha digitada (vazia = nenhum atendido). */
export function avaliarRequisitosSenha(senha: string): RequisitosSenhaLocadora {
  return {
    tamanho: senha.length >= TAMANHO_MINIMO_REQUISITO_SENHA,
    maiuscula: /\p{Lu}/u.test(senha),
    numero: /\p{Nd}/u.test(senha),
  };
}

/** Quantidade de requisitos atendidos (0 a 3). Cada um acende uma barra do medidor. */
export function nivelSenha(requisitos: RequisitosSenhaLocadora): number {
  return [requisitos.tamanho, requisitos.maiuscula, requisitos.numero].filter(Boolean).length;
}
