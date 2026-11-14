/**
 * Campos da etapa 2 do cadastro da locadora (tela 37): responsável, e-mail de acesso, senha com medidor e
 * requisitos, confirmação e aceite dos termos. As máscaras vêm de `@/lib/formatters`; os textos, de `@/lib/mensagens`.
 *
 * Adaptações ao desenho (decisões na entrega): sem "Cargo na empresa" e sem "Celular" (não existem no contrato);
 * a ajuda do e-mail diz que o acesso é por CPF; os campos usam a variante `portal` (rótulo de 14px semibold, campo
 * branco com sombra, como o desenho); o campo de confirmação não tem o ícone de conferido (o botão de mostrar a
 * senha ocupa a borda direita), e o texto "Senhas coincidem" aparece abaixo dele.
 */
import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Campo, CampoSenha, Icone } from '@/components/ui';
import { formatarCpf } from '@/lib/formatters';
import {
  MENSAGEM_CADASTRO_ROTULO_CONFIRMAR_SENHA,
  MENSAGEM_CADASTRO_ROTULO_SENHA,
  MENSAGEM_LOCADORA_ACEITE_CONJUNCAO,
  MENSAGEM_LOCADORA_ACEITE_PREFIXO,
  MENSAGEM_LOCADORA_ACEITE_PRIVACIDADE,
  MENSAGEM_LOCADORA_ACEITE_SUFIXO,
  MENSAGEM_LOCADORA_ACEITE_TERMOS,
  MENSAGEM_LOCADORA_DICA_EMAIL_ACESSO,
  MENSAGEM_LOCADORA_PLACEHOLDER_CPF,
  MENSAGEM_LOCADORA_PLACEHOLDER_EMAIL_RESPONSAVEL,
  MENSAGEM_LOCADORA_PLACEHOLDER_NOME_RESPONSAVEL,
  MENSAGEM_LOCADORA_REQUISITO_ATENDIDO,
  MENSAGEM_LOCADORA_REQUISITO_MAIUSCULA,
  MENSAGEM_LOCADORA_REQUISITO_NUMERO,
  MENSAGEM_LOCADORA_REQUISITO_PENDENTE,
  MENSAGEM_LOCADORA_REQUISITO_TAMANHO,
  MENSAGEM_LOCADORA_REQUISITOS_TITULO,
  MENSAGEM_LOCADORA_ROTULO_CPF_RESPONSAVEL,
  MENSAGEM_LOCADORA_ROTULO_EMAIL_ACESSO,
  MENSAGEM_LOCADORA_ROTULO_NOME_RESPONSAVEL,
  MENSAGEM_LOCADORA_SENHAS_COINCIDEM,
  mensagemLocadoraNivelSenha,
  mensagemLocadoraNumeroNivelSenha,
} from '@/lib/mensagens';
import type { FormularioResponsavel } from './esquemaResponsavel';
import { avaliarRequisitosSenha, nivelSenha, TOTAL_BARRAS_FORCA_SENHA } from './forcaSenhaLocadora';

export interface CamposResponsavelProps {
  registrar: UseFormRegister<FormularioResponsavel>;
  erros: FieldErrors<FormularioResponsavel>;
}

