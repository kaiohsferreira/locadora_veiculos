import type { ReactNode } from 'react';
import { EstadoErro, EstadoVazio, Botao, Paginacao } from '@/components/ui';
import { FiltroHistoricoReservas } from '@/features/area-cliente/reservas/componentes/FiltroHistoricoReservas';
import { TabelaHistoricoReservas } from '@/features/area-cliente/reservas/componentes/TabelaHistoricoReservas';
import type { EstadoListaReservas } from '@/features/area-cliente/reservas/estadoLista';
import { TAMANHO_PAGINA_HISTORICO } from '@/features/area-cliente/reservas/parametrosReservas';
import { obterMensagemErro } from '@/lib/erros';
import {
  MENSAGEM_RESERVAS_BUSCA_VAZIA_TITULO,
  MENSAGEM_RESERVAS_ERRO_ATUALIZACAO,
  MENSAGEM_RESERVAS_ERRO_TITULO,
  MENSAGEM_RESERVAS_HISTORICO_DESCRICAO,
  MENSAGEM_RESERVAS_HISTORICO_TITULO,
  MENSAGEM_RESERVAS_LIMPAR_BUSCA,
  MENSAGEM_RESERVAS_RODAPE_ITENS,
  MENSAGEM_RESERVAS_VAZIO_HISTORICO_DESCRICAO,
  MENSAGEM_RESERVAS_VAZIO_HISTORICO_TITULO,
} from '@/lib/mensagens';

export interface PainelHistoricoReservasProps {
  estado: EstadoListaReservas;
  pagina: number;
  aoMudarPagina: (pagina: number) => void;
  /** Texto digitado agora na busca. */
  buscaDigitada: string;
  /** Busca que está valendo na consulta (vazia quando não há busca). */
  buscaAplicada: string;
  aoMudarBusca: (texto: string) => void;
  aoEnviarBusca: () => void;
  aoLimparBusca: () => void;
}

/**
 * Aba Histórico: busca por código ou modelo, tabela de 6 colunas, estados e paginação de 10 em 10.
 * Vazio sem busca e vazio com busca têm textos diferentes; com busca, o botão limpa o filtro.
 */
export function PainelHistoricoReservas({
  estado,
  pagina,
  aoMudarPagina,
  buscaDigitada,
  buscaAplicada,
  aoMudarBusca,
  aoEnviarBusca,
  aoLimparBusca,
}: PainelHistoricoReservasProps) {
  const { dados, carregando, erro, atualizando, aoTentarNovamente } = estado;

  let conteudo: ReactNode;
  if (erro && !dados) {
    conteudo = (
      <EstadoErro
        titulo={MENSAGEM_RESERVAS_ERRO_TITULO}
        mensagem={obterMensagemErro(erro)}
        aoTentarNovamente={aoTentarNovamente}
        tentando={atualizando}
      />
    );
  } else if (carregando) {
    conteudo = <TabelaHistoricoReservas itens={[]} carregando />;
  } else if (!dados || dados.total === 0) {
    const buscando = buscaAplicada !== '';
    conteudo = (
      <EstadoVazio
        titulo={buscando ? MENSAGEM_RESERVAS_BUSCA_VAZIA_TITULO : MENSAGEM_RESERVAS_VAZIO_HISTORICO_TITULO}
        descricao={buscando ? undefined : MENSAGEM_RESERVAS_VAZIO_HISTORICO_DESCRICAO}
        acao={
          buscando ? (
            <Botao variante="contorno" onClick={aoLimparBusca}>
              {MENSAGEM_RESERVAS_LIMPAR_BUSCA}
            </Botao>
          ) : undefined
        }
      />
    );
  } else {
    conteudo = (
      <>
        {erro ? (
          <p role="alert" className="bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
            {MENSAGEM_RESERVAS_ERRO_ATUALIZACAO}
          </p>
        ) : null}
        <TabelaHistoricoReservas itens={dados.itens} carregando={false} />
        <div className="border-t border-borda bg-steel-fundo">
          <Paginacao
            pagina={pagina}
            tamanhoPagina={TAMANHO_PAGINA_HISTORICO}
            total={dados.total}
            aoMudarPagina={aoMudarPagina}
            desabilitada={atualizando}
            rotuloItens={MENSAGEM_RESERVAS_RODAPE_ITENS}
          />
        </div>
      </>
    );
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center justify-between">
        <div>
          <h2 className="text-marca font-bold tracking-tight text-navy">{MENSAGEM_RESERVAS_HISTORICO_TITULO}</h2>
          <p className="text-legenda text-status-neutro">{MENSAGEM_RESERVAS_HISTORICO_DESCRICAO}</p>
        </div>
        <FiltroHistoricoReservas valor={buscaDigitada} aoMudar={aoMudarBusca} aoEnviar={aoEnviarBusca} />
      </div>
      <div aria-busy={carregando || atualizando} className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        {conteudo}
      </div>
    </section>
  );
}
