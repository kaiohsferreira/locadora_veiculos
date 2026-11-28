import { useState, type ReactNode } from 'react';
import { generatePath, useParams, useSearchParams } from 'react-router-dom';
import { BotaoLink, Esqueleto, EstadoErro } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_DETALHE_VEICULO as texto } from '@/lib/mensagensDetalheVeiculo';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { PeriodoLocacaoVeiculo, VeiculoLocacao } from '@/types/veiculos';
import { avaliarPeriodo, hojeLocal } from '../busca/parametrosBusca';
import { FichaVeiculo } from '../detalhe/FichaVeiculo';
import { GaleriaVeiculo } from '../detalhe/GaleriaVeiculo';
import { ResumoLocacaoVeiculo } from '../detalhe/ResumoLocacaoVeiculo';
import { useDetalheVeiculo } from '../hooks/useDetalheVeiculo';

const MAIOR_ID_VEICULO = 2_147_483_647;

export default function DetalheVeiculoPagina() {
  const { veiculoId = '' } = useParams<'veiculoId'>();
  const [parametros] = useSearchParams();
  const id = Number(veiculoId);
  if (!/^\d+$/.test(veiculoId) || id < 1 || id > MAIOR_ID_VEICULO) {
    return (
      <EstruturaDetalhe parametros={parametros} titulo={texto.titulo}>
        <EstadoErro titulo={texto.ausente} mensagem={texto.dicaAusente} />
      </EstruturaDetalhe>
    );
  }

  return <DetalheVeiculo key={id} idVeiculo={id} />;
}

interface EstruturaDetalheProps {
  parametros: URLSearchParams;
  titulo: string;
  children: ReactNode;
  resumo?: ReactNode;
}

function EstruturaDetalhe({ parametros, titulo, children, resumo }: EstruturaDetalheProps) {
  useTituloDocumento(titulo);
  return (
    <div className="bg-offwhite">
      <div className="mx-auto w-full max-w-7xl space-y-5 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
        <BotaoLink
          to={{ pathname: CAMINHOS.resultadosBusca, search: parametros.toString() }}
          variante="fantasma"
          icone="arrow_back"
        >
          {texto.voltar}
        </BotaoLink>
        <div className={resumo ? 'grid min-w-0 items-start gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(20rem,1fr)]' : undefined}>
          <div className="min-w-0 space-y-5">
            <h1 className="break-words text-titulo text-navy sm:text-titulo-hero">{titulo}</h1>
            {children}
          </div>
          {resumo}
        </div>
      </div>
    </div>
  );
}

function DetalheVeiculo({ idVeiculo }: { idVeiculo: number }) {
  const [parametros, definirParametros] = useSearchParams();
  const datas: PeriodoLocacaoVeiculo = {
    dataRetirada: parametros.get('dataRetirada') ?? '',
    dataDevolucao: parametros.get('dataDevolucao') ?? '',
  };
  const hoje = hojeLocal();
  const { diarias } = avaliarPeriodo(datas, hoje);
  const consulta = useDetalheVeiculo(idVeiculo, diarias > 0 ? datas : undefined);
  const [ultimoDetalhe, guardarDetalhe] = useState<VeiculoLocacao>();
  // A consulta corrente controla o resumo; a última ficha fica visível enquanto o novo período é consultado.
  if (consulta.data && consulta.data !== ultimoDetalhe) guardarDetalhe(consulta.data);

  const refazerConsulta = () => void consulta.refetch();
  const atualizarFotos = useAoFalharFotoCapa(refazerConsulta);
  const naoEncontrado = consulta.error instanceof ErroApi && consulta.error.status === 404;
  const veiculo = naoEncontrado ? undefined : consulta.data ?? ultimoDetalhe;
  const titulo = veiculo ? `${veiculo.marca} ${veiculo.modelo}` : texto.titulo;

  function aplicarPeriodo(periodo: PeriodoLocacaoVeiculo) {
    if (datas.dataRetirada === periodo.dataRetirada && datas.dataDevolucao === periodo.dataDevolucao) {
      refazerConsulta();
      return;
    }
    const novosParametros = new URLSearchParams(parametros);
    novosParametros.set('dataRetirada', periodo.dataRetirada);
    novosParametros.set('dataDevolucao', periodo.dataDevolucao);
    definirParametros(novosParametros);
  }

  const destinoCheckout = generatePath(CAMINHOS.checkoutReserva, { veiculoId: String(idVeiculo) });
  const parametrosCheckout = new URLSearchParams({ ...datas });

  const resumo = veiculo && (
    <ResumoLocacaoVeiculo
      datas={datas}
      hoje={hoje}
      detalhe={consulta.data}
      diarias={diarias}
      situacaoConsulta={consulta.fetchStatus}
      falhou={consulta.isError}
      destinoCheckout={`${destinoCheckout}?${parametrosCheckout}`}
      aoAplicar={aplicarPeriodo}
      aoTentarNovamente={refazerConsulta}
    />
  );

  return (
    <EstruturaDetalhe parametros={parametros} titulo={titulo} resumo={resumo}>
      {naoEncontrado ? (
        <EstadoErro titulo={texto.ausente} mensagem={texto.dicaAusente} />
      ) : veiculo ? (
        <>
          <GaleriaVeiculo
            fotos={veiculo.fotos}
            titulo={titulo}
            erroFotos={veiculo.erroFotos}
            aoFalharFoto={() => {
              if (consulta.fetchStatus === 'idle') atualizarFotos();
            }}
          />
          <FichaVeiculo veiculo={veiculo} />
        </>
      ) : consulta.isError ? (
        <EstadoErro
          titulo={texto.erro}
          mensagem={obterMensagemErro(consulta.error)}
          aoTentarNovamente={refazerConsulta}
          tentando={consulta.fetchStatus !== 'idle'}
        />
      ) : (
        <div aria-busy="true" className="space-y-4">
          <output className="block text-corpo-denso text-texto-apoio">
            {consulta.fetchStatus === 'paused' ? texto.aguardandoConexao : texto.carregando}
          </output>
          <div className="grid gap-6 lg:grid-cols-3" aria-hidden="true">
            <Esqueleto className="aspect-[16/9] w-full lg:col-span-2" />
            <Esqueleto className="h-52 w-full" />
          </div>
        </div>
      )}
    </EstruturaDetalhe>
  );
}
