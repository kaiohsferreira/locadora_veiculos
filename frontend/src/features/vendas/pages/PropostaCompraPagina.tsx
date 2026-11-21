import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { BotaoLink, EstadoErro, Esqueleto, Icone } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { useEnviarPropostaCompra } from '@/features/vendas/hooks/useEnviarPropostaCompra';
import { useSimulacaoParcelamento } from '@/features/vendas/hooks/useSimulacaoParcelamento';
import { useVeiculoAVenda } from '@/features/vendas/hooks/useVeiculoAVenda';
import {
  CAMPOS_SIMULACAO_INICIAIS,
  camposValidos,
  converterCampos,
  validarCampos,
  type CamposSimulacao,
} from '@/features/vendas/proposta/camposSimulacao';
import { CartaoVeiculoProposta } from '@/features/vendas/proposta/CartaoVeiculoProposta';
import { ConfirmacaoProposta } from '@/features/vendas/proposta/ConfirmacaoProposta';
import { FormularioSimulacao } from '@/features/vendas/proposta/FormularioSimulacao';
import { ResumoSimulacao } from '@/features/vendas/proposta/ResumoSimulacao';
import { lerIdVeiculo } from '@/features/vendas/proposta/textosProposta';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import {
  PROPOSTA_ENTRAR_PARA_ENVIAR,
  PROPOSTA_SO_CLIENTE,
  PROPOSTA_SUBTITULO,
  PROPOSTA_TITULO_PAGINA,
  PROPOSTA_VEICULO_INDISPONIVEL_TEXTO,
  PROPOSTA_VEICULO_INDISPONIVEL_TITULO,
  PROPOSTA_VER_CATALOGO,
  PROPOSTA_VOLTAR_CATALOGO,
} from '@/lib/mensagensProposta';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS, type EstadoRedirecionamentoLogin } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { PropostaCompra } from '@/types/vendas';

const STATUS_NAO_ENCONTRADO = 404;
const STATUS_NAO_AUTENTICADO = 401;
const PERFIL_CLIENTE = ['cliente'] as const;

/** Erro 404 da API (veículo fora do catálogo). */
function ehNaoEncontrado(erro: unknown): boolean {
  return erro instanceof ErroApi && erro.status === STATUS_NAO_ENCONTRADO;
}

/** Cabeçalho da página: link de volta, título e subtítulo. */
function Cabecalho() {
  return (
    <header className="flex flex-col gap-2">
      <Link
        to={CAMINHOS.seminovos}
        className="inline-flex w-fit items-center gap-1 text-corpo-denso font-medium text-navy hover:underline"
      >
        <Icone nome="arrow_back" tamanho={18} />
        {PROPOSTA_VOLTAR_CATALOGO}
      </Link>
      <h1 className="text-titulo text-navy">{PROPOSTA_TITULO_PAGINA}</h1>
      <p className="text-corpo text-texto-suave">{PROPOSTA_SUBTITULO}</p>
    </header>
  );
}

/** Esqueleto da página enquanto o carro carrega: as mesmas duas colunas da tela pronta. */
function CarregandoProposta() {
  return (
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-12" aria-busy="true">
      <div className="flex flex-col gap-6 lg:col-span-8">
        <Esqueleto className="aspect-[16/9] w-full rounded-cartao" />
        <Esqueleto className="h-40 w-full rounded-cartao" />
      </div>
      <div className="lg:col-span-4">
        <Esqueleto className="h-80 w-full rounded-cartao" />
      </div>
    </div>
  );
}

/**
 * P6-FE-02 · Proposta de compra (RF-27). Sem protótipo: segue o design system e o molde das telas prontas (duas colunas
 * como o cadastro de veículo; contêiner e estados como Minhas reservas). O carro vem de GET /api/veiculos/a-venda/{id};
 * a simulação é refeita enquanto a pessoa digita; o envio exige sessão de cliente e, no sucesso, a confirmação mostra os
 * números gravados pelo servidor.
 */
