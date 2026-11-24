import { Etiqueta, Icone, ImagemVeiculo } from '@/components/ui';
import { formatarQuilometragem } from '@/lib/formatters';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import type { ContextoRetirada } from '@/types/retirada';
import { caucaoPodeSerTentada, mostrarBlocoCaucao } from '../regrasCaucaoRetirada';
import { mostrarTotalPrevisto, selodaSituacaoEscuro, textoDiarias, textoSituacaoReserva } from '../regrasRetirada';
import { CartaoRetirada } from './CartaoRetirada';
import { PlacaRetirada } from './PlacaRetirada';

export interface ResumoReservaRetiradaProps { contexto: ContextoRetirada; }

const ROTULO_BLOCO = 'text-micro font-semibold uppercase tracking-wider text-texto-apoio';

/**
 * Caixa "Garantia / Caução" do desenho (html :175-185), dentro da Tarifação, depois do "Total previsto". Só com a caução
 * exigida e sem locação já aberta. Na linha de baixo, no lugar de "Pré-autorizado no Mastercard final 4018" (cortado: a
 * tela nunca mostra caução aprovada), a situação: antes do clique ou, curto, indisponível (ajuste A4: o texto inteiro fica
 * só no item da pré-autorização). O cargo da sessão não muda esta caixa.
 */
function GarantiaCaucaoRetirada({ contexto }: ResumoReservaRetiradaProps) {
  const podeSerTentada = caucaoPodeSerTentada(contexto);
  return (
    <div className="flex flex-col gap-1 rounded bg-branco p-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-micro font-semibold uppercase text-texto-apoio">{M.caucaoRotulo}</span>
        <span className="text-corpo-denso font-bold text-texto-desenho">{contexto.valorCaucaoStr}</span>
      </div>
      <span className={`flex items-start gap-1.5 text-micro font-semibold ${podeSerTentada ? 'text-texto-apoio' : 'text-status-perigo'}`}>
        <Icone nome={podeSerTentada ? 'schedule' : 'block'} tamanho={16} />
        <span>{podeSerTentada ? M.caucaoSituacaoAoIniciar : M.caucaoIndisponivelCurto}</span>
      </span>
    </div>
  );
}

