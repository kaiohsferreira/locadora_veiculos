/**
 * Passos da ativação da locadora na tela 40. São dois: "Conta criada" (sempre concluído quando a pessoa está na tela)
 * e "Cadastre o primeiro veículo" (concluído quando a locadora já tem veículo). O progresso é calculado a partir deles.
 */
import {
  MENSAGEM_PAINEL_PASSO_CONTA_DESCRICAO,
  MENSAGEM_PAINEL_PASSO_CONTA_TITULO,
  MENSAGEM_PAINEL_PASSO_VEICULO_DESCRICAO,
  MENSAGEM_PAINEL_PASSO_VEICULO_TITULO,
} from '@/lib/mensagens';

export type SituacaoPasso = 'concluido' | 'proximo';

export interface PassoAtivacao {
  id: 'conta' | 'veiculo';
  titulo: string;
  descricao: string;
  situacao: SituacaoPasso;
  /** Posição de 1 a 2, como no desenho. */
  numero: number;
}

export interface ProgressoAtivacao {
  concluidos: number;
  total: number;
  restantes: number;
  /** De 0 a 100, para a largura da barra. */
  percentual: number;
}

/**
 * Os dois passos. Sem veículo (tela 40), o cadastro do veículo é o próximo passo; com veículo, já está concluído.
 */
export function montarPassos(primeiroAcesso: boolean): PassoAtivacao[] {
  return [
    {
      id: 'conta',
      titulo: MENSAGEM_PAINEL_PASSO_CONTA_TITULO,
      descricao: MENSAGEM_PAINEL_PASSO_CONTA_DESCRICAO,
      situacao: 'concluido',
      numero: 1,
    },
    {
      id: 'veiculo',
      titulo: MENSAGEM_PAINEL_PASSO_VEICULO_TITULO,
      descricao: MENSAGEM_PAINEL_PASSO_VEICULO_DESCRICAO,
      situacao: primeiroAcesso ? 'proximo' : 'concluido',
      numero: 2,
    },
  ];
}

/** Concluídos, total, restantes e percentual dos passos. */
export function progressoAtivacao(passos: readonly PassoAtivacao[]): ProgressoAtivacao {
  const total = passos.length;
  const concluidos = passos.filter((passo) => passo.situacao === 'concluido').length;
  return {
    concluidos,
    total,
    restantes: total - concluidos,
    percentual: total > 0 ? (concluidos / total) * 100 : 0,
  };
}

/** Número do passo que está em andamento (o primeiro não concluído), ou o último quando todos estão prontos. */
export function etapaEmAndamento(passos: readonly PassoAtivacao[]): number {
  const proximo = passos.find((passo) => passo.situacao !== 'concluido');
  return proximo ? proximo.numero : passos.length;
}

/** Passos e progresso da tela 40, a partir da flag `primeiroAcesso` da resposta. */
export function prepararPrimeiroAcesso(primeiroAcesso: boolean): {
  passos: PassoAtivacao[];
  progresso: ProgressoAtivacao;
} {
  const passos = montarPassos(primeiroAcesso);
  return { passos, progresso: progressoAtivacao(passos) };
}
