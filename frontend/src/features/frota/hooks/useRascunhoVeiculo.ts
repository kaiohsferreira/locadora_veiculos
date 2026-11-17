import { useState } from 'react';
import {
  apagarRascunhoVeiculo,
  chaveRascunhoVeiculo,
  guardarRascunhoVeiculo,
  lerRascunhoArmazenado,
  obterArmazenamentoRascunho,
} from '@/features/frota/formulario/rascunhoVeiculo';
import type { ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';

/** Rascunho do cadastro neste navegador. Só existe no cadastro (`ativo` verdadeiro); na edição tudo é nulo ou falso. */
export interface RascunhoVeiculoTela {
  /** Valores guardados quando a tela abriu (nulo sem rascunho). Lido uma vez, na montagem. */
  rascunhoInicial: ValoresFormularioVeiculo | null;
  /** Grava os valores neste navegador. Falso quando não foi possível. */
  salvar(valores: ValoresFormularioVeiculo): boolean;
  /** Apaga o rascunho desta sessão (cadastro concluído ou descartado). */
  apagar(): void;
}

/**
 * Rascunho do cadastro de veículo, separado por usuário e locadora da sessão. A leitura acontece uma vez, na montagem:
 * o formulário parte dela. Sem armazenamento ou sem identidade da sessão, a tela segue sem rascunho.
 */
export function useRascunhoVeiculo(ativo: boolean): RascunhoVeiculoTela {
  const { usuario } = useAutenticacao();
  const chave = ativo
    ? chaveRascunhoVeiculo({ idUsuario: usuario?.idUsuario, idEmpresa: usuario?.idEmpresa })
    : null;
  const [rascunhoInicial] = useState<ValoresFormularioVeiculo | null>(() =>
    lerRascunhoArmazenado(obterArmazenamentoRascunho(), chave),
  );

  return {
    rascunhoInicial,
    salvar: (valores) => guardarRascunhoVeiculo(obterArmazenamentoRascunho(), chave, valores),
    apagar: () => apagarRascunhoVeiculo(obterArmazenamentoRascunho(), chave),
  };
}
