import { useRef } from 'react';
import { useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, BotaoLink, Campo, Cartao, EstadoErro, Etiqueta, useAviso } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import { formatarNumero, formatarQuilometragem } from '@/lib/formatters';
import { MENSAGENS_VISTORIA as M } from '@/lib/mensagensVistoria';
import { CAMINHOS } from '@/routes/caminhos';
import { useRegistrarVistoriaEntrada, useVistoriaEntrada } from '../hooks/useVistoriaEntrada';
import { converterVistoriaEntrada, esquemaVistoriaEntrada, lerIdLocacao, type CamposVistoriaEntrada } from '../vistoria/validacaoVistoria';

interface FormularioEntradaProps {
  idLocacao: number;
  atualizando: boolean;
}

function FormularioEntrada({ idLocacao, atualizando }: FormularioEntradaProps) {
  const registro = useRegistrarVistoriaEntrada(idLocacao);
  const aviso = useAviso();
  const enviando = useRef(false);
  const { register, handleSubmit, setError, formState: { errors, isSubmitting } } = useForm<CamposVistoriaEntrada>({
    resolver: zodResolver(esquemaVistoriaEntrada),
    defaultValues: { kmRegistrado: '', nivelCombustivel: '', observacoes: '' },
  });

  async function enviar(campos: CamposVistoriaEntrada) {
    if (enviando.current || atualizando) return;
    enviando.current = true;
    try {
      await registro.mutateAsync(converterVistoriaEntrada(campos));
      aviso.sucesso(M.sucesso);
    } catch (erro: unknown) {
      aplicarErrosDeCampo(erro, ['kmRegistrado', 'nivelCombustivel', 'observacoes'] as const,
        (campo, mensagem) => setError(campo, { type: 'server', message: mensagem }, { shouldFocus: true }));
      aviso.erro(obterMensagemErro(erro));
    } finally {
      enviando.current = false;
    }
  }

  // O callback lê a ref somente no evento de envio; handleSubmit não o executa ao renderizar.
  // eslint-disable-next-line react/refs -- guarda síncrona contra envio duplo no evento.
  const aoEnviar = handleSubmit(enviar, () => aviso.erro(M.dadosInvalidos));
  const ocupado = atualizando || registro.isPending || isSubmitting;
  return (
    <Cartao titulo="Registrar vistoria">
      <form noValidate onSubmit={aoEnviar} aria-busy={ocupado}>
        <fieldset disabled={ocupado} className="space-y-6">
          <legend className="sr-only">Dados da vistoria de entrada</legend>
          <div className="grid gap-6 sm:grid-cols-2">
            <Campo rotulo="Quilometragem registrada (km)" obrigatorio required inputMode="numeric"
              {...register('kmRegistrado')} erro={errors.kmRegistrado?.message} />
            <Campo rotulo="Nível de combustível" obrigatorio required inputMode="decimal"
              dica={M.combustivelDica} {...register('nivelCombustivel')} erro={errors.nivelCombustivel?.message} />
          </div>
          <div className="space-y-2">
            <label htmlFor="vistoria-observacoes" className="block text-sm font-medium text-texto">Observações (opcional)</label>
            <textarea id="vistoria-observacoes" rows={4} {...register('observacoes')}
              aria-invalid={errors.observacoes ? true : undefined}
              aria-describedby={errors.observacoes ? 'vistoria-observacoes-erro' : undefined}
              className="w-full rounded-lg border border-borda bg-branco p-4 text-texto focus-visible:outline focus-visible:outline-2 focus-visible:outline-navy" />
            {errors.observacoes && <p id="vistoria-observacoes-erro" role="alert" className="text-sm text-status-perigo">{errors.observacoes.message}</p>}
          </div>
          {registro.isError && <p role="alert" className="text-sm text-status-perigo">{obterMensagemErro(registro.error)}</p>}
          <Botao type="submit" carregando={registro.isPending || isSubmitting} disabled={ocupado}>Registrar vistoria de entrada</Botao>
        </fieldset>
      </form>
    </Cartao>
  );
}

/** RF-18: entrada somente. A rota permanece protegida pelo perfil funcionário. */
export default function VistoriaAvariasPagina() {
  const { locacaoId } = useParams<'locacaoId'>();
  const id = lerIdLocacao(locacaoId);
  const consulta = useVistoriaEntrada(id);
  useTituloDocumento(M.titulo);
  const contexto = consulta.data;
  const vistoria = contexto?.vistoria;

  return (
    <div className="mx-auto w-full max-w-5xl space-y-6 px-4 py-8 sm:px-6">
      <header className="space-y-2">
        <BotaoLink to={CAMINHOS.locacoes} variante="fantasma" icone="arrow_back">Voltar às locações</BotaoLink>
        <h1 className="text-2xl font-bold text-navy">{M.titulo}</h1>
        <p className="text-texto-suave">{M.descricao}</p>
      </header>
      {id === null ? <EstadoErro mensagem={M.idInvalido} />
        : consulta.isPending ? <CarregandoPagina />
        : consulta.isError ? <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => { void consulta.refetch(); }} tentando={consulta.isFetching} />
        : contexto && <>
          <Cartao titulo={`Locação ${formatarNumero(contexto.locacao.idLocacao)}`}>
            <dl className="grid gap-4 text-sm sm:grid-cols-2">
              <div><dt className="text-texto-suave">Reserva</dt><dd>{formatarNumero(contexto.locacao.idReserva)}</dd></div>
              <div><dt className="text-texto-suave">Veículo</dt><dd>{formatarNumero(contexto.locacao.idVeiculo)}</dd></div>
              <div><dt className="text-texto-suave">Retirada</dt><dd>{contexto.locacao.dataRetiradaStr}</dd></div>
              <div><dt className="text-texto-suave">Devolução prevista</dt><dd>{contexto.locacao.dataDevolucaoPrevistaStr}</dd></div>
            </dl>
          </Cartao>
          {vistoria ? <Cartao titulo="Vistoria registrada">
            <output className="mb-6 block space-y-2"><Etiqueta rotulo="Entrada registrada" tom="sucesso" /><span className="block">{M.registrada}</span></output>
            <dl className="grid gap-4 text-sm sm:grid-cols-2">
              <div><dt className="text-texto-suave">Registrada em</dt><dd>{vistoria.dataHoraStr}</dd></div>
              <div><dt className="text-texto-suave">Funcionário responsável</dt><dd>{formatarNumero(vistoria.idFuncionario)}</dd></div>
              <div><dt className="text-texto-suave">Quilometragem registrada</dt><dd>{formatarQuilometragem(vistoria.kmRegistrado)}</dd></div>
              <div><dt className="text-texto-suave">Nível de combustível (0 a 1)</dt><dd>{vistoria.nivelCombustivel === null ? M.semCombustivel : formatarNumero(vistoria.nivelCombustivel, 2)}</dd></div>
              <div className="sm:col-span-2"><dt className="text-texto-suave">Observações</dt><dd className="whitespace-pre-wrap break-words">{vistoria.observacoes || M.semObservacoes}</dd></div>
            </dl>
          </Cartao> : <FormularioEntrada key={id} idLocacao={id} atualizando={consulta.isFetching} />}
        </>}
    </div>
  );
}
