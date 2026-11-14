import { Link, Navigate } from 'react-router-dom';
import { BotaoLink, Icone } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import {
  MENSAGEM_CADASTRO_LINK_LOGIN,
  MENSAGEM_LOCADORA_AVISO_SEM_LOGIN,
  MENSAGEM_LOCADORA_BOTAO_PAINEL,
  MENSAGEM_LOCADORA_BOTAO_PRIMEIRO_VEICULO,
  MENSAGEM_LOCADORA_FAIXA_ETAPA_3,
  MENSAGEM_LOCADORA_FAIXA_SEGURANCA_CONFIRMACAO,
  MENSAGEM_LOCADORA_RESUMO_ACESSO,
  MENSAGEM_LOCADORA_RESUMO_ATIVO,
  MENSAGEM_LOCADORA_RESUMO_CIDADE_UF,
  MENSAGEM_LOCADORA_RESUMO_EMPRESA,
  MENSAGEM_LOCADORA_RESUMO_RESPONSAVEL,
  MENSAGEM_LOCADORA_RESUMO_TITULO,
  MENSAGEM_LOCADORA_ROTULO_CNPJ,
  MENSAGEM_LOCADORA_ROTULO_EMAIL_ACESSO,
  MENSAGEM_LOCADORA_ROTULO_NOME_FANTASIA,
  MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_COM_SESSAO,
  MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_INICIO,
  MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_SEM_SESSAO,
  MENSAGEM_LOCADORA_TITULO_CONFIRMACAO,
  MENSAGEM_LOCADORA_TITULO_PAGINA_CONFIRMACAO,
} from '@/lib/mensagens';
import { formatarCnpj } from '@/lib/formatters';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';
import { useCadastroLocadora } from '@/features/locadoras/cadastro/ContextoCadastroLocadora';
import { IndicadorEtapasDesenhoLocadora } from '@/features/locadoras/cadastro/IndicadorEtapasDesenhoLocadora';

const FOCO_LINK =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

interface ItemResumoProps {
  rotulo: string;
  valor: string;
}

/** Um dado do resumo: rótulo pequeno e valor em destaque (lista de definição, para leitores de tela). */
function ItemResumo({ rotulo, valor }: ItemResumoProps) {
  return (
    <div>
      <dt className="text-legenda text-status-neutro">{rotulo}</dt>
      <dd className="break-words text-corpo-denso font-semibold text-navy">{valor}</dd>
    </div>
  );
}

/**
 * P10-FE-02 · Cadastro da locadora, confirmação: etapa 3 de 3 (RF-55, desenho 38). Mostra só o que a conta
 * criada devolveu (o 201), inclusive o e-mail de acesso do responsável; o digitado é reserva. Nenhum dado é inventado. Com sessão, leva ao painel; sem sessão
 * (o login automático falhou), oferece "Fazer login". Recarregar a página perde o resumo, como no cliente.
 */
