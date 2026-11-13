/**
 * Conversão da resposta plana do login com Google (ReturnGoogleLoginVO) na união discriminada
 * `ResultadoLoginGoogle`, usada pelas telas. Função pura: sem React, sem API.
 *
 * Combinações incoerentes (ex.: AUTENTICADO sem sessão, CONFIRMACAO sem e-mail, sessão fora de
 * AUTENTICADO) devolvem null: quem chama trata como erro genérico. Campos nulos e ausentes valem igual.
 */
import {
  PERFIS_API,
  type RespostaLogin,
  type ResultadoLoginGoogle,
  type ReturnGoogleLoginVO,
} from '@/types/autenticacao';

/** Texto com algo além de espaços, ou null (ausente, nulo, vazio ou de outro tipo). */
function textoOuNulo(valor: unknown): string | null {
  return typeof valor === 'string' && valor.trim() !== '' ? valor : null;
}

/** Confere a forma mínima da sessão que o login devolve (ReturnLoginVO). */
export function ehRespostaLoginValida(valor: unknown): valor is RespostaLogin {
  if (typeof valor !== 'object' || valor === null) return false;
  const sessao = valor as Record<string, unknown>;
  return (
    textoOuNulo(sessao.token) !== null &&
    typeof sessao.expiraEm === 'string' &&
    typeof sessao.perfil === 'string' &&
    (PERFIS_API as readonly string[]).includes(sessao.perfil) &&
    typeof sessao.idUsuario === 'number' &&
    Number.isFinite(sessao.idUsuario) &&
    typeof sessao.nome === 'string'
  );
}

/**
 * Converte a resposta de POST /api/auth/google/login. Devolve null quando a combinação
 * de situação, sessão, nome e e-mail não bate com as regras de cada situação.
 */
export function converterResultadoLoginGoogle(resposta: ReturnGoogleLoginVO): ResultadoLoginGoogle | null {
  const situacao: unknown = resposta.situacao;
  const sessao: unknown = resposta.sessao ?? null;
  const nome = textoOuNulo(resposta.nome);
  const email = textoOuNulo(resposta.email);

  if (situacao === 'AUTENTICADO') {
    if (!ehRespostaLoginValida(sessao)) return null;
    return { situacao: 'AUTENTICADO', sessao };
  }

  // Só AUTENTICADO traz sessão. Nas demais situações, sessão preenchida é incoerente.
  if (sessao !== null) return null;

  if (situacao === 'CONFIRMACAO_NECESSARIA' || situacao === 'CADASTRO_NECESSARIO') {
    // Sem e-mail não há o que vincular nem pré-preencher.
    if (email === null) return null;
    return { situacao, nome, email };
  }

  if (situacao === 'CONTA_AMBIGUA') {
    // Nenhum cadastro foi escolhido: nome e e-mail não podem vir.
    if (nome !== null || email !== null) return null;
    return { situacao: 'CONTA_AMBIGUA' };
  }

  return null;
}
