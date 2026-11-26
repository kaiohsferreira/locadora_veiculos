import { useEffect, useMemo, useRef, useState } from 'react';
import { Botao, EstadoErro, Icone } from '@/components/ui';
import { oitavosDoNivel } from '@/components/ui/calculoCombustivel';
import { VALOR_AUSENTE, formatarMoeda, formatarNumero } from '@/lib/formatters';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { ContextoDevolucao } from '@/types/devolucao';
import type { CalculoCobrancasExtras } from '@/types/cobrancasExtras';
import type { useEncerrarLocacao } from '../hooks/useDevolucao';
import type { useSimulacaoEncerramento } from '../hooks/useSimulacaoEncerramento';
import {
  dadosDeEncerramento,
  falhaDoEncerramento,
  podeIniciarEncerramento,
  quilometrosRodados,
  rotuloSeloTotal,
  type DesfechoCaucaoPrevisto,
} from '../regras/regrasDevolucao';
import { CaixaConfirmacaoEncerramento } from './CaixaConfirmacaoEncerramento';

type ConsultaSimulacao = ReturnType<typeof useSimulacaoEncerramento>;
type ConsultaEncerramento = ReturnType<typeof useEncerrarLocacao>;

export interface CartaoCobrancasExtrasProps {
  contexto: ContextoDevolucao;
  /** Memória de cálculo do servidor (simulação, sem gravar). O total confirmado não sai daqui. */
  simulacao: ConsultaSimulacao;
  encerramento: ConsultaEncerramento;
  /** Nulo quando o servidor não trouxe o total (sem política ou sem saída): a caução e o selo não são mostrados. */
  desfecho: DesfechoCaucaoPrevisto | null;
}

interface LinhaCobrancaProps {
  titulo: string;
  apoio: string;
  valor: string;
  destaque: boolean;
}

function LinhaCobranca({ titulo, apoio, valor, destaque }: LinhaCobrancaProps) {
  return (
    <li className="flex items-start justify-between gap-3">
      <div className="flex min-w-0 flex-col">
        <span className="text-corpo-denso font-medium text-navy-profundo">{titulo}</span>
        <span className={`text-apoio break-words ${destaque ? 'text-frota-oficina' : 'text-texto-apoio'}`}>{apoio}</span>
      </div>
      <span className="shrink-0 text-dado-tabular font-semibold text-navy-profundo">{valor}</span>
    </li>
  );
}

/** Linhas do detalhamento, na ordem do desenho (km, combustível, atraso e avarias), vindas da memória do servidor. */
function LinhasDetalhamento({ calculo, franquiaKm }: { calculo: CalculoCobrancasExtras; franquiaKm: number }) {
  const { quilometragem, combustivel, atraso, avarias } = calculo;
  return (
    <>
      <LinhaCobranca
        titulo={M.kmExcedenteTitulo}
        apoio={M.kmExcedenteApoio(formatarNumero(quilometrosRodados(quilometragem.kmEntrada, quilometragem.kmSaida)), franquiaKm)}
        valor={formatarMoeda(quilometragem.valor)}
        destaque={false}
      />
      <LinhaCobranca
        titulo={M.combustivelItem}
        apoio={M.combustivelApoio(oitavosDoNivel(combustivel.fracaoFaltante), formatarMoeda(combustivel.valorTanqueCompleto))}
        valor={formatarMoeda(combustivel.valor)}
        destaque={combustivel.valor > 0}
      />
      <LinhaCobranca
        titulo={M.atrasoTitulo}
        apoio={M.atrasoApoio(atraso.dataSaidaStr, atraso.toleranciaAtrasoMinutos)}
        valor={formatarMoeda(atraso.valor)}
        destaque={false}
      />
      <LinhaCobranca
        titulo={M.avariasTitulo}
        apoio={M.avariasApoio(avarias.itens.length)}
        valor={formatarMoeda(avarias.valor)}
        destaque={avarias.valor > 0}
      />
    </>
  );
}

/**
 * Cartão "Cobranças Extras" da tela 06 (anexo, tela 06; desenho 06). Mostra o cálculo automático, a caução pelo caso e o total
 * que o servidor confirma. O encerramento só acontece depois da caixa de confirmação. Só para quem pode encerrar.
 */