export default function PropostaCompraPagina() {
  useTituloDocumento(PROPOSTA_TITULO_PAGINA);
  const { veiculoId } = useParams<'veiculoId'>();
  const idVeiculo = lerIdVeiculo(veiculoId);

  // O app não restaura a rolagem entre telas: vinda do catálogo rolado, a proposta abriria no meio. Abre no topo.
  useEffect(() => {
    window.scrollTo({ top: 0 });
  }, [veiculoId]);
  const navegar = useNavigate();
  const localizacao = useLocation();
  const { autenticado, possuiPerfil } = useAutenticacao();

  const consultaVeiculo = useVeiculoAVenda(idVeiculo);
  const aoFalharFoto = useAoFalharFotoCapa(() => {
    void consultaVeiculo.refetch();
  });
  const envio = useEnviarPropostaCompra();

  const [campos, setCampos] = useState<CamposSimulacao>(CAMPOS_SIMULACAO_INICIAIS);
  const [propostaEnviada, setPropostaEnviada] = useState<PropostaCompra | null>(null);
  const [erroEnvio, setErroEnvio] = useState<string | null>(null);

  const veiculo = consultaVeiculo.data ?? null;
  const erros = veiculo ? validarCampos(campos, veiculo.valorVenda) : {};
  const valido = veiculo !== null && camposValidos(erros);
  const valores = converterCampos(campos);
  const simulacao = useSimulacaoParcelamento(
    veiculo ? idVeiculo : null,
    valido ? valores.valorEntrada : null,
    valido ? valores.numeroParcelas : null,
    veiculo?.valorVenda,
  );

  function mudarCampo(campo: keyof CamposSimulacao, valor: string) {
    setCampos((anteriores) => ({ ...anteriores, [campo]: valor }));
    setErroEnvio(null);
  }

  function irParaLogin() {
    const estado: EstadoRedirecionamentoLogin = { de: `${localizacao.pathname}${localizacao.search}` };
    navegar(CAMINHOS.entrar, { state: estado });
  }

  function enviar() {
    if (idVeiculo === null || !valido || valores.valorEntrada === null || valores.numeroParcelas === null) return;
    if (!autenticado) {
      irParaLogin();
      return;
    }
    if (!possuiPerfil(PERFIL_CLIENTE)) {
      setErroEnvio(PROPOSTA_SO_CLIENTE);
      return;
    }
    setErroEnvio(null);
    envio.mutate(
      { idVeiculo, valorEntrada: valores.valorEntrada, numeroParcelas: valores.numeroParcelas },
      {
        onSuccess: (proposta) => {
          setPropostaEnviada(proposta);
          window.scrollTo({ top: 0 });
        },
        onError: (erro) => {
          if (erro instanceof ErroApi && erro.status === STATUS_NAO_AUTENTICADO) {
            irParaLogin();
            return;
          }
          setErroEnvio(obterMensagemErro(erro));
        },
      },
    );
  }

  let conteudo;
  if (propostaEnviada !== null) {
    conteudo = <ConfirmacaoProposta proposta={propostaEnviada} locadora={veiculo?.locadoraNomeFantasia ?? ''} />;
  } else if (idVeiculo === null || (consultaVeiculo.isError && ehNaoEncontrado(consultaVeiculo.error))) {
    conteudo = (
      <div className="flex flex-col items-center gap-4 rounded-cartao bg-branco p-8 text-center shadow-cartao">
        <Icone nome="car_crash" tamanho={48} className="text-texto-suave" />
        <h2 className="text-secao text-navy">{PROPOSTA_VEICULO_INDISPONIVEL_TITULO}</h2>
        <p className="max-w-md text-corpo text-texto-suave">{PROPOSTA_VEICULO_INDISPONIVEL_TEXTO}</p>
        <BotaoLink to={CAMINHOS.seminovos}>{PROPOSTA_VER_CATALOGO}</BotaoLink>
      </div>
    );
  } else if (consultaVeiculo.isError) {
    conteudo = (
      <EstadoErro
        mensagem={obterMensagemErro(consultaVeiculo.error)}
        aoTentarNovamente={() => void consultaVeiculo.refetch()}
        tentando={consultaVeiculo.isFetching}
      />
    );
  } else if (veiculo === null) {
    conteudo = <CarregandoProposta />;
  } else {
    const erroSimulacao = simulacao.isError && valido ? obterMensagemErro(simulacao.error) : null;
    conteudo = (
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
        <div className="flex min-w-0 flex-col gap-6 lg:col-span-8">
          <CartaoVeiculoProposta veiculo={veiculo} aoFalharFoto={aoFalharFoto} />
          <FormularioSimulacao campos={campos} erros={erros} aoMudar={mudarCampo} desabilitado={envio.isPending} />
        </div>
        <div className="min-w-0 lg:col-span-4">
          <div className="lg:sticky lg:top-24">
            <ResumoSimulacao
              simulacao={valido ? (simulacao.data ?? null) : null}
              calculando={valido && simulacao.isFetching}
              erro={erroEnvio ?? erroSimulacao}
              aviso={autenticado ? null : PROPOSTA_ENTRAR_PARA_ENVIAR}
              enviando={envio.isPending}
              podeEnviar={valido && simulacao.data !== undefined && !simulacao.isError}
              aoEnviar={enviar}
            />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-8 px-4 py-8 sm:px-6 lg:px-8">
      <Cabecalho />
      {conteudo}
    </div>
  );
}
