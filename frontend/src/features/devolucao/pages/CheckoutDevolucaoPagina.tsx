import { useParams } from 'react-router-dom';
import { EstadoErro } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { lerIdLocacao } from '@/features/locacoes/vistoria/validacaoVistoria';
import { useDevolucao } from '../hooks/useDevolucao';
import { CabecalhoDevolucao } from '../componentes/CabecalhoDevolucao';
import { ConteudoDevolucao } from '../componentes/ConteudoDevolucao';

/** Tela 06, devolução do veículo (P5-FE-01): check-out, cobranças extras e encerramento da locação. */
export default function CheckoutDevolucaoPagina() {
  const { locacaoId } = useParams<'locacaoId'>();
  const id = lerIdLocacao(locacaoId);
  const consulta = useDevolucao(id);
  useTituloDocumento(M.titulo);
  const contexto = consulta.data ?? null;
  return (
    <div className="mx-auto w-full max-w-6xl space-y-6 px-4 py-8 sm:px-6">
      <CabecalhoDevolucao contexto={id === null ? null : contexto} />
      {/* Erro só troca a tela quando não há dados. Com dados já carregados (ex.: a atualização falhou depois de um 409), a caixa e a mensagem ficam. */}
      {id === null ? <EstadoErro mensagem={M.idInvalido} />
        : consulta.isPending ? <CarregandoPagina />
          : consulta.isError && contexto === null ? <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => { void consulta.refetch(); }} tentando={consulta.isFetching} />
            : contexto && (
              <>
                {consulta.isError && <p role="alert" className="text-corpo-denso text-status-perigo">{obterMensagemErro(consulta.error)}</p>}
                <ConteudoDevolucao contexto={contexto} atualizando={consulta.isFetching} />
              </>
            )}
    </div>
  );
}