/** Coluna da esquerda do desenho 16: código e situação, veículo, condutor, período e local, e tarifa. */
export function ResumoReservaRetirada({ contexto }: ResumoReservaRetiradaProps) {
  const situacao = textoSituacaoReserva(contexto.statusReserva);
  const escuro = selodaSituacaoEscuro(contexto.statusReserva);
  return (
    <CartaoRetirada labelledBy="titulo-resumo-retirada" semEspacamento className="flex flex-col gap-6">
      <div className="flex items-start justify-between gap-2 bg-steel-fundo/50 p-6 pb-4">
        <div className="flex flex-col">
          <span className="text-micro font-semibold uppercase text-texto-apoio">{M.codigoReserva}</span>
          <span id="titulo-resumo-retirada" className="text-valor-destaque font-bold tracking-tight text-texto-desenho">{`#${contexto.codigoReserva}`}</span>
        </div>
        {escuro ? (
          <span className="inline-flex items-center gap-1.5 rounded-sm bg-navy-profundo px-2 py-1 text-rotulo font-semibold tracking-wide text-branco">
            <Icone nome="verified" tamanho={14} preenchido className="text-retirada-selo-icone" />
            {situacao}
          </span>
        ) : (
          <Etiqueta status={contexto.statusReserva} rotulo={situacao} />
        )}
      </div>

      <div className="flex flex-col gap-6 px-6 pb-6">
        <div className="flex flex-col gap-4 rounded bg-steel-fundo p-4">
          <div className="flex items-center justify-between gap-2">
            <span className={ROTULO_BLOCO}>{M.veiculoAtribuido}</span>
            <span className="text-micro font-semibold text-orange-acao">{M.grupo(contexto.categoriaNome)}</span>
          </div>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
            <ImagemVeiculo
              url={contexto.fotoCapa?.url ?? null}
              alt={`${contexto.marca} ${contexto.modelo}`}
              className="h-20 w-full shrink-0 rounded !bg-steel-alto shadow-sm sm:w-28"
            />
            <div className="flex min-w-0 flex-1 flex-col gap-1">
              <h3 className="text-titulo-md font-bold leading-snug text-texto-desenho">{`${contexto.marca} ${contexto.modelo}`}</h3>
              <span className="text-apoio text-texto-apoio">{contexto.anoModelo}</span>
              <PlacaRetirada placa={contexto.placa} />
            </div>
          </div>
          {contexto.erroFotoCapa && (
            <output className="flex items-center gap-2 text-apoio text-status-pendente">
              <Icone nome="image_not_supported" tamanho={18} />
              <span>{contexto.erroFotoCapa}</span>
            </output>
          )}
          <div className="flex items-center justify-between gap-2 text-apoio text-texto-apoio">
            <span>{`${M.quilometragemAtual}: ${formatarQuilometragem(contexto.kmAtual)}`}</span>
            <Etiqueta status={contexto.statusVeiculo} />
          </div>
        </div>

        <div className="flex flex-col gap-1">
          <span className={ROTULO_BLOCO}>{M.dadosCondutor}</span>
          <div className="flex flex-col gap-2 rounded bg-steel-fundo p-4">
            <span className="text-corpo-denso font-bold text-texto-desenho">{contexto.clienteNome}</span>
            <dl className="grid grid-cols-2 gap-1 text-apoio text-texto-apoio">
              <div><dt className="inline text-contorno">{M.cpf}</dt> <dd className="inline">{contexto.clienteCpfMascarado}</dd></div>
              <div><dt className="inline text-contorno">{M.cnh}</dt> <dd className="inline">{contexto.cnhNumero}</dd></div>
              <div><dt className="inline text-contorno">{M.categoria}</dt> <dd className="inline">{contexto.cnhCategoria}</dd></div>
              <div><dt className="inline text-contorno">{M.validade}</dt> <dd className="inline">{contexto.cnhValidadeStr}</dd></div>
            </dl>
            <div className="flex items-center justify-between gap-2 pt-1 text-apoio text-texto-apoio">
              <span>{M.cadastroNaLocadora}</span>
              {contexto.statusCadastroLocadora ? <Etiqueta status={contexto.statusCadastroLocadora} /> : <span>{M.semCadastro}</span>}
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-1">
          <span className={ROTULO_BLOCO}>{M.periodo}</span>
          <div className="flex flex-col gap-2 rounded bg-steel-fundo p-4">
            <div className="flex items-start gap-2">
              <Icone nome="calendar_today" tamanho={20} className="mt-0.5 text-frota-oficina" />
              <div className="flex flex-col">
                <span className="text-corpo-denso font-semibold text-texto-desenho">{`${contexto.dataRetiradaPrevistaStr} → ${contexto.dataDevolucaoPrevistaStr}`}</span>
                <span className="text-apoio text-texto-apoio">{textoDiarias(contexto.diariasPrevistas)}</span>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <Icone nome="location_on" tamanho={20} className="mt-0.5 text-navy-profundo" />
              <div className="flex flex-col">
                <span className="text-corpo-denso font-semibold text-texto-desenho">{contexto.localRetiradaNome}</span>
                <span className="text-apoio text-texto-apoio">{contexto.localRetiradaEndereco}</span>
              </div>
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-2 rounded bg-steel-fundo p-4">
          <div className="flex items-center justify-between gap-2 pb-1">
            <span className={ROTULO_BLOCO}>{M.tarifacao}</span>
            <span className="text-apoio text-texto-apoio">{M.diaria} <strong className="text-texto-desenho">{contexto.valorDiariaStr}</strong></span>
          </div>
          {mostrarTotalPrevisto(contexto.valorEstimadoStr) && (
            <div className="flex items-center justify-between gap-2">
              <span className="text-corpo-denso font-medium text-texto-desenho">{M.totalPrevisto}</span>
              <span className="text-valor-destaque font-bold text-navy-profundo">{contexto.valorEstimadoStr}</span>
            </div>
          )}
          {mostrarBlocoCaucao(contexto) && <GarantiaCaucaoRetirada contexto={contexto} />}
        </div>
      </div>
    </CartaoRetirada>
  );
}
