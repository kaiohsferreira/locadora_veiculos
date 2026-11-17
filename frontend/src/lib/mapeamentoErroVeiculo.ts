/**
 * Erros das chamadas de veículo viram instruções para a tela: qual campo marcar e qual aviso mostrar. Sem React.
 *
 * - 400: cada chave de `errors` que é campo do formulário vai para o campo (sem diferença de maiúsculas). Sem campo
 *   aproveitável, o aviso geral.
 * - 409: o corpo não traz o campo, então a leitura é pelo texto (ver TEXTO_SERVIDOR_* em `mensagensVeiculos`).
 *   Placa, chassi e renavam vão para o campo; qualquer outro 409 mostra o texto do servidor.
 * - 404: se for o próprio veículo, a tela sabe (`veiculoNaoEncontrado`); senão (ex.: categoria) é aviso geral.
 * - Nunca mostra a mensagem técnica: o cliente HTTP já filtrou e entregou a mensagem pronta.
 */
import { aplicarErrosDeCampo } from '@/lib/erros';
import { MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import { CAMPOS_FORMULARIO_VEICULO, type CampoFormularioVeiculo } from '@/lib/esquemaVeiculo';
import {
  TEXTO_SERVIDOR_VEICULO_CHASSI_DUPLICADO,
  TEXTO_SERVIDOR_VEICULO_NAO_ENCONTRADO,
  TEXTO_SERVIDOR_VEICULO_PLACA_DUPLICADA,
  TEXTO_SERVIDOR_VEICULO_RENAVAM_DUPLICADO,
} from '@/lib/mensagensVeiculos';
import { ErroApi } from '@/types/api';

/** Mensagem por campo do formulário, já pronta para o campo. */
export type ErrosCamposVeiculo = Partial<Record<CampoFormularioVeiculo, string>>;

/** O que a tela faz com o erro: marca campos e/ou mostra um aviso geral. */
export interface ResultadoErroVeiculo {
  campos: ErrosCamposVeiculo;
  /** Aviso geral (nulo quando o erro foi para campos). */
  aviso: string | null;
  /** Verdadeiro quando o veículo pedido não existe (ou é de outra locadora): a tela mostra o estado de erro. */
  veiculoNaoEncontrado: boolean;
}

/** Campo do formulário que cada texto de duplicidade do servidor marca. */
const CAMPO_POR_DUPLICIDADE: ReadonlyArray<readonly [string, CampoFormularioVeiculo]> = [
  [TEXTO_SERVIDOR_VEICULO_PLACA_DUPLICADA, 'placa'],
  [TEXTO_SERVIDOR_VEICULO_CHASSI_DUPLICADO, 'chassi'],
  [TEXTO_SERVIDOR_VEICULO_RENAVAM_DUPLICADO, 'renavam'],
];

function avisoGeral(mensagem: string): ResultadoErroVeiculo {
  return { campos: {}, aviso: mensagem, veiculoNaoEncontrado: false };
}

/** 400: os campos conhecidos recebem a mensagem do servidor. Sem campo aproveitável, aviso geral. */
function interpretarValidacao(erro: ErroApi): ResultadoErroVeiculo {
  const campos: ErrosCamposVeiculo = {};
  const marcou = aplicarErrosDeCampo(erro, CAMPOS_FORMULARIO_VEICULO, (campo, mensagem) => {
    campos[campo] = mensagem;
  });
  return marcou ? { campos, aviso: null, veiculoNaoEncontrado: false } : avisoGeral(erro.mensagem);
}

/** 409: placa, chassi e renavam pelo texto exato; outro conflito mostra o texto do servidor. */
function interpretarConflito(erro: ErroApi): ResultadoErroVeiculo {
  const duplicidade = CAMPO_POR_DUPLICIDADE.find(([texto]) => texto === erro.mensagem);
  if (duplicidade) {
    return { campos: { [duplicidade[1]]: erro.mensagem }, aviso: null, veiculoNaoEncontrado: false };
  }
  return avisoGeral(erro.mensagem);
}

/** 404: o veículo pedido não existe (ou não é desta locadora), ou é outro recurso (ex.: categoria). */
function interpretarNaoEncontrado(erro: ErroApi): ResultadoErroVeiculo {
  return {
    campos: {},
    aviso: erro.mensagem,
    veiculoNaoEncontrado: erro.mensagem === TEXTO_SERVIDOR_VEICULO_NAO_ENCONTRADO,
  };
}

/**
 * Lê o erro de qualquer chamada de veículo (consulta, cadastro ou edição). Nunca lança: o que não é `ErroApi`
 * vira a mensagem padrão.
 */
export function interpretarErroVeiculo(erro: unknown): ResultadoErroVeiculo {
  if (!(erro instanceof ErroApi)) return avisoGeral(MENSAGEM_ERRO_API_PADRAO);
  switch (erro.status) {
    case 400:
      return interpretarValidacao(erro);
    case 404:
      return interpretarNaoEncontrado(erro);
    case 409:
      return interpretarConflito(erro);
    default:
      return avisoGeral(erro.mensagem);
  }
}

/** Primeiro campo do formulário, na ordem da tela, que tem erro. Sem erro, nulo. */
export function primeiroCampoVeiculoComErro(erros: ErrosCamposVeiculo): CampoFormularioVeiculo | null {
  return CAMPOS_FORMULARIO_VEICULO.find((campo) => erros[campo] !== undefined) ?? null;
}