/** Nome e CPF lado a lado; e-mail de acesso na linha inteira, com a ajuda corrigida (acesso por CPF). */
export function CamposIdentidadeResponsavel({ registrar, erros }: CamposResponsavelProps) {
  return (
    <div className="grid grid-cols-1 gap-x-6 gap-y-4 md:grid-cols-2">
      <Campo
        variante="portal"
        rotulo={MENSAGEM_LOCADORA_ROTULO_NOME_RESPONSAVEL}
        iconeInicial="person"
        placeholder={MENSAGEM_LOCADORA_PLACEHOLDER_NOME_RESPONSAVEL}
        obrigatorio
        autoComplete="name"
        erro={erros.nome?.message}
        {...registrar('nome')}
      />
      <Campo
        variante="portal"
        rotulo={MENSAGEM_LOCADORA_ROTULO_CPF_RESPONSAVEL}
        iconeInicial="badge"
        placeholder={MENSAGEM_LOCADORA_PLACEHOLDER_CPF}
        obrigatorio
        inputMode="numeric"
        autoComplete="off"
        mascara={formatarCpf}
        erro={erros.cpf?.message}
        {...registrar('cpf')}
      />
      <div className="md:col-span-2">
        <Campo
          variante="portal"
          rotulo={MENSAGEM_LOCADORA_ROTULO_EMAIL_ACESSO}
          iconeInicial="mail"
          placeholder={MENSAGEM_LOCADORA_PLACEHOLDER_EMAIL_RESPONSAVEL}
          dica={MENSAGEM_LOCADORA_DICA_EMAIL_ACESSO}
          dicaIcone="info"
          obrigatorio
          type="email"
          autoComplete="email"
          erro={erros.email?.message}
          {...registrar('email')}
        />
      </div>
    </div>
  );
}

export interface BlocoSenhaResponsavelProps extends CamposResponsavelProps {
  /** Senha digitada agora (só para o medidor e os requisitos; nunca é guardada). */
  senha: string;
  /** Confirmação digitada agora (só para o texto "Senhas coincidem"). */
  confirmacao: string;
}

/** Senha e confirmação com o medidor, a lista de requisitos e o texto de coincidência do desenho 37. */
export function BlocoSenhaResponsavel({ registrar, erros, senha, confirmacao }: BlocoSenhaResponsavelProps) {
  const confirmacaoIgual = confirmacao !== '' && confirmacao === senha;

  return (
    <div className="grid grid-cols-1 gap-x-6 gap-y-4 md:grid-cols-2">
      <div className="flex flex-col gap-2">
        <CampoSenha
          variante="portal"
          rotulo={MENSAGEM_CADASTRO_ROTULO_SENHA}
          iconeInicial="lock"
          obrigatorio
          autoComplete="new-password"
          erro={erros.senha?.message}
          {...registrar('senha', { deps: ['confirmarSenha'] })}
        />
        <MedidorSenha senha={senha} />
      </div>

      <div className="flex flex-col gap-2">
        <CampoSenha
          variante="portal"
          rotulo={MENSAGEM_CADASTRO_ROTULO_CONFIRMAR_SENHA}
          iconeInicial="lock_clock"
          obrigatorio
          autoComplete="new-password"
          erro={erros.confirmarSenha?.message}
          {...registrar('confirmarSenha')}
        />
        {confirmacaoIgual && (
          <p className="flex items-center gap-1 text-legenda font-medium text-status-sucesso">
            <Icone nome="done_all" tamanho={14} />
            {MENSAGEM_LOCADORA_SENHAS_COINCIDEM}
          </p>
        )}
      </div>

      <div className="md:col-span-2">
        <RequisitosSenha senha={senha} />
      </div>
    </div>
  );
}

/**
 * Medidor de quatro barras, como o desenho: cada requisito atendido acende uma barra (ver `forcaSenhaLocadora`).
 * O nível e a barra são anunciados em texto, não só pela cor.
 */
