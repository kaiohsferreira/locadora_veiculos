import { generatePath } from 'react-router-dom';
import { BotaoLink, Cartao, EstadoErro } from '@/components/ui';
import { oitavosDoNivel } from '@/components/ui/calculoCombustivel';
import { formatarMoeda } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { usePermissoes } from '@/routes/autenticacao/usePermissoes';
import { CAMINHOS } from '@/routes/caminhos';
import type { CalculoCobrancasExtras } from '@/types/cobrancasExtras';
import type { ContextoDevolucao } from '@/types/devolucao';
import { useEncerrarLocacao } from '../hooks/useDevolucao';
import { useSimulacaoEncerramento } from '../hooks/useSimulacaoEncerramento';
import {
  desfechoCaucaoPrevisto,
  faseDevolucao,
  kmRodados,
  podeEncerrarLocacao,
  textoFranquia,
  valorCaucaoPrevista,
  type DesfechoCaucaoPrevisto,
} from '../regras/regrasDevolucao';
import { CartaoCobrancasExtras } from './CartaoCobrancasExtras';
import { ColunaDevolucao, type DivergenciaCombustivel } from './ColunaDevolucao';
import { ColunaRetirada } from './ColunaRetirada';
import { EstadoFinalizada } from './EstadoFinalizada';
import { FormularioSaida } from './FormularioSaida';

export interface ConteudoDevolucaoProps {
  contexto: ContextoDevolucao;
  /** Atualização do contexto em curso (desabilita o envio e marca a região como ocupada). */
  atualizando: boolean;
}

/**
 * Falta de combustível na devolução (N1: oitavo mais próximo). Mostra também quando a falta é menor que um oitavo, se o
 * servidor cobrou a reposição: a linha de cobrança do cartão não pode aparecer sem a divergência que a explica.
 */
function divergenciaDe(calculo: CalculoCobrancasExtras): DivergenciaCombustivel | null {
  const oitavos = oitavosDoNivel(calculo.combustivel.fracaoFaltante);
  if (oitavos === 0 && calculo.combustivel.valor <= 0) return null;
  return { oitavos, reposicaoStr: formatarMoeda(calculo.combustivel.valor) };
}

/**
 * Corpo da tela 06 conforme o estado da locação (anexo, tela 06, estados A a D). As colunas e o cartão de cobranças são só
 * exibição: o total e a caução vêm do servidor, e o encerramento passa pela caixa de confirmação.
 */
export function ConteudoDevolucao({ contexto, atualizando }: ConteudoDevolucaoProps) {
  const { locacao, entrada, comparacao } = contexto;
  const fase = faseDevolucao(contexto);
  const podeEncerrar = podeEncerrarLocacao(usePermissoes().cargo);
  const encerramento = useEncerrarLocacao(locacao.idLocacao);
  const simulacao = useSimulacaoEncerramento(locacao.idLocacao, comparacao?.saida.idVistoria ?? null, fase === 'ABERTA' && podeEncerrar);
  const memoria = simulacao.data ?? null;
  const franquia = memoria ? textoFranquia(memoria.configuracao.franquiaKm, memoria.configuracao.baseFranquia === 'TOTAL') : null;
  // Sem total do servidor (sem política ou sem saída), a caução não tem desfecho para mostrar. Caução baixada (CANCELADO) vira sem caução.
  const desfecho: DesfechoCaucaoPrevisto | null = contexto.totalExtras === null
    ? null
    : desfechoCaucaoPrevisto(valorCaucaoPrevista(locacao.valorCaucao, contexto.caucao.situacao), contexto.totalExtras);
  const saida = comparacao?.saida ?? null;

  return (
    <div aria-busy={atualizando} className="flex flex-col gap-6">
      {fase === 'SEM_ENTRADA' && (
        <Cartao titulo={M.entradaNecessariaTitulo}>
          <p className="mb-4">{M.semEntrada}</p>
          <BotaoLink to={generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(locacao.idLocacao) })}>{M.irParaVistoriaEntrada}</BotaoLink>
        </Cartao>
      )}
      {fase === 'CANCELADA' && <EstadoErro mensagem={M.cancelada} />}
      {fase === 'SEM_SAIDA' && entrada && (
        <>
          <p className="rounded-lg bg-status-pendente-fundo p-4 text-corpo-denso text-navy-profundo">{M.avisoSemSaida}</p>
          <ColunaRetirada entrada={entrada} avarias={contexto.avariasEntrada} franquia={null} />
          <FormularioSaida key={locacao.idLocacao} contexto={contexto} atualizando={atualizando} />
        </>
      )}
      {(fase === 'ABERTA' || fase === 'FINALIZADA') && entrada && saida && comparacao && (
        <>
          <div className="grid gap-6 lg:grid-cols-2">
            <ColunaRetirada entrada={entrada} avarias={contexto.avariasEntrada} franquia={franquia} />
            <ColunaDevolucao
              saida={saida}
              quantidadeAvariasEntrada={contexto.avariasEntrada.length}
              novas={comparacao.avariasNovas}
              indeterminadas={comparacao.avariasIndeterminadas}
              comparacaoRealizada={comparacao.comparacaoRealizada}
              rodados={kmRodados(entrada, saida)}
              franquia={franquia}
              excedenteStr={memoria ? formatarMoeda(memoria.quilometragem.valor) : null}
              divergencia={memoria ? divergenciaDe(memoria) : null}
            />
          </div>
          {fase === 'ABERTA' && podeEncerrar && (
            <div className="grid gap-6 lg:grid-cols-12">
              <div className="min-w-0 lg:col-span-5 lg:col-start-8">
                <CartaoCobrancasExtras contexto={contexto} simulacao={simulacao} encerramento={encerramento} desfecho={desfecho} />
              </div>
            </div>
          )}
          {fase === 'ABERTA' && !podeEncerrar && (
            <div className="grid gap-6 lg:grid-cols-12">
              <p className="rounded-cartao bg-branco p-6 text-corpo-denso text-navy-profundo shadow-md lg:col-span-5 lg:col-start-8">{M.semPermissaoEncerrar}</p>
            </div>
          )}
          {fase === 'FINALIZADA' && <EstadoFinalizada contexto={contexto} resultado={encerramento.data ?? null} />}
        </>
      )}
    </div>
  );
}
