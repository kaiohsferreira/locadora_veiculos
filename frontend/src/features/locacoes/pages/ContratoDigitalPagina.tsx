import { Link, generatePath, useParams } from 'react-router-dom';
import { BotaoLink, EstadoErro, Etiqueta, Icone } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGEM_AREA_NAO_PERMITIDA, MENSAGEM_REGISTRO_NAO_ENCONTRADO } from '@/lib/mensagens';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import { MENSAGENS_VISTORIA } from '@/lib/mensagensVistoria';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { ContratoLocacao } from '@/types/contratoLocacao';
import { lerIdLocacao } from '../vistoria/validacaoVistoria';
import { AreaAssinatura } from '../contrato/AreaAssinatura';
import { PapelContrato, EstiloImpressaoContrato } from '../contrato/PapelContrato';
import { CartaoVeiculoContrato, CartoesResumo, TrilhaContrato } from '../contrato/ResumoContrato';
import { ehSemVistoriaEntrada } from '../contrato/regrasContrato';
import { useContratoLocacao } from '../contrato/useContratoLocacao';

/**
 * Emissão e assinatura do contrato digital (tela 08, P4-FE-03). A página lê o documento do servidor (montado na hora
 * enquanto não há assinatura; a cópia guardada depois dela) e não calcula valor nem texto de cláusula.
 */
export default function ContratoDigitalPagina() {
  const { locacaoId } = useParams<'locacaoId'>();
  const id = lerIdLocacao(locacaoId);
  const consulta = useContratoLocacao(id);
  useTituloDocumento(M.titulo);

  if (id === null) {
    return (
      <div className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
        <EstadoErro titulo={M.erroCarregar} mensagem={MENSAGENS_VISTORIA.idInvalido} />
      </div>
    );
  }
  if (consulta.isPending) return <CarregandoPagina />;
  if (consulta.isError) {
    return (
      <ErroContrato
        idLocacao={id}
        erro={consulta.error}
        tentando={consulta.isFetching}
        aoTentarNovamente={() => {
          void consulta.refetch();
        }}
      />
    );
  }
  return <ConteudoContrato contrato={consulta.data} idLocacao={id} />;
}

interface PropsErroContrato {
  idLocacao: number;
  erro: unknown;
  tentando: boolean;
  aoTentarNovamente: () => void;
}

/** Erros da leitura. 409 sem vistoria de entrada (RP-06) leva ao registro da vistoria; 403 e 404 seguem o desenho. */
function ErroContrato({ idLocacao, erro, tentando, aoTentarNovamente }: PropsErroContrato) {
  const mensagem = obterMensagemErro(erro);
  const status = erro instanceof ErroApi ? erro.status : 0;

  if (status === 403) {
    return (
      <div className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
        <EstadoErro titulo={MENSAGEM_AREA_NAO_PERMITIDA} />
      </div>
    );
  }
  if (status === 404) {
    return (
      <div className="mx-auto flex w-full max-w-7xl flex-col items-center gap-4 px-4 py-8 sm:px-6">
        <EstadoErro titulo={MENSAGEM_REGISTRO_NAO_ENCONTRADO} />
        <BotaoLink to={CAMINHOS.retiradas} variante="contorno" icone="arrow_back">
          {MENSAGENS_RETIRADA.voltarRetiradas}
        </BotaoLink>
      </div>
    );
  }
  if (status === 409 && ehSemVistoriaEntrada(mensagem)) {
    return (
      <div className="mx-auto flex w-full max-w-7xl flex-col items-center gap-4 px-4 py-8 sm:px-6">
        <EstadoErro titulo={M.contratoIndisponivel} mensagem={mensagem} />
        <BotaoLink
          to={generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(idLocacao) })}
          icone="fact_check"
        >
          {M.registrarVistoriaEntrada}
        </BotaoLink>
      </div>
    );
  }
  return (
    <div className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
      <EstadoErro
        titulo={status === 409 ? undefined : M.erroCarregar}
        mensagem={mensagem}
        aoTentarNovamente={status === 409 ? undefined : aoTentarNovamente}
        rotuloTentarNovamente={M.tentarNovamente}
        tentando={tentando}
      />
    </div>
  );
}

interface PropsConteudoContrato {
  contrato: ContratoLocacao;
  idLocacao: number;
}

function ConteudoContrato({ contrato, idLocacao }: PropsConteudoContrato) {
  const { documento } = contrato;
  const assinado = contrato.situacao === 'ASSINADO';

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-4 py-8 sm:px-6">
      <EstiloImpressaoContrato />

      <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div className="flex flex-col gap-2">
          <nav aria-label={MENSAGENS_RETIRADA.trilhaRotulo} className="flex items-center gap-1 text-micro text-texto-apoio">
            <Link to={CAMINHOS.locacoes} className="transition-colors hover:text-navy-profundo">
              {MENSAGENS_RETIRADA.trilhaLocacoes}
            </Link>
            <Icone nome="chevron_right" tamanho={14} className="text-contorno" />
            <span>{MENSAGENS_RETIRADA.trilhaDespacho}</span>
            <Icone nome="chevron_right" tamanho={14} className="text-contorno" />
            <span aria-current="page" className="font-semibold text-frota-oficina">
              {M.migalha}
            </span>
          </nav>
          <div className="flex flex-wrap items-baseline gap-x-4 gap-y-2">
            <h1 className="text-titulo-documentos font-semibold tracking-tight text-texto-desenho">{M.titulo}</h1>
            {assinado && <Etiqueta tom="sucesso" rotulo={M.seloAssinado} />}
          </div>
          <p className="text-corpo-denso text-texto-apoio">{M.subtitulo}</p>
        </div>
        <div className="flex flex-wrap gap-2 rounded-xl bg-branco p-2 shadow-sm">
          <div className="flex items-center gap-2 rounded-lg bg-steel-fundo px-3 py-1">
            <Icone nome="person_check" tamanho={18} className="text-orange-acao" />
            <div className="flex flex-col">
              <span className="text-apoio text-texto-apoio">{M.rotuloLocatario}</span>
              <span className="text-corpo-denso font-semibold text-texto-desenho">{documento.locatario.nome}</span>
            </div>
          </div>
          <div className="flex items-center gap-2 rounded-lg bg-steel-fundo px-3 py-1">
            <Icone nome="pin" tamanho={18} className="text-contorno" />
            <div className="flex flex-col">
              <span className="text-apoio text-texto-apoio">{M.rotuloContrato}</span>
              <span className="text-corpo-denso font-semibold text-texto-desenho">{M.numeroLocacao(documento.locacao.numero)}</span>
            </div>
          </div>
        </div>
      </header>

      <TrilhaContrato contrato={contrato} />

      <div className="grid grid-cols-1 items-start gap-6 lg:grid-cols-12">
        <div className="lg:col-span-7">
          <PapelContrato contrato={contrato} />
        </div>
        <div className="flex flex-col gap-4 lg:col-span-5">
          <CartoesResumo documento={documento} />
          <AreaAssinatura contrato={contrato} idLocacao={idLocacao} />
          <CartaoVeiculoContrato documento={documento} />
        </div>
      </div>
    </div>
  );
}
