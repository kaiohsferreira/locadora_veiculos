import type { ReactNode } from 'react';
import { Botao, Cartao, Esqueleto, Icone } from '@/components/ui';
import { formatarMoeda } from '@/lib/formatters';
import {
  PROPOSTA_BOTAO_ENVIANDO,
  PROPOSTA_BOTAO_ENVIAR,
  PROPOSTA_RESUMO_AGUARDANDO,
  PROPOSTA_RESUMO_CALCULANDO,
  PROPOSTA_RESUMO_ENTRADA,
  PROPOSTA_RESUMO_FINANCIADO,
  PROPOSTA_RESUMO_NOTA,
  PROPOSTA_RESUMO_ULTIMA_PARCELA,
  PROPOSTA_RESUMO_VALOR_VEICULO,
  PROPOSTA_TITULO_RESUMO,
} from '@/lib/mensagensProposta';
import type { SimulacaoParcelamento } from '@/types/vendas';
import { ultimaParcelaDiferente } from './regrasSimulacao';

export interface ResumoSimulacaoProps {
  /** Resultado da API (o anterior continua aqui enquanto o novo chega). Nulo antes da primeira simulação. */
  simulacao: SimulacaoParcelamento | null;
  /** A simulação está sendo refeita. */
  calculando: boolean;
  /** Mensagem de erro do envio ou da simulação, já em português. */
  erro?: string | null;
  /** Texto de apoio abaixo do botão (ex.: "você vai entrar na sua conta"). */
  aviso?: string | null;
  enviando: boolean;
  /** O botão fica desligado enquanto os campos têm erro ou ainda não há simulação. */
  podeEnviar: boolean;
  aoEnviar: () => void;
}

/** Linha "rótulo ........ valor" do resumo. */
function Linha({ rotulo, valor, destaque = false }: { rotulo: string; valor: ReactNode; destaque?: boolean }) {
  const classeTexto = destaque ? 'font-semibold text-navy' : 'text-texto-suave';
  return (
    <div className={`flex items-baseline justify-between gap-4 text-corpo-denso ${classeTexto}`}>
      <dt>{rotulo}</dt>
      <dd className="text-right tabular-nums">{valor}</dd>
    </div>
  );
}

/**
 * Cartão "Resumo da simulação": valor do veículo, entrada, valor financiado e o destaque "N × parcela". A observação da
 * última parcela só aparece quando ela é diferente das demais. O resultado mostrado é sempre o que a API devolveu.
 */
export function ResumoSimulacao({ simulacao, calculando, erro, aviso, enviando, podeEnviar, aoEnviar }: ResumoSimulacaoProps) {
  return (
    <Cartao titulo={PROPOSTA_TITULO_RESUMO}>
      <div className="flex flex-col gap-6">
        {erro && (
          <div role="alert" className="flex items-start gap-2 rounded-cartao bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
            <Icone nome="error" tamanho={18} className="mt-0.5" />
            <span>{erro}</span>
          </div>
        )}

        {simulacao === null ? (
          calculando ? (
            <div className="flex flex-col gap-3" aria-hidden="true">
              <Esqueleto className="h-4 w-full" />
              <Esqueleto className="h-4 w-3/4" />
              <Esqueleto className="h-10 w-1/2" />
            </div>
          ) : (
            <p className="text-corpo-denso text-texto-suave">{PROPOSTA_RESUMO_AGUARDANDO}</p>
          )
        ) : (
          <div className="flex flex-col gap-4" aria-live="polite" aria-busy={calculando}>
            <dl className="flex flex-col gap-2">
              <Linha rotulo={PROPOSTA_RESUMO_VALOR_VEICULO} valor={formatarMoeda(simulacao.valorVenda)} />
              <Linha rotulo={PROPOSTA_RESUMO_ENTRADA} valor={`− ${formatarMoeda(simulacao.valorEntrada)}`} />
              <div className="border-t border-borda pt-2">
                <Linha rotulo={PROPOSTA_RESUMO_FINANCIADO} valor={formatarMoeda(simulacao.valorFinanciado)} destaque />
              </div>
            </dl>

            <div className="rounded-cartao bg-steel-fundo px-4 py-4 text-center">
              <p className="whitespace-nowrap text-titulo text-navy tabular-nums lg:text-secao xl:text-titulo" data-testid="parcela-destaque">
                {simulacao.numeroParcelas} × {formatarMoeda(simulacao.valorParcela)}
              </p>
              {ultimaParcelaDiferente(simulacao.valorParcela, simulacao.valorUltimaParcela) && (
                <p className="mt-1 text-corpo-denso text-texto-suave" data-testid="ultima-parcela">
                  {PROPOSTA_RESUMO_ULTIMA_PARCELA}: {formatarMoeda(simulacao.valorUltimaParcela)}
                </p>
              )}
            </div>

            {calculando && (
              <p className="flex items-center gap-1 text-legenda text-texto-suave">
                <Icone nome="progress_activity" tamanho={14} className="motion-safe:animate-spin" />
                {PROPOSTA_RESUMO_CALCULANDO}
              </p>
            )}
          </div>
        )}

        <div className="flex flex-col gap-2">
          <Botao
            tamanho="grande"
            larguraTotal
            icone="send"
            carregando={enviando}
            disabled={!podeEnviar || enviando}
            onClick={aoEnviar}
          >
            {enviando ? PROPOSTA_BOTAO_ENVIANDO : PROPOSTA_BOTAO_ENVIAR}
          </Botao>
          {aviso && <p className="text-center text-legenda text-texto-suave">{aviso}</p>}
          <p className="text-center text-legenda text-texto-suave">{PROPOSTA_RESUMO_NOTA}</p>
        </div>
      </div>
    </Cartao>
  );
}
