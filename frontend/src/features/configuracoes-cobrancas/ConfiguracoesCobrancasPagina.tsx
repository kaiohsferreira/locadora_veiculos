import { useRef, useState } from 'react';
import { Botao, Cartao, EstadoErro, Paginacao, Select, useAviso } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_COBRANCAS_EXTRAS as M } from '@/lib/mensagensCobrancasExtras';
import type { ConfiguracaoCobrancasExtras } from '@/types/configuracoesCobrancasExtras';
import { FormularioConfiguracaoCobrancas } from './FormularioConfiguracaoCobrancas';
import { useConfiguracaoCobrancas, useConfiguracoesCobrancas, useDesativarConfiguracaoCobrancas } from './useConfiguracoesCobrancas';

export default function ConfiguracoesCobrancasPagina() {
  useTituloDocumento('Políticas de cobranças extras');
  const [pagina, setPagina] = useState(1);
  const [idCategoria, setIdCategoria] = useState('');
  const [edicao, setEdicao] = useState<number | 'nova' | null>(null);
  const [desativacao, setDesativacao] = useState<ConfiguracaoCobrancasExtras | null>(null);
  const categorias = useCategorias();
  const lista = useConfiguracoesCobrancas({ pagina, tamanhoPagina: 10, ...(idCategoria ? { idCategoria: Number(idCategoria) } : {}) });
  const detalhe = useConfiguracaoCobrancas(typeof edicao === 'number' ? edicao : null);
  const desativar = useDesativarConfiguracaoCobrancas();
  const aviso = useAviso();
  const enviando = useRef(false);
  async function confirmarDesativacao() {
    if (!desativacao || enviando.current) return;
    enviando.current = true;
    try { await desativar.mutateAsync(desativacao.idConfiguracao); aviso.sucesso(M.desativado); setDesativacao(null); setPagina(1); }
    catch (erro: unknown) { aviso.erro(obterMensagemErro(erro)); }
    finally { enviando.current = false; }
  }
  return <div className="mx-auto min-w-0 w-full max-w-5xl space-y-6 break-words">
    <header className="space-y-4"><h1 className="text-2xl font-bold text-navy">Políticas de cobranças extras</h1><p>Defina as regras da locadora por categoria de veículo. Alterações geram uma nova versão; apurações já registradas preservam a política utilizada.</p><Botao onClick={() => { setEdicao('nova'); setDesativacao(null); }}>Nova política</Botao></header>
    <Cartao titulo="Políticas cadastradas">
      <div className="space-y-4">
        <Select rotulo="Filtrar por categoria" value={idCategoria} opcaoVazia="Todas as categorias" opcoes={(categorias.data?.itens ?? []).map(c => ({ valor: String(c.idCategoria), rotulo: c.nome }))} onChange={event => { setIdCategoria(event.target.value); setPagina(1); }} />
        {lista.isPending ? <CarregandoPagina /> : lista.isError ? <EstadoErro mensagem={obterMensagemErro(lista.error)} aoTentarNovamente={() => { void lista.refetch(); }} /> : <>
          {lista.data.itens.length === 0 ? <p>Nenhuma política cadastrada para este filtro.</p> : <ul className="space-y-4">{lista.data.itens.map(c => <li key={c.idConfiguracao} className="space-y-2 rounded-lg border border-borda p-4">
            <h3 className="font-semibold">{c.categoriaNome} · Versão {c.versao}</h3><p>Franquia: {c.franquiaKm} km {c.baseFranquia === 'TOTAL' ? 'no total' : 'por diária prevista'}. Atualizada em {c.updatedAtStr}.</p>
            <div className="flex flex-wrap gap-4"><Botao variante="contorno" disabled={desativar.isPending} aria-label={`Editar política de ${c.categoriaNome}`} onClick={() => { setEdicao(c.idConfiguracao); setDesativacao(null); }}>Editar</Botao><Botao variante="fantasma" disabled={desativar.isPending} aria-label={`Desativar política de ${c.categoriaNome}`} onClick={() => { setDesativacao(c); setEdicao(null); }}>Desativar</Botao></div>
          </li>)}</ul>}
          <Paginacao pagina={lista.data.pagina} tamanhoPagina={lista.data.tamanhoPagina} total={lista.data.total} aoMudarPagina={setPagina} desabilitada={lista.isFetching} />
        </>}
      </div>
    </Cartao>
    {desativacao && <Cartao titulo="Confirmar desativação"><div className="space-y-4"><p>Desativar a política de {desativacao.categoriaNome}? Novas apurações exigirão uma política ativa. Apurações existentes serão preservadas.</p><div className="flex flex-wrap gap-4"><Botao variante="perigo" carregando={desativar.isPending} disabled={desativar.isPending} onClick={() => { void confirmarDesativacao(); }}>Confirmar desativação</Botao><Botao variante="contorno" disabled={desativar.isPending} onClick={() => setDesativacao(null)}>Cancelar desativação</Botao></div></div></Cartao>}
    {edicao === 'nova' ? <Cartao titulo="Nova política"><FormularioConfiguracaoCobrancas key="nova" aoConcluir={() => setEdicao(null)} aoCancelar={() => setEdicao(null)} /></Cartao>
      : typeof edicao === 'number' && <Cartao titulo="Editar política">{detalhe.isPending ? <CarregandoPagina /> : detalhe.isError ? <EstadoErro mensagem={obterMensagemErro(detalhe.error)} aoTentarNovamente={() => { void detalhe.refetch(); }} /> : detalhe.data && <FormularioConfiguracaoCobrancas key={edicao} inicial={detalhe.data} aoConcluir={() => setEdicao(null)} aoCancelar={() => setEdicao(null)} recarregar={async () => { const atual = await detalhe.refetch({ throwOnError: true }); if (!atual.data) throw new Error('Política indisponível.'); return atual.data; }} />}</Cartao>}
  </div>;
}
