import { useEffect, useRef, type MutableRefObject, type ReactNode } from 'react';
import { Gaveta } from '@/components/ui';
import { MENSAGEM_PIX_FECHAR, MENSAGEM_PIX_TITULO } from '@/lib/mensagens';
import { useGerarPixPagamentoCliente } from '@/features/financeiro/hooks/usePagamentosCliente';
import {
  chaveDaGeracaoPix,
  devePedirPixAoAbrir,
  estadoGavetaPix,
  type EstadoGavetaPix,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import type { CobrancaPix, MeuPagamento } from '@/types/pagamentos';
import {
  ConteudoPixErro,
  ConteudoPixGerando,
  ConteudoPixPronto,
  OrigemCobrancaPix,
  ResumoCobrancaPix,
} from './ConteudoPixPagamento';

export interface GavetaPixApresentacaoProps {
  /** Cobrança da linha ou da busca de pendência (a tela já tem os dados dela). */
  pagamento: MeuPagamento;
  /** Estado escolhido por estadoGavetaPix. */
  estado: EstadoGavetaPix;
  /** PIX devolvido pelo servidor; nulo enquanto não chegou. */
  cobranca: CobrancaPix | undefined;
  /** Erro da geração (quando o estado é ERRO). */
  erro: unknown;
  /** Chave da geração atual: uma geração nova recomeça a contagem. */
  chaveGeracao: string;
  aoGerarNovo: () => void;
  aoFechar: () => void;
  /** Botão que abriu a gaveta: recebe o foco de volta quando ela fecha. */
  refGatilho: MutableRefObject<HTMLElement | null>;
}

/**
 * Corpo visual da gaveta de PIX da tela 30, sem chamada à API: o cabeçalho (título, origem e número da cobrança) e o
 * conteúdo do estado (gerando, pronto, expirado ou erro). A gaveta inteira passa por aqui, então os testes de cortes da
 * P4 cobrem todos os estados.
 */
export function GavetaPixApresentacao({
  pagamento,
  estado,
  cobranca,
  erro,
  chaveGeracao,
  aoGerarNovo,
  aoFechar,
  refGatilho,
}: GavetaPixApresentacaoProps) {
  let conteudo: ReactNode;
  if (estado === 'ERRO') {
    conteudo = <ConteudoPixErro erro={erro} aoTentarNovamente={aoGerarNovo} />;
  } else if (estado === 'PRONTO' && cobranca !== undefined) {
    // A chave muda a cada geração: a contagem recomeça do valor novo do servidor.
    conteudo = <ConteudoPixPronto key={chaveGeracao} cobranca={cobranca} aoGerarNovo={aoGerarNovo} />;
  } else {
    conteudo = <ConteudoPixGerando />;
  }

  return (
    <Gaveta
      titulo={MENSAGEM_PIX_TITULO}
      icone="bolt"
      bloqueada={false}
      aoFechar={aoFechar}
      refGatilho={refGatilho}
      rotuloFechar={MENSAGEM_PIX_FECHAR}
      variante="pagamento"
      subtitulo={<OrigemCobrancaPix pagamento={pagamento} />}
    >
      <div className="flex flex-1 flex-col gap-6 overflow-y-auto p-6">
        <ResumoCobrancaPix pagamento={pagamento} />
        {conteudo}
      </div>
    </Gaveta>
  );
}

export interface GavetaPixPagamentoProps {
  /** Cobrança da linha ou da busca de pendência (a tela já tem os dados dela). */
  pagamento: MeuPagamento;
  aoFechar: () => void;
  /** Botão que abriu a gaveta: recebe o foco de volta quando ela fecha. */
  refGatilho: MutableRefObject<HTMLElement | null>;
}

/**
 * Gaveta de pagamento por PIX da tela 30 (P7-FE-02). Ao abrir, chama a rota de gerar ou reabrir: o servidor devolve o PIX
 * gravado se ainda for válido, ou gera outro. Fechar não cancela nada no servidor. Não há polling: o PIX pago só vira PAGO
 * pela baixa manual do funcionário.
 */
export function GavetaPixPagamento({ pagamento, aoFechar, refGatilho }: GavetaPixPagamentoProps) {
  const { mutate, data, error, isError, isPending, submittedAt } = useGerarPixPagamentoCliente();
  const jaIniciou = useRef(false);
  const idPagamento = pagamento.idPagamento;

  // Uma chamada por abertura. O guarda evita a segunda chamada do modo de desenvolvimento do React.
  useEffect(() => {
    if (!devePedirPixAoAbrir(jaIniciou.current)) return;
    jaIniciou.current = true;
    mutate(idPagamento);
  }, [mutate, idPagamento]);

  function gerarNovo() {
    mutate(idPagamento);
  }

  return (
    <GavetaPixApresentacao
      pagamento={pagamento}
      estado={estadoGavetaPix(isError, isPending, data !== undefined)}
      cobranca={data}
      erro={error}
      chaveGeracao={chaveDaGeracaoPix(submittedAt)}
      aoGerarNovo={gerarNovo}
      aoFechar={aoFechar}
      refGatilho={refGatilho}
    />
  );
}
