/**
 * Validação do formulário de veículo (cadastro e edição), com Zod. Espelha o VeiculoVOValidator do servidor: as mesmas
 * regras, na mesma ordem, e a mesma frase para cada uma. Cada campo para na primeira regra que falha.
 *
 * Os valores do formulário são todos texto (como o campo digita). A conversão para o pedido está em `conversaoVeiculo`.
 * O servidor valida de novo e é a palavra final.
 */
import { z } from 'zod';
import { ehDataIsoFutura, ehDataIsoValida } from '@/lib/validadores';
import { converterMoedaPtBrParaNumero } from '@/lib/formatters';
import {
  anoLimiteVeiculo,
  LIMITES_VEICULO,
  PADRAO_CHASSI_VEICULO,
  PADRAO_PLACA_VEICULO,
  PADRAO_RENAVAM_VEICULO,
} from '@/lib/limitesVeiculo';
import { CARACTERE_NULO, normalizarPlacaParaServidor } from '@/lib/normalizacaoVeiculo';
import {
  MENSAGEM_VEICULO_ANO_MODELO_ANTERIOR,
  MENSAGEM_VEICULO_CATEGORIA_OBRIGATORIA,
  MENSAGEM_VEICULO_CHASSI_INVALIDO,
  MENSAGEM_VEICULO_DATA_FUTURA,
  MENSAGEM_VEICULO_DATA_INVALIDA,
  MENSAGEM_VEICULO_KM_OBRIGATORIO,
  MENSAGEM_VEICULO_MARCA_OBRIGATORIA,
  MENSAGEM_VEICULO_MODELO_OBRIGATORIO,
  MENSAGEM_VEICULO_NOME_CAMPO_NOTA_FISCAL,
  MENSAGEM_VEICULO_PLACA_INVALIDA,
  MENSAGEM_VEICULO_RENAVAM_INVALIDO,
  MENSAGEM_VEICULO_VALOR_DIARIA,
  MENSAGEM_VEICULO_VALOR_DIARIA_PRECISAO,
  MENSAGEM_VEICULO_VALOR_VENDA,
  MENSAGEM_VEICULO_VALOR_VENDA_PRECISAO,
  mensagemVeiculoAnoFabricacaoIntervalo,
  mensagemVeiculoAnoModeloMaximo,
  mensagemVeiculoCampoInvalido,
  mensagemVeiculoCorTamanho,
  mensagemVeiculoFornecedorTamanho,
  mensagemVeiculoMarcaTamanho,
  mensagemVeiculoModeloTamanho,
  mensagemVeiculoNotaFiscalTamanho,
} from '@/lib/mensagensVeiculos';

/** Valores do formulário, todos em texto como a tela os guarda. */
export interface ValoresFormularioVeiculo {
  idCategoria: string;
  placa: string;
  chassi: string;
  renavam: string;
  marca: string;
  modelo: string;
  anoFabricacao: string;
  anoModelo: string;
  cor: string;
  kmAtual: string;
  valorDiaria: string;
  valorVenda: string;
  /** Data "AAAA-MM-DD" (campo de data) ou vazio. */
  dataAquisicao: string;
  /** Fornecedor ou concessionária de compra, texto livre ou vazio. */
  fornecedor: string;
  /** Número da nota fiscal de compra, texto livre ou vazio. */
  numeroNotaFiscalCompra: string;
}

/**
 * Campos do formulário, na ordem da tela (desenho 05): identificação, parâmetros comerciais e aquisição. Os erros do
 * servidor (400 e 409) são aplicados a estes campos, e o primeiro com erro na lista é o que recebe o foco.
 */
export const CAMPOS_FORMULARIO_VEICULO = [
  'placa',
  'chassi',
  'renavam',
  'marca',
  'modelo',
  'anoFabricacao',
  'anoModelo',
  'cor',
  'idCategoria',
  'valorDiaria',
  'valorVenda',
  'dataAquisicao',
  'kmAtual',
  'fornecedor',
  'numeroNotaFiscalCompra',
] as const satisfies readonly (keyof ValoresFormularioVeiculo)[];

/** Um campo do formulário de veículo. */
export type CampoFormularioVeiculo = (typeof CAMPOS_FORMULARIO_VEICULO)[number];

/** Regra de um campo: o texto passa ou não, e a frase que aparece quando não passa. */
interface RegraCampo {
  passa: (valor: string) => boolean;
  mensagem: string;
}

/** Ano digitado como número; texto que não é ano vale zero (como o servidor lê um campo vazio). */
function lerAno(texto: string): number {
  return /^\d+$/.test(texto.trim()) ? Number(texto.trim()) : 0;
}

/** Não tem mais casas decimais do que o limite (zeros à direita não contam). */
function casasDecimaisDentroDoLimite(texto: string, maximo: number): boolean {
  const decimais = (texto.split(',')[1] ?? '').replace(/0+$/, '');
  return decimais.length <= maximo;
}

