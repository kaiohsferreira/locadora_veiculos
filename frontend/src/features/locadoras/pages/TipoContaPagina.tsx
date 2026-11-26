import type { ReactNode } from 'react';
import { Navigate, Link } from 'react-router-dom';
import { BotaoLink, Icone } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import {
  MENSAGEM_CADASTRO_ABERTURA,
  MENSAGEM_CADASTRO_LINK_LOGIN,
  MENSAGEM_CADASTRO_TRILHA_ROTULO,
  MENSAGEM_CADASTRO_VOLTAR_INICIO,
  MENSAGEM_LOCADORA_ETIQUETA_PESSOA_FISICA,
  MENSAGEM_LOCADORA_ETIQUETA_PESSOA_JURIDICA,
  MENSAGEM_LOCADORA_JA_POSSUI_CADASTRO_TIPO,
  MENSAGEM_LOCADORA_PF_BOTAO,
  MENSAGEM_LOCADORA_PF_TEXTO,
  MENSAGEM_LOCADORA_PF_TITULO,
  MENSAGEM_LOCADORA_PF_VANTAGEM_1,
  MENSAGEM_LOCADORA_PF_VANTAGEM_2,
  MENSAGEM_LOCADORA_PF_VANTAGEM_3,
  MENSAGEM_LOCADORA_PJ_BOTAO,
  MENSAGEM_LOCADORA_PJ_TEXTO,
  MENSAGEM_LOCADORA_PJ_TITULO,
  MENSAGEM_LOCADORA_PJ_VANTAGEM_1,
  MENSAGEM_LOCADORA_PJ_VANTAGEM_2,
  MENSAGEM_LOCADORA_PJ_VANTAGEM_3,
  MENSAGEM_LOCADORA_SELO_AMBIENTE,
  MENSAGEM_LOCADORA_SUBTITULO_TIPO_CONTA,
  MENSAGEM_LOCADORA_TITULO_TIPO_CONTA,
  MENSAGEM_LOCADORA_TITULO_TIPO_CONTA_PAGINA,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';

const FOCO_LINK =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

/** Lista de vantagens de um cartão (ícone de conferido verde, texto ao lado). */
function ListaVantagens({ itens }: { itens: readonly string[] }) {
  return (
    <ul className="mb-8 flex flex-col gap-3">
      {itens.map((item) => (
        <li key={item} className="flex items-start gap-2 text-corpo text-texto">
          <Icone nome="check_circle" tamanho={20} preenchido className="mt-0.5 text-status-sucesso" />
          <span>{item}</span>
        </li>
      ))}
    </ul>
  );
}

/**
 * P10-FE-01 · Tipo de conta (tela 35, rota /cadastro). Duas escolhas: conta de cliente (cadastro do cliente,
 * sem mudança) ou locadora (etapa 1 da locadora). Os textos que o desenho promete sem função no sistema
 * (tarifas corporativas, seminovos "verificados", "em tempo real") foram ajustados ou tirados (ver relatório).
 */
export default function TipoContaPagina() {
  useTituloDocumento(MENSAGEM_LOCADORA_TITULO_TIPO_CONTA);
  const { autenticado } = useAutenticacao();

  // Quem já tem sessão não precisa escolher tipo de conta.
  if (autenticado) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }

  return (
    <div className="relative w-full overflow-clip py-8">
      {/* Brilhos de fundo do desenho (decorativos). */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -left-24 -top-32 h-96 w-96 rounded-xl bg-status-pendente/5 blur-3xl"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -right-32 top-1/2 h-[32rem] w-[32rem] rounded-xl bg-navy/5 blur-3xl"
      />

      <div className="relative mx-auto max-w-[1360px] px-6">
        <nav aria-label={MENSAGEM_CADASTRO_TRILHA_ROTULO} className="mb-8">
          <ol className="flex flex-wrap items-center gap-1 text-corpo-denso text-status-neutro">
            <li>
              <Link
                to={CAMINHOS.inicio}
                className={`flex items-center gap-1 rounded transition-colors hover:text-navy ${FOCO_LINK}`}
              >
                <Icone nome="arrow_back" tamanho={16} />
                {MENSAGEM_CADASTRO_VOLTAR_INICIO}
              </Link>
            </li>
            <li aria-hidden="true" className="text-borda-forte">
              /
            </li>
            <li aria-current="page" className="text-navy">
              {MENSAGEM_CADASTRO_ABERTURA}
            </li>
          </ol>
        </nav>

        <div className="mx-auto max-w-4xl">
          <div className="mx-auto mb-10 max-w-2xl text-center">
            <span className="inline-flex items-center gap-1.5 rounded-sm bg-steel-alto px-3 py-1 text-micro uppercase tracking-wider text-navy">
              <Icone nome="shield_person" tamanho={14} />
              {MENSAGEM_LOCADORA_SELO_AMBIENTE}
            </span>
            <h1 className="mt-3 text-titulo tracking-tight text-navy">{MENSAGEM_LOCADORA_TITULO_TIPO_CONTA_PAGINA}</h1>
            <p className="mt-2 text-corpo text-status-neutro">{MENSAGEM_LOCADORA_SUBTITULO_TIPO_CONTA}</p>
          </div>

          <div className="grid grid-cols-1 items-stretch gap-6 md:grid-cols-2">
            <CartaoTipoConta
              titulo={MENSAGEM_LOCADORA_PF_TITULO}
              texto={MENSAGEM_LOCADORA_PF_TEXTO}
              etiqueta={MENSAGEM_LOCADORA_ETIQUETA_PESSOA_FISICA}
              icone="person"
              classesIcone="bg-steel-fundo text-navy"
              classesEtiqueta="bg-steel-fundo text-navy"
              vantagens={[MENSAGEM_LOCADORA_PF_VANTAGEM_1, MENSAGEM_LOCADORA_PF_VANTAGEM_2, MENSAGEM_LOCADORA_PF_VANTAGEM_3]}
              acao={
                <Link
                  to={CAMINHOS.cadastroDadosPessoais}
                  className={`flex w-full items-center justify-center gap-2 rounded-md bg-steel-fundo px-6 py-3 text-corpo font-semibold text-navy transition-colors hover:bg-steel-suave ${FOCO_LINK}`}
                >
                  <span>{MENSAGEM_LOCADORA_PF_BOTAO}</span>
                  <Icone nome="arrow_forward" tamanho={18} />
                </Link>
              }
            />

            <CartaoTipoConta
              destacado
              titulo={MENSAGEM_LOCADORA_PJ_TITULO}
              texto={MENSAGEM_LOCADORA_PJ_TEXTO}
              etiqueta={MENSAGEM_LOCADORA_ETIQUETA_PESSOA_JURIDICA}
              etiquetaIcone="verified"
              icone="storefront"
              classesIcone="bg-orange-claro text-orange"
              classesEtiqueta="bg-orange-claro text-status-pendente"
              vantagens={[MENSAGEM_LOCADORA_PJ_VANTAGEM_1, MENSAGEM_LOCADORA_PJ_VANTAGEM_2, MENSAGEM_LOCADORA_PJ_VANTAGEM_3]}
              acao={
                <BotaoLink
                  to={CAMINHOS.cadastroLocadoraEmpresa}
                  tamanho="grande"
                  larguraTotal
                  iconeDireita="rocket_launch"
                  className="shadow-lg"
                >
                  {MENSAGEM_LOCADORA_PJ_BOTAO}
                </BotaoLink>
              }
            />
          </div>

          <div className="mt-10 text-center">
            <p className="inline-flex flex-wrap items-center justify-center gap-1 text-corpo text-status-neutro">
              <span>{MENSAGEM_LOCADORA_JA_POSSUI_CADASTRO_TIPO}</span>
              <Link
                to={CAMINHOS.entrar}
                className={`inline-flex items-center gap-0.5 rounded font-semibold text-navy underline decoration-status-pendente underline-offset-4 ${FOCO_LINK}`}
              >
                <span>{MENSAGEM_CADASTRO_LINK_LOGIN}</span>
                <Icone nome="chevron_right" tamanho={16} />
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

interface CartaoTipoContaProps {
  titulo: string;
  texto: string;
  etiqueta: string;
  /** Ícone da etiqueta (opcional; só o cartão da locadora tem). */
  etiquetaIcone?: string;
  icone: string;
  classesIcone: string;
  classesEtiqueta: string;
  vantagens: readonly string[];
  /** Ação do rodapé do cartão (link ou botão), já montada pela página. */
  acao: ReactNode;
  /** Cartão em destaque: tarja laranja no topo e sombra maior. */
  destacado?: boolean;
}

/** Cartão de um tipo de conta: ícone, etiqueta, título, texto, vantagens e a ação. */
function CartaoTipoConta({
  titulo,
  texto,
  etiqueta,
  etiquetaIcone,
  icone,
  classesIcone,
  classesEtiqueta,
  vantagens,
  acao,
  destacado = false,
}: CartaoTipoContaProps) {
  return (
    <section
      aria-label={titulo}
      className={`relative flex flex-col justify-between overflow-hidden rounded-lg bg-branco p-6 shadow-sm transition-shadow duration-200 hover:shadow-md md:p-8 ${
        destacado ? 'shadow-md hover:shadow-lg' : ''
      }`}
    >
      {destacado && <span aria-hidden="true" className="absolute inset-x-0 top-0 h-2 bg-orange" />}

      <div>
        <div className="mb-6 flex items-center justify-between gap-4">
          <span
            aria-hidden="true"
            className={`flex h-14 w-14 shrink-0 items-center justify-center rounded-full shadow-inner ${classesIcone}`}
          >
            <Icone nome={icone} tamanho={28} preenchido={destacado} />
          </span>
          <span
            className={`inline-flex items-center gap-1.5 rounded-sm px-2.5 py-1 text-legenda font-semibold uppercase tracking-wider ${classesEtiqueta}`}
          >
            {etiquetaIcone !== undefined && <Icone nome={etiquetaIcone} tamanho={14} />}
            {etiqueta}
          </span>
        </div>

        <h2 className="mb-2 text-secao text-navy">{titulo}</h2>
        <p className="mb-6 text-corpo text-status-neutro">{texto}</p>

        <ListaVantagens itens={vantagens} />
      </div>

      <div className="mt-auto pt-2">{acao}</div>
    </section>
  );
}
