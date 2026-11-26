import { useRef } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Campo, Etiqueta, Icone, useAviso } from '@/components/ui';
import { SEGMENTOS_COMBUSTIVEL, descreverNivelCombustivel } from '@/components/ui/calculoCombustivel';
import type { VistoriaEntrada } from '@/types/vistorias';
import { ErroApi } from '@/types/api';
import { formatarNumero, formatarQuilometragem } from '@/lib/formatters';
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_DEVOLUCAO } from '@/lib/mensagensDevolucao';
import { MENSAGENS_VISTORIA as M } from '@/lib/mensagensVistoria';
import {
  MENSAGEM_VISTORIA_COMBUSTIVEL_CAMPO,
  MENSAGEM_VISTORIA_COMBUSTIVEL_ROTULO,
  MENSAGEM_VISTORIA_DADOS_LEGENDA,
  MENSAGEM_VISTORIA_FUNCIONARIO,
  MENSAGEM_VISTORIA_KM_REGISTRADO,
  MENSAGEM_VISTORIA_OBSERVACOES,
  MENSAGEM_VISTORIA_OBSERVACOES_ROTULO,
  MENSAGEM_VISTORIA_ODOMETRO_ROTULO,
  MENSAGEM_VISTORIA_REGISTRADA_EM,
  MENSAGEM_VISTORIA_STATUS_REGISTRADA,
  REGUA_COMBUSTIVEL_VISTORIA,
  mensagemKmAcimaDaRetirada,
} from '@/lib/mensagensVistoriaAvarias';
import { useRegistrarVistoriaEntrada } from '../hooks/useVistoriaEntrada';
import { converterVistoriaEntrada, esquemaVistoriaEntrada, kmDigitado, nivelDigitado, type CamposVistoriaEntrada } from './validacaoVistoria';
import { kmAcimaDaRetirada } from './avariasVistoria';

/** Índices dos oito segmentos do medidor, na ordem da barra. */
const SEGMENTOS = Array.from({ length: SEGMENTOS_COMBUSTIVEL }, (_, indice) => indice);

interface MedidorVistoriaProps {
  /** Nível exato de 0 a 1 (o valor gravado). A barra mostra o oitavo mais próximo; o texto, o exato. */
  nivel: number;
}

/**
 * Medidor da tela 19 (desenho): rótulo em caixa alta, selo verde com "{k}/8 • nome (percentual%)" e barra de oito
 * segmentos laranja. Só visual: a barra é escondida do leitor de tela, que anuncia o selo.
 */
function MedidorVistoria({ nivel }: MedidorVistoriaProps) {
  const descricao = descreverNivelCombustivel(nivel);
  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="flex items-center gap-1 text-micro font-bold uppercase text-texto-apoio">
          <Icone nome="local_gas_station" tamanho={16} className="text-frota-oficina" />
          {MENSAGEM_VISTORIA_COMBUSTIVEL_ROTULO}
        </span>
        <span className="rounded bg-frota-disponivel-fundo px-2 py-0.5 text-dado-tabular font-bold text-vistoria-conferido">{descricao.texto}</span>
      </div>
      <div aria-hidden="true" className="grid h-6 w-full grid-cols-8 items-center gap-1.5 rounded-md bg-steel-suave p-1">
        {SEGMENTOS.map((indice) => (
          <span
            key={indice}
            className={`h-full rounded-sm shadow-sm transition-colors motion-reduce:transition-none ${indice < descricao.oitavos ? 'bg-orange-acao' : 'bg-steel-alto'}`}
          />
        ))}
      </div>
      <div className="flex justify-between px-1 text-micro text-texto-apoio">
        {REGUA_COMBUSTIVEL_VISTORIA.map((texto) => (
          <span key={texto} className={texto.startsWith('Cheio') ? 'font-bold text-texto-desenho' : undefined}>{texto}</span>
        ))}
      </div>
    </div>
  );
}

