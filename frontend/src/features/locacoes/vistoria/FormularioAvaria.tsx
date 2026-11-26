import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Campo, Select, type OpcaoSelect } from '@/components/ui';
import { ANGULOS_AVARIA } from '@/types/avarias';
import { GRAVIDADE_AVARIA } from '@/types/enums';
import { MENSAGENS_DEVOLUCAO } from '@/lib/mensagensDevolucao';
import {
  MENSAGEM_FORM_AVARIA_ANGULO,
  MENSAGEM_FORM_AVARIA_CANCELAR,
  MENSAGEM_FORM_AVARIA_DESCRICAO,
  MENSAGEM_FORM_AVARIA_DICA_POSICAO,
  MENSAGEM_FORM_AVARIA_ENVIAR,
  MENSAGEM_FORM_AVARIA_FOTO,
  MENSAGEM_FORM_AVARIA_GRAVIDADE,
  MENSAGEM_FORM_AVARIA_POSICAO_X,
  MENSAGEM_FORM_AVARIA_POSICAO_Y,
  MENSAGEM_FORM_AVARIA_SEM_ANGULO,
  MENSAGEM_FORM_AVARIA_TITULO,
  MENSAGEM_FORM_AVARIA_VALOR,
} from '@/lib/mensagensVistoriaAvarias';
import { esquemaAvaria, normalizarAvaria, validarFotoAvaria, type CamposAvaria, type CamposAvariaNormalizados } from './validacaoAvaria';
import { ROTULO_ANGULO } from './avariasVistoria';
import type { MarcacaoDiagrama } from './DiagramaAvarias';

export interface FormularioAvariaProps {
  /** Marcação feita no diagrama: preenche ângulo e posição. Nulo quando o formulário abre pelo botão. */
  marcacao: MarcacaoDiagrama | null;
  aoAdicionar(campos: CamposAvariaNormalizados, foto: File): void;
  aoCancelar(): void;
}

const OPCOES_ANGULO: OpcaoSelect[] = ANGULOS_AVARIA.map((angulo) => ({ valor: angulo, rotulo: ROTULO_ANGULO[angulo] }));
const OPCOES_GRAVIDADE: OpcaoSelect[] = GRAVIDADE_AVARIA.map((gravidade) => ({
  valor: gravidade,
  rotulo: MENSAGENS_DEVOLUCAO.gravidades[gravidade],
}));

const VALORES_INICIAIS: CamposAvaria = { angulo: '', descricao: '', gravidade: '', valorEstimado: '', posX: '', posY: '' };

/** Formata a posição do clique com duas casas, como o servidor lê. Sem posição (teclado), o campo fica vazio. */
function textoPosicao(valor: number | null): string {
  return valor === null ? '' : valor.toFixed(2);
}

/**
 * Formulário "Adicionar Avaria" (sem desenho na tela 19). A avaria fica local até o registro da vistoria: a foto é
 * obrigatória (o servidor a exige no POST) e a posição vem em tudo ou nada com o ângulo.
 */
