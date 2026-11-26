import { Icone, ImagemVeiculo } from '@/components/ui';
import { CronometroDevolucao } from '@/features/area-cliente/locacoes/componentes/CronometroDevolucao';
import {
  calcularContagemRegressiva,
  combustivelCheio,
  locacaoEstaAtrasada,
  percentualCombustivel,
  percentualProgresso,
} from '@/features/area-cliente/locacoes/regrasLocacoes';
import { useAgora } from '@/features/area-cliente/locacoes/hooks/useAgora';
import { FotoVeiculo } from '@/features/area-cliente/reservas/componentes/FotoVeiculo';
import { montarEnderecoLocadora, montarLinkMapa } from '@/features/area-cliente/reservas/enderecoLocadora';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import { formatarPlaca, formatarQuilometragem } from '@/lib/formatters';
import {
  mensagemLocacoesCodigoReserva,
  mensagemLocacoesCombustivel,
  mensagemLocacoesLocalEndereco,
  mensagemLocacoesRetiradoEm,
  MENSAGEM_LOCACOES_COMBUSTIVEL_NAO_INFORMADO,
  MENSAGEM_LOCACOES_COMBUSTIVEL_RETIRADA,
  MENSAGEM_LOCACOES_DEVOLUCAO_PREVISTA,
  MENSAGEM_LOCACOES_KM_RETIRADA,
  MENSAGEM_LOCACOES_RETIRADA_EFETUADA,
  MENSAGEM_LOCACOES_SITUACAO_ATIVA,
  MENSAGEM_LOCACOES_SITUACAO_ATRASADA,
} from '@/lib/mensagensLocacoesCliente';
import { mensagemLocacaoNumero } from '@/lib/mensagensPagamentosCliente';
import { mensagemReservasDiarias, MENSAGEM_RESERVAS_COMO_CHEGAR, MENSAGEM_RESERVAS_COMO_CHEGAR_NOVA_ABA } from '@/lib/mensagensReservas';
import type { LocacaoCliente } from '@/types/locacoesCliente';

/** Intervalo do cronômetro: a contagem atualiza a cada segundo. */
const INTERVALO_CRONOMETRO_MS = 1000;

/**
 * Caixa da foto no celular, dentro da caixa steel-fundo: sem foto, steel-suave (anexo, item 5). No computador a foto
 * fica no bloco grande, com a caixa própria.
 */
const CLASSE_FUNDO_FOTO_CELULAR = 'bg-steel-suave md:bg-steel-fundo';

/**
 * Bloco grande da foto (desenho 27, 16/10). Entre md (768 px) e xl (1280 px) o cartão empilha numa coluna e a caixa
 * ocupa a largura toda: a altura é limitada e a foto fica inteira e centralizada (object-contain). A partir de xl, o
 * desenho de duas colunas volta, com a foto cortada no formato como antes.
 */
export const CLASSE_CAIXA_FOTO_GRANDE =
  'relative hidden aspect-[16/10] w-full overflow-hidden rounded-xl bg-steel-fundo p-2 md:block md:max-h-80 md:[&_img]:!object-contain xl:max-h-none xl:[&_img]:!object-cover';

export interface CartaoLocacaoAtivaProps {
  locacao: LocacaoCliente;
  /** Chamado quando a foto de capa falha ao abrir. A tela decide se refaz a consulta. */
  aoFalharFotoCapa?: () => void;
}

/**
 * Cartão da locação em andamento (desenho 27): veículo e foto à esquerda; locadora, nome, roteiro, cronômetro e
 * "Como chegar" à direita. Em telas pequenas tudo empilha numa coluna, como na tela 28. A data da devolução vem do
 * servidor e o cronômetro só a lê.
 */
