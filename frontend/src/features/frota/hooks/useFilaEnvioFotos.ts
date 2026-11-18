import { useEffect, useState, useSyncExternalStore } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { filaSemPendencias } from '@/features/frota/formulario/fluxoCadastroVeiculo';
import { FilaEnvioFotos, type ArquivoParaFila, type OpcoesFilaEnvioFotos } from '@/lib/filaFotosVeiculo';
import { erroRecusouArquivo, mensagemErroEnvioFoto } from '@/lib/fotosVeiculo';
import {
  criarAmbienteReducaoDoNavegador,
  detectarSuporteReducao,
  memorizarSuporteReducao,
  prepararFotoParaEnvio,
} from '@/lib/reducaoFotoVeiculo';
import type { ItemFilaFoto } from '@/types/fotosVeiculo';
import { useEnviarFotoVeiculo } from './useEnviarFotoVeiculo';
import { CHAVE_VEICULOS } from './chavesVeiculos';

/** Suporte do navegador à redução: a sonda roda uma vez por carga da página (quem chama guarda a promessa). */
const obterSuporteReducao = memorizarSuporteReducao(() => detectarSuporteReducao(criarAmbienteReducaoDoNavegador()));

/** Controle da fila exposto à tela. */
export interface FilaFotosVeiculo {
  /** Fotos na fila e as que já foram gravadas neste lote (até a lista ser atualizada). */
  itens: readonly ItemFilaFoto[];
  /** Adiciona fotos até o limite de vagas. Devolve as que não couberam (suas prévias já foram liberadas). */
  adicionar(novos: readonly ArquivoParaFila[], gravadas: number): ArquivoParaFila[];
  tentarDeNovo(chave: number): void;
  /** Tira da fila uma foto que não está enviando nem gravada. Se nada mais estiver pendente, a tela recebe o fim do lote. */
  remover(chave: number): void;
  /** Tira do lote a foto já gravada com este id (depois de removida no servidor). A prévia é liberada. */
  descartarGravada(idVeiculoFoto: number): void;
}

/** Opções da tela que usa a fila (todas opcionais). */
export interface OpcoesFilaFotosTela {
  /**
   * Fim de um lote de envio. `temFalha` diz se alguma foto do lote ficou com erro. Também é chamado quando o veículo
   * recebe o id com a fila vazia (nada para enviar), para a tela de cadastro saber que pode seguir. E é chamado com
   * `temFalha` falso quando a pessoa tira da fila as fotos com erro e nada mais está pendente.
   */
  aoFimDoLote?: (temFalha: boolean) => void;
}

/**
 * Ponte entre a fila (criada uma vez) e as opções da tela deste render. A fila só lê a ponte no fim de um lote ou numa
 * ação da pessoa; quem a atualiza é um efeito, depois do render (nunca durante o render).
 */
interface PonteFilaFotos {
  fila: FilaEnvioFotos | null;
  aoFimDoLote?: (temFalha: boolean) => void;
}

/** Fila e ponte, criadas juntas uma única vez, com a ponte atrás de métodos (não é um ref do React). */
interface ControleFilaFotos {
  fila: FilaEnvioFotos;
  lerPonte(): PonteFilaFotos;
  atualizarPonte(ponte: PonteFilaFotos): void;
}

/** Cria a fila de envio. Fora do componente: a fila só lê a ponte quando um lote termina. */
function criarFilaEnvioFotos(
  lerPonte: () => PonteFilaFotos,
  enviar: OpcoesFilaEnvioFotos['enviar'],
  invalidarVeiculos: () => Promise<unknown>,
): FilaEnvioFotos {
  return new FilaEnvioFotos({
    enviar,
    liberarPrevia: (previa) => URL.revokeObjectURL(previa),
    mensagemDeErro: mensagemErroEnvioFoto,
    aoConcluirLote: async () => {
      await invalidarVeiculos();
      const temFalha = lerPonte().fila?.obterInstantaneo().some((item) => item.situacao === 'erro') ?? false;
      lerPonte().aoFimDoLote?.(temFalha);
    },
    preparar: async (arquivo, cabecalho) => {
      const suporte = await obterSuporteReducao();
      return prepararFotoParaEnvio(arquivo, cabecalho, suporte, criarAmbienteReducaoDoNavegador());
    },
    criarPrevia: (arquivo) => URL.createObjectURL(arquivo),
    recusaDoArquivo: erroRecusouArquivo,
  });
}

