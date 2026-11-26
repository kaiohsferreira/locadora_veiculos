import { z } from 'zod';
import { ANGULOS_AVARIA, type AnguloAvaria } from '@/types/avarias';
import { GRAVIDADE_AVARIA, type GravidadeAvaria } from '@/types/enums';
import { mensagemRecusaFoto, validarArquivoFoto, type ArquivoFotoConferido } from '@/lib/fotosVeiculo';
import { MENSAGENS_DEVOLUCAO } from '@/lib/mensagensDevolucao';
import {
  MENSAGEM_FORM_AVARIA_ESCOLHA_FOTO,
  MENSAGEM_FORM_AVARIA_ESCOLHA_GRAVIDADE,
  MENSAGEM_FORM_AVARIA_POSICAO_INCOMPLETA,
  MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA,
} from '@/lib/mensagensVistoriaAvarias';

/** Maior valor aceito pelo servidor para o custo estimado (faixa de 0 a 99999999,99). */
export const VALOR_ESTIMADO_MAXIMO = 99_999_999.99;
/** Percentuais da posição no quadrante: de 0 a 100, com até duas casas (contrato avaria, v6). */
export const POSICAO_MINIMA = 0;
export const POSICAO_MAXIMA = 100;
const TAMANHO_MAXIMO_DESCRICAO = 200;

/** Valor digitado com vírgula ou ponto e até duas casas. O servidor só aceita ponto; a conversão é feita na normalização. */
const FORMATO_VALOR = /^\d{1,8}(?:[.,]\d{1,2})?$/;
/** Posição digitada com ponto decimal e até duas casas (mesmo formato do servidor). */
const FORMATO_POSICAO = /^\d{1,3}(?:\.\d{1,2})?$/;

/** Campos do formulário da avaria, como o usuário digita (texto). A foto é conferida à parte. */
export interface CamposAvaria {
  /** "" (sem ângulo) ou um dos ângulos do contrato. */
  angulo: string;
  descricao: string;
  /** "" (não escolhida) ou uma das gravidades do contrato. */
  gravidade: string;
  valorEstimado: string;
  posX: string;
  posY: string;
}

/** Valores já normalizados para o envio, no formato que o servidor lê. */
export interface CamposAvariaNormalizados {
  angulo: AnguloAvaria | null;
  descricao: string;
  gravidade: GravidadeAvaria;
  valorEstimado: string | null;
  posX: string | null;
  posY: string | null;
}

function posicaoValida(texto: string): boolean {
  if (!FORMATO_POSICAO.test(texto)) return false;
  const numero = Number(texto);
  return numero >= POSICAO_MINIMA && numero <= POSICAO_MAXIMA;
}

/**
 * Esquema do formulário da avaria (Zod). Regras: descrição de 1 a 200 caracteres sem caractere nulo, gravidade obrigatória,
 * custo estimado opcional e posição em tudo ou nada (ângulo, X e Y vêm juntos ou todos vazios, como no servidor).
 */
export const esquemaAvaria = z
  .object({
    angulo: z.string().refine((valor) => valor === '' || (ANGULOS_AVARIA as readonly string[]).includes(valor)),
    descricao: z
      .string()
      .trim()
      .min(1, MENSAGENS_DEVOLUCAO.descricaoInvalida)
      .max(TAMANHO_MAXIMO_DESCRICAO, MENSAGENS_DEVOLUCAO.descricaoInvalida)
      .refine((valor) => !valor.includes('\0'), MENSAGENS_DEVOLUCAO.descricaoInvalida),
    gravidade: z
      .string()
      .refine((valor) => (GRAVIDADE_AVARIA as readonly string[]).includes(valor), MENSAGEM_FORM_AVARIA_ESCOLHA_GRAVIDADE),
    valorEstimado: z
      .string()
      .trim()
      .refine(
        (valor) => valor === '' || (FORMATO_VALOR.test(valor) && Number(valor.replace(',', '.')) <= VALOR_ESTIMADO_MAXIMO),
        MENSAGENS_DEVOLUCAO.valorFaixa,
      ),
    posX: z.string().trim(),
    posY: z.string().trim(),
  })
  .superRefine((campos, contexto) => {
    const preenchidos = [campos.angulo !== '', campos.posX !== '', campos.posY !== ''];
    if (preenchidos.some(Boolean) && !preenchidos.every(Boolean)) {
      contexto.addIssue({ code: 'custom', path: ['angulo'], message: MENSAGEM_FORM_AVARIA_POSICAO_INCOMPLETA });
      return;
    }
    if (campos.posX !== '' && !posicaoValida(campos.posX)) {
      contexto.addIssue({ code: 'custom', path: ['posX'], message: MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA });
    }
    if (campos.posY !== '' && !posicaoValida(campos.posY)) {
      contexto.addIssue({ code: 'custom', path: ['posY'], message: MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA });
    }
  });

/** Converte o texto digitado no formato do envio: vírgula vira ponto, valor com duas casas, posição com duas casas. */
export function normalizarAvaria(campos: CamposAvaria): CamposAvariaNormalizados {
  // Os valores já passaram pelo esquema (ângulo e gravidade só entram com um valor da lista).
  const angulo = campos.angulo === '' ? null : (campos.angulo as AnguloAvaria);
  const valor = campos.valorEstimado.trim();
  const posX = campos.posX.trim();
  const posY = campos.posY.trim();
  return {
    angulo,
    descricao: campos.descricao.trim(),
    gravidade: campos.gravidade as GravidadeAvaria,
    valorEstimado: valor === '' ? null : Number(valor.replace(',', '.')).toFixed(2),
    posX: angulo === null || posX === '' ? null : Number(posX).toFixed(2),
    posY: angulo === null || posY === '' ? null : Number(posY).toFixed(2),
  };
}

/** Mensagem da foto da avaria: obrigatória, JPEG ou PNG e até 10 MB (mesmos textos da tela 05). Nulo quando passa. */
export function validarFotoAvaria(arquivo: ArquivoFotoConferido | null): string | null {
  if (arquivo === null) return MENSAGEM_FORM_AVARIA_ESCOLHA_FOTO;
  const motivo = validarArquivoFoto(arquivo);
  return motivo === null ? null : mensagemRecusaFoto(motivo);
}

/**
 * Posição do clique no quadrante, em percentual de 0 a 100 com duas casas. O ponto é limitado ao quadrante, para que
 * um clique na borda não passe de 100.
 */
export function posicaoNoQuadrante(
  larguraQuadrante: number,
  alturaQuadrante: number,
  xNoQuadrante: number,
  yNoQuadrante: number,
): { posX: number; posY: number } {
  const percentual = (parte: number, total: number): number => {
    if (!(total > 0)) return POSICAO_MINIMA;
    const valor = Math.min(POSICAO_MAXIMA, Math.max(POSICAO_MINIMA, (parte / total) * POSICAO_MAXIMA));
    return Math.round(valor * 100) / 100;
  };
  return { posX: percentual(xNoQuadrante, larguraQuadrante), posY: percentual(yNoQuadrante, alturaQuadrante) };
}
