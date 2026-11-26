import { useState } from 'react';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import {
  AbasLocacoes,
  ID_ABA_LOCACOES_ANDAMENTO,
  ID_ABA_LOCACOES_HISTORICO,
  ID_PAINEL_LOCACOES,
  type ContagemAbaLocacao,
} from '@/features/area-cliente/locacoes/componentes/AbasLocacoes';
import { CabecalhoLocacoes } from '@/features/area-cliente/locacoes/componentes/CabecalhoLocacoes';
import {
  PainelEmAndamento,
  type EstadoListaLocacoes,
} from '@/features/area-cliente/locacoes/componentes/PainelEmAndamento';
import { PainelHistoricoLocacoes } from '@/features/area-cliente/locacoes/componentes/PainelHistoricoLocacoes';
import { useLocacoesCliente, useTotalLocacoesCliente } from '@/features/area-cliente/locacoes/hooks/useMinhasLocacoes';
import { TAMANHO_PAGINA_LOCACOES, ultimaPaginaLocacoes } from '@/features/area-cliente/locacoes/regrasLocacoes';
import { descreverAtualizacao } from '@/features/area-cliente/reservas/atualizacao';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { MENSAGEM_LOCACOES_TITULO_PAGINA } from '@/lib/mensagensLocacoesCliente';
import type { FiltroLocacoes, RespostaLocacoes, SituacaoLocacao } from '@/types/locacoesCliente';

/**
 * T-FE-02 · Minhas locações (RF-46), tela 27. Mostra só as locações do próprio cliente (o servidor decide quais), em
 * duas abas: Em andamento (com o cronômetro da devolução e as últimas locações) e Histórico. Não há aba Próximas: as
 * reservas futuras ficam em Minhas reservas.
 */
export default function MinhasLocacoesPagina() {
  useTituloDocumento(MENSAGEM_LOCACOES_TITULO_PAGINA);
  const [situacao, setSituacao] = useState<SituacaoLocacao>('EM_ANDAMENTO');
  const [pagina, setPagina] = useState(1);
  /** Momento de referência do "Atualizado hoje às ...": fixado quando a tela abre. */
  const [agora] = useState(() => new Date());

  const filtro: FiltroLocacoes = { situacao, pagina, tamanhoPagina: TAMANHO_PAGINA_LOCACOES };
  const consulta = useLocacoesCliente(filtro);
  /** Imagem de capa que falha: refaz a consulta uma vez nesta visita (URL assinada nova); depois, ícone de carro. */
  const aoFalharFotoCapa = useAoFalharFotoCapa(() => void consulta.refetch());
  const totalAndamento = useTotalLocacoesCliente('EM_ANDAMENTO');
  const totalHistorico = useTotalLocacoesCliente('HISTORICO');
  const dados = consulta.data;

  // Página além da última (a lista encolheu): volta para a última existente. Feito durante o render, como o React
  // indica para estado derivado. Não roda com dado de outra consulta.
  if (dados !== undefined && !consulta.isPlaceholderData) {
    const ultima = ultimaPaginaLocacoes(dados.total, filtro.tamanhoPagina);
    if (pagina > ultima) setPagina(ultima);
  }

  function trocarSituacao(nova: SituacaoLocacao) {
    if (nova === situacao) return;
    setSituacao(nova);
    setPagina(1);
  }

  const estado: EstadoListaLocacoes = {
    dados,
    carregando: consulta.isPending,
    erro: consulta.error,
    atualizando: consulta.isFetching,
    aoTentarNovamente: () => void consulta.refetch(),
  };

  const contagens: Record<SituacaoLocacao, ContagemAbaLocacao> = {
    EM_ANDAMENTO: contagemDe(totalAndamento.data, totalAndamento.isError),
    HISTORICO: contagemDe(totalHistorico.data, totalHistorico.isError),
  };

  // overflow-clip (não overflow-hidden): os círculos decorativos passam da largura do celular, e overflow-hidden ainda
  // deixa a página rolar de lado quando uma aba ganha foco (mesmo ajuste da tela 28).
  return (
    <div className="relative overflow-clip">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-32 right-1/4 h-[500px] w-[500px] rounded-full bg-orange/5 blur-3xl"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute left-10 top-48 h-[420px] w-[420px] rounded-full bg-steel-alto/60 blur-3xl"
      />

      <div className="relative z-10 mx-auto flex w-full max-w-7xl flex-col gap-8 px-4 py-8 sm:px-6 lg:px-8">
        <CabecalhoLocacoes />

        <AbasLocacoes
          situacao={situacao}
          aoMudar={trocarSituacao}
          contagens={contagens}
          atualizacao={descreverAtualizacao(consulta.dataUpdatedAt, agora)}
          atualizando={consulta.isFetching}
        />

        <div
          role="tabpanel"
          id={ID_PAINEL_LOCACOES}
          aria-labelledby={situacao === 'EM_ANDAMENTO' ? ID_ABA_LOCACOES_ANDAMENTO : ID_ABA_LOCACOES_HISTORICO}
        >
          {situacao === 'EM_ANDAMENTO' ? (
            <PainelEmAndamento
              estado={estado}
              pagina={pagina}
              aoMudarPagina={setPagina}
              aoFalharFotoCapa={aoFalharFotoCapa}
              aoVerHistorico={() => trocarSituacao('HISTORICO')}
            />
          ) : (
            <PainelHistoricoLocacoes
              estado={estado}
              pagina={pagina}
              aoMudarPagina={setPagina}
              aoFalharFotoCapa={aoFalharFotoCapa}
            />
          )}
        </div>
      </div>
    </div>
  );
}

/** Contador de uma aba: o total quando chega; "indisponível" se a consulta falhou; senão, carregando. */
function contagemDe(total: RespostaLocacoes | undefined, falhou: boolean): ContagemAbaLocacao {
  if (total !== undefined) return total.total;
  return falhou ? 'indisponivel' : 'carregando';
}
