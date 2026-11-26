import { Icone } from '@/components/ui';
import { formatarCnpj, formatarPlaca } from '@/lib/formatters';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import type { ContratoDadosLocacao, ContratoLocacao } from '@/types/contratoLocacao';
import { TITULO_CLAUSULA_VALORES, dadosDaClausula, valoresPrevistos } from './regrasContrato';

/**
 * Impressão pelo navegador (window.print) só do papel: o resto da tela fica oculto e o papel ocupa a página.
 * O bloco é fixo (sem interpolação) e se aplica à folha de impressão, não à tela.
 */
export const CSS_IMPRESSAO_CONTRATO = `@media print {
  body * { visibility: hidden !important; }
  #documento-contrato, #documento-contrato * { visibility: visible !important; }
  #documento-contrato { position: absolute; left: 0; top: 0; width: 100%; max-width: none; box-shadow: none; }
}`;

/** Estilo de impressão do papel do contrato. Fica dentro da página, ao lado do papel. */
export function EstiloImpressaoContrato() {
  return <style>{CSS_IMPRESSAO_CONTRATO}</style>;
}

interface PropsPapelContrato {
  contrato: ContratoLocacao;
}

/**
 * Papel do contrato (desenho 08, coluna da esquerda). Mostra o documento que o servidor devolve: o montado na hora enquanto
 * não há assinatura, e a cópia guardada depois dela. Nenhum texto de cláusula é escrito aqui.
 */
