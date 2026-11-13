/**
 * Campos do cadastro, por tela. Cada grupo recebe o `register` e os `erros` do React Hook Form.
 * As máscaras vêm de `@/lib/formatters`; os textos, de `@/lib/mensagens`.
 *
 * - Etapa 1 (dados pessoais): `CamposDadosPessoais`, no estilo do design 20 (campo com ícone e "conferido").
 * - Etapa 2 (documentos e senha): `BlocoHabilitacao`, `BlocoEndereco` e `BlocoSenha`, em blocos no
 *   estilo do design 21 (cartões brancos com rótulo em maiúsculas).
 */
import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Campo, CampoSenha, Select } from '@/components/ui';
import { formatarCep, formatarCpf, formatarTelefone, limitarDigitos } from '@/lib/formatters';
import {
  MENSAGEM_CADASTRO_APOIO_NASCIMENTO,
  MENSAGEM_CADASTRO_APOIO_NOME,
  MENSAGEM_CADASTRO_BLOCO_CNH_TEXTO,
  MENSAGEM_CADASTRO_BLOCO_CNH_TITULO,
  MENSAGEM_CADASTRO_BLOCO_ENDERECO_TEXTO,
  MENSAGEM_CADASTRO_BLOCO_ENDERECO_TITULO,
  MENSAGEM_CADASTRO_BLOCO_SENHA_TITULO,
  MENSAGEM_CADASTRO_DICA_EMAIL,
  MENSAGEM_CADASTRO_DICA_SENHA,
  MENSAGEM_CADASTRO_DICA_TELEFONE,
  MENSAGEM_CADASTRO_PLACEHOLDER_NOME,
  MENSAGEM_CADASTRO_ROTULO_BAIRRO,
  MENSAGEM_CADASTRO_ROTULO_CEP,
  MENSAGEM_CADASTRO_ROTULO_CIDADE,
  MENSAGEM_CADASTRO_ROTULO_CNH_CATEGORIA,
  MENSAGEM_CADASTRO_ROTULO_CNH_NUMERO,
  MENSAGEM_CADASTRO_ROTULO_CNH_VALIDADE,
  MENSAGEM_CADASTRO_ROTULO_COMPLEMENTO,
  MENSAGEM_CADASTRO_ROTULO_CONFIRMAR_SENHA,
  MENSAGEM_CADASTRO_ROTULO_CPF,
  MENSAGEM_CADASTRO_ROTULO_EMAIL,
  MENSAGEM_CADASTRO_ROTULO_LOGRADOURO,
  MENSAGEM_CADASTRO_ROTULO_NASCIMENTO,
  MENSAGEM_CADASTRO_ROTULO_NOME,
  MENSAGEM_CADASTRO_ROTULO_NUMERO,
  MENSAGEM_CADASTRO_ROTULO_SENHA,
  MENSAGEM_CADASTRO_ROTULO_TELEFONE,
  MENSAGEM_CADASTRO_ROTULO_UF,
} from '@/lib/mensagens';
import { CATEGORIA_CNH, criarOpcoes, ROTULO_CATEGORIA_CNH } from '@/types/enums';
import { BlocoDadosCadastro } from './BlocoDadosCadastro';
import {
  LIMITES_CADASTRO,
  type DadosPessoaisCadastro,
  type FormularioDocumentos,
} from './esquemaCadastro';
import type { CamposValidosDadosPessoais } from './etapasCadastro';
import { OPCOES_UF } from './ufs';

const OPCOES_CATEGORIA = criarOpcoes(CATEGORIA_CNH, ROTULO_CATEGORIA_CNH);

/** Número da CNH: só dígitos, no máximo 11. */
function mascaraCnh(valor: string): string {
  return limitarDigitos(valor, LIMITES_CADASTRO.cnhNumero);
}

/** Id do bloco da senha: o texto do cabeçalho (a regra da senha) descreve os dois campos. */
const ID_BLOCO_SENHA = 'cadastro-bloco-senha';

export interface CamposDadosPessoaisProps {
  registrar: UseFormRegister<DadosPessoaisCadastro>;
  erros: FieldErrors<DadosPessoaisCadastro>;
  validos: CamposValidosDadosPessoais;
}

/** Etapa 1: nome (linha inteira), CPF e nascimento, e-mail e telefone, em grade de duas colunas. */
export function CamposDadosPessoais({ registrar, erros, validos }: CamposDadosPessoaisProps) {
  return (
    <div className="grid grid-cols-1 gap-x-6 gap-y-4 pt-1 md:grid-cols-2">
      <div className="md:col-span-2">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_CADASTRO_ROTULO_NOME}
          apoioRotulo={MENSAGEM_CADASTRO_APOIO_NOME}
          iconeInicial="badge"
          placeholder={MENSAGEM_CADASTRO_PLACEHOLDER_NOME}
          obrigatorio
          autoComplete="name"
          valido={validos.nome}
          erro={erros.nome?.message}
          {...registrar('nome')}
        />
      </div>
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_CADASTRO_ROTULO_CPF}
        iconeInicial="fingerprint"
        obrigatorio
        inputMode="numeric"
        autoComplete="off"
        mascara={formatarCpf}
        valido={validos.cpf}
        erro={erros.cpf?.message}
        {...registrar('cpf')}
      />
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_CADASTRO_ROTULO_NASCIMENTO}
        apoioRotulo={MENSAGEM_CADASTRO_APOIO_NASCIMENTO}
        iconeInicial="calendar_today"
        obrigatorio
        type="date"
        autoComplete="bday"
        valido={validos.dataNascimento}
        erro={erros.dataNascimento?.message}
        {...registrar('dataNascimento')}
      />
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_CADASTRO_ROTULO_EMAIL}
        iconeInicial="alternate_email"
        dica={MENSAGEM_CADASTRO_DICA_EMAIL}
        dicaIcone="mark_email_read"
        obrigatorio
        type="email"
        autoComplete="email"
        valido={validos.email}
        erro={erros.email?.message}
        {...registrar('email')}
      />
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_CADASTRO_ROTULO_TELEFONE}
        iconeInicial="call"
        dica={MENSAGEM_CADASTRO_DICA_TELEFONE}
        obrigatorio
        type="tel"
        autoComplete="tel"
        inputMode="tel"
        mascara={formatarTelefone}
        valido={validos.telefone}
        erro={erros.telefone?.message}
        {...registrar('telefone')}
      />
    </div>
  );
}