/** Cria o controle (fila e ponte) de uma vez. A ponte começa vazia e a tela a preenche no primeiro efeito. */
function criarControleFilaFotos(
  enviar: OpcoesFilaEnvioFotos['enviar'],
  invalidarVeiculos: () => Promise<unknown>,
): ControleFilaFotos {
  let ponte: PonteFilaFotos = { fila: null };
  const fila = criarFilaEnvioFotos(() => ponte, enviar, invalidarVeiculos);
  return {
    fila,
    lerPonte: () => ponte,
    atualizarPonte: (nova) => {
      ponte = nova;
    },
  };
}

/**
 * Fila sequencial de envio das fotos de um veículo. Uma chamada por foto, em ordem; erro numa foto não para as
 * seguintes. Sem id do veículo (cadastro), as fotos ficam na fila até o id chegar. Ao desmontar, o envio em curso
 * é cancelado e as prévias são liberadas. Quando um lote termina, a lista do veículo e as capas são atualizadas.
 * Quando a pessoa tira da fila as fotos com erro e nada mais está pendente, a tela recebe o fim do lote (sem falha).
 */
export function useFilaEnvioFotos(idVeiculo: number | null, opcoes: OpcoesFilaFotosTela = {}): FilaFotosVeiculo {
  const queryClient = useQueryClient();
  const { mutateAsync } = useEnviarFotoVeiculo();
  const [controle] = useState<ControleFilaFotos>(() =>
    criarControleFilaFotos(
      (id, arquivo, aoProgredir, sinal) => mutateAsync({ idVeiculo: id, arquivo, aoProgredir, sinal }),
      () => queryClient.invalidateQueries({ queryKey: [CHAVE_VEICULOS] }),
    ),
  );
  const { fila } = controle;

  // Primeiro efeito: a ponte fica com a fila e as opções deste render antes dos demais efeitos usarem.
  useEffect(() => {
    controle.atualizarPonte({ fila, aoFimDoLote: opcoes.aoFimDoLote });
  });

  useEffect(() => {
    fila.ativar();
    return () => fila.encerrar();
  }, [fila]);

  useEffect(() => {
    fila.definirVeiculo(idVeiculo);
    // Com id e sem nada na fila, não há lote para enviar: a tela recebe o fim agora (a fila não avisa nesse caso).
    if (idVeiculo !== null && fila.obterInstantaneo().length === 0) controle.lerPonte().aoFimDoLote?.(false);
  }, [controle, fila, idVeiculo]);

  const itens = useSyncExternalStore(fila.inscrever, fila.obterInstantaneo);

  /** Sem foto aguardando, enviando nem com erro, a tela recebe o fim do lote sem falha (ela decide se segue). */
  function avisarSeSemPendencias(): void {
    if (idVeiculo === null) return;
    if (filaSemPendencias(fila.obterInstantaneo().map((item) => item.situacao))) {
      controle.lerPonte().aoFimDoLote?.(false);
    }
  }

  return {
    itens,
    adicionar: (novos, gravadas) => fila.adicionar(novos, gravadas),
    tentarDeNovo: (chave) => fila.tentarDeNovo(chave),
    remover: (chave) => {
      fila.remover(chave);
      avisarSeSemPendencias();
    },
    descartarGravada: (idVeiculoFoto) => fila.descartarGravada(idVeiculoFoto),
  };
}
