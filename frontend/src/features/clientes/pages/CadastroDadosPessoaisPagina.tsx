import { useEffect, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { Botao, Icone } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { CamposDadosPessoais } from '@/features/clientes/cadastro/CamposCadastro';
import { IndicadorEtapasCabecalho } from '@/features/clientes/cadastro/IndicadorEtapas';
import {
  DADOS_PESSOAIS_INICIAL,
  ESQUEMA_DADOS_PESSOAIS,
  type DadosPessoaisCadastro,
} from '@/features/clientes/cadastro/esquemaCadastro';
import {
  calcularCamposValidos,
  ehCampoDaEtapa1,
  errosParaAplicarNaEtapa,
} from '@/features/clientes/cadastro/etapasCadastro';
import { primeiroCampoNaTela } from '@/features/clientes/cadastro/fluxoCadastro';
import {
  lerPreenchimentoCadastro,
  type PreenchimentoCadastro,
} from '@/features/clientes/cadastro/preenchimentoCadastro';
import { useCadastroEtapas } from '@/features/clientes/cadastro/useCadastroEtapas';
import { limparPreenchimentoGoogle, obterPreenchimentoGoogle } from '@/lib/preenchimentoGoogle';
import {
  GOOGLE_CADASTRO_NECESSARIO,
  MENSAGEM_CADASTRO_ABERTURA,
  MENSAGEM_CADASTRO_BOTAO_CONTINUAR,
  MENSAGEM_CADASTRO_DECLARACAO_TITULAR,
  MENSAGEM_CADASTRO_ETIQUETA_PESSOA_FISICA,
  MENSAGEM_CADASTRO_FAIXA_ESCURA_BOTAO,
  MENSAGEM_CADASTRO_FAIXA_ESCURA_TEXTO,
  MENSAGEM_CADASTRO_FAIXA_ESCURA_TITULO,
  MENSAGEM_CADASTRO_FAIXA_ETAPA_1,
  MENSAGEM_CADASTRO_FAIXA_SENHA,
  MENSAGEM_CADASTRO_JA_POSSUI_CADASTRO,
  MENSAGEM_CADASTRO_LINK_LOGIN,
  MENSAGEM_CADASTRO_PILAR_GRATIS_TEXTO,
  MENSAGEM_CADASTRO_PILAR_GRATIS_TITULO,
  MENSAGEM_CADASTRO_PILAR_RESERVA_TEXTO,
  MENSAGEM_CADASTRO_PILAR_RESERVA_TITULO,
  MENSAGEM_CADASTRO_PILAR_VERIFICADO_TEXTO,
  MENSAGEM_CADASTRO_PILAR_VERIFICADO_TITULO,
  MENSAGEM_CADASTRO_SELO,
  MENSAGEM_CADASTRO_SUBTITULO,
  MENSAGEM_CADASTRO_TITULO_CARTAO,
  MENSAGEM_CADASTRO_TITULO_PAGINA,
  MENSAGEM_CADASTRO_TRILHA_ROTULO,
  MENSAGEM_CADASTRO_VERIFICADO_TEXTO,
  MENSAGEM_CADASTRO_VERIFICADO_TITULO,
  MENSAGEM_CADASTRO_VOLTAR_INICIO,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';

interface PilarCadastro {
  icone: string;
  titulo: string;
  texto: string;
  /** Classes do quadrado do ícone (fundo e cor, só tokens). */
  classesIcone: string;
}

const PILARES: readonly PilarCadastro[] = [
  {
    icone: 'event_available',
    titulo: MENSAGEM_CADASTRO_PILAR_RESERVA_TITULO,
    texto: MENSAGEM_CADASTRO_PILAR_RESERVA_TEXTO,
    classesIcone: 'bg-steel-suave text-navy',
  },
  {
    icone: 'money_off',
    titulo: MENSAGEM_CADASTRO_PILAR_GRATIS_TITULO,
    texto: MENSAGEM_CADASTRO_PILAR_GRATIS_TEXTO,
    classesIcone: 'bg-status-pendente-fundo text-status-pendente',
  },
  {
    icone: 'shield_person',
    titulo: MENSAGEM_CADASTRO_PILAR_VERIFICADO_TITULO,
    texto: MENSAGEM_CADASTRO_PILAR_VERIFICADO_TEXTO,
    classesIcone: 'bg-steel-linha text-navy-escuro',
  },
];

const FOCO_LINK =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

/**
 * P2-FE-02 · Cadastro do cliente, etapa 1: dados pessoais (RF-07, design 20).
 * Só valida e guarda os dados em memória (provedor das duas telas): a API não é chamada aqui.
 * O envio de tudo, a CNH e a senha ficam na etapa 2 (`CadastroDocumentosPagina`).
 */
export default function CadastroDadosPessoaisPagina() {
  useTituloDocumento(MENSAGEM_CADASTRO_TITULO_PAGINA);
  const navigate = useNavigate();
  const { autenticado } = useAutenticacao();
  const {
    dadosPessoais,
    veioDoGoogle: veioDoGoogleSalvo,
    errosServidor,
    salvarDadosPessoais,
    removerErrosServidor,
  } = useCadastroEtapas();

  // Lê o pré-preenchimento do Google na montagem, só uma vez. O inicializador do estado roda duas
  // vezes no StrictMode, mas as duas leituras veem o mesmo valor: a limpeza fica no efeito abaixo,
  // depois do mount, e o estado (já capturado) não se perde na remontagem simulada.
  const [preenchimentoGoogle] = useState<PreenchimentoCadastro>(() =>
    lerPreenchimentoCadastro(obterPreenchimentoGoogle()),
  );
  // Ao voltar da etapa 2 o preenchimento já foi consumido: o provedor lembra a origem.
  const veioDoGoogle = veioDoGoogleSalvo || preenchimentoGoogle.email !== '';

  // Consome o pré-preenchimento: a memória é limpa depois que a tela já capturou os valores.
  useEffect(() => {
    limparPreenchimentoGoogle();
  }, []);

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    control,
    subscribe,
    getFieldState,
    clearErrors,
    formState: { errors },
  } = useForm<DadosPessoaisCadastro>({
    // Dados já digitados (ao voltar da etapa 2) vencem; senão, nome e e-mail do Google como valores iniciais.
    defaultValues: dadosPessoais ?? { ...DADOS_PESSOAIS_INICIAL, ...preenchimentoGoogle },
    resolver: zodResolver(ESQUEMA_DADOS_PESSOAIS),
  });

  // Editar um campo com erro vindo da API limpa o erro daquele campo (a validação normal segue valendo).
  useEffect(() => {
    return subscribe({
      formState: { values: true },
      callback: ({ name, type }) => {
        if (type !== 'change' || name === undefined || !ehCampoDaEtapa1(name)) return;
        if (getFieldState(name).error?.type === 'server') clearErrors(name);
      },
    });
  }, [subscribe, getFieldState, clearErrors]);

  // Erros da API que apontam para um campo desta tela (ex.: 409 de CPF ou 400 em um campo da etapa 1):
  // a etapa 2 devolveu a pessoa para cá. Mostra o erro no campo e leva o foco ao primeiro, na ordem da tela.
  useEffect(() => {
    const erros = errosParaAplicarNaEtapa(1, errosServidor);
    if (erros.length === 0) return;
    for (const erro of erros) {
      if (ehCampoDaEtapa1(erro.campo)) setError(erro.campo, { type: 'server', message: erro.mensagem });
    }
    const primeiro = primeiroCampoNaTela(erros.map((erro) => erro.campo));
    if (primeiro !== undefined && ehCampoDaEtapa1(primeiro)) setFocus(primeiro);
    removerErrosServidor(1);
  }, [errosServidor, removerErrosServidor, setError, setFocus]);

  const valores = useWatch({ control });
  const validos = calcularCamposValidos(valores);

  /** Envio já validado pelo Zod: guarda os dados em memória e abre a etapa 2. Não chama a API. */
  function continuar(dados: DadosPessoaisCadastro) {
    salvarDadosPessoais(dados, veioDoGoogle);
    navigate(CAMINHOS.cadastroDocumentos);
  }

  // Quem já tem sessão não precisa de cadastro.
  if (autenticado) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }

  return (
    <div className="relative w-full overflow-hidden py-8">
      {/* Brilhos de fundo do design (decorativos). */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -left-24 -top-32 h-96 w-96 rounded-xl bg-status-pendente/5 blur-3xl"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -right-32 top-1/2 h-[32rem] w-[32rem] rounded-xl bg-navy/5 blur-3xl"
      />

      <div className="relative mx-auto max-w-[1360px] px-6">
        <nav aria-label={MENSAGEM_CADASTRO_TRILHA_ROTULO} className="mb-6">
          <ol className="flex flex-wrap items-center gap-1 text-corpo text-status-neutro">
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
            <li aria-current="page" className="flex items-center text-navy">
              {MENSAGEM_CADASTRO_ABERTURA}
              <span className="ml-2 rounded-sm bg-steel-suave px-2 py-0.5 text-corpo uppercase tracking-wider text-navy-escuro">
                {MENSAGEM_CADASTRO_ETIQUETA_PESSOA_FISICA}
              </span>
            </li>
          </ol>
        </nav>

        <div className="mx-auto max-w-[860px]">
          <section className="overflow-hidden rounded-lg bg-branco shadow-destaque">
            {veioDoGoogle && (
              <output className="flex items-start gap-3 border-b border-steel bg-steel-claro px-8 py-4 text-corpo-denso text-navy">
                <Icone nome="info" tamanho={20} className="mt-0.5 text-steel" />
                <span>{GOOGLE_CADASTRO_NECESSARIO}</span>
              </output>
            )}

            <IndicadorEtapasCabecalho />

            <div className="px-8 pb-4 pt-8">
              <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
                <div className="max-w-xl space-y-1">
                  <div className="inline-flex items-center gap-1 rounded-sm bg-status-pendente-fundo px-2.5 py-1 text-corpo text-status-pendente">
                    <Icone nome="verified" tamanho={15} />
                    {MENSAGEM_CADASTRO_SELO}
                  </div>
                  <h1 className="text-corpo tracking-tight text-navy">{MENSAGEM_CADASTRO_TITULO_CARTAO}</h1>
                  <p className="text-corpo text-status-neutro">{MENSAGEM_CADASTRO_SUBTITULO}</p>
                </div>

                <div className="flex items-start gap-1 self-start rounded bg-steel-fundo p-2 md:max-w-xs">
                  <Icone nome="verified_user" tamanho={20} className="mt-0.5 text-status-pendente" />
                  <div className="flex flex-col">
                    <span className="text-corpo font-semibold text-navy">{MENSAGEM_CADASTRO_VERIFICADO_TITULO}</span>
                    <span className="mt-0.5 text-micro font-normal tracking-normal text-status-neutro">
                      {MENSAGEM_CADASTRO_VERIFICADO_TEXTO}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <form
              noValidate
              onSubmit={(evento) => {
                void handleSubmit(continuar)(evento);
              }}
              className="space-y-6 px-8 pb-8"
            >
              <CamposDadosPessoais registrar={register} erros={errors} validos={validos} />

              <p className="rounded-sm bg-steel-suave p-2 text-legenda leading-relaxed text-status-neutro">
                {MENSAGEM_CADASTRO_DECLARACAO_TITULAR}
              </p>

              <div className="flex flex-col items-center justify-between gap-4 pt-4 sm:flex-row">
                <Link
                  to={CAMINHOS.entrar}
                  className={`order-2 flex items-center gap-1 rounded text-corpo text-status-neutro transition-colors hover:text-navy sm:order-1 ${FOCO_LINK}`}
                >
                  <Icone nome="account_circle" tamanho={18} />
                  <span>
                    {MENSAGEM_CADASTRO_JA_POSSUI_CADASTRO}{' '}
                    <strong className="ml-1 text-navy underline decoration-status-pendente">
                      {MENSAGEM_CADASTRO_LINK_LOGIN}
                    </strong>
                  </span>
                </Link>

                <Botao
                  type="submit"
                  tamanho="grande"
                  iconeDireita="arrow_forward"
                  className="order-1 w-full !rounded !px-8 tracking-wide shadow-lg sm:order-2 sm:w-auto"
                >
                  {MENSAGEM_CADASTRO_BOTAO_CONTINUAR}
                </Botao>
              </div>
            </form>

            <div className="flex items-center justify-between bg-steel-suave px-8 py-2 text-status-neutro">
              <span className="flex items-center gap-1 text-corpo">
                <Icone nome="lock" tamanho={16} className="text-status-pendente" />
                {MENSAGEM_CADASTRO_FAIXA_SENHA}
              </span>
              <span className="hidden font-mono text-micro font-normal tracking-normal md:inline">
                {MENSAGEM_CADASTRO_FAIXA_ETAPA_1}
              </span>
            </div>
          </section>

          <ul className="mt-8 grid grid-cols-1 gap-6 md:grid-cols-3">
            {PILARES.map((pilar) => (
              <li
                key={pilar.titulo}
                className="flex flex-col gap-1 rounded-lg bg-branco p-6 shadow-sm transition-transform hover:-translate-y-0.5 motion-reduce:transition-none motion-reduce:hover:translate-y-0"
              >
                <span
                  aria-hidden="true"
                  className={`mb-1 flex h-10 w-10 items-center justify-center rounded ${pilar.classesIcone}`}
                >
                  <Icone nome={pilar.icone} tamanho={24} />
                </span>
                <h2 className="text-corpo text-navy">{pilar.titulo}</h2>
                <p className="text-corpo text-status-neutro">{pilar.texto}</p>
              </li>
            ))}
          </ul>

          <div className="mt-6 flex flex-col items-center justify-between gap-4 rounded-lg bg-gradient-to-r from-navy to-navy-escuro p-4 text-branco shadow-md sm:flex-row">
            <div className="flex items-center gap-4">
              <span
                aria-hidden="true"
                className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-branco/10"
              >
                <Icone nome="directions_car" tamanho={28} className="text-orange-claro" />
              </span>
              <div>
                <p className="text-corpo text-branco">{MENSAGEM_CADASTRO_FAIXA_ESCURA_TITULO}</p>
                <p className="text-corpo text-steel-neblina">{MENSAGEM_CADASTRO_FAIXA_ESCURA_TEXTO}</p>
              </div>
            </div>
            <Link
              to={CAMINHOS.seminovos}
              className="flex items-center gap-1 whitespace-nowrap rounded-sm bg-branco/15 px-4 py-2 text-corpo text-branco transition-colors hover:bg-branco/25 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-branco"
            >
              {MENSAGEM_CADASTRO_FAIXA_ESCURA_BOTAO}
              <Icone nome="arrow_forward" tamanho={16} />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