export interface BlocoDocumentosProps {
  registrar: UseFormRegister<FormularioDocumentos>;
  erros: FieldErrors<FormularioDocumentos>;
}

/** Etapa 2, bloco "Dados da CNH": número do registro, categoria e validade, digitados. */
export function BlocoHabilitacao({ registrar, erros }: BlocoDocumentosProps) {
  return (
    <BlocoDadosCadastro
      icone="id_card"
      titulo={MENSAGEM_CADASTRO_BLOCO_CNH_TITULO}
      descricao={MENSAGEM_CADASTRO_BLOCO_CNH_TEXTO}
      colunas={3}
    >
      <Campo
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_CNH_NUMERO}
        obrigatorio
        inputMode="numeric"
        autoComplete="off"
        mascara={mascaraCnh}
        erro={erros.cnhNumero?.message}
        {...registrar('cnhNumero')}
      />
      <Select
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_CNH_CATEGORIA}
        obrigatorio
        opcoes={OPCOES_CATEGORIA}
        erro={erros.cnhCategoria?.message}
        {...registrar('cnhCategoria')}
      />
      <Campo
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_CNH_VALIDADE}
        obrigatorio
        type="date"
        erro={erros.cnhValidade?.message}
        {...registrar('cnhValidade')}
      />
    </BlocoDadosCadastro>
  );
}

/** Etapa 2, bloco "Endereço (opcional)": os mesmos campos de sempre, todos opcionais. */
export function BlocoEndereco({ registrar, erros }: BlocoDocumentosProps) {
  return (
    <BlocoDadosCadastro
      icone="home_pin"
      titulo={MENSAGEM_CADASTRO_BLOCO_ENDERECO_TITULO}
      descricao={MENSAGEM_CADASTRO_BLOCO_ENDERECO_TEXTO}
      colunas={3}
    >
      <Campo
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_CEP}
        inputMode="numeric"
        autoComplete="postal-code"
        mascara={formatarCep}
        erro={erros.cep?.message}
        {...registrar('cep')}
      />
      <div className="md:col-span-2 [&>div]:h-full">
        <Campo
          variante="cartao"
          rotulo={MENSAGEM_CADASTRO_ROTULO_LOGRADOURO}
          autoComplete="address-line1"
          erro={erros.logradouro?.message}
          {...registrar('logradouro')}
        />
      </div>
      <Campo
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_NUMERO}
        autoComplete="off"
        erro={erros.numero?.message}
        {...registrar('numero')}
      />
      <Campo
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_COMPLEMENTO}
        autoComplete="address-line2"
        erro={erros.complemento?.message}
        {...registrar('complemento')}
      />
      <Campo
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_BAIRRO}
        erro={erros.bairro?.message}
        {...registrar('bairro')}
      />
      <div className="md:col-span-2 [&>div]:h-full">
        <Campo
          variante="cartao"
          rotulo={MENSAGEM_CADASTRO_ROTULO_CIDADE}
          autoComplete="address-level2"
          erro={erros.cidade?.message}
          {...registrar('cidade')}
        />
      </div>
      <Select
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_UF}
        opcoes={OPCOES_UF}
        autoComplete="address-level1"
        erro={erros.uf?.message}
        {...registrar('uf')}
      />
    </BlocoDadosCadastro>
  );
}

/** Etapa 2, bloco "Senha de acesso": senha e confirmação (a confirmação só existe no front). */
export function BlocoSenha({ registrar, erros }: BlocoDocumentosProps) {
  return (
    <BlocoDadosCadastro
      id={ID_BLOCO_SENHA}
      icone="lock"
      titulo={MENSAGEM_CADASTRO_BLOCO_SENHA_TITULO}
      descricao={MENSAGEM_CADASTRO_DICA_SENHA}
      colunas={2}
    >
      <CampoSenha
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_SENHA}
        obrigatorio
        autoComplete="new-password"
        aria-describedby={`${ID_BLOCO_SENHA}-texto`}
        erro={erros.senha?.message}
        {...registrar('senha', { deps: ['confirmarSenha'] })}
      />
      <CampoSenha
        variante="cartao"
        rotulo={MENSAGEM_CADASTRO_ROTULO_CONFIRMAR_SENHA}
        obrigatorio
        autoComplete="new-password"
        erro={erros.confirmarSenha?.message}
        {...registrar('confirmarSenha')}
      />
    </BlocoDadosCadastro>
  );
}
