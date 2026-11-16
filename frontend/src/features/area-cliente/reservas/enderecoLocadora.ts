import type { ReservaCliente } from '@/types/reservas';

/** Campos de endereço da locadora dentro da reserva. */
export type CamposEnderecoLocadora = Pick<
  ReservaCliente,
  'locadoraLogradouro' | 'locadoraNumero' | 'locadoraBairro' | 'locadoraCidade' | 'locadoraUf'
>;

function aparar(valor: string | null | undefined): string {
  return (valor ?? '').trim();
}

/** "Rua X, 100 - Bairro". Partes vazias não geram vírgula nem traço soltos. */
export function montarEnderecoLocadora(reserva: CamposEnderecoLocadora): string {
  const rua = [aparar(reserva.locadoraLogradouro), aparar(reserva.locadoraNumero)].filter(Boolean).join(', ');
  return [rua, aparar(reserva.locadoraBairro)].filter(Boolean).join(' - ');
}

/** "Cidade - UF". */
export function montarCidadeUf(reserva: CamposEnderecoLocadora): string {
  return [aparar(reserva.locadoraCidade), aparar(reserva.locadoraUf)].filter(Boolean).join(' - ');
}

/** Link para o mapa com o endereço da locadora (abre em outra aba). */
export function montarLinkMapa(reserva: CamposEnderecoLocadora): string {
  const endereco = [montarEnderecoLocadora(reserva), montarCidadeUf(reserva)].filter(Boolean).join(', ');
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(endereco)}`;
}
