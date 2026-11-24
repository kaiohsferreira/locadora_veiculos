/** Prefixo do código de reserva (AV-00042). A leitura não diferencia maiúsculas de minúsculas. */
const PREFIXO_CODIGO = 'AV-';
/** Maior inteiro de 32 bits: o id da reserva é int no banco e na rota. */
const MAIOR_ID_RESERVA = 2147483647;

/**
 * Lê o código que o atendente digita na página de entrada: AV-00042, av-42, #AV-00042, 42 ou 00042. Devolve o id da reserva
 * ou null quando não é um código válido. Mesma leitura do servidor (RegrasReserva.LerCodigoDaBusca), mais a regra só do
 * front: o id tem de ser maior que zero, porque a rota da tela 16 aceita só id positivo. Validação de entrada antes de navegar.
 */
export function lerCodigoReserva(texto: string): number | null {
  let busca = texto.trim();
  if (busca.startsWith('#')) busca = busca.slice(1).trim();
  if (busca === '') return null;
  if (busca.toUpperCase().startsWith(PREFIXO_CODIGO)) busca = busca.slice(PREFIXO_CODIGO.length);
  if (!/^\d+$/.test(busca)) return null;
  const numero = Number(busca);
  return Number.isSafeInteger(numero) && numero > 0 && numero <= MAIOR_ID_RESERVA ? numero : null;
}
