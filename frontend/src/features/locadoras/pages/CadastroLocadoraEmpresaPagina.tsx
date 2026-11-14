import { useCallback, useEffect, useRef, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { Botao, Icone } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import {
  MENSAGEM_CADASTRO_ABERTURA,
  MENSAGEM_CADASTRO_FAIXA_SENHA,
  MENSAGEM_CADASTRO_LINK_LOGIN,
  MENSAGEM_CADASTRO_TRILHA_ROTULO,
  MENSAGEM_CADASTRO_VOLTAR_INICIO,
  MENSAGEM_LOCADORA_BOTAO_CONTINUAR,
  MENSAGEM_LOCADORA_ETIQUETA_PESSOA_JURIDICA,
  MENSAGEM_LOCADORA_FAIXA_ETAPA_1,
  MENSAGEM_LOCADORA_LINK_JA_POSSUI_CADASTRO,
  MENSAGEM_LOCADORA_PILAR_ETAPAS_TEXTO,
  MENSAGEM_LOCADORA_PILAR_ETAPAS_TITULO,
  MENSAGEM_LOCADORA_PILAR_PAINEL_TEXTO,
  MENSAGEM_LOCADORA_PILAR_PAINEL_TITULO,
  MENSAGEM_LOCADORA_PILAR_RESERVAS_TEXTO,
  MENSAGEM_LOCADORA_PILAR_RESERVAS_TITULO,
  MENSAGEM_LOCADORA_SUBTITULO_EMPRESA,
  MENSAGEM_LOCADORA_TITULO_EMPRESA,
  MENSAGEM_LOCADORA_TITULO_PAGINA_EMPRESA,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';
import { CamposEnderecoSede, CamposIdentificacaoEmpresa } from '@/features/locadoras/cadastro/CamposEmpresa';
import { IndicadorEtapasLocadora } from '@/features/locadoras/cadastro/IndicadorEtapasLocadora';
import { useCadastroLocadora } from '@/features/locadoras/cadastro/ContextoCadastroLocadora';
import {
  CAMPOS_EMPRESA,
  calcularCnpjValido,
  DADOS_EMPRESA_INICIAL,
  ESQUEMA_EMPRESA,
  type FormularioEmpresa,
} from '@/features/locadoras/cadastro/esquemaLocadora';
import {
  CAMPO_API_POR_CAMPO_LOCADORA,
  camposBloqueadosAoAvancar,
  cnpjConferidoPelaVerificacao,
  ehCampoVerificavelLocadora,
  estadoVigenteLocadora,
  mensagemDeDuplicidadeLocadora,
  normalizarValorVerificacaoLocadora,
  precisaConsultarLocadora,
  TIPO_ERRO_DUPLICADO_LOCADORA,
  valorVerificavelLocadora,
  type CampoVerificavelLocadora,
  type RegistroVerificacaoLocadora,
  type ResultadoVerificacaoLocadora,
} from '@/features/locadoras/cadastro/verificacaoLocadora';
import { useVerificarLocadora } from '@/features/locadoras/hooks/useVerificarLocadora';
import {
  camposEmConflitoLocadora,
  errosDaEtapaLocadora,
} from '@/features/locadoras/cadastro/mapeamentoErroLocadora';

interface PilarLocadora {
  icone: string;
  titulo: string;
  texto: string;
  /** Classes do quadrado do ícone (fundo e cor, só tokens). */
  classesIcone: string;
}

/** Cartões de apoio do desenho 36, com o texto ajustado ao que o sistema faz (ver relatório). */
const PILARES: readonly PilarLocadora[] = [
  {
    icone: 'checklist',
    titulo: MENSAGEM_LOCADORA_PILAR_ETAPAS_TITULO,
    texto: MENSAGEM_LOCADORA_PILAR_ETAPAS_TEXTO,
    classesIcone: 'bg-steel-suave text-navy',
  },
  {
    icone: 'dashboard',
    titulo: MENSAGEM_LOCADORA_PILAR_PAINEL_TITULO,
    texto: MENSAGEM_LOCADORA_PILAR_PAINEL_TEXTO,
    classesIcone: 'bg-status-pendente-fundo text-status-pendente',
  },
  {
    icone: 'schedule',
    titulo: MENSAGEM_LOCADORA_PILAR_RESERVAS_TITULO,
    texto: MENSAGEM_LOCADORA_PILAR_RESERVAS_TEXTO,
    classesIcone: 'bg-steel-linha text-navy-escuro',
  },
];

/** O que a tela lembra de cada verificação (CNPJ e e-mail): para qual valor vale e em que ponto está. */
type RegistrosVerificacaoEmpresa = Partial<Record<CampoVerificavelLocadora, RegistroVerificacaoLocadora>>;

const FOCO_LINK =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

/**
 * P10-FE-01 · Cadastro da locadora, etapa 1: dados da empresa (RF-53, desenho 36). Só valida e guarda os dados
 * em memória (provedor das etapas). A única chamada à API é a verificação de CNPJ e e-mail já cadastrados (ao
 * sair do campo e ao continuar), que nunca bloqueia por falha: só um `existe` impede de avançar.
 * A senha não é desta etapa.
 */
export default function CadastroLocadoraEmpresaPagina() {
  useTituloDocumento(MENSAGEM_LOCADORA_TITULO_PAGINA_EMPRESA);
  const navigate = useNavigate();
  const { autenticado } = useAutenticacao();
  const { verificar } = useVerificarLocadora();
  const { dadosEmpresa, salvarDadosEmpresa, errosServidor, removerErrosServidor, conflitos } = useCadastroLocadora();

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    control,
    subscribe,
    getFieldState,
    getValues,
    clearErrors,
    formState: { errors },
  } = useForm<FormularioEmpresa>({
    // Dados já digitados (ao voltar de uma etapa seguinte) vencem; senão, o formulário vazio.
    defaultValues: dadosEmpresa ?? DADOS_EMPRESA_INICIAL,
    resolver: zodResolver(ESQUEMA_EMPRESA),
  });

  // Editar um campo com duplicidade apontada pela verificação limpa o erro daquele campo (a validação normal segue).
  useEffect(() => {
    return subscribe({
      formState: { values: true },
      callback: ({ name, type }) => {
        if (type !== 'change' || name === undefined || !ehCampoVerificavelLocadora(name)) return;
        const tipoErro = getFieldState(name).error?.type;
        if (tipoErro === TIPO_ERRO_DUPLICADO_LOCADORA || tipoErro === 'server') clearErrors(name);
      },
    });
  }, [subscribe, getFieldState, clearErrors]);

  // Erros que o envio da etapa 2 devolveu para esta tela (CNPJ, e-mail da empresa ou dados da sede). Aplicados
  // uma vez, no campo certo, com o foco no primeiro.
  useEffect(() => {
    const erros = errosDaEtapaLocadora(1, errosServidor);
    if (erros.length === 0) return;
    for (const erro of erros) {
      const campo = CAMPOS_EMPRESA.find((nome) => nome === erro.campo);
      if (campo !== undefined) setError(campo, { type: 'server', message: erro.mensagem });
    }
    const primeiro = CAMPOS_EMPRESA.find((campo) => erros.some((erro) => erro.campo === campo));
    if (primeiro !== undefined) setFocus(primeiro);
    removerErrosServidor(1);
  }, [errosServidor, removerErrosServidor, setError, setFocus]);

  // Verificação de CNPJ e e-mail já cadastrados. O registro diz para qual valor (normalizado) a resposta
  // vale: resposta de valor antigo é descartada. A ref guarda o mesmo dado de forma síncrona (o estado só
  // serve para redesenhar o ícone de "conferido").
  const [registros, setRegistros] = useState<RegistrosVerificacaoEmpresa>({});
  const registrosRef = useRef<RegistrosVerificacaoEmpresa>({});
  const emAndamento = useRef<Partial<Record<CampoVerificavelLocadora, Promise<ResultadoVerificacaoLocadora>>>>({});
  const montada = useRef(false);
  const [verificandoAvanco, setVerificandoAvanco] = useState(false);
  const avancoEmCurso = useRef(false);

  useEffect(() => {
    montada.current = true;
    return () => {
      montada.current = false;
    };
  }, []);

  const guardarRegistro = useCallback((campo: CampoVerificavelLocadora, registro: RegistroVerificacaoLocadora) => {
    registrosRef.current = { ...registrosRef.current, [campo]: registro };
    setRegistros(registrosRef.current);
  }, []);

  /** Mostra o erro de duplicidade no campo, se o valor digitado ainda é o que foi verificado. */
  const marcarDuplicado = useCallback(
    (campo: CampoVerificavelLocadora, valor: string) => {
      if (normalizarValorVerificacaoLocadora(campo, getValues(campo)) !== valor) return;
      setError(campo, { type: TIPO_ERRO_DUPLICADO_LOCADORA, message: mensagemDeDuplicidadeLocadora(campo) });
    },
    [getValues, setError],
  );

  /**
   * Verifica o valor do campo se ele passa no esquema do próprio campo e se ainda não foi verificado: o mesmo
   * valor não se repete, e uma verificação em andamento do mesmo valor é aproveitada. Devolve o resultado (ou
   * null quando o valor não é verificável) e nunca lança.
   */
  const verificarCampo = useCallback(
    async (
      campo: CampoVerificavelLocadora,
      valorDigitado: string,
      tentarNovamenteIndisponivel = false,
    ): Promise<ResultadoVerificacaoLocadora | null> => {
      const valor = valorVerificavelLocadora(campo, valorDigitado);
      if (valor === null) return null;

      const registro = registrosRef.current[campo];
      if (!precisaConsultarLocadora(registro, valor, tentarNovamenteIndisponivel)) {
        if (registro?.estado === 'verificando') return emAndamento.current[campo] ?? null;
        if (registro?.estado === 'existe') marcarDuplicado(campo, valor);
        return registro?.estado ?? null;
      }

      guardarRegistro(campo, { valor, estado: 'verificando' });
      const promessa = verificar(CAMPO_API_POR_CAMPO_LOCADORA[campo], valor).then((resultado) => {
        if (registrosRef.current[campo]?.valor === valor) {
          guardarRegistro(campo, { valor, estado: resultado });
          if (resultado === 'existe') marcarDuplicado(campo, valor);
        }
        return resultado;
      });
      emAndamento.current[campo] = promessa;
      return promessa;
    },
    [verificar, guardarRegistro, marcarDuplicado],
  );

  const aoSairCnpj = useCallback(() => {
    void verificarCampo('cnpj', getValues('cnpj'));
  }, [verificarCampo, getValues]);

  const aoSairEmail = useCallback(() => {
    void verificarCampo('email', getValues('email'));
  }, [verificarCampo, getValues]);

  const valores = useWatch({ control });
  const cnpjDigitado = valores.cnpj ?? '';
  const cnpjConferido = cnpjConferidoPelaVerificacao(
    calcularCnpjValido(cnpjDigitado),
    estadoVigenteLocadora(registros.cnpj, 'cnpj', cnpjDigitado),
  );

  /**
   * Envio já validado pelo esquema. Antes de abrir a próxima etapa, verifica na hora o CNPJ e o e-mail que
   * ainda não foram verificados (em paralelo, aproveitando a chamada em andamento). Só `existe` do valor atual
   * impede de avançar (erro no campo, foco no primeiro); falha da verificação segue em frente.
   */
  async function continuar(dados: FormularioEmpresa) {
    if (avancoEmCurso.current) return;

    // CNPJ ou e-mail que o servidor já recusou no envio (409) continuam bloqueados até o valor mudar.
    const emConflito = camposEmConflitoLocadora(dados, conflitos);
    if (emConflito.length > 0) {
      for (const campo of emConflito) {
        setError(campo, { type: TIPO_ERRO_DUPLICADO_LOCADORA, message: mensagemDeDuplicidadeLocadora(campo) });
      }
      setFocus(emConflito[0]);
      return;
    }
    avancoEmCurso.current = true;
    setVerificandoAvanco(true);
    try {
      const [resultadoCnpj, resultadoEmail] = await Promise.all([
        verificarCampo('cnpj', dados.cnpj, true),
        verificarCampo('email', dados.email, true),
      ]);
      if (!montada.current) return;

      // A pessoa editou algum campo enquanto esperava: não avança com dados que já não são os da tela.
      const atuais = getValues();
      if (CAMPOS_EMPRESA.some((campo) => atuais[campo] !== dados[campo])) return;

      const bloqueados = camposBloqueadosAoAvancar(resultadoCnpj, resultadoEmail);
      if (bloqueados.length > 0) {
        setFocus(bloqueados[0]);
        return;
      }

      salvarDadosEmpresa(dados);
      navigate(CAMINHOS.cadastroLocadoraResponsavel);
    } finally {
      avancoEmCurso.current = false;
      if (montada.current) setVerificandoAvanco(false);
    }
  }

  // Quem já tem sessão não precisa de cadastro.
  if (autenticado) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }

  return (
    <div className="relative w-full overflow-hidden py-8">
      {/* Brilhos de fundo do desenho (decorativos). */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -left-24 -top-32 h-96 w-96 rounded-xl bg-status-pendente/5 blur-3xl"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -right-32 top-1/2 h-[32rem] w-[32rem] rounded-xl bg-navy/5 blur-3xl"
      />

      <div className="relative mx-auto max-w-4xl px-6">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-2">
          <nav aria-label={MENSAGEM_CADASTRO_TRILHA_ROTULO}>
            <ol className="flex flex-wrap items-center gap-1 text-corpo-denso text-status-neutro">
              <li>
                <Link
                  to={CAMINHOS.inicio}
                  className={`flex items-center gap-1 rounded transition-colors hover:text-navy ${FOCO_LINK}`}
                >
                  <Icone nome="arrow_back" tamanho={16} />
                  {MENSAGEM_CADASTRO_VOLTAR_INICIO}
                </Link>
              </li>
              <li aria-hidden="true" className="text-borda-forte">
                /
              </li>
              <li aria-current="page" className="text-navy">
                {MENSAGEM_CADASTRO_ABERTURA}
              </li>
            </ol>
          </nav>
          <span className="inline-flex items-center gap-1 rounded-sm bg-steel-suave px-2 py-1 text-legenda uppercase tracking-wider text-navy">
            <Icone nome="verified_user" tamanho={15} className="text-status-pendente" />
            {MENSAGEM_LOCADORA_ETIQUETA_PESSOA_JURIDICA}
          </span>
        </div>

        <section className="overflow-hidden rounded-lg bg-branco shadow-destaque">
          <IndicadorEtapasLocadora etapaAtual={1} />

          <div className="px-6 pb-6 pt-8 md:px-8 md:pb-8">
            <div className="mb-6">
              <h1 className="text-titulo tracking-tight text-navy">{MENSAGEM_LOCADORA_TITULO_EMPRESA}</h1>
              <p className="mt-1 text-corpo text-status-neutro">{MENSAGEM_LOCADORA_SUBTITULO_EMPRESA}</p>
            </div>

            <form
              noValidate
              onSubmit={(evento) => {
                void handleSubmit(continuar)(evento);
              }}
              className="space-y-6"
            >
              <CamposIdentificacaoEmpresa
                registrar={register}
                erros={errors}
                cnpjConferido={cnpjConferido}
                aoSairCnpj={aoSairCnpj}
                aoSairEmail={aoSairEmail}
              />

              <CamposEnderecoSede registrar={register} erros={errors} />

              <div className="flex flex-col items-center justify-between gap-4 pt-4 sm:flex-row">
                <Link
                  to={CAMINHOS.entrar}
                  className={`order-2 flex items-center gap-1 rounded text-corpo text-status-neutro transition-colors hover:text-navy sm:order-1 ${FOCO_LINK}`}
                >
                  <span>
                    {MENSAGEM_LOCADORA_LINK_JA_POSSUI_CADASTRO}{' '}
                    <strong className="ml-1 text-navy underline decoration-status-pendente">
                      {MENSAGEM_CADASTRO_LINK_LOGIN}
                    </strong>
                  </span>
                </Link>

                <Botao
                  type="submit"
                  tamanho="grande"
                  iconeDireita="arrow_forward"
                  carregando={verificandoAvanco}
                  className="order-1 w-full tracking-wide shadow-lg sm:order-2 sm:w-auto"
                >
                  {MENSAGEM_LOCADORA_BOTAO_CONTINUAR}
                </Botao>
              </div>
            </form>
          </div>

          <div className="flex items-center justify-between bg-steel-fundo px-6 py-2 text-status-neutro md:px-8">
            <span className="flex items-center gap-1 text-corpo">
              <Icone nome="lock" tamanho={16} className="text-status-pendente" />
              {MENSAGEM_CADASTRO_FAIXA_SENHA}
            </span>
            <span className="font-mono text-micro font-normal tracking-normal">{MENSAGEM_LOCADORA_FAIXA_ETAPA_1}</span>
          </div>
        </section>

        <ul className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-3">
          {PILARES.map((pilar) => (
            <li key={pilar.titulo} className="flex items-start gap-3 rounded-lg bg-branco p-4 shadow-sm">
              <span
                aria-hidden="true"
                className={`flex h-10 w-10 shrink-0 items-center justify-center rounded ${pilar.classesIcone}`}
              >
                <Icone nome={pilar.icone} tamanho={22} />
              </span>
              <div className="min-w-0">
                <h2 className="text-corpo-denso font-semibold text-navy">{pilar.titulo}</h2>
                <p className="mt-0.5 text-legenda text-status-neutro">{pilar.texto}</p>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
