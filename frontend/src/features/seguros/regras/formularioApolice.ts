import { z } from 'zod';
import { converterMoedaPtBrParaNumero, mascararMoedaDigitada } from '@/lib/formatters';
import {
  SEGUROS_ERRO_COBERTURA_TAMANHO,
  SEGUROS_ERRO_FIM,
  SEGUROS_ERRO_INICIO,
  SEGUROS_ERRO_NUMERO,
  SEGUROS_ERRO_NUMERO_TAMANHO,
  SEGUROS_ERRO_PREMIO,
  SEGUROS_ERRO_PREMIO_PRECISAO,
  SEGUROS_ERRO_SEGURADORA,
  SEGUROS_ERRO_SEGURADORA_TAMANHO,
  SEGUROS_ERRO_VEICULO,
  SEGUROS_ERRO_VIGENCIA,
} from '@/lib/mensagensSeguros';
import type { Apolice, ApoliceRequisicao, VeiculoPlaca } from '@/types/seguros';

/** Limites da API (SeguroVOValidator). */
export const TAMANHO_MAXIMO_SEGURADORA = 100;
export const TAMANHO_MAXIMO_NUMERO_APOLICE = 30;
export const TAMANHO_MAXIMO_COBERTURA = 200;
/** NUMERIC(10,2): até 8 dígitos inteiros. */
export const PREMIO_MAXIMO = 99999999.99;

const DATA_ISO = /^\d{4}-\d{2}-\d{2}$/;

/** Campos do formulário (as datas vêm do campo de data como "yyyy-MM-dd"; o prêmio, com a máscara de reais). */
export interface ValoresApolice {
  veiculo: VeiculoPlaca | null;
  seguradora: string;
  numeroApolice: string;
  dataInicio: string;
  dataFim: string;
  valorPremio: string;
  cobertura: string;
}

/** Nomes dos campos que podem receber erro (do formulário ou do servidor). */
export type CampoApolice = Exclude<keyof ValoresApolice, never>;

const esquemaVeiculo = z.object({ idVeiculo: z.number(), placa: z.string(), marca: z.string(), modelo: z.string() });

/** Regras do formulário, as mesmas do SeguroVOValidator, para o erro aparecer no campo antes de enviar. */
export const esquemaApolice = z
  .object({
    veiculo: esquemaVeiculo.nullable().refine((veiculo) => veiculo !== null, { message: SEGUROS_ERRO_VEICULO }),
    seguradora: z
      .string()
      .trim()
      .min(1, { message: SEGUROS_ERRO_SEGURADORA })
      .max(TAMANHO_MAXIMO_SEGURADORA, { message: SEGUROS_ERRO_SEGURADORA_TAMANHO }),
    numeroApolice: z
      .string()
      .trim()
      .min(1, { message: SEGUROS_ERRO_NUMERO })
      .max(TAMANHO_MAXIMO_NUMERO_APOLICE, { message: SEGUROS_ERRO_NUMERO_TAMANHO }),
    dataInicio: z.string().regex(DATA_ISO, { message: SEGUROS_ERRO_INICIO }),
    dataFim: z.string().regex(DATA_ISO, { message: SEGUROS_ERRO_FIM }),
    valorPremio: z.string().superRefine((texto, contexto) => {
      const valor = converterMoedaPtBrParaNumero(texto);
      if (valor === null || valor <= 0) contexto.addIssue({ code: 'custom', message: SEGUROS_ERRO_PREMIO });
      else if (valor > PREMIO_MAXIMO) contexto.addIssue({ code: 'custom', message: SEGUROS_ERRO_PREMIO_PRECISAO });
    }),
    cobertura: z.string().max(TAMANHO_MAXIMO_COBERTURA, { message: SEGUROS_ERRO_COBERTURA_TAMANHO }),
  })
  .superRefine((valores, contexto) => {
    // Datas "yyyy-MM-dd" comparam certo como texto. Só compara quando as duas estão preenchidas.
    if (DATA_ISO.test(valores.dataInicio) && DATA_ISO.test(valores.dataFim) && valores.dataFim <= valores.dataInicio) {
      contexto.addIssue({ code: 'custom', path: ['dataFim'], message: SEGUROS_ERRO_VIGENCIA });
    }
  });

/** Formulário vazio do cadastro. */
export function valoresIniciaisCadastro(): ValoresApolice {
  return { veiculo: null, seguradora: '', numeroApolice: '', dataInicio: '', dataFim: '', valorPremio: '', cobertura: '' };
}

/** Formulário da edição, com os dados atuais da apólice (o veículo só com a placa, que é o que a apólice traz). */
export function valoresIniciaisEdicao(apolice: Apolice): ValoresApolice {
  return {
    veiculo: { idVeiculo: apolice.idVeiculo, placa: apolice.placaVeiculo, marca: '', modelo: '' },
    seguradora: apolice.seguradora,
    numeroApolice: apolice.numeroApolice,
    dataInicio: apolice.dataInicio,
    dataFim: apolice.dataFim,
    valorPremio: mascararMoedaDigitada(String(Math.round(apolice.valorPremio * 100))),
    cobertura: apolice.cobertura ?? '',
  };
}

/** Corpo do POST/PUT a partir do formulário já validado. Cobertura vazia vai como nula; textos saem aparados. */
export function montarRequisicaoApolice(valores: ValoresApolice): ApoliceRequisicao {
  const cobertura = valores.cobertura.trim();
  return {
    idVeiculo: valores.veiculo?.idVeiculo ?? 0,
    seguradora: valores.seguradora.trim(),
    numeroApolice: valores.numeroApolice.trim(),
    dataInicio: valores.dataInicio,
    dataFim: valores.dataFim,
    valorPremio: converterMoedaPtBrParaNumero(valores.valorPremio) ?? 0,
    cobertura: cobertura.length > 0 ? cobertura : null,
  };
}

/** "ABC1D23 · Jeep Renegade", ou só a placa quando a marca e o modelo não vieram (edição). */
export function descricaoVeiculo(veiculo: VeiculoPlaca): string {
  const nome = `${veiculo.marca} ${veiculo.modelo}`.trim();
  return nome ? `${veiculo.placa} · ${nome}` : veiculo.placa;
}
