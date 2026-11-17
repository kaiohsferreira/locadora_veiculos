import { useId } from 'react';
import { Icone } from '@/components/ui';
import { CLASSES_ERRO, classesRotulo } from '@/components/ui/estilosCampo';
import { MENSAGEM_VEICULO_ROTULO_COR } from '@/lib/mensagensVeiculos';
import { CORES_VEICULO, type CorVeiculo } from '@/lib/listasVeiculo';

export interface SeletorCorVeiculoProps {
  /** Cor marcada (texto); vazio quando não há cor, que é opcional. */
  valor: string;
  erro?: string;
  aoMudar: (valor: string) => void;
}

/** Amostra fora das quatro: valor gravado por outra via, mostrado como opção extra com a cor neutra. */
const AMOSTRA_FORA_DA_LISTA: Omit<CorVeiculo, 'valor' | 'rotulo'> = {
  classeAmostra: 'bg-steel-linha',
  classeMarcado: 'text-texto-desenho',
};

/**
 * Cor externa da tela 05: quatro amostras em grupo de rádio (navegável pelas setas). Clicar na cor marcada desmarca
 * (a cor é opcional). Cor que veio de fora da lista aparece como opção extra, e fica marcada até o usuário trocá-la.
 */
export function SeletorCorVeiculo({ valor, erro, aoMudar }: SeletorCorVeiculoProps) {
  const idRotulo = useId();
  const idErro = `${idRotulo}-erro`;
  const idGrupo = `cor-${idRotulo}`;
  const atual = valor.trim();
  const extra = atual.length > 0 && !CORES_VEICULO.some((cor) => cor.valor === atual);
  const opcoes: CorVeiculo[] = [
    ...CORES_VEICULO,
    ...(extra ? [{ valor: atual, rotulo: atual, ...AMOSTRA_FORA_DA_LISTA }] : []),
  ];

  return (
    <div className="flex flex-col gap-1">
      <span id={idRotulo} className={classesRotulo('desenho')}>
        {MENSAGEM_VEICULO_ROTULO_COR}
      </span>
      <div role="radiogroup" aria-labelledby={idRotulo} aria-describedby={erro ? idErro : undefined} className="grid grid-cols-4 gap-1">
        {opcoes.map((cor) => {
          const marcada = atual === cor.valor;
          return (
            <label
              key={cor.valor}
              className={`flex cursor-pointer items-center gap-1 rounded p-1.5 transition-all has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-orange-acao ${
                marcada ? 'bg-steel-alto' : 'bg-steel-fundo hover:bg-steel-suave'
              }`}
            >
              <input
                type="radio"
                name={idGrupo}
                value={cor.valor}
                checked={marcada}
                onChange={() => aoMudar(cor.valor)}
                onClick={() => {
                  if (marcada) aoMudar('');
                }}
                className="sr-only"
              />
              <span
                aria-hidden="true"
                className={`flex h-5 w-5 items-center justify-center rounded-md shadow-sm ${cor.classeAmostra}`}
              >
                {marcada && <Icone nome="check" tamanho={14} className={cor.classeMarcado} />}
              </span>
              <span className={`truncate text-micro text-texto-desenho ${marcada ? 'font-semibold' : 'font-medium'}`}>
                {cor.rotulo}
              </span>
            </label>
          );
        })}
      </div>
      {erro && (
        <p id={idErro} role="alert" className={CLASSES_ERRO}>
          <Icone nome="error" tamanho={14} />
          {erro}
        </p>
      )}
    </div>
  );
}
