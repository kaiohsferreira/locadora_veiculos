/**
 * Regras de cada linha da tabela, sem React. Decidem quais ações aparecem e como o nome vira iniciais no avatar.
 * O servidor também recusa (409); a tela só não oferece a ação.
 */
import type { FuncionarioEquipe } from '@/types/equipe';

/**
 * Desativar aparece só para quem está ativo e não é o próprio usuário (a linha "Você" não tem essa ação).
 */
export function podeDesativar(funcionario: FuncionarioEquipe): boolean {
  return funcionario.ativo && !funcionario.ehVoce;
}

/** Reativar aparece para quem está inativo. */
export function podeReativar(funcionario: FuncionarioEquipe): boolean {
  return !funcionario.ativo;
}

/**
 * Iniciais do avatar: primeira letra do primeiro nome e do último sobrenome, em maiúsculas
 * ("Marina Costa" → "MC"; "Ana" → "A"). Nome vazio devolve string vazia.
 */
export function iniciaisDoNome(nome: string): string {
  const palavras = nome.trim().split(/\s+/).filter((palavra) => palavra.length > 0);
  if (palavras.length === 0) return '';
  const primeira = palavras[0].charAt(0);
  const ultima = palavras.length > 1 ? palavras[palavras.length - 1].charAt(0) : '';
  return `${primeira}${ultima}`.toLocaleUpperCase('pt-BR');
}
