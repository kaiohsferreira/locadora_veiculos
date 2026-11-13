/**
 * Conversão pura do formulário de cadastro para o corpo do POST /api/clientes.
 * Mesma normalização do ClienteService do backend: textos com trim, máscaras
 * removidas, e-mail em minúsculas, UF em maiúsculas e opcionais vazios em null.
 *
 * A confirmação de senha nunca entra no resultado.
 */
import { somenteDigitos } from '@/lib/formatters';
import type { RequisicaoCadastroCliente } from '@/types/clientes';
import { ehCategoriaCnh, type FormularioCadastro } from './esquemaCadastro';

/** Texto sem as bordas; vazio vira null. */
function textoOuNulo(valor: string): string | null {
  const limpo = valor.trim();
  return limpo === '' ? null : limpo;
}

/** Só os dígitos; vazio (ou sem dígitos) vira null. */
function digitosOuNulo(valor: string): string | null {
  const digitos = somenteDigitos(valor);
  return digitos === '' ? null : digitos;
}

/**
 * Monta o corpo da API a partir de um formulário já validado pelo esquema.
 * Lança erro só se a categoria da CNH estiver vazia, o que a validação impede.
 */
export function converterFormularioParaRequisicao(
  formulario: FormularioCadastro,
): RequisicaoCadastroCliente {
  if (!ehCategoriaCnh(formulario.cnhCategoria)) {
    throw new Error('Categoria da CNH ausente: o formulário deveria ter sido validado antes.');
  }

  return {
    nome: formulario.nome.trim(),
    cpf: somenteDigitos(formulario.cpf),
    dataNascimento: formulario.dataNascimento,
    email: formulario.email.trim().toLowerCase(),
    telefone: somenteDigitos(formulario.telefone),
    cnhNumero: somenteDigitos(formulario.cnhNumero),
    cnhCategoria: formulario.cnhCategoria,
    cnhValidade: formulario.cnhValidade,
    logradouro: textoOuNulo(formulario.logradouro),
    numero: textoOuNulo(formulario.numero),
    complemento: textoOuNulo(formulario.complemento),
    bairro: textoOuNulo(formulario.bairro),
    cidade: textoOuNulo(formulario.cidade),
    uf: textoOuNulo(formulario.uf)?.toUpperCase() ?? null,
    cep: digitosOuNulo(formulario.cep),
    senha: formulario.senha,
  };
}
