/**
 * Conversão pura dos dados da empresa (etapa 1) para a parte empresa do corpo do POST /api/empresas.
 * A mesma normalização do backend: textos com trim, máscaras removidas, e-mail em minúsculas, UF em
 * maiúsculas e o complemento vazio em null. A etapa 2 (responsável e senha) completa o corpo depois.
 */
import { somenteDigitos } from '@/lib/formatters';
import type { DadosEmpresaCadastro, RequisicaoCadastroLocadora } from '@/types/empresas';
import type { FormularioEmpresa } from './esquemaLocadora';
import type { FormularioResponsavel } from './esquemaResponsavel';

/** Texto sem as bordas; vazio vira null. */
function textoOuNulo(valor: string): string | null {
  const limpo = valor.trim();
  return limpo === '' ? null : limpo;
}

/** Monta a parte empresa do corpo a partir de um formulário já validado pelo esquema. */
export function converterDadosEmpresa(formulario: FormularioEmpresa): DadosEmpresaCadastro {
  return {
    razaoSocial: formulario.razaoSocial.trim(),
    nomeFantasia: formulario.nomeFantasia.trim(),
    cnpj: somenteDigitos(formulario.cnpj),
    telefone: somenteDigitos(formulario.telefone),
    email: formulario.email.trim().toLowerCase(),
    cep: somenteDigitos(formulario.cep),
    logradouro: formulario.logradouro.trim(),
    numero: formulario.numero.trim(),
    complemento: textoOuNulo(formulario.complemento),
    bairro: formulario.bairro.trim(),
    cidade: formulario.cidade.trim(),
    uf: formulario.uf.trim().toUpperCase(),
  };
}

/**
 * Corpo completo do POST /api/empresas: a parte da empresa (etapa 1) mais o responsável e a senha (etapa 2).
 * Normalização igual à do backend: CPF só dígitos, nome e e-mail aparados (e-mail em minúsculas). A senha
 * vai como foi digitada (sem trim). `aceiteTermos` vai como `true` quando marcado; o esquema já exige isso.
 */
export function montarRequisicaoCadastroLocadora(
  dadosEmpresa: FormularioEmpresa,
  responsavel: FormularioResponsavel,
): RequisicaoCadastroLocadora {
  return {
    ...converterDadosEmpresa(dadosEmpresa),
    responsavelNome: responsavel.nome.trim(),
    responsavelCpf: somenteDigitos(responsavel.cpf),
    responsavelEmail: responsavel.email.trim().toLowerCase(),
    senha: responsavel.senha,
    aceiteTermos: responsavel.aceiteTermos === true,
  };
}
