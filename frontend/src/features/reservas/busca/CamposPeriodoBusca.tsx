import { Botao, Campo, Select } from '@/components/ui';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { useCidadesLocadoras } from '../hooks/useVeiculosDisponiveis';
import { avaliarPeriodo, lerLocal, valorLocal, type ErrosBusca, type RascunhoBusca } from './parametrosBusca';

export interface CamposPeriodoBuscaProps {
  rascunho: RascunhoBusca;
  hoje: string;
  erros: ErrosBusca;
  aoAlterar: (campo: keyof RascunhoBusca, valor: string) => void;
  textoBotao: string;
  buscando?: boolean;
}

/** Pílula "N diárias" no fim da linha do rótulo da devolução, como nos desenhos 03 e 04. */
const CLASSES_PILULA_DIARIAS = 'whitespace-nowrap rounded bg-steel-suave px-1.5 py-0.5 text-micro font-semibold text-navy';

export function CamposPeriodoBusca({ rascunho, hoje, erros, aoAlterar, textoBotao, buscando }: CamposPeriodoBuscaProps) {
  const cidades = useCidadesLocadoras();
  const periodo = avaliarPeriodo(rascunho, hoje);
  const localAtual = lerLocal(rascunho.local);
  const opcoes = (cidades.data ?? []).map((local) => ({
    valor: valorLocal(local),
    rotulo: texto.cidadeUf(local.cidade, local.uf),
  }));
  // Mantém perceptível o local de um link compartilhado, inclusive enquanto a lista carrega.
  if (localAtual && !opcoes.some((opcao) => opcao.valor === rascunho.local)) {
    opcoes.unshift({ valor: rascunho.local, rotulo: texto.cidadeUf(localAtual.cidade, localAtual.uf) });
  }

  return (
    <div className="space-y-3">
      <div className="grid items-start gap-4 md:grid-cols-2 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1fr)_auto]">
        <Select
          rotulo={texto.local}
          name="local"
          opcoes={opcoes}
          opcaoVazia={texto.qualquerCidade}
          erro={erros.local}
          value={rascunho.local}
          iconeInicial="location_on"
          onChange={(evento) => aoAlterar('local', evento.target.value)}
        />
        <Campo
          rotulo={texto.retirada}
          name="dataRetirada"
          type="date"
          className="min-w-0"
          obrigatorio
          required
          min={hoje}
          max="9999-12-30"
          erro={erros.dataRetirada}
          value={rascunho.dataRetirada}
          iconeInicial="calendar_today"
          onChange={(evento) => aoAlterar('dataRetirada', evento.target.value)}
        />
        <div className="min-w-0">
          <Campo
            rotulo={texto.devolucao}
            name="dataDevolucao"
            type="date"
            className="min-w-0"
            obrigatorio
            required
            min={periodo.minimoDevolucao}
            max="9999-12-31"
            erro={erros.dataDevolucao}
            value={rascunho.dataDevolucao}
            iconeInicial="event_repeat"
            apoioRotulo={periodo.diarias > 0 ? texto.numeroDiarias(periodo.diarias) : undefined}
            apoioRotuloClasse={CLASSES_PILULA_DIARIAS}
            apoioRotuloEmLinha
            onChange={(evento) => aoAlterar('dataDevolucao', evento.target.value)}
          />
        </div>
        <Botao type="submit" larguraTotal icone="search" className="!text-navy lg:mt-6" carregando={buscando}>
          {textoBotao}
        </Botao>
      </div>
      {cidades.isPending && <output className="block text-legenda text-texto-apoio">{texto.carregandoCidades}</output>}
      {cidades.isError && <p role="alert" className="text-legenda text-status-perigo">{texto.erroCidades}</p>}
      {cidades.data?.length === 0 && <p className="text-legenda text-texto-apoio">{texto.semCidades}</p>}
    </div>
  );
}
