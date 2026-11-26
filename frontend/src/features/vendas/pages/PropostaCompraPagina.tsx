import { useEffect, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { BotaoLink, EstadoErro, Esqueleto, Icone } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { useEnviarPropostaCompra } from '@/features/vendas/hooks/useEnviarPropostaCompra';
import { useSimulacaoParcelamento } from '@/features/vendas/hooks/useSimulacaoParcelamento';
import { useVeiculoAVenda } from '@/features/vendas/hooks/useVeiculoAVenda';
import { camposValidos, converterCampos, validarCampos, type CamposSimulacao } from '@/features/vendas/proposta/camposSimulacao';
import { CartaoVeiculoProposta } from '@/features/vendas/proposta/CartaoVeiculoProposta';
import { ConfirmacaoProposta } from '@/features/vendas/proposta/ConfirmacaoProposta';
import { decidirAntesDoEnvio, decidirDepoisDoEnvio } from '@/features/vendas/proposta/decisaoEnvio';
import { lerCamposDoEndereco, montarRetornoLogin } from '@/features/vendas/proposta/enderecoProposta';
import { simulacaoEhDosValores } from '@/features/vendas/proposta/regrasSimulacao';
import { SimuladorProposta } from '@/features/vendas/proposta/SimuladorProposta';
import { lerIdVeiculo } from '@/features/vendas/proposta/textosProposta';
import { TrilhaProposta } from '@/features/vendas/proposta/TrilhaProposta';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import {
  PROPOSTA_ENTRAR_PARA_ENVIAR,
  PROPOSTA_SO_CLIENTE,
  PROPOSTA_SUBTITULO,
  PROPOSTA_TITULO_DOCUMENTO,
  PROPOSTA_TITULO_PAGINA,
  PROPOSTA_VEICULO_INDISPONIVEL_TEXTO,
  PROPOSTA_VEICULO_INDISPONIVEL_TITULO,
  PROPOSTA_VER_CATALOGO,
} from '@/lib/mensagensProposta';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS, type EstadoRedirecionamentoLogin } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { PropostaCompra } from '@/types/vendas';

const STATUS_NAO_ENCONTRADO = 404;
const PERFIL_CLIENTE = ['cliente'] as const;

/** Erro 404 da API (veículo fora do catálogo). */
function ehNaoEncontrado(erro: unknown): boolean {
  return erro instanceof ErroApi && erro.status === STATUS_NAO_ENCONTRADO;
}

/** Cabeçalho do desenho 31: título e subtítulo. */
function Cabecalho() {
  return (
    <header className="flex flex-col gap-1">
      <h1 className="text-titulo font-bold tracking-tight text-navy">{PROPOSTA_TITULO_PAGINA}</h1>
      <p className="text-corpo text-texto-suave">{PROPOSTA_SUBTITULO}</p>
    </header>
  );
}

/** Esqueleto da página enquanto o carro carrega: as mesmas duas colunas da tela pronta. */
function CarregandoProposta() {
  return (
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-12" aria-busy="true">
      <div className="flex flex-col gap-6 lg:col-span-5">
        <Esqueleto className="aspect-[16/10] w-full rounded-cartao" />
        <Esqueleto className="h-40 w-full rounded-cartao" />
      </div>
      <div className="lg:col-span-7">
        <Esqueleto className="h-96 w-full rounded-cartao" />
      </div>
    </div>
  );
}

/**
 * P6-FE-02 · Proposta de compra (RF-27), desenho 31_proposta_de_compra_jeep_renegade. O carro vem de
 * GET /api/veiculos/a-venda/{id}; a simulação é refeita no servidor a cada mudança e, enquanto o resultado não é o dos
 * valores dos campos, a tela mostra "Calculando…" e não deixa enviar. Na ida para o login, a entrada e as parcelas vão no
 * endereço de volta e são restauradas. O envio exige sessão de cliente; a confirmação mostra o código e os números
 * gravados pelo servidor.
 *
 * Fora do desenho por não existirem no sistema: o selo "AutoVia Seminovos Premium", o "ID Negociação" antes do envio,
 * "Crédito Pré-aprovado via CPF", a faixa "Ambiente Criptografado" e o aviso final do CPF.
 */
export default function PropostaCompraPagina() {
  useTituloDocumento(PROPOSTA_TITULO_DOCUMENTO);
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

  // Na volta do login, os campos vêm do endereço (montarRetornoLogin); sem eles, R$ 0,00 e 12 parcelas.
  const [campos, setCampos] = useState<CamposSimulacao>(() => lerCamposDoEndereco(localizacao.search));
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
  const simulacaoAtual =
    valido && simulacaoEhDosValores(simulacao.data, idVeiculo, valores.valorEntrada, valores.numeroParcelas);
  const calculando = valido && !simulacaoAtual && !simulacao.isError;

  function mudarCampo(campo: keyof CamposSimulacao, valor: string) {
    setCampos((anteriores) => ({ ...anteriores, [campo]: valor }));
    setErroEnvio(null);
  }

  function irParaLogin() {
    const estado: EstadoRedirecionamentoLogin = { de: montarRetornoLogin(localizacao.pathname, campos) };
    navegar(CAMINHOS.entrar, { state: estado });
  }

  function enviar() {
    if (idVeiculo === null || !simulacaoAtual || valores.valorEntrada === null || valores.numeroParcelas === null) return;
    const decisao = decidirAntesDoEnvio(autenticado, possuiPerfil(PERFIL_CLIENTE));
    if (decisao === 'irParaLogin') {
      irParaLogin();
      return;
    }
    if (decisao === 'avisarSoCliente') {
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
          if (decidirDepoisDoEnvio(erro) === 'irParaLogin') {
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
      <div className="grid grid-cols-1 items-start gap-8 lg:grid-cols-12">
        <div className="min-w-0 lg:col-span-5">
          <CartaoVeiculoProposta veiculo={veiculo} aoFalharFoto={aoFalharFoto} />
        </div>
        <div className="min-w-0 lg:col-span-7">
          <SimuladorProposta
            veiculo={veiculo}
            campos={campos}
            erros={erros}
            aoMudar={mudarCampo}
            simulacao={simulacaoAtual ? (simulacao.data ?? null) : null}
            calculando={calculando}
            erro={erroEnvio ?? erroSimulacao}
            aviso={autenticado ? null : PROPOSTA_ENTRAR_PARA_ENVIAR}
            enviando={envio.isPending}
            podeEnviar={simulacaoAtual && !simulacao.isError}
            aoEnviar={enviar}
          />
        </div>
      </div>
    );
  }

  const nomeVeiculo = veiculo && propostaEnviada === null ? `${veiculo.marca} ${veiculo.modelo}` : null;

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <TrilhaProposta nomeVeiculo={nomeVeiculo} />
      <Cabecalho />
      {conteudo}
    </div>
  );
}
