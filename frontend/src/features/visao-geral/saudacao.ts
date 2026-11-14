/** Saudação pela hora local: manhã (5h às 11h59), tarde (12h às 17h59) e noite (demais horas). */
import { MENSAGEM_SAUDACAO_MANHA, MENSAGEM_SAUDACAO_NOITE, MENSAGEM_SAUDACAO_TARDE } from '@/lib/mensagens';

/** Saudação para a hora (0 a 23). */
export function saudacaoPorHora(hora: number): string {
  if (hora >= 5 && hora < 12) return MENSAGEM_SAUDACAO_MANHA;
  if (hora >= 12 && hora < 18) return MENSAGEM_SAUDACAO_TARDE;
  return MENSAGEM_SAUDACAO_NOITE;
}

/** Primeiro nome de um nome completo; ausente ou vazio devolve undefined. */
export function primeiroNome(nomeCompleto: string | undefined): string | undefined {
  const nome = nomeCompleto?.trim().split(/\s+/)[0];
  return nome ? nome : undefined;
}

/** "Bom dia, Marina" quando há nome; "Bom dia" quando não há. */
export function montarSaudacao(hora: number, nomeCompleto: string | undefined): string {
  const saudacao = saudacaoPorHora(hora);
  const nome = primeiroNome(nomeCompleto);
  return nome ? `${saudacao}, ${nome}` : saudacao;
}