export function CartaoCobrancasExtras({ contexto, simulacao, encerramento, desfecho }: CartaoCobrancasExtrasProps) {
  const [caixaAberta, setCaixaAberta] = useState(false);
  const enviandoRef = useRef(false);
  const botaoEncerrarRef = useRef<HTMLButtonElement>(null);
  // Quando a caixa é cancelada, o foco volta ao botão de encerrar (senão o teclado cai no início da página).
  const voltarFocoRef = useRef(false);
  const falha = useMemo(() => (encerramento.error ? falhaDoEncerramento(encerramento.error) : null), [encerramento.error]);
  // Depois do sucesso, a caixa fica travada até a tela trocar para o estado final.
  const ocupado = encerramento.isPending || encerramento.isSuccess;
  const memoria = simulacao.data ?? null;
  const dados = dadosDeEncerramento(contexto);

  useEffect(() => {
    if (!caixaAberta && voltarFocoRef.current) {
      voltarFocoRef.current = false;
      botaoEncerrarRef.current?.focus();
    }
  }, [caixaAberta]);

  function cancelarCaixa() {
    voltarFocoRef.current = true;
    setCaixaAberta(false);
  }

  function confirmar() {
    if (dados === null || !podeIniciarEncerramento({ temDados: dados !== null, enviando: enviandoRef.current, ocupado })) return;
    enviandoRef.current = true;
    encerramento.mutate(dados, { onSettled: () => { enviandoRef.current = false; } });
  }

  return (
    <section aria-labelledby="cobrancas-extras" aria-busy={ocupado} className="flex min-w-0 flex-col gap-4 rounded-cartao bg-branco p-6 shadow-md">
      <header className="flex flex-wrap items-center justify-between gap-2 pb-2">
        <div className="flex items-center gap-2">
          <Icone nome="receipt_long" tamanho={22} className="text-orange-acao" />
          <h2 id="cobrancas-extras" className="text-valor-destaque font-bold text-navy-profundo">{M.cobrancasTitulo}</h2>
        </div>
        <span className="rounded-sm bg-steel-alto px-2 py-0.5 text-micro font-semibold text-navy-profundo">{M.calculoAutomatico}</span>
      </header>

      {simulacao.isPending && <output className="block text-apoio text-texto-apoio">{M.calculando}</output>}
      {simulacao.isError && (
        <EstadoErro mensagem={obterMensagemErro(simulacao.error)} aoTentarNovamente={() => { void simulacao.refetch(); }} tentando={simulacao.isFetching} />
      )}
      {memoria && (
        <ul className="flex flex-col gap-3">
          <LinhasDetalhamento calculo={memoria} franquiaKm={memoria.configuracao.franquiaKm} />
        </ul>
      )}

      <div className="flex items-center justify-between gap-3 rounded bg-steel-fundo p-4">
        <span className="text-corpo-denso font-semibold text-navy-profundo">{M.subtotal}</span>
        <span className="text-titulo-md font-bold text-navy-profundo">{contexto.totalExtrasStr ?? VALOR_AUSENTE}</span>
      </div>

      {desfecho !== null && (desfecho.temCaucao ? (
        <div className="flex flex-col gap-2 rounded bg-steel-linha p-4">
          <div className="flex items-center justify-between gap-2">
            <span className="text-micro font-bold uppercase tracking-wider text-navy-profundo">{M.caucaoTitulo}</span>
            <span className="text-dado-tabular font-bold text-navy-profundo">{formatarMoeda(contexto.locacao.valorCaucao)}</span>
          </div>
          {desfecho.retido > 0 && (
            <div className="flex items-center justify-between gap-2 text-apoio text-texto-apoio">
              <span>{M.retencaoExtras}</span>
              <span className="text-dado-tabular font-semibold text-frota-oficina">{M.retencaoValor(formatarMoeda(desfecho.retido))}</span>
            </div>
          )}
          <div className="flex items-center justify-between gap-2 text-corpo-denso font-semibold text-navy-profundo">
            <span>{M.caucaoLiberadaAoCliente}</span>
            <span className="text-dado-tabular font-bold">{formatarMoeda(desfecho.liberado)}</span>
          </div>
          {desfecho.frase !== '' && <p className="text-apoio text-texto-apoio">{desfecho.frase}</p>}
        </div>
      ) : (
        desfecho.frase !== '' && <p className="rounded bg-steel-fundo p-4 text-apoio text-texto-apoio">{desfecho.frase}</p>
      ))}

      <div className="flex flex-col gap-1 pt-2">
        <span className="text-micro font-bold uppercase tracking-wider text-texto-apoio">{M.totalTitulo}</span>
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-titulo-hero font-bold tracking-tight text-navy-profundo">{contexto.totalExtrasStr ?? VALOR_AUSENTE}</span>
          {desfecho !== null && <span className="rounded-sm bg-steel-suave px-2 py-0.5 text-micro font-bold uppercase text-navy-profundo">{rotuloSeloTotal(desfecho.selo)}</span>}
        </div>
      </div>

      {dados === null || desfecho === null ? (
        <p className="text-apoio text-texto-apoio">{M.semPolitica}</p>
      ) : caixaAberta ? (
        <CaixaConfirmacaoEncerramento
          contexto={contexto}
          desfecho={desfecho}
          enviando={ocupado}
          falha={falha}
          aoConfirmar={confirmar}
          aoCancelar={cancelarCaixa}
        />
      ) : (
        <div className="flex flex-col gap-2">
          {/* Altura, texto e canto do desenho 06 (h-12, title-md, rounded-lg = rounded, shadow-md). O "!" garante que vence a base da variante laranjaAcao (h-10). Ícone de 22px como filho, porque o `icone` da Botao é de 18px. */}
          <Botao
            ref={botaoEncerrarRef}
            variante="laranjaAcao"
            larguraTotal
            className="!h-12 !rounded !text-titulo-md !font-bold !shadow-md hover:!shadow-lg"
            onClick={() => setCaixaAberta(true)}
          >
            <Icone nome="check_circle" tamanho={22} />
            {M.encerrarBotao}
          </Botao>
          <p className="text-center text-apoio text-texto-apoio">{M.encerrarNota}</p>
        </div>
      )}
    </section>
  );
}
