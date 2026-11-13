/**
 * Lógica pura do cadastro em duas telas (etapa 1: dados pessoais; etapa 2: documentos e senha).
 * Sem React e sem Axios: só decide de qual etapa é cada campo, separa os erros por etapa e junta
 * os dados das duas telas no formulário completo, que o envio final valida pelo esquema completo.
 */
import {
  ESQUEMA_CADASTRO,
  ESQUEMA_DADOS_PESSOAIS,
  type DadosPessoaisCadastro,
  type DocumentosCadastro,
  type FormularioCadastro,
  type FormularioDocumentos,
} from './esquemaCadastro';

/** Etapa do cadastro: 1 = dados pessoais, 2 = documentos e senha. */
export type EtapaCadastro = 1 | 2;

/** Campos da etapa 1. Todo outro campo do formulário pertence à etapa 2. */
export const CAMPOS_ETAPA_1: readonly (keyof FormularioCadastro)[] = [
  'nome',
  'cpf',
  'dataNascimento',
  'email',
  'telefone',
];

/** Campos da etapa 2, na ordem da tela. */
export const CAMPOS_ETAPA_2: readonly (keyof FormularioCadastro)[] = [
  'cnhNumero',
  'cnhCategoria',
  'cnhValidade',
  'cep',
  'logradouro',
  'numero',
  'complemento',
  'bairro',
  'cidade',
  'uf',
  'senha',
  'confirmarSenha',
];

/** Erro de um campo do cadastro (vindo da API ou da validação do formulário completo). */
export interface ErroCampoCadastro {
  campo: string;
  mensagem: string;
}

/** O campo é da etapa 1 (dados pessoais). */
export function ehCampoDaEtapa1(campo: string): campo is keyof DadosPessoaisCadastro {
  return (CAMPOS_ETAPA_1 as readonly string[]).includes(campo);
}

/** O campo é da etapa 2 (documentos e senha). */
export function ehCampoDaEtapa2(campo: string): campo is keyof FormularioDocumentos {
  return (CAMPOS_ETAPA_2 as readonly string[]).includes(campo);
}

/** De qual etapa é o campo. Null quando o nome não é de nenhum campo do cadastro. */
export function etapaDoCampo(campo: string): EtapaCadastro | null {
  if (ehCampoDaEtapa1(campo)) return 1;
  if (ehCampoDaEtapa2(campo)) return 2;
  return null;
}

/** Erros separados por etapa. `semEtapa` guarda os campos desconhecidos (viram alerta geral). */
export interface ErrosPorEtapa {
  etapa1: ErroCampoCadastro[];
  etapa2: ErroCampoCadastro[];
  semEtapa: ErroCampoCadastro[];
}

/** Separa os erros pela etapa do campo, mantendo a ordem em que chegaram. */
export function separarErrosPorEtapa(erros: readonly ErroCampoCadastro[]): ErrosPorEtapa {
  const resultado: ErrosPorEtapa = { etapa1: [], etapa2: [], semEtapa: [] };
  for (const erro of erros) {
    const etapa = etapaDoCampo(erro.campo);
    if (etapa === 1) resultado.etapa1.push(erro);
    else if (etapa === 2) resultado.etapa2.push(erro);
    else resultado.semEtapa.push(erro);
  }
  return resultado;
}

/** Etapa para onde o usuário deve ir: a primeira que tem erro (a 1 vem antes da 2). Null sem erros de campo. */
export function etapaDeDestino(erros: ErrosPorEtapa): EtapaCadastro | null {
  if (erros.etapa1.length > 0) return 1;
  if (erros.etapa2.length > 0) return 2;
  return null;
}

/** Valores do formulário da etapa 2 no momento do envio (a senha só existe lá e só chega até aqui no envio). */
export interface DadosEtapa2 extends DocumentosCadastro {
  senha: string;
  confirmarSenha: string;
}

/**
 * Junta os dados das duas etapas no formulário completo (o mesmo `FormularioCadastro` do envio de
 * sempre). A senha entra aqui, só no momento do envio, vinda do formulário da etapa 2.
 */
