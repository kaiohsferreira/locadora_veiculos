import { Icone, Campo, EstadoErro } from '@/components/ui';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { useCategoriasVeiculos } from '../hooks/useVeiculosDisponiveis';
import {
  calcularLimiteControle, montarLeituraFaixaDiaria, posicaoDoControle, valorDoControle, type FiltrosLateral,
} from './parametrosBusca';
import type { CamposPeriodoBuscaProps } from './CamposPeriodoBusca';

interface FiltrosDisponibilidadeProps extends Pick<CamposPeriodoBuscaProps, 'rascunho' | 'erros' | 'aoAlterar'> {
  aoLimpar: () => void;
  /** Aplica os filtros da lateral na hora, junto com o local e as datas digitados (se válidos). Sem argumento, aplica o que está na tela. */
  aoAplicar: (alteracao?: Partial<FiltrosLateral>) => void;
}

const FAIXAS_DIARIA = [
  { campo: 'valorDiariaMin', rotulo: texto.diariaMin, rotuloControle: texto.sliderMin, placeholder: texto.placeholderMinimo },
  { campo: 'valorDiariaMax', rotulo: texto.diariaMax, rotuloControle: texto.sliderMax, placeholder: texto.semLimite },
] as const;

export function FiltrosDisponibilidade({ rascunho, erros, aoAlterar, aoLimpar, aoAplicar }: FiltrosDisponibilidadeProps) {
  const categorias = useCategoriasVeiculos();
  const opcoes = [
    { valor: '', nome: texto.todosGrupos },
    ...(categorias.data?.itens ?? []).map(({ idCategoria, nome }) => ({ valor: String(idCategoria), nome })),
  ];
  const limiteControle = calcularLimiteControle(rascunho.valorDiariaMin, rascunho.valorDiariaMax);

  return (
    <aside aria-labelledby="titulo-filtros-busca" className="min-w-0">
      <div className="mb-3 flex items-center justify-between gap-2">
        <h2 id="titulo-filtros-busca" className="flex items-center gap-2 text-nome-veiculo text-navy">
          <Icone nome="tune" tamanho={20} /> {texto.tituloFiltros}
        </h2>
        <button type="button" onClick={aoLimpar} className="min-h-10 text-rotulo font-semibold text-frota-oficina hover:underline">
          {texto.limparFiltros}
        </button>
      </div>
      <div className="space-y-6 rounded-cartao border border-borda bg-branco p-4 shadow-cartao">
        <fieldset aria-describedby={erros.idCategoria ? 'erro-categoria-busca' : undefined} className="space-y-1">
          <legend className="mb-2 text-rotulo uppercase text-navy">{texto.categoriaVeiculo}</legend>
          {opcoes.map(({ valor, nome }) => (
            <label key={valor} className="flex min-h-10 cursor-pointer items-center gap-2 rounded p-2 text-corpo-denso text-navy hover:bg-steel-fundo has-[:checked]:bg-steel-suave has-[:checked]:font-semibold">
              <input
                type="radio"
                name="idCategoria"
                value={valor}
                checked={rascunho.idCategoria === valor}
                onChange={() => {
                  aoAlterar('idCategoria', valor);
                  aoAplicar({ idCategoria: valor });
                }}
                className="accent-navy"
              />
              {nome}
            </label>
          ))}
          {erros.idCategoria && <p id="erro-categoria-busca" role="alert" className="text-legenda text-status-perigo">{erros.idCategoria}</p>}
          {categorias.isPending && <output className="block text-legenda text-texto-apoio">{texto.carregandoCategorias}</output>}
          {categorias.isError && (
            <EstadoErro titulo={texto.erroCategorias} aoTentarNovamente={() => void categorias.refetch()} tentando={categorias.isFetching} />
          )}
        </fieldset>
        <fieldset className="grid grid-cols-2 gap-3 border-t border-borda pt-2">
          <legend className="col-span-2 flex w-full items-center justify-between gap-2">
            <span className="text-rotulo uppercase text-navy">{texto.faixaDiaria}</span>
            {/* Só visual: a faixa já está nos campos de mínimo e máximo, e o nome do grupo fica só com o título. */}
            <span className="text-dado-tabular font-bold text-frota-oficina" aria-hidden="true">
              {montarLeituraFaixaDiaria(rascunho)}
            </span>
          </legend>
          {FAIXAS_DIARIA.map(({ campo, rotulo, rotuloControle, placeholder }) => (
            <div key={campo} className="min-w-0">
              <input
                type="range"
                aria-label={rotuloControle}
                aria-valuetext={campo === 'valorDiariaMax' && !rascunho[campo] ? texto.semLimite : undefined}
                className="mb-2 min-h-6 w-full cursor-pointer accent-navy"
                min="0"
                max={limiteControle}
                step="1"
                value={posicaoDoControle(campo, rascunho[campo], limiteControle)}
                onChange={(evento) => aoAlterar(campo, valorDoControle(campo, Number(evento.target.value), limiteControle))}
                onPointerUp={() => aoAplicar()}
                onKeyUp={() => aoAplicar()}
                onBlur={() => aoAplicar()}
              />
              <Campo
                name={campo}
                rotulo={rotulo}
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                placeholder={placeholder}
                className="min-w-0"
                value={rascunho[campo]}
                erro={erros[campo]}
                onChange={(evento) => aoAlterar(campo, evento.target.value)}
                onBlur={() => aoAplicar()}
                onKeyDown={(evento) => {
                  // Enter aplica os filtros na hora (com o local e as datas digitados, se válidos) e não envia o formulário:
                  // o Buscar é quem cria passo no histórico.
                  if (evento.key !== 'Enter') return;
                  evento.preventDefault();
                  aoAplicar();
                }}
              />
            </div>
          ))}
        </fieldset>
      </div>
    </aside>
  );
}
