import { EstadoErro, Icone } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { ListaHistoricoLocacoes } from '@/features/area-cliente/locacoes/componentes/ListaHistoricoLocacoes';
import { mensagemErroLista, TAMANHO_ULTIMAS_LOCACOES } from '@/features/area-cliente/locacoes/regrasLocacoes';
import { useLocacoesCliente } from '@/features/area-cliente/locacoes/hooks/useMinhasLocacoes';
import {
  mensagemLocacoesVerTodas,
  MENSAGEM_LOCACOES_ERRO_TITULO,
  MENSAGEM_LOCACOES_ULTIMAS_DESCRICAO,
  MENSAGEM_LOCACOES_ULTIMAS_TITULO,
} from '@/lib/mensagensLocacoesCliente';

export interface SecaoUltimasLocacoesProps {
  /** Leva para a aba Histórico (só aparece quando há mais locações do que os cartões mostrados). */
  aoVerTodas: () => void;
}

/**
 * "Últimas locações do histórico" (desenho 27, dentro da aba Em andamento): as três últimas encerradas ou canceladas,
 * com o link para a aba Histórico. Sem histórico, a seção não aparece.
 */
export function SecaoUltimasLocacoes({ aoVerTodas }: SecaoUltimasLocacoesProps) {
  const consulta = useLocacoesCliente({
    pagina: 1,
    tamanhoPagina: TAMANHO_ULTIMAS_LOCACOES,
    situacao: 'HISTORICO',
  });
  const aoFalharFotoCapa = useAoFalharFotoCapa(() => void consulta.refetch());
  const dados = consulta.data;

  if (consulta.isPending) {
    return (
      <section aria-busy="true" className="flex flex-col gap-4 pt-6">
        <ListaHistoricoLocacoes itens={[]} carregando />
      </section>
    );
  }

  if (consulta.isError && !dados) {
    return (
      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        <EstadoErro
          titulo={MENSAGEM_LOCACOES_ERRO_TITULO}
          mensagem={mensagemErroLista(consulta.error)}
          aoTentarNovamente={() => void consulta.refetch()}
          tentando={consulta.isFetching}
        />
      </section>
    );
  }

  if (!dados || dados.total === 0) return null;

  return (
    <section aria-labelledby="titulo-ultimas-locacoes" className="flex flex-col gap-4 pt-6">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div className="flex flex-col gap-1">
          <h2 id="titulo-ultimas-locacoes" className="text-marca font-bold tracking-tight text-navy-profundo">
            {MENSAGEM_LOCACOES_ULTIMAS_TITULO}
          </h2>
          <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_LOCACOES_ULTIMAS_DESCRICAO}</p>
        </div>
        {dados.total > TAMANHO_ULTIMAS_LOCACOES && (
          <button
            type="button"
            onClick={aoVerTodas}
            className="inline-flex items-center gap-1 self-start text-corpo-denso font-semibold text-frota-oficina hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-steel"
          >
            <span>{mensagemLocacoesVerTodas(dados.total)}</span>
            <Icone nome="arrow_forward" tamanho={18} />
          </button>
        )}
      </div>
      {/* Celular: painel branco com a lista compacta. Computador: os cartões ficam soltos, como no desenho 27. */}
      <div className="overflow-hidden rounded-cartao bg-branco p-4 shadow-cartao md:overflow-visible md:bg-transparent md:p-0 md:shadow-none">
        <ListaHistoricoLocacoes itens={dados.itens} carregando={false} aoFalharFotoCapa={aoFalharFotoCapa} />
      </div>
    </section>
  );
}