export function CartaoLocacaoAtiva({ locacao, aoFalharFotoCapa }: CartaoLocacaoAtivaProps) {
  const agora = useAgora(INTERVALO_CRONOMETRO_MS);
  const contagem = calcularContagemRegressiva(locacao.devolucaoPrevistaEfetiva, agora);
  const atrasada = locacaoEstaAtrasada(locacao.atrasada, contagem);
  const progresso = percentualProgresso(locacao.dataRetirada, locacao.devolucaoPrevistaEfetiva, agora);
  const endereco = montarEnderecoLocadora(locacao);
  const titulo = `${locacao.marca} ${locacao.modelo}`;
  const nivel = locacao.combustivelRetirada;

  return (
    <article
      aria-labelledby={`locacao-${locacao.idLocacao}-titulo`}
      className="flex flex-col gap-8 overflow-hidden rounded-2xl bg-branco p-4 shadow-xl lg:p-6 xl:flex-row"
    >
      <div className="flex flex-col justify-between gap-4 xl:w-5/12">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-rotulo font-bold ${
              atrasada ? 'bg-status-perigo-fundo text-status-perigo' : 'bg-steel-alto text-navy-profundo'
            }`}
          >
            <span
              aria-hidden="true"
              className={`h-2 w-2 rounded-full motion-safe:animate-pulse ${atrasada ? 'bg-status-perigo' : 'bg-orange-acao'}`}
            />
            {atrasada ? MENSAGEM_LOCACOES_SITUACAO_ATRASADA : MENSAGEM_LOCACOES_SITUACAO_ATIVA}
          </span>
          <span className="rounded-full bg-steel-suave px-3 py-1 text-rotulo font-semibold text-navy-profundo">
            {mensagemLocacaoNumero(locacao.idLocacao)}
          </span>
          {locacao.codigoReserva && (
            <span className="rounded-full bg-steel-suave px-3 py-1 text-rotulo font-semibold text-navy-profundo">
              {mensagemLocacoesCodigoReserva(locacao.codigoReserva)}
            </span>
          )}
        </div>

        <div className={CLASSE_CAIXA_FOTO_GRANDE}>
          <ImagemVeiculo
            url={locacao.fotoCapa ? urlDaImagem(locacao.fotoCapa) : null}
            alt={titulo}
            ajuste="cover"
            className="h-full w-full rounded-lg"
            tamanhoIcone={64}
            aoFalhar={aoFalharFotoCapa}
          />
          <span className="absolute bottom-3 left-3 flex items-center gap-1.5 rounded bg-branco px-2.5 py-1 shadow-md">
            <span aria-hidden="true" className="inline-block h-2 w-2.5 rounded-sm bg-navy-claro" />
            <span className="text-corpo-denso font-bold tracking-widest text-navy-profundo">{formatarPlaca(locacao.placa)}</span>
          </span>
          <span className="absolute right-3 top-3 rounded bg-navy-profundo/80 px-2 py-1 text-legenda font-bold text-branco">
            {locacao.categoriaNome}
          </span>
        </div>

        {/* Celular (desenho 27, item 5 do anexo): foto compacta ao lado do texto; categoria acima do nome e placa abaixo. */}
        <div className="flex min-w-0 items-center gap-3 rounded bg-steel-fundo p-3 md:hidden">
          <FotoVeiculo
            fotoCapa={locacao.fotoCapa}
            descricao={titulo}
            compacta
            aoFalhar={aoFalharFotoCapa}
            classeFundo={CLASSE_FUNDO_FOTO_CELULAR}
          />
          <div className="flex min-w-0 flex-1 flex-col items-start gap-1">
            <span className="inline-block rounded-sm bg-steel-suave px-2.5 py-0.5 text-micro font-semibold uppercase tracking-wider text-navy-profundo">
              {locacao.categoriaNome}
            </span>
            <h2 className="break-words text-corpo font-bold leading-tight tracking-tighter text-navy-profundo">{titulo}</h2>
            <span className="inline-flex h-6 items-center gap-1.5 overflow-hidden rounded-sm bg-branco px-2 shadow-sm">
              <span aria-hidden="true" className="inline-block h-2 w-2.5 rounded-sm bg-navy-claro" />
              <span className="text-micro font-bold tracking-widest text-navy-profundo">{formatarPlaca(locacao.placa)}</span>
            </span>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2 rounded-xl bg-steel-fundo p-3 text-center">
          <div className="flex flex-col">
            <span className="text-legenda text-texto-apoio">{MENSAGEM_LOCACOES_KM_RETIRADA}</span>
            <span className="text-corpo-denso font-bold text-navy-profundo">{formatarQuilometragem(locacao.kmSaida)}</span>
          </div>
          <div className="flex flex-col">
            <span className="text-legenda text-texto-apoio">{MENSAGEM_LOCACOES_COMBUSTIVEL_RETIRADA}</span>
            <span className="text-corpo-denso font-bold text-navy-profundo">
              {nivel == null
                ? MENSAGEM_LOCACOES_COMBUSTIVEL_NAO_INFORMADO
                : mensagemLocacoesCombustivel(percentualCombustivel(nivel), combustivelCheio(nivel))}
            </span>
          </div>
        </div>
      </div>

      <div className="flex min-w-0 flex-col justify-between gap-4 xl:w-7/12">
        <div className="flex flex-col gap-2">
          <p className="flex items-center gap-1 text-rotulo font-semibold uppercase tracking-wide text-frota-oficina">
            <Icone nome="location_on" tamanho={16} />
            {mensagemLocacoesRetiradoEm(locacao.locadoraNomeFantasia)}
          </p>
          {/* Computador: o título fica aqui. No celular o nome aparece na caixa compacta (acima). */}
          <h2
            id={`locacao-${locacao.idLocacao}-titulo`}
            className="hidden break-words text-marca font-bold tracking-tight text-navy-profundo md:block sm:text-titulo-acesso"
          >
            {titulo}
          </h2>
        </div>

        <div className="hidden flex-col gap-4 rounded-xl bg-steel-fundo p-4 md:flex md:flex-row md:items-center md:justify-between">
          <div className="flex items-start gap-3">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-steel-linha text-navy-profundo">
              <Icone nome="key" tamanho={20} />
            </div>
            <div className="flex min-w-0 flex-col">
              <span className="text-micro font-semibold uppercase text-texto-apoio">{MENSAGEM_LOCACOES_RETIRADA_EFETUADA}</span>
              <span className="text-corpo font-bold text-navy-profundo">{locacao.dataRetiradaStr}</span>
              <span className="break-words text-legenda text-texto-apoio">
                {mensagemLocacoesLocalEndereco(locacao.locadoraNomeFantasia, endereco)}
              </span>
            </div>
          </div>
          <div className="hidden flex-col items-center px-4 md:flex">
            <Icone nome="trending_flat" tamanho={24} className="text-texto-apoio" />
            <span className="text-legenda text-texto-apoio">{mensagemReservasDiarias(locacao.diarias)}</span>
          </div>
          <div className="flex items-start gap-3">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-orange-acao/15 text-frota-oficina">
              <Icone nome="flag" tamanho={20} />
            </div>
            <div className="flex min-w-0 flex-col">
              <span className="text-micro font-semibold uppercase text-frota-oficina">{MENSAGEM_LOCACOES_DEVOLUCAO_PREVISTA}</span>
              <span className="text-corpo font-bold text-navy-profundo">{locacao.devolucaoPrevistaEfetivaStr}</span>
              <span className="break-words text-legenda text-texto-apoio">
                {mensagemLocacoesLocalEndereco(locacao.locadoraNomeFantasia, endereco)}
              </span>
            </div>
          </div>
        </div>

        {/* Celular (desenho 27, item 6 do anexo): linha vertical com os dois pontos e o selo de diárias no topo, como na 28. */}
        <div className="flex flex-col gap-3 rounded-xl bg-steel-fundo p-4 md:hidden">
          <div className="flex justify-end">
            <span className="rounded-sm bg-steel-suave px-2 py-0.5 text-micro font-semibold text-navy-profundo">
              {mensagemReservasDiarias(locacao.diarias)}
            </span>
          </div>
          <ol className="relative space-y-4 before:absolute before:bottom-4 before:left-3 before:top-4 before:w-0.5 before:bg-borda-forte">
            <li className="relative pl-8">
              <span
                aria-hidden="true"
                className="absolute left-1 top-1 flex h-4 w-4 items-center justify-center rounded-full bg-orange-acao ring-4 ring-steel-fundo"
              >
                <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-branco" />
              </span>
              <div className="flex min-w-0 flex-col gap-0.5">
                <p className="text-micro font-bold uppercase text-frota-oficina">{MENSAGEM_LOCACOES_RETIRADA_EFETUADA}</p>
                <p className="text-corpo font-bold tabular-nums text-navy-profundo">{locacao.dataRetiradaStr}</p>
                <p className="break-words text-corpo-denso font-semibold text-navy-profundo">{locacao.locadoraNomeFantasia}</p>
                {endereco && <p className="break-words text-legenda text-texto-apoio">{endereco}</p>}
              </div>
            </li>
            <li className="relative pl-8">
              <span
                aria-hidden="true"
                className="absolute left-1 top-1 flex h-4 w-4 items-center justify-center rounded-full bg-navy-profundo ring-4 ring-steel-fundo"
              >
                <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-branco" />
              </span>
              <div className="flex min-w-0 flex-col gap-0.5">
                <p className="text-micro font-bold uppercase text-navy-profundo">{MENSAGEM_LOCACOES_DEVOLUCAO_PREVISTA}</p>
                <p className="text-corpo font-bold tabular-nums text-navy-profundo">{locacao.devolucaoPrevistaEfetivaStr}</p>
                <p className="break-words text-corpo-denso font-semibold text-navy-profundo">{locacao.locadoraNomeFantasia}</p>
                {endereco && <p className="break-words text-legenda text-texto-apoio">{endereco}</p>}
              </div>
            </li>
          </ol>
        </div>

        <CronometroDevolucao
          devolucaoPrevistaEfetiva={locacao.devolucaoPrevistaEfetiva}
          toleranciaAtrasoMinutos={locacao.toleranciaAtrasoMinutos}
          atrasadaServidor={locacao.atrasada}
          progresso={progresso}
          agora={agora}
        />

        {endereco && (
          <a
            href={montarLinkMapa(locacao)}
            target="_blank"
            rel="noreferrer"
            className="inline-flex min-h-10 w-full items-center justify-center gap-2 rounded-sm bg-steel-suave px-4 text-corpo-denso font-semibold text-navy-profundo transition-colors hover:bg-steel-alto focus-visible:outline focus-visible:outline-2 focus-visible:outline-steel sm:w-auto sm:self-start"
          >
            <Icone nome="near_me" tamanho={18} className="text-orange-acao" />
            <span>{MENSAGEM_RESERVAS_COMO_CHEGAR}</span>
            <span className="sr-only">{MENSAGEM_RESERVAS_COMO_CHEGAR_NOVA_ABA}</span>
          </a>
        )}
      </div>
    </article>
  );
}