export interface FormularioOdometroProps {
  idLocacao: number;
  kmSaida: number;
  /** Id do formulário, para o botão "Registrar vistoria de entrada" fora do cartão (rodapé fixo). */
  formId: string;
  /** Depois de a vistoria ser registrada: a tela envia as avarias locais. */
  aoRegistrada(idVistoria: number): Promise<void>;
  /** Registro recusado com 409 (a entrada já existe ou a regra mudou): a tela relê a entrada e trata as avarias locais. */
  aoRegistroRecusado(): Promise<void>;
  /** Avisa a tela enquanto a vistoria está sendo gravada: o mapa e a adição de avaria ficam travados. */
  aoRegistrando(registrando: boolean): void;
  atualizando: boolean;
}

/** Formulário de km, combustível e observações da vistoria de entrada (regra e erros da tela 19 anterior, mantidos). */
export function FormularioOdometro({ idLocacao, kmSaida, formId, aoRegistrada, aoRegistroRecusado, aoRegistrando, atualizando }: FormularioOdometroProps) {
  const registro = useRegistrarVistoriaEntrada(idLocacao);
  const aviso = useAviso();
  const enviando = useRef(false);
  const { register, handleSubmit, setError, control, formState: { errors, isSubmitting } } = useForm<CamposVistoriaEntrada>({
    resolver: zodResolver(esquemaVistoriaEntrada),
    defaultValues: { kmRegistrado: '', nivelCombustivel: '', observacoes: '' },
  });
  const kmTexto = useWatch({ control, name: 'kmRegistrado' });
  const nivelTexto = useWatch({ control, name: 'nivelCombustivel' });
  const km = kmDigitado(kmTexto);
  const diferenca = km === null ? null : kmAcimaDaRetirada(km, kmSaida);
  const nivel = nivelDigitado(nivelTexto);

  async function enviar(campos: CamposVistoriaEntrada): Promise<void> {
    if (enviando.current || atualizando) return;
    const kmCampo = kmDigitado(campos.kmRegistrado);
    if (kmCampo !== null && kmCampo < kmSaida) {
      setError('kmRegistrado', { type: 'validate', message: MENSAGENS_DEVOLUCAO.km }, { shouldFocus: true });
      return;
    }
    enviando.current = true;
    aoRegistrando(true);
    try {
      const vistoria = await registro.mutateAsync(converterVistoriaEntrada(campos));
      aviso.sucesso(M.sucesso);
      await aoRegistrada(vistoria.idVistoria);
    } catch (erro: unknown) {
      aplicarErrosDeCampo(erro, ['kmRegistrado', 'nivelCombustivel', 'observacoes'] as const,
        (campo, mensagem) => setError(campo, { type: 'server', message: mensagem }, { shouldFocus: true }));
      aviso.erro(obterMensagemErro(erro));
      if (erro instanceof ErroApi && erro.status === 409) await aoRegistroRecusado();
    } finally {
      enviando.current = false;
      aoRegistrando(false);
    }
  }

  // O callback lê a ref somente no evento de envio; handleSubmit não o executa ao renderizar.
  // eslint-disable-next-line react/refs -- guarda síncrona contra envio duplo no evento.
  const aoEnviar = handleSubmit(enviar, () => aviso.erro(M.dadosInvalidos));
  const ocupado = atualizando || registro.isPending || isSubmitting;

  return (
    <form id={formId} noValidate onSubmit={aoEnviar} aria-busy={ocupado} className="flex flex-col gap-4">
      <fieldset disabled={ocupado} className="flex flex-col gap-4">
        <legend className="sr-only">{MENSAGEM_VISTORIA_DADOS_LEGENDA}</legend>
        <div className="flex flex-col gap-1">
          <Campo
            rotulo={MENSAGEM_VISTORIA_ODOMETRO_ROTULO}
            obrigatorio
            required
            inputMode="numeric"
            iconeInicial="pin"
            erro={errors.kmRegistrado?.message}
            {...register('kmRegistrado')}
          />
          <span className="flex items-center gap-1 text-apoio text-texto-apoio">
            {M.kmSaida}: {formatarQuilometragem(kmSaida)}
          </span>
          {diferenca !== null && (
            <span className="flex items-center gap-1 text-apoio font-medium text-vistoria-conferido">
              <Icone nome="check_circle" tamanho={14} />
              {mensagemKmAcimaDaRetirada(formatarNumero(diferenca), formatarQuilometragem(kmSaida))}
            </span>
          )}
        </div>
        <div className="flex flex-col gap-2">
          <Campo
            rotulo={MENSAGEM_VISTORIA_COMBUSTIVEL_CAMPO}
            obrigatorio
            required
            inputMode="decimal"
            dica={M.combustivelDica}
            erro={errors.nivelCombustivel?.message}
            {...register('nivelCombustivel')}
          />
          {nivel !== null && <MedidorVistoria nivel={nivel} />}
        </div>
        <div className="space-y-2">
          <label htmlFor="vistoria-observacoes" className="block text-corpo-denso font-semibold text-texto">{MENSAGEM_VISTORIA_OBSERVACOES_ROTULO}</label>
          <textarea
            id="vistoria-observacoes"
            rows={3}
            {...register('observacoes')}
            aria-invalid={errors.observacoes ? true : undefined}
            aria-describedby={errors.observacoes ? 'vistoria-observacoes-erro' : undefined}
            className="w-full rounded-lg border border-borda bg-branco p-3 text-texto focus-visible:outline focus-visible:outline-2 focus-visible:outline-navy"
          />
          {errors.observacoes && <p id="vistoria-observacoes-erro" role="alert" className="text-apoio text-status-perigo">{errors.observacoes.message}</p>}
        </div>
        {registro.isError && <p role="alert" className="text-apoio text-status-perigo">{obterMensagemErro(registro.error)}</p>}
      </fieldset>
    </form>
  );
}

