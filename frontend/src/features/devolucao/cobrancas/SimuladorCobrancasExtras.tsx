import { useEffect, useRef, useState } from 'react';
import { Botao, Cartao, EstadoErro, useAviso } from '@/components/ui';
import { MENSAGENS_COBRANCAS_EXTRAS as M, mensagemConfirmacaoApuracao } from '@/lib/mensagensCobrancasExtras';
import { obterMensagemErro } from '@/lib/erros';
import { ErroApi } from '@/types/api';
import type { SimulacaoCobrancasExtras } from '@/types/cobrancasExtras';
import type { ContextoDevolucao } from '@/types/devolucao';
import { motivoBloqueioSimulacao } from './validacaoCobrancasExtras';
import { ResultadoSimulacao } from './ResultadoSimulacao';
import { usePermissoes } from '@/routes/autenticacao/usePermissoes';
import { podeApurarCobrancasExtras } from './permissaoCobrancasExtras';
import { useApuracaoCobrancasExtras, useRegistrarApuracaoCobrancasExtras, useSimularCobrancasExtras } from './useCobrancasExtras';

export interface SimuladorCobrancasExtrasProps { contexto: ContextoDevolucao; atualizando: boolean }
interface PreviaCobrancas { contexto: ContextoDevolucao; resultado: SimulacaoCobrancasExtras }
interface ContextoObservado { contexto: ContextoDevolucao; atualizando: boolean; consultando: boolean }
export function SimuladorCobrancasExtras({ contexto, atualizando }: SimuladorCobrancasExtrasProps) {
  const id = contexto.locacao.idLocacao;
  const apuracao = useApuracaoCobrancasExtras(id);
  const simulacao = useSimularCobrancasExtras(id);
  const registro = useRegistrarApuracaoCobrancasExtras(id);
  // Só ATENDENTE e ADMINISTRADOR simulam e apuram (decisão do Kaio em 24/11/2026). Só afeta a tela: o servidor confere de novo.
  const podeApurar = podeApurarCobrancasExtras(usePermissoes().cargo);
  const aviso = useAviso();
  const [previa, setPrevia] = useState<PreviaCobrancas | null>(null);
  const [confirmacao, setConfirmacao] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [observado, setObservado] = useState<ContextoObservado>({ contexto, atualizando, consultando: apuracao.isFetching });
  const geracao = useRef(0);
  const enviando = useRef(false);
  // Ajusta somente quando as entradas mudam, antes de expor uma prévia antiga.
  if (observado.contexto !== contexto || observado.atualizando !== atualizando || observado.consultando !== apuracao.isFetching) {
    setObservado({ contexto, atualizando, consultando: apuracao.isFetching });
    setPrevia(null); setConfirmacao(false);
    if (observado.contexto !== contexto || observado.atualizando !== atualizando) setErro(null);
  }
  useEffect(() => {
    geracao.current += 1;
    return () => { geracao.current += 1; };
  }, [contexto, atualizando, apuracao.isFetching]);
  const bloqueio = motivoBloqueioSimulacao(contexto);
  const ocupado = atualizando || apuracao.isFetching || simulacao.isPending || registro.isPending;
  const vigente = previa?.contexto === contexto && !ocupado ? previa.resultado : null;
  function mensagem(falha: unknown) {
    return falha instanceof ErroApi && falha.status === 409 ? M.erro409 : obterMensagemErro(falha);
  }
  async function simular() {
    if (ocupado || enviando.current || bloqueio || apuracao.data || !podeApurar) return;
    enviando.current = true; setPrevia(null); setConfirmacao(false); setErro(null);
    const versao = geracao.current;
    try {
      const resultado = await simulacao.mutateAsync();
      if (versao === geracao.current) setPrevia({ contexto, resultado });
    } catch (falha: unknown) { if (versao === geracao.current) { setErro(mensagem(falha)); aviso.erro(mensagem(falha)); } }
    finally { enviando.current = false; }
  }
  async function registrar() {
    if (!vigente || !confirmacao || ocupado || enviando.current || bloqueio || !podeApurar) return;
    enviando.current = true; setErro(null);
    const politica = vigente.configuracao;
    setPrevia(null); setConfirmacao(false);
    try { await registro.mutateAsync({ idConfiguracao: politica.idConfiguracao, versaoEsperada: politica.versao }); aviso.sucesso(M.confirmado); }
    catch (falha: unknown) { setErro(mensagem(falha)); aviso.erro(mensagem(falha)); }
    finally { enviando.current = false; }
  }
  // Cargo sem permissão de apurar: sem consulta registrada (ou consultando), o cartão não aparece. Com apuração ou erro, mostra só a leitura.
  if (!podeApurar && !apuracao.data && !apuracao.isError) return null;
  return <Cartao titulo="Cobranças extras"><div className="min-w-0 w-full space-y-6 break-words" aria-busy={ocupado}>
    {apuracao.isPending ? <output className="block">Consultando apuração registrada…</output>
      : apuracao.isError ? <EstadoErro mensagem={obterMensagemErro(apuracao.error)} aoTentarNovamente={() => { void apuracao.refetch(); }} />
      : apuracao.data ? <><p>Apuração {apuracao.data.idApuracao} registrada em {apuracao.data.dataApuracaoStr}. Política versão {apuracao.data.versaoConfiguracao} preservada.</p><ResultadoSimulacao resultado={apuracao.data.calculo} /><p>{apuracao.data.cobrancas.length === 0 ? 'Apuração registrada sem cobranças adicionais.' : `${apuracao.data.cobrancas.length} cobrança(s) registrada(s).`}</p>{typeof apuracao.data.idPagamentoLancado === 'number' && <p>{M.pagamentoLancado(apuracao.data.idPagamentoLancado)}</p>}</>
      : podeApurar ? <>
        <p className="rounded-lg bg-status-pendente-fundo p-4">{M.aviso}</p>
        {bloqueio ? <output className="block">{bloqueio}</output> : <>
          <Botao disabled={ocupado} carregando={simulacao.isPending} onClick={() => { void simular(); }}>Simular com política cadastrada</Botao>
          {vigente && <><ResultadoSimulacao resultado={vigente} />
            <label className="flex items-start gap-4"><input type="checkbox" className="mt-1 h-4 w-4 shrink-0 accent-navy" checked={confirmacao} onChange={event => setConfirmacao(event.target.checked)} /><span>{mensagemConfirmacaoApuracao(vigente.total)}</span></label>
            <Botao disabled={!confirmacao || ocupado} onClick={() => { void registrar(); }}>Confirmar e registrar apuração</Botao>
          </>}
        </>}
      </> : null}
    {registro.isPending && <output className="block">Registrando e conferindo apuração…</output>}
    {erro && <p role="alert" className="text-status-perigo">{erro}</p>}
  </div></Cartao>;
}
