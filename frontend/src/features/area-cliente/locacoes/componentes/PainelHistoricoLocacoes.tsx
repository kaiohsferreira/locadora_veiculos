import type { ReactNode } from 'react';
import { EstadoErro, EstadoVazio, Paginacao } from '@/components/ui';
import type { EstadoListaLocacoes } from '@/features/area-cliente/locacoes/componentes/PainelEmAndamento';
import { ListaHistoricoLocacoes } from '@/features/area-cliente/locacoes/componentes/ListaHistoricoLocacoes';
import { mensagemErroLista, TAMANHO_PAGINA_LOCACOES } from '@/features/area-cliente/locacoes/regrasLocacoes';
import { primeiraMensagemErroFotoCapa } from '@/lib/fotosVeiculo';
import { AvisoFotosReservas } from '@/features/area-cliente/reservas/componentes/AvisoFotosReservas';
import {
  MENSAGEM_LOCACOES_ERRO_TITULO,
  MENSAGEM_LOCACOES_HISTORICO_DESCRICAO,
  MENSAGEM_LOCACOES_HISTORICO_TITULO,
  MENSAGEM_LOCACOES_RODAPE_ITENS,
  MENSAGEM_LOCACOES_VAZIO_HISTORICO_DESCRICAO,
  MENSAGEM_LOCACOES_VAZIO_HISTORICO_TITULO,
} from '@/lib/mensagensLocacoesCliente';
import { MENSAGEM_RESERVAS_ERRO_ATUALIZACAO } from '@/lib/mensagensReservas';

export interface PainelHistoricoLocacoesProps {
  estado: EstadoListaLocacoes;
  pagina: number;
  aoMudarPagina: (pagina: number) => void;
  /** Foto de capa que falhou em um cartão: a tela refaz a consulta uma vez. */
  aoFalharFotoCapa: () => void;
}

/**
 * Aba Histórico: as locações finalizadas e canceladas, paginadas de 5 em 5, com os estados de carregando, vazio e
 * erro. Erro com dado já na tela mantém o dado e avisa.
 */
export function PainelHistoricoLocacoes({ estado, pagina, aoMudarPagina, aoFalharFotoCapa }: PainelHistoricoLocacoesProps) {
  const { dados, carregando, erro, atualizando, aoTentarNovamente } = estado;

  let conteudo: ReactNode;
  if (erro && !dados) {
    conteudo = (
      <EstadoErro
        titulo={MENSAGEM_LOCACOES_ERRO_TITULO}
        mensagem={mensagemErroLista(erro)}
        aoTentarNovamente={aoTentarNovamente}
        tentando={atualizando}
      />
    );
  } else if (carregando) {
    conteudo = (
      <div className="p-4">
        <ListaHistoricoLocacoes itens={[]} carregando />
      </div>
    );
  } else if (!dados || dados.total === 0) {
    conteudo = (
      <EstadoVazio titulo={MENSAGEM_LOCACOES_VAZIO_HISTORICO_TITULO} descricao={MENSAGEM_LOCACOES_VAZIO_HISTORICO_DESCRICAO} />
    );
  } else {
    const erroFotoCapa = primeiraMensagemErroFotoCapa(dados.itens);
    conteudo = (
      <>
        {erro ? (
          <p role="alert" className="bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
            {MENSAGEM_RESERVAS_ERRO_ATUALIZACAO}
          </p>
        ) : null}
        {erroFotoCapa !== null && (
          <div className="p-4 pb-0">
            <AvisoFotosReservas mensagem={erroFotoCapa} />
          </div>
        )}
        <div className="p-4">
          <ListaHistoricoLocacoes itens={dados.itens} carregando={false} aoFalharFotoCapa={aoFalharFotoCapa} />
        </div>
        <div className="border-t border-borda bg-steel-fundo [&_nav>ul]:flex-wrap">
          <Paginacao
            pagina={pagina}
            tamanhoPagina={TAMANHO_PAGINA_LOCACOES}
            total={dados.total}
            aoMudarPagina={aoMudarPagina}
            desabilitada={atualizando}
            rotuloItens={MENSAGEM_LOCACOES_RODAPE_ITENS}
          />
        </div>
      </>
    );
  }

  return (
    <section aria-labelledby="titulo-historico-locacoes" className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <h2 id="titulo-historico-locacoes" className="text-marca font-bold tracking-tight text-navy-profundo">
          {MENSAGEM_LOCACOES_HISTORICO_TITULO}
        </h2>
        <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_LOCACOES_HISTORICO_DESCRICAO}</p>
      </div>
      <div aria-busy={carregando || atualizando} className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        {conteudo}
      </div>
    </section>
  );
}