/** Não tem mais dígitos inteiros do que o limite (zeros à esquerda não contam). */
function digitosInteirosDentroDoLimite(texto: string, maximo: number): boolean {
  const inteiros = (texto.split(',')[0] ?? '').replace(/\./g, '').replace(/^0+/, '');
  return inteiros.length <= maximo;
}

/**
 * Valor em reais acima de zero, com as casas e os dígitos dentro dos limites da coluna. Vazio ou fora do formato
 * pt-BR não passa (o servidor lê como zero, então a frase é a de "maior que zero").
 */
function valorEmReaisValido(
  mensagemMinimo: string,
  digitosInteiros: number,
  mensagemPrecisao: string,
): RegraCampo[] {
  return [
    {
      passa: (valor) => {
        const numero = converterMoedaPtBrParaNumero(valor);
        return numero !== null && numero > 0;
      },
      mensagem: mensagemMinimo,
    },
    {
      passa: (valor) =>
        casasDecimaisDentroDoLimite(valor.trim(), LIMITES_VEICULO.casasDecimaisValor) &&
        digitosInteirosDentroDoLimite(valor.trim(), digitosInteiros),
      mensagem: mensagemPrecisao,
    },
  ];
}

/** Aplica as regras na ordem e acrescenta a primeira que falhar. Sem falha, não acrescenta nada. */
function regrasDoCampo(regras: readonly RegraCampo[]) {
  return (valor: string, ctx: z.RefinementCtx): void => {
    const falha = regras.find((regra) => !regra.passa(valor));
    if (falha) ctx.addIssue({ code: 'custom', message: falha.mensagem, input: valor });
  };
}

/** Regras de marca e modelo (texto obrigatório, até o limite, sem caractere nulo), na ordem do servidor. */
function regrasTexto(obrigatoria: string, maximo: number, mensagemTamanho: string, nomeCampo: string): RegraCampo[] {
  return [
    { passa: (valor) => valor.trim().length > 0, mensagem: obrigatoria },
    { passa: (valor) => valor.trim().length <= maximo, mensagem: mensagemTamanho },
    { passa: (valor) => !valor.includes(CARACTERE_NULO), mensagem: mensagemVeiculoCampoInvalido(nomeCampo) },
  ];
}

/** Regras de texto opcional quando preenchido (até o limite, sem caractere nulo), como o campo cor. */
function regrasTextoOpcional(maximo: number, mensagemTamanho: string, nomeCampo: string): RegraCampo[] {
  return [
    { passa: (valor) => valor.trim().length <= maximo, mensagem: mensagemTamanho },
    { passa: (valor) => !valor.includes(CARACTERE_NULO), mensagem: mensagemVeiculoCampoInvalido(nomeCampo) },
  ];
}

/**
 * Esquema do formulário. A data de referência é injetada (padrão: agora), para os anos e a data de aquisição
 * não dependerem do relógio nos testes. Os anos do modelo são conferidos entre si no fim, com o campo de fabricação.
 */
