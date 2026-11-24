import { useId, type FormEvent, type ReactNode } from 'react';
import { Icone } from '@/components/ui';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import type { ContextoRetirada } from '@/types/retirada';
import { contarConferidos, linhaDeEnvio, textoAvisoCnhVencida, textoResultadoItem } from '../regrasRetirada';
import { ITENS_CONFERENCIA, type ConferenciaRetirada as ValoresConferencia, type ItemConferencia } from '../validacaoRetirada';
import { CartaoRetirada } from './CartaoRetirada';

/** Id do formulário: o botão principal, que fica na barra fixa, envia por ele (atributo form). */
export const ID_FORMULARIO_RETIRADA = 'formulario-conferencia-retirada';

const ICONE_ITEM: Readonly<Record<ItemConferencia, string>> = {
  cnhOriginal: 'badge',
  comprovanteResidencia: 'home_pin',
  termoMultas: 'gavel',
};

export interface ConferenciaRetiradaProps {
  contexto: ContextoRetirada;
  valores: ValoresConferencia;
  /** Nome de quem está logado, para a linha de resultado. Nulo quando não há nome. */
  nomeFuncionario: string | null;
  /** Bloqueia os interruptores e o formulário (consulta atualizando, envio ou retirada indisponível). */
  bloqueada: boolean;
  ocupada: boolean;
  indisponivel: boolean;
  /** Erro da abertura da locação (texto do servidor e aviso de conflito ou de recuperação). */
  alertaErro: ReactNode;
  aoAlternar: (item: ItemConferencia) => void;
  aoEnviar: (evento: FormEvent<HTMLFormElement>) => void;
}

interface ItemProps {
  item: ItemConferencia;
  contexto: ContextoRetirada;
  marcado: boolean;
  nomeFuncionario: string | null;
  bloqueado: boolean;
  aoAlternar: (item: ItemConferencia) => void;
}

/** Um dos três atestados. O interruptor é um role="switch" nomeado pelo título do próprio item. */
function ItemConferenciaRetirada({ item, contexto, marcado, nomeFuncionario, bloqueado, aoAlternar }: ItemProps) {
  const idTitulo = useId();
  const textos = M.itens[item];
  const linhas: ReactNode[] = [];
  if (item === 'cnhOriginal') {
    const itens = M.itens.cnhOriginal;
    linhas.push(<span key="dados" className="mt-0.5 text-apoio text-texto-apoio">{itens.dados(contexto.cnhCategoria, contexto.cnhValidadeStr)}</span>);
    linhas.push(<span key="envio" className="text-apoio text-texto-apoio">{linhaDeEnvio(contexto.cnhEnviadaEmStr, itens.envioComData, itens.semEnvio)}</span>);
  }
  if (item === 'comprovanteResidencia') {
    const itens = M.itens.comprovanteResidencia;
    linhas.push(<span key="envio" className="text-apoio text-texto-apoio">{linhaDeEnvio(contexto.comprovanteResidenciaEnviadoEmStr, itens.envioComData, itens.semEnvio)}</span>);
  }
  return (
    <li className="flex items-start justify-between gap-4 rounded bg-steel-fundo p-4 transition-colors hover:bg-steel-suave">
      <div className="flex min-w-0 items-start gap-4">
        <span className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded bg-steel-alto text-navy-profundo">
          <Icone nome={ICONE_ITEM[item]} tamanho={20} />
        </span>
        <div className="flex min-w-0 flex-col gap-1">
          <span id={idTitulo} className="text-corpo-denso font-bold leading-snug text-texto-desenho">{textos.titulo}</span>
          <span className="text-apoio text-texto-apoio">{textos.descricao}</span>
          {linhas}
          {marcado && (
            <span className="mt-1 flex items-center gap-1 text-micro font-semibold text-frota-disponivel">
              <Icone nome="check" tamanho={14} />
              {textoResultadoItem(nomeFuncionario)}
            </span>
          )}
        </div>
      </div>
      <button
        type="button"
        role="switch"
        aria-checked={marcado}
        aria-labelledby={idTitulo}
        disabled={bloqueado}
        onClick={() => aoAlternar(item)}
        className={`relative mt-0.5 h-6 w-12 shrink-0 rounded-full transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao disabled:cursor-not-allowed disabled:opacity-60 ${marcado ? 'bg-navy-profundo' : 'bg-contorno-suave'}`}
      >
        <span
          aria-hidden="true"
          className={`absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-branco shadow-md transition-transform ${marcado ? 'translate-x-6' : 'translate-x-0'}`}
        />
      </button>
    </li>
  );
}

/**
 * Cartão da conferência do desenho 16. Os três itens são atestados do atendente; nada é gravado. No topo aparecem, nesta
 * ordem, o aviso de retirada indisponível (acerto B de 24/11/2026, no mesmo molde) e o de CNH vencida, que não bloqueia nada.
 */
export function ConferenciaRetirada({ contexto, valores, nomeFuncionario, bloqueada, ocupada, indisponivel, alertaErro, aoAlternar, aoEnviar }: ConferenciaRetiradaProps) {
  const idTitulo = useId();
  const conferidos = contarConferidos(valores);
  const completo = conferidos === ITENS_CONFERENCIA.length;
  const aviso = textoAvisoCnhVencida(contexto);
  return (
    <CartaoRetirada labelledBy={idTitulo} className="flex flex-col gap-6">
      <form id={ID_FORMULARIO_RETIRADA} noValidate onSubmit={aoEnviar} aria-busy={ocupada} className="flex flex-col gap-6">
        {indisponivel && (
          <p role="alert" className="flex items-start gap-2 rounded bg-status-perigo-fundo p-4 text-corpo-denso text-status-perigo">
            <Icone nome="block" tamanho={18} />
            <span>{M.indisponivel}</span>
          </p>
        )}
        {aviso && (
          <p role="alert" className="flex items-start gap-2 rounded bg-status-perigo-fundo p-4 text-corpo-denso text-status-perigo">
            <Icone nome="warning" tamanho={18} />
            <span>{aviso}</span>
          </p>
        )}
        <fieldset disabled={bloqueada} className="flex min-w-0 flex-col gap-6">
          <legend className="sr-only">{M.legendaConferencia}</legend>
          <div className="flex flex-col gap-1">
            <div className="flex items-center justify-between gap-4">
              <h2 id={idTitulo} className="text-titulo-md font-bold text-texto-desenho">{M.checklist}</h2>
              <span className={`rounded-xl px-2.5 py-0.5 text-micro font-bold text-branco ${completo ? 'bg-navy-profundo' : 'bg-frota-oficina'}`}>
                {M.contadorConferidos(conferidos)}
              </span>
            </div>
            <p className="text-apoio text-texto-apoio">{M.instrucao}</p>
          </div>
          <ul className="flex flex-col gap-2">
            {ITENS_CONFERENCIA.map((item) => (
              <ItemConferenciaRetirada
                key={item}
                item={item}
                contexto={contexto}
                marcado={valores[item]}
                nomeFuncionario={nomeFuncionario}
                bloqueado={bloqueada}
                aoAlternar={aoAlternar}
              />
            ))}
          </ul>
          {alertaErro}
        </fieldset>
      </form>
    </CartaoRetirada>
  );
}
