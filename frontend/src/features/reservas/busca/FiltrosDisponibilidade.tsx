import { Icone, Campo, EstadoErro } from '@/components/ui';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { useCategoriasVeiculos } from '../hooks/useVeiculosDisponiveis';
import type { CamposPeriodoBuscaProps } from './CamposPeriodoBusca';

interface FiltrosDisponibilidadeProps extends Pick<CamposPeriodoBuscaProps, 'rascunho' | 'erros' | 'aoAlterar'> {
  aoLimpar: () => void;
}

const LIMITE_INICIAL_CONTROLE = 500;
const FAIXAS_DIARIA = [
  { campo: 'valorDiariaMin', rotulo: texto.diariaMin, rotuloControle: texto.sliderMin, placeholder: '0' },
  { campo: 'valorDiariaMax', rotulo: texto.diariaMax, rotuloControle: texto.sliderMax, placeholder: texto.semLimite },
] as const;

export function FiltrosDisponibilidade({ rascunho, erros, aoAlterar, aoLimpar }: FiltrosDisponibilidadeProps) {
  const categorias = useCategoriasVeiculos();
  const opcoes = [
    { valor: '', nome: texto.todosGrupos },
    ...(categorias.data?.itens ?? []).map(({ idCategoria, nome }) => ({ valor: String(idCategoria), nome })),
  ];
  const limiteControle = Math.max(
    LIMITE_INICIAL_CONTROLE,
    Math.ceil(Number(rascunho.valorDiariaMin) || 0),
    Math.ceil(Number(rascunho.valorDiariaMax) || 0),
  );

  return (
    <aside aria-labelledby="titulo-filtros-busca" className="min-w-0">
      <div className="mb-3 flex items-center justify-between gap-2">
        <h2 id="titulo-filtros-busca" className="flex items-center gap-2 text-secao text-navy">
          <Icone nome="tune" tamanho={20} /> {texto.tituloFiltros}
        </h2>
        <button type="button" onClick={aoLimpar} className="min-h-10 text-corpo-denso font-semibold text-status-pendente hover:underline">
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
                onChange={() => aoAlterar('idCategoria', valor)}
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
          <legend className="text-rotulo uppercase text-navy">{texto.faixaDiaria}</legend>
          {FAIXAS_DIARIA.map(({ campo, rotulo, rotuloControle, placeholder }) => (
            <div key={campo} className="min-w-0">
              <input
                type="range"
                aria-label={rotuloControle}
                aria-valuetext={campo === 'valorDiariaMax' && !rascunho[campo] ? texto.semLimite : undefined}
                className="mb-2 min-h-6 w-full cursor-pointer accent-navy"
                min="0"
                max={limiteControle}
                step="0.01"
                value={rascunho[campo] || (campo === 'valorDiariaMin' ? 0 : limiteControle)}
                onChange={(evento) => aoAlterar(campo, evento.target.value)}
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
              />
            </div>
          ))}
        </fieldset>
      </div>
    </aside>
  );
}