export function juntarFormulario(
  dadosPessoais: DadosPessoaisCadastro,
  dadosEtapa2: DadosEtapa2,
): FormularioCadastro {
  return {
    nome: dadosPessoais.nome,
    cpf: dadosPessoais.cpf,
    dataNascimento: dadosPessoais.dataNascimento,
    email: dadosPessoais.email,
    telefone: dadosPessoais.telefone,
    cnhNumero: dadosEtapa2.cnhNumero,
    cnhCategoria: dadosEtapa2.cnhCategoria,
    cnhValidade: dadosEtapa2.cnhValidade,
    cep: dadosEtapa2.cep,
    logradouro: dadosEtapa2.logradouro,
    numero: dadosEtapa2.numero,
    complemento: dadosEtapa2.complemento,
    bairro: dadosEtapa2.bairro,
    cidade: dadosEtapa2.cidade,
    uf: dadosEtapa2.uf,
    senha: dadosEtapa2.senha,
    confirmarSenha: dadosEtapa2.confirmarSenha,
  };
}

/** Só a parte da etapa 2 que pode ficar em memória entre as telas (CNH e endereço, nunca a senha). */
export function extrairDocumentos(valores: DocumentosCadastro): DocumentosCadastro {
  return {
    cnhNumero: valores.cnhNumero,
    cnhCategoria: valores.cnhCategoria,
    cnhValidade: valores.cnhValidade,
    cep: valores.cep,
    logradouro: valores.logradouro,
    numero: valores.numero,
    complemento: valores.complemento,
    bairro: valores.bairro,
    cidade: valores.cidade,
    uf: valores.uf,
  };
}

/** Resultado da validação do formulário completo antes do envio. */
export type ResultadoValidacaoCompleta =
  | { ok: true; formulario: FormularioCadastro }
  | { ok: false; erros: ErroCampoCadastro[] };

/**
 * Valida o formulário completo pelo esquema completo (o mesmo do backend). Cada etapa já validou a
 * sua parte, mas o envio final confere tudo de novo (ex.: a data mudou entre as telas). Um erro por
 * campo (o primeiro, na ordem das regras).
 */
export function validarCadastroCompleto(
  formulario: FormularioCadastro,
  esquema: typeof ESQUEMA_CADASTRO = ESQUEMA_CADASTRO,
): ResultadoValidacaoCompleta {
  const resultado = esquema.safeParse(formulario);
  if (resultado.success) return { ok: true, formulario };

  const erros: ErroCampoCadastro[] = [];
  for (const problema of resultado.error.issues) {
    const campo = problema.path[0];
    if (typeof campo !== 'string' || erros.some((erro) => erro.campo === campo)) continue;
    erros.push({ campo, mensagem: problema.message });
  }
  return { ok: false, erros };
}

/** Quais campos da etapa 1 têm hoje um valor válido (mostram o ícone de "conferido"). */
export type CamposValidosDadosPessoais = Readonly<Record<keyof DadosPessoaisCadastro, boolean>>;

/**
 * Confere cada campo da etapa 1 pelas mesmas regras do esquema da etapa 1. Campo vazio ou ausente
 * nunca é válido. Serve só para o ícone de "conferido": a validação de verdade é a do envio.
 */
export function calcularCamposValidos(
  valores: Partial<DadosPessoaisCadastro>,
  esquema: typeof ESQUEMA_DADOS_PESSOAIS = ESQUEMA_DADOS_PESSOAIS,
): CamposValidosDadosPessoais {
  const campoValido = (campo: keyof DadosPessoaisCadastro): boolean =>
    esquema.shape[campo].safeParse(valores[campo] ?? '').success;
  return {
    nome: campoValido('nome'),
    cpf: campoValido('cpf'),
    dataNascimento: campoValido('dataNascimento'),
    email: campoValido('email'),
    telefone: campoValido('telefone'),
  };
}

/** Campos que o backend pode apontar em um 400 (os nomes do ClienteVO): todos, menos a confirmação de senha (só no front). */
export const CAMPOS_PAYLOAD_API: readonly (keyof FormularioCadastro)[] = [
  ...CAMPOS_ETAPA_1,
  ...CAMPOS_ETAPA_2.filter((campo) => campo !== 'confirmarSenha'),
];

/**
 * Quais erros pendentes (vindos da API) a tela da etapa deve aplicar agora. A etapa 1 aplica os seus.
 * A etapa 2 só aplica os seus quando não resta erro pendente da etapa 1: a pessoa está indo resolver a
 * etapa 1 primeiro, e os da etapa 2 esperam a vez (assim não se perdem se a etapa 2 ainda estiver montada).
 */
export function errosParaAplicarNaEtapa(
  etapa: EtapaCadastro,
  pendentes: readonly ErroCampoCadastro[],
): ErroCampoCadastro[] {
  const separados = separarErrosPorEtapa(pendentes);
  if (etapa === 1) return separados.etapa1;
  return separados.etapa1.length > 0 ? [] : separados.etapa2;
}
