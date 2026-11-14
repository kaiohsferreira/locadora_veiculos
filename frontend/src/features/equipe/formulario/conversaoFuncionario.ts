/**
 * Conversão entre o formulário e os VOs da API (sem React). Só valores já validados pelo esquema chegam aqui.
 *
 * - Cadastro: envia CPF só com dígitos e a senha inicial. A empresa nunca é enviada (vem do servidor).
 * - Edição: não envia CPF nem senha (o CPF é imutável e a senha não se troca por esta tela).
 * - A senha não é aparada: o que a pessoa digitou é o que vai.
 */
import { dataBrParaIso, isoParaDataBr } from '@/lib/datasBr';
import { somenteDigitos } from '@/lib/formatters';
import type { FuncionarioEquipe, RequisicaoCadastroFuncionario, RequisicaoEdicaoFuncionario } from '@/types/equipe';
import type { FormularioFuncionario } from './esquemaFuncionario';

/** Valores iniciais do cadastro: cargo Atendente, como no desenho. */
export function valoresIniciaisCadastro(): FormularioFuncionario {
  return {
    nome: '',
    cpf: '',
    matricula: '',
    email: '',
    dataAdmissao: '',
    cargo: 'ATENDENTE',
    senhaInicial: '',
    confirmarSenha: '',
  };
}

/** Valores iniciais da edição, vindos da consulta. CPF e senhas ficam vazios: não são editados aqui. */
export function valoresIniciaisEdicao(funcionario: FuncionarioEquipe): FormularioFuncionario {
  return {
    nome: funcionario.nome,
    cpf: '',
    matricula: funcionario.matricula,
    email: funcionario.email ?? '',
    dataAdmissao: isoParaDataBr(funcionario.dataAdmissao),
    cargo: funcionario.cargo,
    senhaInicial: '',
    confirmarSenha: '',
  };
}

/**
 * Corpo do POST. A data vai como "AAAA-MM-DD"; `?? ''` só existe para o tipo, pois o esquema já garantiu a data.
 */
export function montarRequisicaoCadastro(valores: FormularioFuncionario): RequisicaoCadastroFuncionario {
  return {
    nome: valores.nome.trim(),
    cpf: somenteDigitos(valores.cpf),
    matricula: valores.matricula.trim(),
    email: valores.email.trim(),
    dataAdmissao: dataBrParaIso(valores.dataAdmissao) ?? '',
    cargo: valores.cargo,
    senhaInicial: valores.senhaInicial,
  };
}

/** Corpo do PUT: só os campos editáveis. Sem CPF e sem senha. */
export function montarRequisicaoEdicao(valores: FormularioFuncionario): RequisicaoEdicaoFuncionario {
  return {
    nome: valores.nome.trim(),
    matricula: valores.matricula.trim(),
    email: valores.email.trim(),
    dataAdmissao: dataBrParaIso(valores.dataAdmissao) ?? '',
    cargo: valores.cargo,
  };
}