export default function CadastroLocadoraConfirmacaoPagina() {
  useTituloDocumento(MENSAGEM_LOCADORA_TITULO_PAGINA_CONFIRMACAO);
  const { autenticado } = useAutenticacao();
  const { resultado } = useCadastroLocadora();

  // Sem a conta criada em memória, a guarda da rota já manda para a etapa certa; esta checagem cobre o caso
  // de a tela ser montada fora dela.
  if (resultado === null) {
    return <Navigate to={CAMINHOS.cadastroLocadoraEmpresa} replace />;
  }

  const { empresa, emailAcesso, loginAutomatico } = resultado;
  const comSessao = autenticado;

  return (
    <div className="mx-auto w-full max-w-3xl px-6 py-8">
      <section className="overflow-hidden rounded-lg bg-branco shadow-destaque">
        <IndicadorEtapasDesenhoLocadora etapaAtual={3} />

        <div className="flex flex-col items-center px-6 py-10 text-center md:px-10">
          <span
            aria-hidden="true"
            className="mb-4 flex h-20 w-20 items-center justify-center rounded-full bg-status-sucesso-fundo text-status-sucesso"
          >
            <Icone nome="check_circle" tamanho={56} preenchido />
          </span>

          <h1 className="text-titulo tracking-tight text-navy">{MENSAGEM_LOCADORA_TITULO_CONFIRMACAO}</h1>
          <p className="mt-2 max-w-lg text-corpo text-status-neutro">
            {MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_INICIO}{' '}
            <strong className="font-semibold text-navy">{empresa.nomeFantasia}</strong>{' '}
            {comSessao ? MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_COM_SESSAO : MENSAGEM_LOCADORA_TEXTO_CONFIRMACAO_SEM_SESSAO}
          </p>

          <div className="mt-6 w-full rounded-lg bg-steel-fundo p-6 text-left">
            <div className="mb-6 flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <Icone nome="fact_check" tamanho={20} className="text-navy" />
                <h2 className="text-corpo-denso font-semibold uppercase tracking-wider text-navy">
                  {MENSAGEM_LOCADORA_RESUMO_TITULO}
                </h2>
              </div>
              {empresa.ativo && (
                <span className="inline-flex items-center gap-1 rounded bg-status-sucesso-fundo px-2 py-0.5 text-legenda font-semibold text-status-sucesso">
                  <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-status-sucesso" />
                  {MENSAGEM_LOCADORA_RESUMO_ATIVO}
                </span>
              )}
            </div>

            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
              <div className="space-y-3">
                <h3 className="text-legenda font-semibold uppercase tracking-wider text-status-neutro">
                  {MENSAGEM_LOCADORA_RESUMO_EMPRESA}
                </h3>
                <dl className="space-y-3">
                  <ItemResumo rotulo={MENSAGEM_LOCADORA_ROTULO_NOME_FANTASIA} valor={empresa.nomeFantasia} />
                  <ItemResumo rotulo={MENSAGEM_LOCADORA_ROTULO_CNPJ} valor={formatarCnpj(empresa.cnpj)} />
                  <ItemResumo rotulo={MENSAGEM_LOCADORA_RESUMO_CIDADE_UF} valor={`${empresa.cidade} - ${empresa.uf}`} />
                </dl>
              </div>

              <div className="space-y-3 sm:pl-4">
                <h3 className="text-legenda font-semibold uppercase tracking-wider text-status-neutro">
                  {MENSAGEM_LOCADORA_RESUMO_ACESSO}
                </h3>
                <dl className="space-y-3">
                  <ItemResumo rotulo={MENSAGEM_LOCADORA_RESUMO_RESPONSAVEL} valor={empresa.responsavelNome} />
                  <ItemResumo rotulo={MENSAGEM_LOCADORA_ROTULO_EMAIL_ACESSO} valor={empresa.responsavelEmail ?? emailAcesso} />
                </dl>
              </div>
            </div>
          </div>

          {!loginAutomatico && (
            <output className="mt-6 flex w-full items-start gap-2 rounded-md bg-status-pendente-fundo p-3 text-left text-corpo-denso font-medium text-status-pendente">
              <Icone nome="info" tamanho={18} className="mt-0.5" />
              {MENSAGEM_LOCADORA_AVISO_SEM_LOGIN}
            </output>
          )}

          <div className="mt-8 flex w-full flex-col items-center justify-center gap-4 sm:flex-row">
            {comSessao ? (
              <BotaoLink to={CAMINHOS.backOffice} icone="dashboard" className="w-full sm:w-auto">
                {MENSAGEM_LOCADORA_BOTAO_PAINEL}
              </BotaoLink>
            ) : (
              <BotaoLink to={CAMINHOS.entrar} icone="login" className="w-full sm:w-auto">
                {MENSAGEM_CADASTRO_LINK_LOGIN}
              </BotaoLink>
            )}
            <Link
              to={CAMINHOS.frotaNovoVeiculo}
              className={`flex h-10 w-full items-center justify-center gap-2 rounded-lg bg-steel-alto px-6 text-corpo-denso font-semibold text-navy transition-colors hover:bg-steel-linha sm:w-auto ${FOCO_LINK}`}
            >
              <Icone nome="add_circle" tamanho={18} />
              {MENSAGEM_LOCADORA_BOTAO_PRIMEIRO_VEICULO}
            </Link>
          </div>
        </div>

        <div className="flex flex-col items-center justify-between gap-1 bg-steel-fundo px-6 py-2 text-status-neutro sm:flex-row md:px-8">
          <span className="flex items-center gap-1.5 text-corpo">
            <Icone nome="lock" tamanho={16} className="text-status-pendente" />
            {MENSAGEM_LOCADORA_FAIXA_SEGURANCA_CONFIRMACAO}
          </span>
          <span className="font-mono text-micro font-normal tracking-normal">{MENSAGEM_LOCADORA_FAIXA_ETAPA_3}</span>
        </div>
      </section>
    </div>
  );
}
