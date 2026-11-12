import {
  MENSAGEM_CADASTRO_ETAPAS_ROTULO,
  MENSAGEM_CADASTRO_ETAPA_1_SITUACAO,
  MENSAGEM_CADASTRO_ETAPA_1_TITULO,
  MENSAGEM_CADASTRO_ETAPA_2_SITUACAO,
  MENSAGEM_CADASTRO_ETAPA_2_TITULO,
  MENSAGEM_CADASTRO_ETAPA_3_SITUACAO,
  MENSAGEM_CADASTRO_ETAPA_3_TITULO,
} from '@/lib/mensagens';

/** Etapa do cadastro. O estado é dito em texto ("Em preenchimento"), não só pela cor. */
interface EtapaCadastro {
  titulo: string;
  situacao: string;
}

const ETAPAS: readonly EtapaCadastro[] = [
  { titulo: MENSAGEM_CADASTRO_ETAPA_1_TITULO, situacao: MENSAGEM_CADASTRO_ETAPA_1_SITUACAO },
  { titulo: MENSAGEM_CADASTRO_ETAPA_2_TITULO, situacao: MENSAGEM_CADASTRO_ETAPA_2_SITUACAO },
  { titulo: MENSAGEM_CADASTRO_ETAPA_3_TITULO, situacao: MENSAGEM_CADASTRO_ETAPA_3_SITUACAO },
];

/** Indicador de três etapas, só informativo (não é clicável). A primeira é a atual. */
export function IndicadorEtapas() {
  return (
    <nav aria-label={MENSAGEM_CADASTRO_ETAPAS_ROTULO}>
      <ol className="grid grid-cols-1 gap-3 md:grid-cols-3 md:gap-4">
        {ETAPAS.map((etapa, indice) => {
          const atual = indice === 0;
          return (
            <li
              key={etapa.titulo}
              aria-current={atual ? 'step' : undefined}
              className={`border-t-4 pt-3 text-corpo-denso ${
                atual ? 'border-orange font-semibold text-navy' : 'border-borda font-medium text-texto-suave'
              }`}
            >
              {indice + 1}. {etapa.titulo} — {etapa.situacao}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
