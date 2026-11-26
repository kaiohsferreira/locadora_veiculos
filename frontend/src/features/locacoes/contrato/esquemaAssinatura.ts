import { z } from 'zod';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { temCaractereDeControle } from './regrasContrato';

/** Valores do formulário de assinatura. O PNG do quadro não entra aqui: é validado à parte (problemaAssinaturaPng). */
export interface ValoresAssinatura {
  nomeAssinante: string;
  aceiteCondicoesGerais: boolean;
  aceitePoliticaCombustivel: boolean;
}

export const LIMITE_NOME_ASSINANTE = 100;

/**
 * Validação antes de chamar a API (F4). Os mesmos limites do servidor: nome obrigatório, até 100 caracteres, sem
 * caractere de controle; o aceite de condições gerais é obrigatório; o de combustível só quando a cláusula consta do
 * documento. O nome é validado como digitado, sem aparar espaços, porque o servidor grava o que chegou.
 */
export function criarEsquemaAssinatura(exigeAceiteCombustivel: boolean) {
  return z
    .object({
      nomeAssinante: z
        .string()
        .refine((valor) => valor.trim().length > 0, M.nomeVazio)
        .refine((valor) => valor.length <= LIMITE_NOME_ASSINANTE, M.nomeTamanho)
        .refine((valor) => !temCaractereDeControle(valor), M.nomeInvalido),
      aceiteCondicoesGerais: z.boolean().refine((valor) => valor, M.aceitesParaAssinar),
      aceitePoliticaCombustivel: z.boolean(),
    })
    .superRefine((valor, contexto) => {
      if (exigeAceiteCombustivel && !valor.aceitePoliticaCombustivel) {
        contexto.addIssue({
          code: 'custom',
          path: ['aceitePoliticaCombustivel'],
          message: M.aceitesParaAssinar,
        });
      }
    });
}
