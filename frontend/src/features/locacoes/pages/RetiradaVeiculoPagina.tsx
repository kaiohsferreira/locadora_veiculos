import { useRef } from 'react';
import { generatePath, useNavigate, useParams } from 'react-router-dom';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, BotaoLink, Cartao, EstadoErro, Etiqueta, useAviso } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { formatarPlaca, formatarQuilometragem } from '@/lib/formatters';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { ContextoRetirada } from '@/types/retirada';
import { useAbrirLocacao, useRetirada } from '../hooks/useRetirada';
import { esquemaConferenciaRetirada, lerIdReserva, retiradaDisponivel, type ConferenciaRetirada } from '../retirada/validacaoRetirada';
interface ConferenciaProps { contexto: ContextoRetirada; atualizando: boolean; }
function Conferencia({ contexto, atualizando }: ConferenciaProps) {
  const registro = useAbrirLocacao(contexto.idReserva);
  const navegar = useNavigate();
  const aviso = useAviso();
  const enviando = useRef(false);
  const { register, handleSubmit, control, formState: { errors, isSubmitting } } = useForm<ConferenciaRetirada>({
    resolver: zodResolver(esquemaConferenciaRetirada), defaultValues: { cpfConferido: false, cnhConferida: false },
  });
  const [cpfConferido, cnhConferida] = useWatch({ control, name: ['cpfConferido', 'cnhConferida'] });
  const conferido = cpfConferido && cnhConferida;
  const ocupado = atualizando || registro.isPending || isSubmitting;
  const disponivel = retiradaDisponivel(contexto);
  async function enviar() {
    if (enviando.current || atualizando || !disponivel) return;
    enviando.current = true;
    try {
      const locacao = await registro.mutateAsync();
      aviso.sucesso(M.sucesso);
      navegar(generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(locacao.idLocacao) }));
    } catch (erro: unknown) { aviso.erro(obterMensagemErro(erro)); }
    finally { enviando.current = false; }
  }
  return <Cartao titulo="Conferência dos documentos" descricao={M.local}>
    <form noValidate onSubmit={(event) => { void handleSubmit(enviar)(event); }} aria-busy={ocupado}>
      <fieldset disabled={ocupado || !disponivel} className="space-y-6">
        <legend className="sr-only">Documentos apresentados pelo cliente</legend>
        <div className="space-y-4">
          <label className="flex items-start gap-4 text-sm">
            <input type="checkbox" required aria-required="true" {...register('cpfConferido')} aria-invalid={!!errors.cpfConferido} className="mt-1 h-4 w-4 accent-navy" />
            <span>Conferi o CPF e a identidade do cliente no documento original.</span>
          </label>
          <label className="flex items-start gap-4 text-sm">
            <input type="checkbox" required aria-required="true" {...register('cnhConferida')} aria-invalid={!!errors.cnhConferida} className="mt-1 h-4 w-4 accent-navy" />
            <span>Conferi a CNH original apresentada pelo cliente, incluindo categoria e validade.</span>
          </label>
        </div>
        {(errors.cpfConferido || errors.cnhConferida) && <p role="alert" className="text-sm text-status-perigo">{M.conferir}</p>}
        {!disponivel && <output className="block text-sm text-texto-suave">{M.indisponivel}</output>}
        {registro.isError && <div role="alert" className="space-y-2 text-sm text-status-perigo">
          <p>{obterMensagemErro(registro.error)}</p>
          <p>{registro.error instanceof ErroApi && registro.error.status === 409 ? M.conflito : M.recuperar}</p>
        </div>}
        <Botao type="submit" className="h-auto min-h-10 w-full whitespace-normal py-2 sm:w-auto" iconeDireita="arrow_forward" carregando={registro.isPending || isSubmitting} disabled={ocupado || !conferido || !disponivel}>Iniciar locação e ir para vistoria</Botao>
      </fieldset>
    </form>
  </Cartao>;
}
export default function RetiradaVeiculoPagina() {
  const { reservaId } = useParams<'reservaId'>();
  const id = lerIdReserva(reservaId);
  const consulta = useRetirada(id);
  const contexto = consulta.data;
  useTituloDocumento(M.titulo);
  return <div className="mx-auto w-full max-w-5xl space-y-6 px-4 py-8 sm:px-6">
    <header className="space-y-2">
      <BotaoLink to={CAMINHOS.reservas} variante="fantasma" icone="arrow_back">Voltar às reservas</BotaoLink>
      <h1 className="text-2xl font-bold text-navy">{M.titulo}</h1><p className="text-texto-suave">{M.descricao}</p>
    </header>
    {id === null ? <EstadoErro mensagem={M.idInvalido} /> : consulta.isPending ? <CarregandoPagina />
      : consulta.isError ? <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => { void consulta.refetch(); }} tentando={consulta.isFetching} />
      : contexto && <>
        <Cartao titulo={`Reserva ${contexto.codigoReserva}`} acoes={<Etiqueta status={contexto.statusReserva} />}>
          <dl className="grid gap-4 text-sm sm:grid-cols-2">
            <div><dt className="text-texto-suave">Retirada prevista</dt><dd>{contexto.dataRetiradaPrevistaStr}</dd></div>
            <div><dt className="text-texto-suave">Devolução prevista</dt><dd>{contexto.dataDevolucaoPrevistaStr}</dd></div>
            <div><dt className="text-texto-suave">Valor da diária</dt><dd>{contexto.valorDiariaStr}</dd></div>
            <div><dt className="text-texto-suave">Valor estimado</dt><dd>{contexto.valorEstimadoStr}</dd></div>
          </dl>
        </Cartao>
        <div className="grid gap-6 lg:grid-cols-2">
          <Cartao titulo="Cliente e documentação">
            <dl className="grid gap-4 text-sm sm:grid-cols-2">
              <div className="sm:col-span-2"><dt className="text-texto-suave">Nome do cliente</dt><dd className="break-words font-semibold">{contexto.clienteNome}</dd></div>
              <div><dt className="text-texto-suave">CPF</dt><dd>{contexto.clienteCpfMascarado}</dd></div>
              <div><dt className="text-texto-suave">CNH</dt><dd>{contexto.cnhNumero}</dd></div>
              <div><dt className="text-texto-suave">Categoria da CNH</dt><dd>{contexto.cnhCategoria}</dd></div>
              <div><dt className="text-texto-suave">Validade da CNH</dt><dd>{contexto.cnhValidadeStr}</dd></div>
              <div className="sm:col-span-2"><dt className="mb-2 text-texto-suave">Cadastro nesta locadora</dt><dd>{contexto.statusCadastroLocadora ? <Etiqueta status={contexto.statusCadastroLocadora} /> : 'Sem cadastro ativo nesta locadora'}</dd></div>
            </dl>
          </Cartao>
          <Cartao titulo="Veículo reservado" acoes={<Etiqueta status={contexto.statusVeiculo} />}>
            <dl className="grid gap-4 text-sm sm:grid-cols-2">
              <div className="sm:col-span-2"><dt className="text-texto-suave">Marca e modelo</dt><dd className="font-semibold">{contexto.marca} {contexto.modelo}</dd></div>
              <div><dt className="text-texto-suave">Placa</dt><dd>{formatarPlaca(contexto.placa)}</dd></div>
              <div><dt className="text-texto-suave">Quilometragem atual</dt><dd>{formatarQuilometragem(contexto.kmAtual)}</dd></div>
            </dl>
          </Cartao>
        </div>
        <div className="flex justify-end"><Botao variante="fantasma" icone="refresh" disabled={consulta.isFetching} onClick={() => { void consulta.refetch(); }}>Atualizar dados</Botao></div>
        {contexto.idLocacaoExistente !== null ? <Cartao titulo="Locação já iniciada">
          <output className="mb-6 block text-texto-suave">{M.existente}</output>
          <BotaoLink to={generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(contexto.idLocacaoExistente) })} iconeDireita="arrow_forward">Continuar para vistoria</BotaoLink>
        </Cartao> : <Conferencia key={id} contexto={contexto} atualizando={consulta.isFetching} />}
      </>}
  </div>;
}