export interface LeituraOdometroProps {
  vistoria: VistoriaEntrada;
  kmSaida: number;
}

/** Vistoria registrada: leitura dos dados gravados, com o medidor só visual (o valor gravado é o exato). */
export function LeituraOdometro({ vistoria, kmSaida }: LeituraOdometroProps) {
  return (
    <div className="flex flex-col gap-4">
      <output className="flex flex-col gap-2">
        <Etiqueta rotulo={MENSAGEM_VISTORIA_STATUS_REGISTRADA} tom="sucesso" />
        <span className="block text-corpo-denso text-texto-desenho">{M.registrada}</span>
      </output>
      <dl className="grid gap-4 text-apoio sm:grid-cols-2">
        <div><dt className="text-texto-apoio">{MENSAGEM_VISTORIA_REGISTRADA_EM}</dt><dd className="text-corpo-denso text-texto-desenho">{vistoria.dataHoraStr}</dd></div>
        {vistoria.operadorStr ? (
          <div><dt className="text-texto-apoio">{MENSAGEM_VISTORIA_FUNCIONARIO}</dt><dd className="text-corpo-denso text-texto-desenho">{vistoria.operadorStr}</dd></div>
        ) : null}
        <div><dt className="text-texto-apoio">{MENSAGEM_VISTORIA_KM_REGISTRADO}</dt><dd className="text-corpo-denso text-texto-desenho">{formatarQuilometragem(vistoria.kmRegistrado)}</dd></div>
        {/* Com nível gravado, o medidor abaixo mostra o valor (sem a linha crua com o número). Sem nível, o medidor não existe: o texto aprovado "Não informado" fica no lugar. */}
        {vistoria.nivelCombustivel === null ? (
          <div><dt className="text-texto-apoio">{MENSAGEM_VISTORIA_COMBUSTIVEL_ROTULO}</dt><dd className="text-corpo-denso text-texto-desenho">{M.semCombustivel}</dd></div>
        ) : null}
        <div className="sm:col-span-2"><dt className="text-texto-apoio">{MENSAGEM_VISTORIA_OBSERVACOES}</dt><dd className="whitespace-pre-wrap break-words text-corpo-denso text-texto-desenho">{vistoria.observacoes || M.semObservacoes}</dd></div>
      </dl>
      {vistoria.nivelCombustivel !== null && <MedidorVistoria nivel={vistoria.nivelCombustivel} />}
      <span className="text-apoio text-texto-apoio">{M.kmSaida}: {formatarQuilometragem(kmSaida)}</span>
    </div>
  );
}
