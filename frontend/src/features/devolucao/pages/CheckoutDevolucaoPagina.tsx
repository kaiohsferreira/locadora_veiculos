import { useRef } from 'react';
import { generatePath, useParams } from 'react-router-dom';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, BotaoLink, Campo, Cartao, EstadoErro, Select, useAviso } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { formatarMoeda, formatarNumero, formatarQuilometragem } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { CAMINHOS } from '@/routes/caminhos';
import type { AvariaComparacao, ComparacaoSaida, ContextoDevolucao, VistoriaDevolucao } from '@/types/devolucao';
import { lerIdLocacao } from '@/features/locacoes/vistoria/validacaoVistoria';
import { useDevolucao, useRegistrarSaida } from '../hooks/useDevolucao';
import { converterSaida, criarEsquemaSaida, type CamposSaida } from '../validacaoSaida';
import { SimuladorCobrancasExtras } from '../cobrancas/SimuladorCobrancasExtras';

interface ListaAvariasProps { titulo: string; avarias: AvariaComparacao[] }
export function ListaAvarias({ titulo, avarias }: ListaAvariasProps) {
  return <section className="space-y-4"><h3 className="font-semibold text-navy">{titulo}</h3>
    {avarias.length === 0 ? <p className="text-texto-suave">{M.vazio}</p> : <ul className="space-y-4">
      {avarias.map(a => <li key={a.idAvaria} className="rounded-lg border border-borda p-4">
        <p className="break-words font-medium">{a.descricao}</p>
        <p className="text-sm text-texto-suave">{M.gravidades[a.gravidade]} · {a.valorEstimado === null ? M.semValor : formatarMoeda(a.valorEstimado)}</p>
      </li>)}
    </ul>}
  </section>;
}
interface ResumoVistoriaProps { vistoria: VistoriaDevolucao }
function ResumoVistoria({ vistoria }: ResumoVistoriaProps) {
  return <dl className="grid gap-4 text-sm sm:grid-cols-2">
    <div><dt className="text-texto-suave">Registrada em</dt><dd>{vistoria.dataHoraStr}</dd></div>
    <div><dt className="text-texto-suave">Responsável</dt><dd>{formatarNumero(vistoria.idFuncionario)}</dd></div>
    <div><dt className="text-texto-suave">Quilometragem</dt><dd>{formatarQuilometragem(vistoria.kmRegistrado)}</dd></div>
    <div><dt className="text-texto-suave">Combustível (0 a 1)</dt><dd>{formatarNumero(vistoria.nivelCombustivel, 2)}</dd></div>
    <div className="sm:col-span-2"><dt className="text-texto-suave">Observações</dt><dd className="whitespace-pre-wrap break-words">{vistoria.observacoes || 'Sem observações.'}</dd></div>
  </dl>;
}
interface ComparacaoRegistradaProps { comparacao: ComparacaoSaida }
export function ComparacaoRegistrada({ comparacao }: ComparacaoRegistradaProps) {
  return <Cartao titulo="Vistoria de saída registrada"><div className="space-y-6">
    <ResumoVistoria vistoria={comparacao.saida} />
    {comparacao.comparacaoRealizada ? <>
      <ListaAvarias titulo="Já existiam" avarias={comparacao.avariasPreexistentes} />
      <ListaAvarias titulo="Novas" avarias={comparacao.avariasNovas} />
      {comparacao.avariasIndeterminadas.length > 0 && <ListaAvarias titulo="Comparação não confirmada" avarias={comparacao.avariasIndeterminadas} />}
    </> : <><output className="block rounded-lg bg-status-pendente-fundo p-4">{M.legado}</output>
      <ListaAvarias titulo="Comparação não confirmada" avarias={comparacao.avariasIndeterminadas} /></>}
  </div></Cartao>;
}
interface FormularioSaidaProps { contexto: ContextoDevolucao; atualizando: boolean }
function FormularioSaida({ contexto, atualizando }: FormularioSaidaProps) {
  const registro = useRegistrarSaida(contexto.locacao.idLocacao);
  const aviso = useAviso();
  const enviando = useRef(false);
  const kmMinimo = Math.max(contexto.locacao.kmSaida, contexto.entrada?.kmRegistrado ?? 0);
  const { register, control, handleSubmit, setValue, formState: { errors, isSubmitting } } = useForm<CamposSaida>({
    resolver: zodResolver(criarEsquemaSaida(kmMinimo, contexto.avariasEntrada.map(a => a.idAvaria))),
    defaultValues: { kmRegistrado: '', nivelCombustivel: '', observacoes: '', idsAvariasEntrada: [], novasAvarias: [], comparacaoConferida: false },
  });
  const { fields, append, remove } = useFieldArray({ control, name: 'novasAvarias' });
  const selecionados = useWatch({ control, name: 'idsAvariasEntrada' });
  const novas = useWatch({ control, name: 'novasAvarias' });
  const ocupado = atualizando || registro.isPending || isSubmitting;
  async function enviar(campos: CamposSaida) {
    if (enviando.current || atualizando) return;
    enviando.current = true;
    try { await registro.mutateAsync(converterSaida(campos)); aviso.sucesso(M.sucesso); }
    catch (erro: unknown) { aviso.erro(obterMensagemErro(erro)); }
    finally { enviando.current = false; }
  }
  return <Cartao titulo="Registrar vistoria de saída">
    <form noValidate onSubmit={event => { void handleSubmit(enviar, () => aviso.erro(M.invalido))(event); }} aria-busy={ocupado}>
      <fieldset disabled={ocupado} className="space-y-6"><legend className="sr-only">Conferência de saída</legend>
        <div className="grid gap-6 sm:grid-cols-2">
          <Campo rotulo="Quilometragem na devolução (km)" obrigatorio required inputMode="numeric" dica={`Mínimo: ${formatarQuilometragem(kmMinimo)}`} {...register('kmRegistrado')} erro={errors.kmRegistrado?.message} />
          <Campo rotulo="Nível de combustível (0 a 1)" obrigatorio required inputMode="decimal" dica="Aceita vírgula e até duas casas decimais." {...register('nivelCombustivel')} erro={errors.nivelCombustivel?.message} />
        </div>
        <fieldset className="space-y-4"><legend className="mb-4 font-semibold text-navy">Avarias de entrada ainda presentes</legend>
          <p className="text-sm text-texto-suave">Marque somente as avarias que continuam presentes. Os dados da entrada serão preservados.</p>
          {contexto.avariasEntrada.length === 0 && <p>{M.vazio}</p>}
          {contexto.avariasEntrada.map(a => <label key={a.idAvaria} className="flex items-start gap-4 rounded-lg border border-borda p-4">
            <input type="checkbox" checked={selecionados.includes(a.idAvaria)} onChange={event => {
              setValue('idsAvariasEntrada', event.target.checked ? [...selecionados, a.idAvaria] : selecionados.filter(id => id !== a.idAvaria), { shouldValidate: true });
              setValue('comparacaoConferida', false);
            }} className="mt-1 h-4 w-4 accent-navy" />
            <span className="break-words">{a.descricao}<span className="block text-sm text-texto-suave">{M.gravidades[a.gravidade]} · {a.valorEstimado === null ? M.semValor : formatarMoeda(a.valorEstimado)}</span></span>
          </label>)}
          {errors.idsAvariasEntrada && <p role="alert" className="text-status-perigo">{errors.idsAvariasEntrada.message}</p>}
        </fieldset>
        <section className="space-y-4"><h3 className="font-semibold text-navy">Novas avarias</h3>
          {fields.map((field, index) => <fieldset key={field.id} className="space-y-4 rounded-lg border border-borda p-4">
            <legend className="px-2">Nova avaria {index + 1}</legend>
            <Campo rotulo="Descrição" obrigatorio required maxLength={200} {...register(`novasAvarias.${index}.descricao`)} erro={errors.novasAvarias?.[index]?.descricao?.message} />
            <div className="grid gap-4 sm:grid-cols-2">
              <Select rotulo="Gravidade" obrigatorio required opcoes={Object.entries(M.gravidades).map(([valor, rotulo]) => ({ valor, rotulo }))} {...register(`novasAvarias.${index}.gravidade`)} erro={errors.novasAvarias?.[index]?.gravidade?.message} />
              <Campo rotulo="Valor estimado (R$)" obrigatorio required inputMode="decimal" {...register(`novasAvarias.${index}.valorEstimado`)} erro={errors.novasAvarias?.[index]?.valorEstimado?.message} />
            </div>
            <Botao variante="fantasma" onClick={() => { remove(index); setValue('comparacaoConferida', false); }} aria-label={`Remover nova avaria ${index + 1}`}>Remover avaria</Botao>
          </fieldset>)}
          <Botao variante="contorno" onClick={() => { append({ descricao: '', gravidade: 'LEVE', valorEstimado: '' }); setValue('comparacaoConferida', false); }} icone="add">Adicionar nova avaria</Botao>
        </section>
        <Campo rotulo="Observações (opcional)" {...register('observacoes')} erro={errors.observacoes?.message} />
        <div className="space-y-4 rounded-lg bg-offwhite p-4">
          <h3 className="font-semibold text-navy">Conferência da comparação</h3>
          <p>Já existiam: {formatarNumero(selecionados.length)} · Novas: {formatarNumero(novas.length)}</p>
          {novas.length > 0 && <ul className="list-inside list-disc">{novas.map((a, i) => <li key={fields[i]?.id ?? i} className="break-words">{a.descricao || 'Descrição pendente'} · {M.gravidades[a.gravidade]}</li>)}</ul>}
          <label className="flex items-start gap-4"><input type="checkbox" {...register('comparacaoConferida')} aria-required="true" aria-invalid={!!errors.comparacaoConferida} aria-describedby={errors.comparacaoConferida ? 'conferencia-erro' : undefined} className="mt-1 h-4 w-4 accent-navy" /><span>Conferi a comparação entre as avarias da entrada e as novas avarias. *</span></label>
          {errors.comparacaoConferida && <p id="conferencia-erro" role="alert" className="text-status-perigo">{errors.comparacaoConferida.message}</p>}
        </div>
        {registro.isError && <p role="alert" className="text-status-perigo">{obterMensagemErro(registro.error)}</p>}
        <Botao type="submit" carregando={registro.isPending || isSubmitting} disabled={ocupado}>Registrar vistoria de saída</Botao>
      </fieldset>
    </form>
  </Cartao>;
}
interface ConteudoDevolucaoProps { contexto: ContextoDevolucao; atualizando: boolean }
export function ConteudoDevolucao({ contexto, atualizando }: ConteudoDevolucaoProps) {
  const { locacao, entrada, comparacao } = contexto;
  return <>
    <Cartao titulo={`Locação ${formatarNumero(locacao.idLocacao)}`}><p>Quilometragem na retirada: {formatarQuilometragem(locacao.kmSaida)}</p></Cartao>
    {entrada && <Cartao titulo="Vistoria de entrada"><ResumoVistoria vistoria={entrada} /></Cartao>}
    {comparacao ? <ComparacaoRegistrada comparacao={comparacao} /> : !entrada ? <Cartao titulo="Vistoria de entrada necessária"><p className="mb-4">{M.semEntrada}</p><BotaoLink to={generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(locacao.idLocacao) })}>Ir para vistoria de entrada</BotaoLink></Cartao>
      : locacao.status !== 'EM_ANDAMENTO' && locacao.status !== 'ATRASADA' ? <EstadoErro mensagem={M.indisponivel} />
      : <><Cartao titulo="Avarias registradas na entrada"><ListaAvarias titulo="Referência para conferência" avarias={contexto.avariasEntrada} /></Cartao><FormularioSaida key={locacao.idLocacao} contexto={contexto} atualizando={atualizando} /></>}
    <SimuladorCobrancasExtras key={locacao.idLocacao} contexto={contexto} atualizando={atualizando} />
  </>;
}
export default function CheckoutDevolucaoPagina() {
  const { locacaoId } = useParams<'locacaoId'>();
  const id = lerIdLocacao(locacaoId);
  const consulta = useDevolucao(id);
  useTituloDocumento(M.titulo);
  return <div className="mx-auto w-full max-w-5xl space-y-6 px-4 py-8 sm:px-6">
    <header className="space-y-2"><BotaoLink to={CAMINHOS.locacoes} variante="fantasma" icone="arrow_back">Voltar às locações</BotaoLink><h1 className="text-2xl font-bold text-navy">{M.titulo}</h1><p className="text-texto-suave">{M.descricao}</p></header>
    {id === null ? <EstadoErro mensagem={M.idInvalido} /> : consulta.isPending ? <CarregandoPagina />
      : consulta.isError ? <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => { void consulta.refetch(); }} tentando={consulta.isFetching} />
      : consulta.data && <ConteudoDevolucao contexto={consulta.data} atualizando={consulta.isFetching} />}
  </div>;
}
