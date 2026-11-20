import { useId } from 'react';
import { EstadoErro, EstadoVazio, Esqueleto } from '@/components/ui';
import { AvisoFotoCapaFrota } from '@/features/frota/componentes/AvisoFotoCapaFrota';
import { primeiraMensagemErroFotoCapa } from '@/lib/fotosVeiculo';
import { formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_ALERTAS_TITULO,
  MENSAGEM_MANUTENCAO_ERRO_ALERTAS,
  MENSAGEM_MANUTENCAO_NENHUM_ALERTA,
  MENSAGEM_MANUTENCAO_TENTAR_DE_NOVO,
  mensagemManutencaoIntervalo,
  mensagemManutencaoSeloAlertas,
} from '@/lib/mensagens';
import { CartaoAlertaRevisao } from '@/features/manutencao/componentes/CartaoAlertaRevisao';
import type { AlertasRevisaoRetorno } from '@/types/manutencao';

export interface ListaAlertasRevisaoProps {
  /** Primeira carga: aparecem os esqueletos. */
  carregando: boolean;
  /** Mensagem de erro do alerta (já convertida), ou nulo quando não há erro. */
  mensagemErro: string | null;
  /** A nova tentativa está em andamento. */
  tentando: boolean;
  /** Alertas da API. Nulo enquanto não há dados. */
  alertas: AlertasRevisaoRetorno | undefined;
  aoTentarNovamente: () => void;
  aoAbrirOrdem: (idVeiculo: number) => void;
  aoVerHistorico: (idVeiculo: number) => void;
  aoFalharFotoCapa: () => void;
}

/**
 * Bloco "Alertas de Veículos Próximos da Revisão" (desenho 14, seção 1). Com alertas, mostra o selo com a contagem e os
 * cartões. Sem alertas, mostra a mensagem de nenhum alerta (sem o selo). Com erro, mostra o erro e a tentativa de novo;
 * os outros blocos da tela seguem.
 */
export function ListaAlertasRevisao({
  carregando,
  mensagemErro,
  tentando,
  alertas,
  aoTentarNovamente,
  aoAbrirOrdem,
  aoVerHistorico,
  aoFalharFotoCapa,
}: ListaAlertasRevisaoProps) {
  const idTitulo = useId();
  const itens = alertas?.itens ?? [];
  const erroCapa = primeiraMensagemErroFotoCapa(itens);

  return (
    <section aria-labelledby={idTitulo} aria-busy={carregando || undefined} className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex flex-wrap items-center gap-2">
          <span aria-hidden="true" className="h-2.5 w-2.5 rounded-full bg-frota-oficina motion-safe:animate-pulse" />
          <h2 id={idTitulo} className="text-nome-veiculo font-semibold text-texto-desenho">
            {MENSAGEM_MANUTENCAO_ALERTAS_TITULO}
          </h2>
          {alertas !== undefined && alertas.total > 0 && (
            <span className="rounded-full bg-frota-oficina-fundo px-2 py-0.5 text-micro font-semibold text-pagamento-texto-pendente">
              {mensagemManutencaoSeloAlertas(alertas.total, formatarNumero(alertas.total))}
            </span>
          )}
        </div>
        {alertas !== undefined && (
          <p className="text-rotulo text-texto-apoio">
            {mensagemManutencaoIntervalo(formatarNumero(alertas.intervaloRevisaoKm))}
          </p>
        )}
      </div>

      {carregando && (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
          {[0, 1, 2, 3].map((indice) => (
            <Esqueleto key={indice} className="h-72 w-full rounded-cartao" />
          ))}
        </div>
      )}

      {!carregando && mensagemErro !== null && (
        <div className="rounded-cartao bg-branco shadow-sm">
          <EstadoErro
            titulo={MENSAGEM_MANUTENCAO_ERRO_ALERTAS}
            mensagem={mensagemErro}
            aoTentarNovamente={aoTentarNovamente}
            tentando={tentando}
            rotuloTentarNovamente={MENSAGEM_MANUTENCAO_TENTAR_DE_NOVO}
          />
        </div>
      )}

      {!carregando && mensagemErro === null && alertas !== undefined && itens.length === 0 && (
        <div className="rounded-cartao bg-branco shadow-sm">
          <EstadoVazio titulo={MENSAGEM_MANUTENCAO_NENHUM_ALERTA} icone="directions_car" />
        </div>
      )}

      {!carregando && mensagemErro === null && itens.length > 0 && (
        <>
          {erroCapa !== null && <AvisoFotoCapaFrota mensagem={erroCapa} />}
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
            {itens.map((alerta) => (
              <CartaoAlertaRevisao
                key={alerta.idVeiculo}
                alerta={alerta}
                aoAbrirOrdem={aoAbrirOrdem}
                aoVerHistorico={aoVerHistorico}
                aoFalharFotoCapa={aoFalharFotoCapa}
              />
            ))}
          </div>
        </>
      )}
    </section>
  );
}
