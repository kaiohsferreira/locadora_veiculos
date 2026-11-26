import { useRef } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Campo, Cartao, Select, useAviso } from '@/components/ui';
import { obterMensagemErro } from '@/lib/erros';
import { formatarMoeda, formatarNumero, formatarQuilometragem } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { ContextoDevolucao } from '@/types/devolucao';
import { converterSaida, criarEsquemaSaida, type CamposSaida } from '../validacaoSaida';
import { useRegistrarSaida } from '../hooks/useDevolucao';

export interface FormularioSaidaProps {
  contexto: ContextoDevolucao;
  /** Atualização do contexto em curso: desabilita o envio. */
  atualizando: boolean;
}

/** Primeiro passo da tela 06 sem vistoria de saída: o formulário atual (vistoria-saida, POST), sem mudança de regra. */
export function FormularioSaida({ contexto, atualizando }: FormularioSaidaProps) {
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
  return <Cartao titulo={M.formulario.titulo}>
    <form noValidate onSubmit={event => { void handleSubmit(enviar, () => aviso.erro(M.invalido))(event); }} aria-busy={ocupado}>
      <fieldset disabled={ocupado} className="space-y-6"><legend className="sr-only">{M.formulario.legenda}</legend>
        <div className="grid gap-6 sm:grid-cols-2">
          <Campo rotulo={M.formulario.km} obrigatorio required inputMode="numeric" dica={M.formulario.kmMinimo(formatarQuilometragem(kmMinimo))} {...register('kmRegistrado')} erro={errors.kmRegistrado?.message} />
          <Campo rotulo={M.formulario.combustivel} obrigatorio required inputMode="decimal" dica={M.formulario.combustivelDica} {...register('nivelCombustivel')} erro={errors.nivelCombustivel?.message} />
        </div>
        <fieldset className="space-y-4"><legend className="mb-4 font-semibold text-navy">{M.formulario.avariasEntradaLegenda}</legend>
          <p className="text-sm text-texto-suave">{M.formulario.avariasEntradaAjuda}</p>
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
        <section className="space-y-4"><h3 className="font-semibold text-navy">{M.formulario.novasTitulo}</h3>
          {fields.map((field, index) => <fieldset key={field.id} className="space-y-4 rounded-lg border border-borda p-4">
            <legend className="px-2">{M.formulario.novaLegenda(index + 1)}</legend>
            <Campo rotulo={M.formulario.descricao} obrigatorio required maxLength={200} {...register(`novasAvarias.${index}.descricao`)} erro={errors.novasAvarias?.[index]?.descricao?.message} />
            <div className="grid gap-4 sm:grid-cols-2">
              <Select rotulo={M.formulario.gravidade} obrigatorio required opcoes={Object.entries(M.gravidades).map(([valor, rotulo]) => ({ valor, rotulo }))} {...register(`novasAvarias.${index}.gravidade`)} erro={errors.novasAvarias?.[index]?.gravidade?.message} />
              <Campo rotulo={M.formulario.valorEstimado} obrigatorio required inputMode="decimal" {...register(`novasAvarias.${index}.valorEstimado`)} erro={errors.novasAvarias?.[index]?.valorEstimado?.message} />
            </div>
            <Botao variante="fantasma" onClick={() => { remove(index); setValue('comparacaoConferida', false); }} aria-label={M.formulario.removerAvariaNome(index + 1)}>{M.formulario.removerAvaria}</Botao>
          </fieldset>)}
          <Botao variante="contorno" onClick={() => { append({ descricao: '', gravidade: 'LEVE', valorEstimado: '' }); setValue('comparacaoConferida', false); }} icone="add">{M.formulario.adicionarAvaria}</Botao>
        </section>
        <Campo rotulo={M.formulario.observacoes} {...register('observacoes')} erro={errors.observacoes?.message} />
        <div className="space-y-4 rounded-lg bg-offwhite p-4">
          <h3 className="font-semibold text-navy">{M.formulario.conferenciaTitulo}</h3>
          <p>{M.formulario.contagem(formatarNumero(selecionados.length), formatarNumero(novas.length))}</p>
          {novas.length > 0 && <ul className="list-inside list-disc">{novas.map((a, i) => <li key={fields[i]?.id ?? i} className="break-words">{a.descricao || M.formulario.descricaoPendente} · {M.gravidades[a.gravidade]}</li>)}</ul>}
          <label className="flex items-start gap-4"><input type="checkbox" {...register('comparacaoConferida')} aria-required="true" aria-invalid={!!errors.comparacaoConferida} aria-describedby={errors.comparacaoConferida ? 'conferencia-erro' : undefined} className="mt-1 h-4 w-4 accent-navy" /><span>{M.formulario.conferir}</span></label>
          {errors.comparacaoConferida && <p id="conferencia-erro" role="alert" className="text-status-perigo">{errors.comparacaoConferida.message}</p>}
        </div>
        {registro.isError && <p role="alert" className="text-status-perigo">{obterMensagemErro(registro.error)}</p>}
        <Botao type="submit" carregando={registro.isPending || isSubmitting} disabled={ocupado}>{M.formulario.registrar}</Botao>
      </fieldset>
    </form>
  </Cartao>;
}
