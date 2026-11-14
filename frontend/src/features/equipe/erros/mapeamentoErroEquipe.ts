/**
 * Erros das chamadas da equipe viram instruções para a tela: qual campo marcar e qual aviso mostrar. Sem React.
 *
 * - 400: cada chave de `errors` que é campo do formulário vai para o campo (sem diferença de maiúsculas).
 * - 409: o contrato não traz o campo, então a leitura é pelo texto (mesmo padrão do cadastro da locadora).
 *   CPF e matrícula vão para o campo; último administrador e alteração do próprio acesso viram aviso.
 * - 403 e 404: aviso com texto próprio. Rede, tempo esgotado e 500 já chegam com a mensagem pronta do cliente HTTP.
 * - Nunca mostra a mensagem técnica do backend: o texto só é usado se for uma das mensagens da equipe ou a
 *   mensagem já convertida pelo cliente HTTP.
 */
import { aplicarErrosDeCampo } from '@/lib/erros';
import {
  MENSAGEM_EQUIPE_ACESSO_NEGADO,
  MENSAGEM_EQUIPE_CPF_JA_CADASTRADO,
  MENSAGEM_EQUIPE_FUNCIONARIO_NAO_ENCONTRADO,
  MENSAGEM_EQUIPE_MATRICULA_JA_CADASTRADA,
  MENSAGEM_EQUIPE_PROPRIO_ACESSO,
  MENSAGEM_EQUIPE_ULTIMO_ADMINISTRADOR,
  MENSAGEM_ERRO_API_PADRAO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import {
  CAMPOS_FORMULARIO_FUNCIONARIO,
  type CampoFormularioFuncionario,
} from '@/features/equipe/formulario/esquemaFuncionario';

/** Mensagem por campo do formulário, já pronta para o campo. */
export type ErrosCamposFuncionario = Partial<Record<CampoFormularioFuncionario, string>>;

/** O que a tela faz com o erro: marca campos e/ou mostra um aviso geral. */
export interface ResultadoErroFuncionario {
  campos: ErrosCamposFuncionario;
  /** Aviso geral (null quando o erro foi para campos). */
  aviso: string | null;
}

export interface ContextoErroFuncionario {
  /** Verdadeiro quando o funcionário da operação é quem está logado. */
  ehPropriaConta: boolean;
}

function avisoGeral(mensagem: string): ResultadoErroFuncionario {
  return { campos: {}, aviso: mensagem };
}

/** 400: campos conhecidos recebem a mensagem do servidor. Sem campo aproveitável, aviso com a mensagem do cliente. */
function interpretarValidacao(erro: ErroApi): ResultadoErroFuncionario {
  const campos: ErrosCamposFuncionario = {};
  const marcou = aplicarErrosDeCampo(erro, CAMPOS_FORMULARIO_FUNCIONARIO, (campo, mensagem) => {
    campos[campo] = mensagem;
  });
  return marcou ? { campos, aviso: null } : avisoGeral(erro.mensagem);
}

/**
 * 409 pelo texto do conflito. A ordem importa: a mensagem do último administrador também cita "administrador",
 * então ela é lida pela frase completa. O aviso de "próprio acesso" só aparece quando é a própria conta e o texto
 * do 409 é esse; qualquer outro 409 desconhecido mostra o texto do servidor.
 */
function interpretarConflito(erro: ErroApi, contexto: ContextoErroFuncionario): ResultadoErroFuncionario {
  const mensagem = erro.mensagem;
  if (/\bCPF\b/.test(mensagem)) {
    return { campos: { cpf: MENSAGEM_EQUIPE_CPF_JA_CADASTRADO }, aviso: null };
  }
  if (/matr[ií]cula/i.test(mensagem)) {
    return { campos: { matricula: MENSAGEM_EQUIPE_MATRICULA_JA_CADASTRADA }, aviso: null };
  }
  if (/pelo menos um administrador ativo/i.test(mensagem)) {
    return avisoGeral(MENSAGEM_EQUIPE_ULTIMO_ADMINISTRADOR);
  }
  if (contexto.ehPropriaConta && /próprio acesso/i.test(mensagem)) {
    return avisoGeral(MENSAGEM_EQUIPE_PROPRIO_ACESSO);
  }
  return avisoGeral(mensagem);
}

/** Primeiro campo do formulário, na ordem da tela, que tem erro do servidor. Sem erro, undefined. */
export function primeiroCampoComErro(erros: ErrosCamposFuncionario): CampoFormularioFuncionario | undefined {
  return CAMPOS_FORMULARIO_FUNCIONARIO.find((campo) => erros[campo] !== undefined);
}

/**
 * Lê o erro de qualquer chamada da equipe (cadastro, edição, situação ou consulta). Nunca lança: o que não é `ErroApi`
 * vira a mensagem padrão.
 */
export function interpretarErroFuncionario(erro: unknown, contexto: ContextoErroFuncionario): ResultadoErroFuncionario {
  if (!(erro instanceof ErroApi)) return avisoGeral(MENSAGEM_ERRO_API_PADRAO);
  switch (erro.status) {
    case 400:
      return interpretarValidacao(erro);
    case 403:
      return avisoGeral(MENSAGEM_EQUIPE_ACESSO_NEGADO);
    case 404:
      return avisoGeral(MENSAGEM_EQUIPE_FUNCIONARIO_NAO_ENCONTRADO);
    case 409:
      return interpretarConflito(erro, contexto);
    default:
      return avisoGeral(erro.mensagem);
  }
}
