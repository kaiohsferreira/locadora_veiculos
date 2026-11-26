import type { ReactNode } from 'react';
import { Icone } from '@/components/ui';
import { SEGMENTOS_COMBUSTIVEL, descreverNivelCombustivel } from '@/components/ui/calculoCombustivel';
import { VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';

export interface BlocoCombustivelProps {
  /** Título da coluna (ex.: "2. Nível de Combustível"). */
  titulo: string;
  /** Nível exato de 0 a 1 gravado no servidor; nulo quando a vistoria não tem o combustível. */
  nivel: number | null;
  /** Divergência (coluna da devolução): fundo, título, régua e selo em laranja, como no desenho 06. */
  divergente?: boolean;
  /** Escala "Vazio (0/8)" e "4/8 (50%)" embaixo da régua (coluna da retirada, desenho 06). */
  comEscala?: boolean;
  /** Linha de baixo do bloco (a divergência e a reposição, na devolução). */
  rodape?: ReactNode;
}

/**
 * Bloco "2. Nível de Combustível" da tela 06 (desenho 06): título e selo na mesma linha, régua de 8 segmentos e, quando há,
 * a escala. Só visual: o oitavo é o mais próximo do nível exato (N1), e a régua é decorativa (aria-hidden); o selo e a escala
 * são o texto que a leitura de tela anuncia. Cores só por token.
 */
export function BlocoCombustivel({ titulo, nivel, divergente = false, comEscala = false, rodape }: BlocoCombustivelProps) {
  const descricao = nivel === null ? null : descreverNivelCombustivel(nivel, ' - ');
  const oitavos = descricao?.oitavos ?? 0;
  const segmentos = Array.from({ length: SEGMENTOS_COMBUSTIVEL }, (_, indice) => indice < oitavos);
  // Na divergência o selo mostra só a fração e o nome do ponto, sem o percentual (anexo, tela 06, TROCAR).
  const textoSelo = descricao === null
    ? VALOR_AUSENTE
    : divergente
      ? `${oitavos}/${SEGMENTOS_COMBUSTIVEL}${descricao.nome ? ` - ${descricao.nome}` : ''}`
      : descricao.texto;

  return (
    <div className={`flex flex-col gap-2 rounded p-4 ${divergente ? 'bg-frota-oficina-fundo/30' : 'bg-steel-fundo'}`}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className={`text-micro uppercase tracking-wider ${divergente ? 'font-bold text-frota-oficina' : 'font-semibold text-texto-apoio'}`}>
          {titulo}
        </span>
        {descricao !== null && (
          <span className={`inline-flex items-center gap-1 rounded-sm px-2 py-0.5 text-dado-tabular font-semibold ${divergente ? 'bg-orange-acao font-bold text-branco' : 'bg-steel-linha text-navy-profundo'}`}>
            <Icone nome={divergente ? 'warning' : 'local_gas_station'} tamanho={14} />
            <span>{textoSelo}</span>
          </span>
        )}
      </div>
      {descricao === null ? (
        <p className="text-corpo-denso text-texto-apoio">{VALOR_AUSENTE}</p>
      ) : (
        <>
          <div aria-hidden="true" className="grid h-3 grid-cols-8 gap-1.5">
            {segmentos.map((cheio, indice) => (
              <span
                key={indice}
                className={`transition-colors motion-reduce:transition-none ${cheio ? (divergente ? 'bg-orange-acao' : 'bg-navy-profundo') : 'bg-steel-alto'}`}
              />
            ))}
          </div>
          {comEscala && (
            <div className="flex justify-between text-micro text-texto-apoio">
              <span>{M.escalaVazio}</span>
              <span>{M.escalaMeio}</span>
            </div>
          )}
        </>
      )}
      {rodape}
    </div>
  );
}
