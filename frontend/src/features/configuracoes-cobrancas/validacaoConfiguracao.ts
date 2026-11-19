import { z } from 'zod';
import type { ConfiguracaoCobrancasExtras, DadosConfiguracaoCobrancasExtras } from '@/types/configuracoesCobrancasExtras';
import { MENSAGENS_COBRANCAS_EXTRAS as M } from '@/lib/mensagensCobrancasExtras';

export interface CamposConfiguracaoCobrancas {
  idCategoria: string; baseFranquia: string; franquiaKm: string; valorKmExcedente: string;
  valorTanqueCompleto: string; toleranciaAtrasoMinutos: string; unidadeAtraso: string;
  fracionamentoAtraso: string; valorUnidadeAtraso: string; baseAvarias: string;
}
const inteiro = z.string().trim().refine(v => /^\d+$/.test(v) && Number(v) <= 2_147_483_647, M.inteiro);
const dinheiro = z.string().trim().refine(v => /^\d+(?:[,.]\d{1,2})?$/.test(v) && Number(v.replace(',', '.')) > 0 && Number(v.replace(',', '.')) <= 99_999_999.99, M.valor);
export const esquemaConfiguracaoCobrancas = z.object({
  idCategoria: inteiro.refine(v => Number(v) > 0, M.modo),
  baseFranquia: z.string().refine(v => v === 'TOTAL' || v === 'POR_DIARIA', M.modo),
  franquiaKm: inteiro, valorKmExcedente: dinheiro, valorTanqueCompleto: dinheiro,
  toleranciaAtrasoMinutos: inteiro,
  unidadeAtraso: z.string().refine(v => v === 'HORA' || v === 'DIARIA', M.modo),
  fracionamentoAtraso: z.string().refine(v => v === 'PROPORCIONAL' || v === 'INICIADA', M.modo),
  valorUnidadeAtraso: dinheiro,
  baseAvarias: z.string().refine(v => v === 'ESTIMATIVA_VISTORIA', M.modo),
});
export function camposConfiguracaoCobrancas(c?: ConfiguracaoCobrancasExtras): CamposConfiguracaoCobrancas {
  if (!c) return { idCategoria: '', baseFranquia: '', franquiaKm: '', valorKmExcedente: '', valorTanqueCompleto: '', toleranciaAtrasoMinutos: '', unidadeAtraso: '', fracionamentoAtraso: '', valorUnidadeAtraso: '', baseAvarias: '' };
  return { idCategoria: String(c.idCategoria), baseFranquia: c.baseFranquia, franquiaKm: String(c.franquiaKm), valorKmExcedente: String(c.valorKmExcedente).replace('.', ','), valorTanqueCompleto: String(c.valorTanqueCompleto).replace('.', ','), toleranciaAtrasoMinutos: String(c.toleranciaAtrasoMinutos), unidadeAtraso: c.unidadeAtraso, fracionamentoAtraso: c.fracionamentoAtraso, valorUnidadeAtraso: String(c.valorUnidadeAtraso).replace('.', ','), baseAvarias: c.baseAvarias };
}
export function converterConfiguracaoCobrancas(campos: CamposConfiguracaoCobrancas): DadosConfiguracaoCobrancasExtras {
  const v = esquemaConfiguracaoCobrancas.parse(campos);
  return { idCategoria: Number(v.idCategoria), baseFranquia: v.baseFranquia as DadosConfiguracaoCobrancasExtras['baseFranquia'], franquiaKm: Number(v.franquiaKm), valorKmExcedente: Number(v.valorKmExcedente.replace(',', '.')), valorTanqueCompleto: Number(v.valorTanqueCompleto.replace(',', '.')), toleranciaAtrasoMinutos: Number(v.toleranciaAtrasoMinutos), unidadeAtraso: v.unidadeAtraso as DadosConfiguracaoCobrancasExtras['unidadeAtraso'], fracionamentoAtraso: v.fracionamentoAtraso as DadosConfiguracaoCobrancasExtras['fracionamentoAtraso'], valorUnidadeAtraso: Number(v.valorUnidadeAtraso.replace(',', '.')), baseAvarias: 'ESTIMATIVA_VISTORIA' };
}