export function PapelContrato({ contrato }: PropsPapelContrato) {
  const { documento } = contrato;
  const { locadora, locatario, veiculo, locacao } = documento;
  const assinado = contrato.situacao === 'ASSINADO';

  return (
    <section className="flex flex-col overflow-hidden rounded-xl bg-branco shadow-md">
      <div className="flex flex-wrap items-center justify-between gap-2 bg-navy px-4 py-2 text-branco">
        <div className="flex items-center gap-2">
          <Icone nome="description" tamanho={20} className="text-orange-acao" />
          <span className="text-rotulo tracking-wide text-steel-fundo">
            {assinado ? M.tituloPapelAssinado : M.tituloPapelPendente}
          </span>
        </div>
        <button
          type="button"
          title={M.imprimir}
          aria-label={M.imprimir}
          onClick={() => window.print()}
          className="rounded p-1.5 text-steel-apagado transition-colors hover:bg-navy-marinho hover:text-branco focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
        >
          <Icone nome="print" tamanho={18} />
        </button>
      </div>

      <div className="max-h-[780px] overflow-y-auto bg-steel-fundo p-6">
        <article
          id="documento-contrato"
          className="mx-auto flex max-w-2xl flex-col gap-4 rounded-lg bg-branco p-8 text-texto-desenho shadow-sm md:p-12"
        >
          <header className="flex flex-col items-center gap-2 border-b-2 border-steel-alto pb-4 text-center">
            <div className="flex items-center gap-2">
              <Icone nome="directions_car" tamanho={26} className="text-orange-acao" />
              <span className="text-titulo-md font-bold tracking-tight text-navy-profundo">
                {locadora.razaoSocial.toUpperCase()}
              </span>
            </div>
            <p className="text-micro uppercase tracking-wider text-contorno">{M.cnpj(formatarCnpj(locadora.cnpj))}</p>
            <p className="mt-2 rounded bg-steel-suave px-3 py-1 text-corpo-denso font-semibold uppercase tracking-wide text-navy">
              {M.tituloContrato(locacao.numero)}
            </p>
          </header>

          <p className="text-justify text-apoio leading-relaxed text-texto-apoio">
            {M.preambulo(
              locadora.razaoSocial.toUpperCase(),
              formatarCnpj(locadora.cnpj),
              locatario.nome,
              locatario.cpfMascarado,
              locatario.cnhCategoria,
            )}
          </p>

          {documento.clausulas.map((clausula) => {
            const linhas = dadosDaClausula(clausula.titulo, documento);
            return (
              <div
                key={clausula.numero}
                className={clausula.numero === 1 ? 'flex flex-col gap-1 rounded-lg bg-steel-fundo/50 p-2' : 'flex flex-col gap-1'}
              >
                <span className="text-corpo-denso font-bold uppercase text-navy-profundo">
                  {M.tituloClausula(clausula.numero, clausula.titulo)}
                </span>
                <p className="text-justify text-apoio leading-relaxed text-texto-apoio">{clausula.texto}</p>
                {clausula.numero === 1 && (
                  <dl className="mt-1 grid grid-cols-2 gap-2 rounded bg-branco p-2 sm:grid-cols-4">
                    <div className="min-w-0">
                      <dt className="text-micro text-contorno">{M.veiculoModelo}</dt>
                      <dd className="break-words text-dado-tabular font-semibold text-texto-desenho">
                        {`${veiculo.marca} ${veiculo.modelo}`}
                      </dd>
                    </div>
                    <div className="min-w-0">
                      <dt className="text-micro text-contorno">{M.veiculoPlaca}</dt>
                      <dd className="break-words text-dado-tabular font-bold text-frota-oficina">{formatarPlaca(veiculo.placa)}</dd>
                    </div>
                    <div className="min-w-0">
                      <dt className="text-micro text-contorno">{M.veiculoChassi}</dt>
                      <dd className="break-all text-dado-tabular font-semibold text-texto-desenho">{veiculo.chassi}</dd>
                    </div>
                    <div className="min-w-0">
                      <dt className="text-micro text-contorno">{M.veiculoRenavam}</dt>
                      <dd className="break-all text-dado-tabular font-semibold text-texto-desenho">{veiculo.renavam}</dd>
                    </div>
                  </dl>
                )}
                {linhas.length > 0 && (
                  <dl className="mt-1 grid grid-cols-2 gap-2 rounded bg-steel-fundo p-2 sm:grid-cols-4">
                    {linhas.map((linha) => (
                      <div key={linha.rotulo} className="min-w-0">
                        <dt className="text-micro text-contorno">{linha.rotulo}</dt>
                        <dd className="break-words text-dado-tabular font-semibold text-texto-desenho">{linha.valor}</dd>
                      </div>
                    ))}
                  </dl>
                )}
                {clausula.titulo === TITULO_CLAUSULA_VALORES && <QuadroValoresPrevistos locacao={documento.locacao} />}
              </div>
            );
          })}

          {contrato.hashSha256 !== null && (
            <footer className="flex justify-between border-t border-steel-alto pt-4 text-micro text-contorno">
              <span>{M.hashPapel(contrato.hashSha256)}</span>
            </footer>
          )}
        </article>
      </div>
    </section>
  );
}

interface PropsQuadroValoresPrevistos {
  locacao: ContratoDadosLocacao;
}

/** Quadro de valores previstos da cláusula de valores (desenho 08, cláusula 3): frase, linha de diárias e total. */
function QuadroValoresPrevistos({ locacao }: PropsQuadroValoresPrevistos) {
  const valores = valoresPrevistos(locacao);
  return (
    <div className="mt-1 flex flex-col gap-2">
      <p className="text-justify text-apoio leading-relaxed text-texto-apoio">{valores.frase}</p>
      <div className="flex flex-col gap-1 rounded bg-steel-fundo p-2 text-dado-tabular">
        <div className="flex items-start justify-between gap-2 py-0.5">
          <span className="min-w-0 break-words text-texto-apoio">{valores.linhaDiarias}</span>
          <span className="shrink-0 font-semibold text-texto-desenho">{valores.subtotal}</span>
        </div>
        <div className="flex items-start justify-between gap-2 border-t border-contorno-suave pt-1 text-corpo-denso font-bold text-navy-profundo">
          <span className="uppercase">{MENSAGENS_RETIRADA.totalPrevisto}</span>
          <span className="shrink-0">{valores.total}</span>
        </div>
      </div>
      <p className="text-justify text-apoio leading-relaxed text-texto-apoio">{M.cobrancasForaDoTotal}</p>
    </div>
  );
}
