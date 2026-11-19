import type { ReactNode } from 'react';
import { Etiqueta } from '@/components/ui';
import { formatarCpf, formatarTelefone, VALOR_AUSENTE } from '@/lib/formatters';
import {
  MENSAGEM_VERIFICACAO_ENDERECO_VAZIO,
  MENSAGEM_VERIFICACAO_ROTULO_CNH_CATEGORIA,
  MENSAGEM_VERIFICACAO_ROTULO_CNH_NUMERO,
  MENSAGEM_VERIFICACAO_ROTULO_CNH_VALIDADE,
  MENSAGEM_VERIFICACAO_ROTULO_CPF,
  MENSAGEM_VERIFICACAO_ROTULO_EMAIL,
  MENSAGEM_VERIFICACAO_ROTULO_MOTIVO_RECUSA,
  MENSAGEM_VERIFICACAO_ROTULO_NASCIMENTO,
  MENSAGEM_VERIFICACAO_ROTULO_TELEFONE,
  MENSAGEM_VERIFICACAO_SECAO_CNH,
  MENSAGEM_VERIFICACAO_SECAO_DADOS,
  MENSAGEM_VERIFICACAO_SECAO_ENDERECO,
  MENSAGEM_VERIFICACAO_SECAO_PEDIDO,
  mensagemVerificacaoPedidoEm,
  mensagemVerificacaoUltimaDecisao,
} from '@/lib/mensagens';
import type { ClienteVerificacao } from '@/types/verificacaoCadastros';
import { dataExibicao, dataHoraExibicao, linhasEndereco } from './regras';

export interface DadosVerificacaoProps {
  cliente: ClienteVerificacao;
}

/**
 * Dados do cadastro no painel (tela 18): dados pessoais (CPF, nascimento, e-mail e telefone, decisão 9), CNH informada
 * (número, categoria e validade), endereço e o pedido nesta locadora (status, data do pedido, última decisão e motivo da
 * recusa quando houver).
 */
export function DadosVerificacao({ cliente }: DadosVerificacaoProps) {
  const linhas = linhasEndereco(cliente);
  return (
    <div className="flex flex-col gap-4">
      <BlocoDados titulo={MENSAGEM_VERIFICACAO_SECAO_DADOS}>
        <CampoDado rotulo={MENSAGEM_VERIFICACAO_ROTULO_CPF} valor={formatarCpf(cliente.cpf)} />
        <CampoDado rotulo={MENSAGEM_VERIFICACAO_ROTULO_NASCIMENTO} valor={dataExibicao(cliente.dataNascimento)} />
        <CampoDado rotulo={MENSAGEM_VERIFICACAO_ROTULO_EMAIL} valor={cliente.email || VALOR_AUSENTE} inteiro />
        <CampoDado rotulo={MENSAGEM_VERIFICACAO_ROTULO_TELEFONE} valor={formatarTelefone(cliente.telefone)} />
      </BlocoDados>

      <BlocoDados titulo={MENSAGEM_VERIFICACAO_SECAO_CNH} colunas={3}>
        <CampoDado rotulo={MENSAGEM_VERIFICACAO_ROTULO_CNH_NUMERO} valor={cliente.cnhNumero || VALOR_AUSENTE} />
        <CampoDado rotulo={MENSAGEM_VERIFICACAO_ROTULO_CNH_CATEGORIA} valor={cliente.cnhCategoria || VALOR_AUSENTE} />
        <CampoDado rotulo={MENSAGEM_VERIFICACAO_ROTULO_CNH_VALIDADE} valor={dataExibicao(cliente.cnhValidade)} />
      </BlocoDados>

      <BlocoDados titulo={MENSAGEM_VERIFICACAO_SECAO_ENDERECO}>
        {linhas.length === 0 ? (
          <p className="col-span-full text-corpo-denso text-texto-apoio">{MENSAGEM_VERIFICACAO_ENDERECO_VAZIO}</p>
        ) : (
          <div className="col-span-full text-corpo-denso text-texto-desenho">
            {linhas.map((linha) => (
              <p key={linha}>{linha}</p>
            ))}
          </div>
        )}
      </BlocoDados>

      <BlocoDados titulo={MENSAGEM_VERIFICACAO_SECAO_PEDIDO}>
        <div className="col-span-full flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <Etiqueta status={cliente.status} />
            <span className="text-corpo-denso text-texto-apoio">
              {mensagemVerificacaoPedidoEm(dataHoraExibicao(cliente.dataSolicitacao))}
            </span>
          </div>
          {cliente.dataVerificacao !== null && (
            <p className="text-corpo-denso text-texto-apoio">
              {mensagemVerificacaoUltimaDecisao(dataHoraExibicao(cliente.dataVerificacao))}
            </p>
          )}
          {cliente.status === 'RECUSADO' && cliente.justificativaRecusa && (
            <p className="text-corpo-denso text-texto-desenho">
              <span className="font-semibold">{MENSAGEM_VERIFICACAO_ROTULO_MOTIVO_RECUSA}: </span>
              {cliente.justificativaRecusa}
            </p>
          )}
        </div>
      </BlocoDados>
    </div>
  );
}

interface BlocoDadosProps {
  titulo: string;
  /** Colunas do bloco a partir de `sm`. Padrão: 2. */
  colunas?: 2 | 3;
  children: ReactNode;
}

/** Bloco de um assunto do painel: título em caixa alta e os campos em grade. */
function BlocoDados({ titulo, colunas = 2, children }: BlocoDadosProps) {
  const grade = colunas === 3 ? 'sm:grid-cols-3' : 'sm:grid-cols-2';
  return (
    <section className="flex flex-col gap-2 rounded bg-steel-fundo p-3">
      <h3 className="text-legenda font-semibold uppercase tracking-wide text-contorno">{titulo}</h3>
      <div className={`grid grid-cols-1 gap-x-4 gap-y-2 ${grade}`}>{children}</div>
    </section>
  );
}

interface CampoDadoProps {
  rotulo: string;
  valor: string;
  /** Ocupa a linha inteira (ex.: e-mail longo). */
  inteiro?: boolean;
}

/** Rótulo em caixa alta e valor abaixo, como no desenho. */
function CampoDado({ rotulo, valor, inteiro = false }: CampoDadoProps) {
  return (
    <div className={`flex min-w-0 flex-col ${inteiro ? 'col-span-full' : ''}`}>
      <span className="text-legenda font-semibold uppercase tracking-wide text-contorno">{rotulo}</span>
      <span className="truncate text-corpo-denso text-texto-desenho">{valor}</span>
    </div>
  );
}
