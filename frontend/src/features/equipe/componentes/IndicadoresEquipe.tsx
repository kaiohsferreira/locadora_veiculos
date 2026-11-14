import { Esqueleto, Etiqueta, type TomEtiqueta } from '@/components/ui';
import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';
import {
  MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES,
  MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES_DETALHE,
  MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES_SELO,
  MENSAGEM_EQUIPE_INDICADOR_ATIVOS,
  MENSAGEM_EQUIPE_INDICADOR_ATIVOS_SELO,
  MENSAGEM_EQUIPE_INDICADOR_INATIVOS,
  MENSAGEM_EQUIPE_INDICADOR_INATIVOS_DETALHE,
  MENSAGEM_EQUIPE_INDICADOR_INATIVOS_SELO,
  mensagemEquipeDeCadastrados,
} from '@/lib/mensagens';
import type { TotaisEquipe } from '@/types/equipe';

export interface IndicadoresEquipeProps {
  /** Totais da resposta da lista. Ausentes enquanto carrega ou quando a lista falhou. */
  totais: TotaisEquipe | undefined;
  carregando: boolean;
}

interface CartaoIndicadorProps {
  titulo: string;
  valor: number | undefined;
  carregando: boolean;
  selo: { rotulo: string; tom: TomEtiqueta };
  detalhe: string;
  /** Classe da faixa de baixo do cartão (cor do token). */
  faixa: string;
  /** Classe do número (cor do token). */
  corNumero: string;
}

/** Cartão de indicador: título, selo, número grande, texto de apoio e faixa de cor. */
function CartaoIndicador({ titulo, valor, carregando, selo, detalhe, faixa, corNumero }: CartaoIndicadorProps) {
  return (
    <section className="relative flex flex-col justify-between gap-3 overflow-hidden rounded-cartao bg-branco p-4 shadow-cartao">
      <div className="flex items-center justify-between gap-2">
        <p className="text-legenda font-semibold uppercase tracking-wide text-texto-suave">{titulo}</p>
        <Etiqueta rotulo={selo.rotulo} tom={selo.tom} />
      </div>
      <div className="flex flex-wrap items-baseline gap-2">
        {carregando && valor === undefined ? (
          <Esqueleto className="h-8 w-16" />
        ) : (
          <span className={`text-titulo font-semibold ${corNumero}`}>
            {valor === undefined ? VALOR_AUSENTE : formatarNumero(valor)}
          </span>
        )}
        <span className="text-corpo-denso text-texto-suave">{detalhe}</span>
      </div>
      <span aria-hidden="true" className={`absolute inset-x-0 bottom-0 h-1 ${faixa}`} />
    </section>
  );
}

/** Três indicadores da equipe: ativos (com o total cadastrado), inativos e administradores ativos. */
export function IndicadoresEquipe({ totais, carregando }: IndicadoresEquipeProps) {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <CartaoIndicador
        titulo={MENSAGEM_EQUIPE_INDICADOR_ATIVOS}
        valor={totais?.totalAtivos}
        carregando={carregando}
        selo={{ rotulo: MENSAGEM_EQUIPE_INDICADOR_ATIVOS_SELO, tom: 'sucesso' }}
        detalhe={totais ? mensagemEquipeDeCadastrados(totais.totalCadastrados) : ''}
        faixa="bg-orange"
        corNumero="text-orange-escuro"
      />
      <CartaoIndicador
        titulo={MENSAGEM_EQUIPE_INDICADOR_INATIVOS}
        valor={totais?.totalInativos}
        carregando={carregando}
        selo={{ rotulo: MENSAGEM_EQUIPE_INDICADOR_INATIVOS_SELO, tom: 'neutro' }}
        detalhe={MENSAGEM_EQUIPE_INDICADOR_INATIVOS_DETALHE}
        faixa="bg-borda-forte"
        corNumero="text-texto-suave"
      />
      <CartaoIndicador
        titulo={MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES}
        valor={totais?.totalAdministradores}
        carregando={carregando}
        selo={{ rotulo: MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES_SELO, tom: 'neutro' }}
        detalhe={MENSAGEM_EQUIPE_INDICADOR_ADMINISTRADORES_DETALHE}
        faixa="bg-navy"
        corNumero="text-navy"
      />
    </div>
  );
}