function MedidorSenha({ senha }: { senha: string }) {
  const nivel = nivelSenha(avaliarRequisitosSenha(senha));
  const vazia = senha === '';
  const tom = tomDoNivel(nivel);

  return (
    <div className="space-y-1">
      <div aria-hidden="true" className="grid grid-cols-4 gap-1.5">
        {Array.from({ length: TOTAL_BARRAS_FORCA_SENHA }, (_, indice) => (
          <span key={indice} className={`h-1.5 rounded-full ${!vazia && indice < nivel ? tom.barra : 'bg-steel-linha'}`} />
        ))}
      </div>
      {!vazia && (
        <div aria-live="polite" className="flex items-center justify-between text-legenda">
          <span className={`flex items-center gap-1 font-semibold ${tom.texto}`}>
            <span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${tom.barra}`} />
            {mensagemLocadoraNivelSenha(nivel)}
          </span>
          <span className="text-status-neutro">{mensagemLocadoraNumeroNivelSenha(nivel, TOTAL_BARRAS_FORCA_SENHA)}</span>
        </div>
      )}
    </div>
  );
}

/** Cores do medidor por nível: verde na senha forte, laranja na média, vermelho na fraca (sempre com texto). */
function tomDoNivel(nivel: number): { barra: string; texto: string } {
  if (nivel >= 3) return { barra: 'bg-status-sucesso', texto: 'text-status-sucesso' };
  if (nivel === 2) return { barra: 'bg-status-pendente', texto: 'text-status-pendente' };
  return { barra: 'bg-status-perigo', texto: 'text-status-perigo' };
}

/** Caixa "Requisitos de segurança da credencial": cada requisito diz se está atendido, em texto e em ícone. */
function RequisitosSenha({ senha }: { senha: string }) {
  const requisitos = avaliarRequisitosSenha(senha);
  const itens = [
    { texto: MENSAGEM_LOCADORA_REQUISITO_TAMANHO, atendido: requisitos.tamanho },
    { texto: MENSAGEM_LOCADORA_REQUISITO_MAIUSCULA, atendido: requisitos.maiuscula },
    { texto: MENSAGEM_LOCADORA_REQUISITO_NUMERO, atendido: requisitos.numero },
  ];

  return (
    <div className="space-y-2 rounded-lg bg-steel-fundo p-4">
      <p className="text-legenda font-semibold uppercase tracking-wider text-navy">{MENSAGEM_LOCADORA_REQUISITOS_TITULO}</p>
      <ul className="grid grid-cols-1 gap-2 sm:grid-cols-3">
        {itens.map((item) => (
          <li
            key={item.texto}
            className={`flex items-center gap-1.5 text-corpo-denso ${item.atendido ? 'text-status-sucesso' : 'text-status-neutro'}`}
          >
            <Icone nome={item.atendido ? 'check_circle' : 'radio_button_unchecked'} tamanho={18} preenchido={item.atendido} />
            <span>{item.texto}</span>
            <span className="sr-only">
              {item.atendido ? MENSAGEM_LOCADORA_REQUISITO_ATENDIDO : MENSAGEM_LOCADORA_REQUISITO_PENDENTE}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

export interface AceiteTermosResponsavelProps {
  registrar: UseFormRegister<FormularioResponsavel>;
  erro?: string;
}

/**
 * Aceite obrigatório dos termos. Os nomes dos termos aparecem em negrito, sem link: não há página de termos
 * (o cadastro do cliente também não tem), então nenhum link é inventado.
 */
export function AceiteTermosResponsavel({ registrar, erro }: AceiteTermosResponsavelProps) {
  return (
    <div className="flex flex-col gap-2">
      <label className="flex cursor-pointer select-none items-start gap-3">
        <input
          type="checkbox"
          aria-invalid={erro ? true : undefined}
          className="mt-0.5 h-5 w-5 shrink-0 cursor-pointer rounded accent-navy focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel"
          {...registrar('aceiteTermos')}
        />
        <span className="text-corpo text-navy">
          {MENSAGEM_LOCADORA_ACEITE_PREFIXO}{' '}
          <strong className="font-semibold">{MENSAGEM_LOCADORA_ACEITE_TERMOS}</strong>{' '}
          {MENSAGEM_LOCADORA_ACEITE_CONJUNCAO}{' '}
          <strong className="font-semibold">{MENSAGEM_LOCADORA_ACEITE_PRIVACIDADE}</strong>{' '}
          {MENSAGEM_LOCADORA_ACEITE_SUFIXO}
        </span>
      </label>
      {erro && (
        <p role="alert" className="flex items-center gap-1 text-legenda font-medium text-status-perigo">
          <Icone nome="error" tamanho={14} />
          {erro}
        </p>
      )}
    </div>
  );
}
