import { useState } from 'react';
import { EstadoErro, Paginacao } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGEM_FINANCEIRO_TITULO_DOCUMENTO } from '@/lib/mensagens';
import type { AbaTitulosFinanceiros, TituloFinanceiro } from '@/types/painelFinanceiro';
import {
  useIndicadoresFinanceiros,
  useTitulosFinanceiros,
} from '@/features/financeiro/hooks/usePainelFinanceiro';
import { ajustarPaginaAoTotal, ehTituloExibivel } from '@/features/financeiro/painel/regrasPainelFinanceiro';
import { AbasTitulosFinanceiras } from '@/features/financeiro/painel/componentes/AbasTitulosFinanceiros';
import { CabecalhoPainelFinanceiro } from '@/features/financeiro/painel/componentes/CabecalhoPainelFinanceiro';
import { CartoesIndicadoresFinanceiros } from '@/features/financeiro/painel/componentes/CartoesIndicadoresFinanceiros';
import { ModalDarBaixa } from '@/features/financeiro/painel/componentes/ModalDarBaixa';
import { TabelaTitulosFinanceiras } from '@/features/financeiro/painel/componentes/TabelaTitulosFinanceiros';

/** Linhas por página da tabela (tamanho fixo na versão mínima: o desenho não tem seletor de linhas). */
const TAMANHO_PAGINA_TITULOS = 20;
const PRIMEIRA_PAGINA = 1;

/**
 * P7-FE-01 · Painel financeiro (tela 09, versão mínima: decisão P12 do Kaio). Quatro cartões com os indicadores, abas
 * com contadores, tabela paginada e o diálogo "Dar baixa". Cada bloco tem o seu estado de carregando, vazio e erro; um
 * erro num bloco não derruba os outros. Acesso só FINANCEIRO e ADMINISTRADOR (o servidor confere de novo).
 */
export default function PainelFinanceiroPagina() {
  useTituloDocumento(MENSAGEM_FINANCEIRO_TITULO_DOCUMENTO);
  const [aba, setAba] = useState<AbaTitulosFinanceiros>('TODOS');
  const [pagina, setPagina] = useState(PRIMEIRA_PAGINA);
  const [baixando, setBaixando] = useState<TituloFinanceiro | null>(null);

  const indicadores = useIndicadoresFinanceiros();
  const titulos = useTitulosFinanceiros({ aba, pagina, tamanhoPagina: TAMANHO_PAGINA_TITULOS });

  const dadosIndicadores = indicadores.data;
  const erroIndicadores = indicadores.isError && dadosIndicadores === undefined;
  const dadosTitulos = titulos.data;
  const erroTitulos = titulos.isError && dadosTitulos === undefined;
  const itensExibidos = (dadosTitulos?.pagina.itens ?? []).filter(ehTituloExibivel);

  // Página além da última (ex.: a última linha dela saiu da lista depois da baixa): volta para a última que existe. Feito
  // durante o render, como o React indica para estado derivado (mesmo padrão da tela de equipe); não há efeito nem laço.
  if (dadosTitulos !== undefined) {
    const paginaExistente = ajustarPaginaAoTotal(pagina, dadosTitulos.pagina.total, TAMANHO_PAGINA_TITULOS);
    if (paginaExistente !== pagina) setPagina(paginaExistente);
  }

  function mudarAba(novaAba: AbaTitulosFinanceiros) {
    setAba(novaAba);
    setPagina(PRIMEIRA_PAGINA);
  }

  return (
    <div className="flex flex-col gap-6">
      <CabecalhoPainelFinanceiro
        periodo={
          dadosIndicadores === undefined
            ? undefined
            : { deStr: dadosIndicadores.deStr, ateStr: dadosIndicadores.ateStr }
        }
        carregandoPeriodo={indicadores.isPending}
      />

      <CartoesIndicadoresFinanceiros
        indicadores={dadosIndicadores}
        carregando={indicadores.isPending}
        erro={erroIndicadores}
        mensagemErro={erroIndicadores ? obterMensagemErro(indicadores.error) : null}
        tentando={indicadores.isFetching}
        aoTentarNovamente={() => void indicadores.refetch()}
      />

      <section className="flex flex-col overflow-hidden rounded-cartao bg-branco shadow-sm">
        <div className="flex flex-col gap-4 p-4">
          <AbasTitulosFinanceiras
            abaAtual={aba}
            contadores={
              dadosTitulos === undefined
                ? undefined
                : {
                    quantidadeTodos: dadosTitulos.quantidadeTodos,
                    quantidadeEmAtraso: dadosTitulos.quantidadeEmAtraso,
                    quantidadeVenceHoje: dadosTitulos.quantidadeVenceHoje,
                  }
            }
            aoMudarAba={mudarAba}
          />
        </div>

        {erroTitulos ? (
          <EstadoErro
            mensagem={obterMensagemErro(titulos.error)}
            aoTentarNovamente={() => void titulos.refetch()}
            tentando={titulos.isFetching}
          />
        ) : (
          <>
            <TabelaTitulosFinanceiras
              itens={itensExibidos}
              carregando={titulos.isPending}
              aoDarBaixa={setBaixando}
            />
            {/* Sem linhas, o rodapé não aparece: a tabela já mostra o vazio (um só texto, como no desenho). */}
            {dadosTitulos !== undefined && dadosTitulos.pagina.total > 0 && (
              <div className="bg-steel-fundo">
                <Paginacao
                  pagina={dadosTitulos.pagina.pagina}
                  tamanhoPagina={dadosTitulos.pagina.tamanhoPagina}
                  total={dadosTitulos.pagina.total}
                  aoMudarPagina={setPagina}
                  desabilitada={titulos.isFetching}
                />
              </div>
            )}
          </>
        )}
      </section>

      {baixando !== null && <ModalDarBaixa titulo={baixando} aoFechar={() => setBaixando(null)} />}
    </div>
  );
}
