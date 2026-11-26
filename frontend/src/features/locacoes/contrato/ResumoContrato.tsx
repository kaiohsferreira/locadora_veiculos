import { Icone } from '@/components/ui';
import { formatarPlaca } from '@/lib/formatters';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import type { ContratoDocumento, ContratoLocacao } from '@/types/contratoLocacao';
import { etapasDoContrato, percentualCombustivel, textoCartaoDiarias, textoFranquia } from './regrasContrato';

interface PropsTrilhaContrato {
  contrato: ContratoLocacao;
}

/** Trilha de quatro etapas (desenho 08, faixa do topo). Os nomes e a conferência vêm da tela 16. */
export function TrilhaContrato({ contrato }: PropsTrilhaContrato) {
  const etapas = etapasDoContrato(contrato);
  return (
    <div className="rounded-xl bg-branco p-4 shadow-sm">
      <ol className="grid grid-cols-1 gap-2 md:grid-cols-4">
        {etapas.map((etapa, indice) => {
          const ativa = etapa.status === 'ativa';
          const concluida = etapa.status === 'concluida';
          return (
            <li
              key={etapa.titulo}
              aria-current={ativa ? 'step' : undefined}
              className={[
                'flex items-center gap-2 rounded-lg p-2',
                ativa ? 'bg-navy text-branco shadow-sm' : 'bg-steel-fundo',
                etapa.status === 'pendente' ? 'opacity-70' : '',
              ].join(' ')}
            >
              <span
                className={[
                  'flex h-8 w-8 shrink-0 items-center justify-center rounded-xl',
                  ativa ? 'bg-orange-acao text-branco' : '',
                  concluida ? 'bg-navy-profundo text-branco' : '',
                  etapa.status === 'pendente' ? 'bg-steel-alto text-texto-apoio' : '',
                ].join(' ')}
              >
                <Icone nome={concluida ? 'done' : ativa ? 'description' : 'key'} tamanho={18} />
              </span>
              <div className="flex min-w-0 flex-col">
                <div className="flex flex-wrap items-center gap-2">
                  <span
                    className={[
                      'text-micro uppercase tracking-wider',
                      ativa ? 'text-retirada-selo-icone' : 'text-texto-apoio',
                    ].join(' ')}
                  >
                    {MENSAGENS_RETIRADA.etapa(indice + 1)}
                  </span>
                  {ativa && (
                    <span className="rounded bg-frota-oficina px-1.5 text-chip-frota font-bold uppercase text-branco">
                      {MENSAGENS_RETIRADA.emAndamento}
                    </span>
                  )}
                </div>
                <span className={['truncate text-corpo-denso font-semibold', ativa ? 'text-branco' : 'text-texto-desenho'].join(' ')}>
                  {etapa.titulo}
                </span>
                {etapa.situacao !== null && (
                  <span className={['text-apoio', ativa ? 'text-steel-palido' : 'text-texto-apoio'].join(' ')}>
                    {etapa.situacao}
                  </span>
                )}
              </div>
            </li>
          );
        })}
      </ol>
    </div>
  );
}

interface PropsCartoesResumo {
  documento: ContratoDocumento;
}

/** Resumo do contrato (desenho 08): franquia, combustível da retirada e caução. Só valores que o documento traz. */
export function CartoesResumo({ documento }: PropsCartoesResumo) {
  const cartoes = [
    { icone: 'speed', rotulo: M.franquiaRotulo, valor: textoFranquia(documento.tabelaCobrancas) },
    {
      icone: 'local_gas_station',
      rotulo: M.combustivelRotulo,
      // Sem nível na vistoria de entrada (nulo no contrato), mostra o texto aprovado de "não informada" (M.franquiaNaoInformada).
      valor:
        documento.vistoriaEntrada.nivelCombustivel === null
          ? M.franquiaNaoInformada
          : M.combustivelNaRetirada(percentualCombustivel(documento.vistoriaEntrada.nivelCombustivel)),
    },
    { icone: 'calendar_month', rotulo: M.diariasRotulo, valor: textoCartaoDiarias(documento.locacao) },
    {
      icone: 'lock',
      rotulo: M.caucaoRotulo,
      valor: documento.locacao.valorCaucaoStr ?? M.semCaucao,
    },
  ];
  return (
    <section className="flex flex-col gap-2 rounded-xl bg-branco p-4 shadow-md">
      <div className="flex items-center justify-between gap-2">
        <h2 className="flex items-center gap-2 text-titulo-md font-bold text-texto-desenho">
          <Icone nome="fact_check" tamanho={20} className="text-frota-oficina" />
          {M.resumoTitulo}
        </h2>
        <span className="rounded bg-steel-suave px-2 py-0.5 text-micro text-texto-apoio">{M.valoresPrevistos}</span>
      </div>
      <dl className="grid grid-cols-2 gap-1">
        {cartoes.map((cartao) => (
          <div key={cartao.rotulo} className="flex items-start gap-2 rounded-lg bg-steel-fundo p-2">
            <Icone nome={cartao.icone} tamanho={18} className="mt-0.5 shrink-0 text-navy" />
            <div className="flex min-w-0 flex-col">
              <dt className="text-micro text-texto-apoio">{cartao.rotulo}</dt>
              <dd className="text-corpo-denso font-semibold text-texto-desenho">{cartao.valor}</dd>
            </div>
          </div>
        ))}
      </dl>
    </section>
  );
}

interface PropsCartaoVeiculo {
  documento: ContratoDocumento;
}

/** Cartão do veículo e da locadora no rodapé da coluna (desenho 08, sem a foto, que o contrato não traz). */
export function CartaoVeiculoContrato({ documento }: PropsCartaoVeiculo) {
  const { veiculo, locadora } = documento;
  return (
    <div className="flex flex-col gap-1 rounded-xl bg-branco p-4 shadow-sm">
      <div className="flex items-center justify-between gap-2">
        <span className="truncate text-corpo-denso font-semibold text-texto-desenho">{`${veiculo.marca} ${veiculo.modelo}`}</span>
        <span className="shrink-0 rounded bg-navy-profundo px-2 py-0.5 text-micro font-bold text-branco">
          {formatarPlaca(veiculo.placa)}
        </span>
      </div>
      <span className="truncate text-apoio text-texto-apoio">{`${locadora.nomeFantasia} • ${locadora.cidade} - ${locadora.uf}`}</span>
    </div>
  );
}