export function FormularioAvaria({ marcacao, aoAdicionar, aoCancelar }: FormularioAvariaProps) {
  const { register, handleSubmit, setValue, reset, formState: { errors } } = useForm<CamposAvaria>({
    resolver: zodResolver(esquemaAvaria),
    defaultValues: VALORES_INICIAIS,
  });
  const [foto, setFoto] = useState<File | null>(null);
  const [erroFoto, setErroFoto] = useState<string | null>(null);
  const [versaoCampoFoto, setVersaoCampoFoto] = useState(0);

  // A marcação do diagrama preenche ângulo e posição; o efeito só roda quando a marcação muda.
  useEffect(() => {
    if (marcacao === null) return;
    setValue('angulo', marcacao.angulo);
    setValue('posX', textoPosicao(marcacao.posX));
    setValue('posY', textoPosicao(marcacao.posY));
  }, [marcacao, setValue]);

  function enviar(campos: CamposAvaria): void {
    const erro = validarFotoAvaria(foto);
    setErroFoto(erro);
    if (erro !== null || foto === null) return;
    aoAdicionar(normalizarAvaria(campos), foto);
    reset(VALORES_INICIAIS);
    setFoto(null);
    setVersaoCampoFoto((versao) => versao + 1);
  }

  const aoEnviar = handleSubmit(enviar);

  return (
    <section aria-labelledby="titulo-formulario-avaria" className="flex flex-col gap-4 rounded-lg bg-steel-fundo p-4">
      <h3 id="titulo-formulario-avaria" className="text-titulo-md font-bold text-texto-desenho">{MENSAGEM_FORM_AVARIA_TITULO}</h3>
      <form noValidate onSubmit={aoEnviar} className="flex flex-col gap-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <Select
            rotulo={MENSAGEM_FORM_AVARIA_ANGULO}
            opcoes={OPCOES_ANGULO}
            opcaoVazia={MENSAGEM_FORM_AVARIA_SEM_ANGULO}
            erro={errors.angulo?.message}
            {...register('angulo')}
          />
          <Select
            rotulo={MENSAGEM_FORM_AVARIA_GRAVIDADE}
            opcoes={OPCOES_GRAVIDADE}
            obrigatorio
            required
            erro={errors.gravidade?.message}
            {...register('gravidade')}
          />
        </div>
        <div className="space-y-2">
          <label htmlFor="avaria-descricao" className="block text-corpo-denso font-semibold text-texto">
            {MENSAGEM_FORM_AVARIA_DESCRICAO} <span aria-hidden="true">*</span>
          </label>
          <textarea
            id="avaria-descricao"
            rows={3}
            required
            aria-required="true"
            aria-invalid={errors.descricao ? true : undefined}
            aria-describedby={errors.descricao ? 'avaria-descricao-erro' : undefined}
            className="w-full rounded-lg border border-borda bg-branco p-3 text-texto focus-visible:outline focus-visible:outline-2 focus-visible:outline-navy"
            {...register('descricao')}
          />
          {errors.descricao && <p id="avaria-descricao-erro" role="alert" className="text-apoio text-status-perigo">{errors.descricao.message}</p>}
        </div>
        <div className="grid gap-4 sm:grid-cols-3">
          <Campo rotulo={MENSAGEM_FORM_AVARIA_VALOR} inputMode="decimal" erro={errors.valorEstimado?.message} {...register('valorEstimado')} />
          <Campo
            rotulo={MENSAGEM_FORM_AVARIA_POSICAO_X}
            inputMode="decimal"
            dica={MENSAGEM_FORM_AVARIA_DICA_POSICAO}
            erro={errors.posX?.message}
            {...register('posX')}
          />
          <Campo rotulo={MENSAGEM_FORM_AVARIA_POSICAO_Y} inputMode="decimal" erro={errors.posY?.message} {...register('posY')} />
        </div>
        <div className="space-y-2">
          <label htmlFor="avaria-foto" className="block text-corpo-denso font-semibold text-texto">
            {MENSAGEM_FORM_AVARIA_FOTO} <span aria-hidden="true">*</span>
          </label>
          <input
            key={versaoCampoFoto}
            id="avaria-foto"
            type="file"
            accept="image/jpeg,image/png"
            required
            aria-required="true"
            aria-invalid={erroFoto ? true : undefined}
            aria-describedby={erroFoto ? 'avaria-foto-erro' : undefined}
            onChange={(evento) => {
              setFoto(evento.target.files?.[0] ?? null);
              setErroFoto(null);
            }}
            className="block w-full text-apoio text-texto-apoio file:mr-3 file:rounded file:border-0 file:bg-steel-alto file:px-3 file:py-2 file:text-corpo-denso file:font-semibold"
          />
          {erroFoto && <p id="avaria-foto-erro" role="alert" className="text-apoio text-status-perigo">{erroFoto}</p>}
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Botao type="submit">{MENSAGEM_FORM_AVARIA_ENVIAR}</Botao>
          <Botao type="button" variante="contorno" onClick={aoCancelar}>{MENSAGEM_FORM_AVARIA_CANCELAR}</Botao>
        </div>
      </form>
    </section>
  );
}
