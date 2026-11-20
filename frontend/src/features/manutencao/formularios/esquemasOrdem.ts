/**
 * Formulários de abrir e de encerrar a ordem de manutenção (tela 14). A validação roda no front antes de chamar a API
 * (regra F4); os textos são os mesmos do servidor. O servidor continua sendo a autoridade (regra de negócio e 409).
 */
import { z } from 'zod';
import { converterMoedaPtBrParaNumero } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_ERRO_CUSTO_PRECISAO,
  MENSAGEM_MANUTENCAO_ERRO_DATA_ENTRADA_FUTURA,
  MENSAGEM_MANUTENCAO_ERRO_DATA_SAIDA_ANTERIOR,
  MENSAGEM_MANUTENCAO_ERRO_DATA_SAIDA_FUTURA,
  MENSAGEM_MANUTENCAO_ERRO_KM,
  MENSAGEM_MANUTENCAO_ERRO_KM_LIMITE,
  MENSAGEM_MANUTENCAO_ERRO_TIPO,
  MENSAGEM_MANUTENCAO_ERRO_VEICULO,
  mensagemManutencaoDescricaoTamanho,
  mensagemManutencaoOficinaTamanho,
} from '@/lib/mensagens';
import { TIPO_MANUTENCAO, type TipoManutencao } from '@/types/enums';
import type { AbrirManutencaoRequisicao, EncerrarManutencaoRequisicao } from '@/types/manutencao';

/** Tamanho máximo da oficina e da descrição (iguais aos do servidor). */
export const TAMANHO_MAXIMO_OFICINA_MANUTENCAO = 100;
export const TAMANHO_MAXIMO_DESCRICAO_MANUTENCAO = 200;
/** Dígitos inteiros do custo (o servidor aceita até oito, com duas casas decimais). */
export const DIGITOS_INTEIROS_CUSTO_MANUTENCAO = 8;
/** Maior quilometragem aceita: o servidor guarda int (32 bits). Acima disso o JSON não converte e a resposta seria genérica. */
export const KM_MAXIMO_MANUTENCAO = 2147483647;

/** Data de hoje no formato AAAA-MM-DD, pelo fuso local (o de Brasília na operação). */
export function dataIsoLocal(agora: Date): string {
  const mes = String(agora.getMonth() + 1).padStart(2, '0');
  const dia = String(agora.getDate()).padStart(2, '0');
  return `${agora.getFullYear()}-${mes}-${dia}`;
}

/** Valores do formulário de abrir ordem, como o usuário digita (texto). */
export type ValoresAbrirOrdem = z.input<ReturnType<typeof criarEsquemaAbrirOrdem>>;

/** Esquema de abrir ordem. `hoje` (AAAA-MM-DD) limita a data de entrada: data futura é recusada. */
export function criarEsquemaAbrirOrdem(hoje: string) {
  return z.object({
    idVeiculo: z.string().refine((valor) => valor.trim() !== '', { error: MENSAGEM_MANUTENCAO_ERRO_VEICULO }),
    tipo: z
      .string()
      .refine((valor) => (TIPO_MANUTENCAO as readonly string[]).includes(valor), { error: MENSAGEM_MANUTENCAO_ERRO_TIPO }),
    oficina: z.string().refine((valor) => valor.trim().length <= TAMANHO_MAXIMO_OFICINA_MANUTENCAO, {
      error: mensagemManutencaoOficinaTamanho(TAMANHO_MAXIMO_OFICINA_MANUTENCAO),
    }),
    descricao: z.string().refine((valor) => valor.trim().length <= TAMANHO_MAXIMO_DESCRICAO_MANUTENCAO, {
      error: mensagemManutencaoDescricaoTamanho(TAMANHO_MAXIMO_DESCRICAO_MANUTENCAO),
    }),
    dataEntrada: z
      .string()
      .refine((valor) => valor === '' || valor <= hoje, { error: MENSAGEM_MANUTENCAO_ERRO_DATA_ENTRADA_FUTURA }),
  });
}

/** Corpo do POST: o veículo vira número; oficina, descrição e data vazias não vão (o servidor usa o padrão). */
export function montarRequisicaoAbrir(valores: ValoresAbrirOrdem): AbrirManutencaoRequisicao {
  const requisicao: AbrirManutencaoRequisicao = {
    idVeiculo: Number(valores.idVeiculo),
    tipo: valores.tipo as TipoManutencao,
  };
  const oficina = valores.oficina.trim();
  const descricao = valores.descricao.trim();
  if (oficina !== '') requisicao.oficina = oficina;
  if (descricao !== '') requisicao.descricao = descricao;
  if (valores.dataEntrada !== '') requisicao.dataEntrada = valores.dataEntrada;
  return requisicao;
}

/** Valores do formulário de encerrar ordem, como o usuário digita (texto). */
export type ValoresEncerrarOrdem = z.input<ReturnType<typeof criarEsquemaEncerrarOrdem>>;

/** Quantos dígitos inteiros o custo digitado tem (o ponto de milhar não conta). */
function digitosInteirosDo(custoDigitado: string): number {
  return custoDigitado.trim().split(',')[0].replace(/\./g, '').length;
}

/**
 * Esquema de encerrar ordem. `hoje` limita a data de saída; `dataEntrada` é a da ordem: a saída não pode ser anterior.
 * A quilometragem tem de ser um número inteiro, zero ou mais.
 */
export function criarEsquemaEncerrarOrdem(hoje: string, dataEntrada: string) {
  return z.object({
    kmAtual: z
      .string()
      .refine((valor) => /^\d+$/.test(valor.trim()), { error: MENSAGEM_MANUTENCAO_ERRO_KM })
      .refine((valor) => !/^\d+$/.test(valor.trim()) || Number(valor.trim()) <= KM_MAXIMO_MANUTENCAO, {
        error: MENSAGEM_MANUTENCAO_ERRO_KM_LIMITE,
      }),
    dataSaida: z
      .string()
      .refine((valor) => valor === '' || valor <= hoje, { error: MENSAGEM_MANUTENCAO_ERRO_DATA_SAIDA_FUTURA })
      .refine((valor) => valor === '' || valor >= dataEntrada, { error: MENSAGEM_MANUTENCAO_ERRO_DATA_SAIDA_ANTERIOR }),
    custo: z.string().refine(
      (valor) =>
        valor.trim() === '' ||
        (converterMoedaPtBrParaNumero(valor) !== null && digitosInteirosDo(valor) <= DIGITOS_INTEIROS_CUSTO_MANUTENCAO),
      { error: MENSAGEM_MANUTENCAO_ERRO_CUSTO_PRECISAO },
    ),
  });
}

/** Corpo do PATCH: a quilometragem vira número; data e custo vazios não vão. */
export function montarRequisicaoEncerrar(valores: ValoresEncerrarOrdem): EncerrarManutencaoRequisicao {
  const requisicao: EncerrarManutencaoRequisicao = { kmAtual: Number(valores.kmAtual.trim()) };
  if (valores.dataSaida !== '') requisicao.dataSaida = valores.dataSaida;
  if (valores.custo.trim() !== '') {
    const custo = converterMoedaPtBrParaNumero(valores.custo);
    if (custo !== null) requisicao.custo = custo;
  }
  return requisicao;
}