export function criarEsquemaVeiculo(hoje: Date = new Date()) {
  const anoLimite = anoLimiteVeiculo(hoje);
  const anoFabricacaoValido = (texto: string) => {
    const ano = lerAno(texto);
    return ano >= LIMITES_VEICULO.anoFabricacaoMinimo && ano <= anoLimite;
  };
  const mensagemIntervaloAno = mensagemVeiculoAnoFabricacaoIntervalo(LIMITES_VEICULO.anoFabricacaoMinimo, anoLimite);

  const esquema = z.object({
    idCategoria: z.string().superRefine(
      regrasDoCampo([
        {
          passa: (valor) => /^\d+$/.test(valor.trim()) && Number(valor.trim()) > 0,
          mensagem: MENSAGEM_VEICULO_CATEGORIA_OBRIGATORIA,
        },
      ]),
    ),

    placa: z.string().superRefine(
      regrasDoCampo([
        {
          passa: (valor) => PADRAO_PLACA_VEICULO.test(normalizarPlacaParaServidor(valor)),
          mensagem: MENSAGEM_VEICULO_PLACA_INVALIDA,
        },
      ]),
    ),

    chassi: z.string().superRefine(
      regrasDoCampo([
        {
          passa: (valor) => PADRAO_CHASSI_VEICULO.test(valor.trim().toUpperCase()),
          mensagem: MENSAGEM_VEICULO_CHASSI_INVALIDO,
        },
      ]),
    ),

    renavam: z.string().superRefine(
      regrasDoCampo([
        { passa: (valor) => PADRAO_RENAVAM_VEICULO.test(valor.trim()), mensagem: MENSAGEM_VEICULO_RENAVAM_INVALIDO },
      ]),
    ),

    marca: z.string().superRefine(
      regrasDoCampo(
        regrasTexto(MENSAGEM_VEICULO_MARCA_OBRIGATORIA, LIMITES_VEICULO.marca, mensagemVeiculoMarcaTamanho(LIMITES_VEICULO.marca), 'Marca'),
      ),
    ),

    modelo: z.string().superRefine(
      regrasDoCampo(
        regrasTexto(
          MENSAGEM_VEICULO_MODELO_OBRIGATORIO,
          LIMITES_VEICULO.modelo,
          mensagemVeiculoModeloTamanho(LIMITES_VEICULO.modelo),
          'Modelo',
        ),
      ),
    ),

    anoFabricacao: z.string().superRefine(
      regrasDoCampo([{ passa: anoFabricacaoValido, mensagem: mensagemIntervaloAno }]),
    ),

    // Intervalo com o próprio ano e comparação com o de fabricação: feitos no superRefine do objeto.
    anoModelo: z.string(),

    cor: z.string().superRefine((valor, ctx) => {
      if (valor.trim().length === 0) return;
      regrasDoCampo([
        { passa: (texto) => texto.trim().length <= LIMITES_VEICULO.cor, mensagem: mensagemVeiculoCorTamanho(LIMITES_VEICULO.cor) },
        { passa: (texto) => !texto.includes(CARACTERE_NULO), mensagem: mensagemVeiculoCampoInvalido('Cor') },
      ])(valor, ctx);
    }),

    // Obrigatório na tela (desenho 05); zero é valor válido. Digitado só com dígitos (ver normalizarKmDigitado).
    kmAtual: z.string().superRefine(
      regrasDoCampo([{ passa: (valor) => valor.trim().length > 0, mensagem: MENSAGEM_VEICULO_KM_OBRIGATORIO }]),
    ),

    valorDiaria: z.string().superRefine(
      regrasDoCampo(
        valorEmReaisValido(
          MENSAGEM_VEICULO_VALOR_DIARIA,
          LIMITES_VEICULO.digitosInteirosValorDiaria,
          MENSAGEM_VEICULO_VALOR_DIARIA_PRECISAO,
        ),
      ),
    ),

    valorVenda: z.string().superRefine((valor, ctx) => {
      if (valor.trim().length === 0) return;
      regrasDoCampo(
        valorEmReaisValido(
          MENSAGEM_VEICULO_VALOR_VENDA,
          LIMITES_VEICULO.digitosInteirosValorVenda,
          MENSAGEM_VEICULO_VALOR_VENDA_PRECISAO,
        ),
      )(valor, ctx);
    }),

    dataAquisicao: z.string().superRefine((valor, ctx) => {
      if (valor.trim().length === 0) return;
      regrasDoCampo([
        { passa: ehDataIsoValida, mensagem: MENSAGEM_VEICULO_DATA_INVALIDA },
        { passa: (texto) => !ehDataIsoFutura(texto, hoje), mensagem: MENSAGEM_VEICULO_DATA_FUTURA },
      ])(valor, ctx);
    }),

    // Opcionais, como o cor: vazio passa; quando preenchidos, até o limite e sem caractere nulo.
    fornecedor: z.string().superRefine((valor, ctx) => {
      if (valor.trim().length === 0) return;
      regrasDoCampo(regrasTextoOpcional(LIMITES_VEICULO.fornecedor, mensagemVeiculoFornecedorTamanho(LIMITES_VEICULO.fornecedor), 'Fornecedor'))(valor, ctx);
    }),

    numeroNotaFiscalCompra: z.string().superRefine((valor, ctx) => {
      if (valor.trim().length === 0) return;
      regrasDoCampo(
        regrasTextoOpcional(
          LIMITES_VEICULO.numeroNotaFiscalCompra,
          mensagemVeiculoNotaFiscalTamanho(LIMITES_VEICULO.numeroNotaFiscalCompra),
          MENSAGEM_VEICULO_NOME_CAMPO_NOTA_FISCAL,
        ),
      )(valor, ctx);
    }),
  });

  return esquema.superRefine((valores, ctx) => {
    const modelo = lerAno(valores.anoModelo);
    if (modelo < lerAno(valores.anoFabricacao)) {
      ctx.addIssue({ code: 'custom', message: MENSAGEM_VEICULO_ANO_MODELO_ANTERIOR, path: ['anoModelo'] });
      return;
    }
    if (modelo > anoLimite) {
      ctx.addIssue({
        code: 'custom',
        message: mensagemVeiculoAnoModeloMaximo(anoLimite),
        path: ['anoModelo'],
      });
    }
  });
}

/** Esquema com a data de hoje (uso da tela). */
export const ESQUEMA_VEICULO = criarEsquemaVeiculo();
