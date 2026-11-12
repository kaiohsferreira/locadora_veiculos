/**
 * Campos do formulário de cadastro, em quatro seções. Cada uma recebe o `register` e os
 * `erros` do React Hook Form. As máscaras vêm de `@/lib/formatters`.
 */
import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Campo, CampoSenha, Select } from '@/components/ui';
import { formatarCep, formatarCpf, formatarTelefone, limitarDigitos } from '@/lib/formatters';
import { MENSAGEM_CADASTRO_DICA_ENDERECO, MENSAGEM_CADASTRO_DICA_SENHA } from '@/lib/mensagens';
import { CATEGORIA_CNH, criarOpcoes, ROTULO_CATEGORIA_CNH } from '@/types/enums';
import { LIMITES_CADASTRO, type FormularioCadastro } from './esquemaCadastro';
import { OPCOES_UF } from './ufs';
import { SecaoFormulario } from './SecaoFormulario';

export interface CamposCadastroProps {
  registrar: UseFormRegister<FormularioCadastro>;
  erros: FieldErrors<FormularioCadastro>;
}

const OPCOES_CATEGORIA = criarOpcoes(CATEGORIA_CNH, ROTULO_CATEGORIA_CNH);

/** Número da CNH: só dígitos, no máximo 11. */
function mascaraCnh(valor: string): string {
  return limitarDigitos(valor, LIMITES_CADASTRO.cnhNumero);
}

/** Dados pessoais: nome, CPF, nascimento, e-mail e celular. */
export function CamposDadosPessoais({ registrar, erros }: CamposCadastroProps) {
  return (
    <SecaoFormulario titulo="Dados pessoais">
      <div className="md:col-span-2">
        <Campo
          rotulo="Nome completo"
          obrigatorio
          autoComplete="name"
          erro={erros.nome?.message}
          {...registrar('nome')}
        />
      </div>
      <Campo
        rotulo="CPF"
        obrigatorio
        inputMode="numeric"
        autoComplete="off"
        mascara={formatarCpf}
        erro={erros.cpf?.message}
        {...registrar('cpf')}
      />
      <Campo
        rotulo="Data de nascimento"
        obrigatorio
        type="date"
        autoComplete="bday"
        erro={erros.dataNascimento?.message}
        {...registrar('dataNascimento')}
      />
      <Campo
        rotulo="E-mail"
        obrigatorio
        type="email"
        autoComplete="email"
        erro={erros.email?.message}
        {...registrar('email')}
      />
      <Campo
        rotulo="Telefone"
        obrigatorio
        type="tel"
        autoComplete="tel"
        inputMode="tel"
        mascara={formatarTelefone}
        erro={erros.telefone?.message}
        {...registrar('telefone')}
      />
    </SecaoFormulario>
  );
}

/** Habilitação: número, categoria e validade da CNH. */
export function CamposHabilitacao({ registrar, erros }: CamposCadastroProps) {
  return (
    <SecaoFormulario titulo="Habilitação (CNH)">
      <Campo
        rotulo="Número da CNH"
        obrigatorio
        inputMode="numeric"
        autoComplete="off"
        mascara={mascaraCnh}
        erro={erros.cnhNumero?.message}
        {...registrar('cnhNumero')}
      />
      <Select
        rotulo="Categoria"
        obrigatorio
        opcoes={OPCOES_CATEGORIA}
        erro={erros.cnhCategoria?.message}
        {...registrar('cnhCategoria')}
      />
      <Campo
        rotulo="Validade da CNH"
        obrigatorio
        type="date"
        erro={erros.cnhValidade?.message}
        {...registrar('cnhValidade')}
      />
    </SecaoFormulario>
  );
}

/** Endereço opcional: o título da seção diz que é opcional. */
export function CamposEndereco({ registrar, erros }: CamposCadastroProps) {
  return (
    <SecaoFormulario titulo="Endereço (opcional)" descricao={MENSAGEM_CADASTRO_DICA_ENDERECO}>
      <Campo
        rotulo="CEP"
        inputMode="numeric"
        autoComplete="postal-code"
        mascara={formatarCep}
        erro={erros.cep?.message}
        {...registrar('cep')}
      />
      <Campo
        rotulo="Logradouro"
        autoComplete="address-line1"
        erro={erros.logradouro?.message}
        {...registrar('logradouro')}
      />
      <Campo rotulo="Número" autoComplete="off" erro={erros.numero?.message} {...registrar('numero')} />
      <Campo
        rotulo="Complemento"
        autoComplete="address-line2"
        erro={erros.complemento?.message}
        {...registrar('complemento')}
      />
      <Campo rotulo="Bairro" erro={erros.bairro?.message} {...registrar('bairro')} />
      <Campo
        rotulo="Cidade"
        autoComplete="address-level2"
        erro={erros.cidade?.message}
        {...registrar('cidade')}
      />
      <Select
        rotulo="UF"
        opcoes={OPCOES_UF}
        autoComplete="address-level1"
        erro={erros.uf?.message}
        {...registrar('uf')}
      />
    </SecaoFormulario>
  );
}

/** Senha de acesso e a confirmação (a confirmação só existe no front). */
export function CamposSenha({ registrar, erros }: CamposCadastroProps) {
  return (
    <SecaoFormulario titulo="Senha de acesso">
      <CampoSenha
        rotulo="Senha"
        obrigatorio
        autoComplete="new-password"
        dica={MENSAGEM_CADASTRO_DICA_SENHA}
        erro={erros.senha?.message}
        {...registrar('senha', { deps: ['confirmarSenha'] })}
      />
      <CampoSenha
        rotulo="Confirmar senha"
        obrigatorio
        autoComplete="new-password"
        erro={erros.confirmarSenha?.message}
        {...registrar('confirmarSenha')}
      />
    </SecaoFormulario>
  );
}
