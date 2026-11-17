/**
 * Props comuns dos blocos da tela de cadastro e edição de veículo (tela 05). Os blocos não gravam nada: recebem os
 * valores, os erros por campo e uma função de mudança, e o formulário (próxima etapa) liga tudo isso.
 */
import type { CampoFormularioVeiculo, ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import type { ErrosCamposVeiculo } from '@/lib/mapeamentoErroVeiculo';

/** Cadastro: formulário vazio, com rascunho e envio de fotos. Edição: veículo já criado, sem rascunho e com "Salvar Alterações". */
export type ModoFormularioVeiculo = 'cadastro' | 'edicao';

export interface PropsCamposVeiculo {
  valores: ValoresFormularioVeiculo;
  /** Erro por campo (validação no navegador ou resposta do servidor). Campo sem erro não aparece no mapa. */
  erros: ErrosCamposVeiculo;
  /** Chamada a cada mudança de um campo, já com a máscara do campo aplicada. */
  aoMudar: (campo: CampoFormularioVeiculo, valor: string) => void;
}
