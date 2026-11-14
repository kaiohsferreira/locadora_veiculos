import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Campo, Icone, Select } from '@/components/ui';
import { formatarCep, formatarCnpj, formatarTelefone } from '@/lib/formatters';
import {
  MENSAGEM_LOCADORA_DICA_NOME_FANTASIA,
  MENSAGEM_LOCADORA_ROTULO_BAIRRO,
  MENSAGEM_LOCADORA_ROTULO_CEP,
  MENSAGEM_LOCADORA_ROTULO_CIDADE,
  MENSAGEM_LOCADORA_ROTULO_CNPJ,
  MENSAGEM_LOCADORA_ROTULO_COMPLEMENTO,
  MENSAGEM_LOCADORA_ROTULO_EMAIL_COMERCIAL,
  MENSAGEM_LOCADORA_ROTULO_LOGRADOURO,
  MENSAGEM_LOCADORA_ROTULO_NOME_FANTASIA,
  MENSAGEM_LOCADORA_ROTULO_NUMERO,
  MENSAGEM_LOCADORA_ROTULO_RAZAO_SOCIAL,
  MENSAGEM_LOCADORA_ROTULO_TELEFONE_COMERCIAL,
  MENSAGEM_LOCADORA_ROTULO_UF,
  MENSAGEM_LOCADORA_TITULO_ENDERECO,
} from '@/lib/mensagens';
import { OPCOES_UF } from '@/features/clientes/cadastro/ufs';
import type { FormularioEmpresa } from './esquemaLocadora';

export interface CamposEmpresaProps {
  registrar: UseFormRegister<FormularioEmpresa>;
  erros: FieldErrors<FormularioEmpresa>;
  /** O CNPJ tem formato e dígitos certos e a verificação não o achou em uso: mostra o ícone de conferido. */
  cnpjConferido: boolean;
  /** Chamado ao sair do campo CNPJ (verificação). */
  aoSairCnpj?: () => void;
  /** Chamado ao sair do campo e-mail comercial (verificação). */
  aoSairEmail?: () => void;
}

/** Opções do `register` com o gancho de saída do campo, quando a tela passou um. */
function opcoesAoSair(aoSair: (() => void) | undefined): { onBlur: () => void } | undefined {
  return aoSair === undefined ? undefined : { onBlur: () => aoSair() };
}

/** Etapa 1: identificação da empresa (razão social e nome fantasia, CNPJ e telefone, e-mail comercial). */
export function CamposIdentificacaoEmpresa({
  registrar,
  erros,
  cnpjConferido,
  aoSairCnpj,
  aoSairEmail,
}: CamposEmpresaProps) {
  return (
    <div className="grid grid-cols-1 gap-x-6 gap-y-4 md:grid-cols-2">
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_LOCADORA_ROTULO_RAZAO_SOCIAL}
        iconeInicial="business"
        obrigatorio
        autoComplete="organization"
        erro={erros.razaoSocial?.message}
        {...registrar('razaoSocial')}
      />
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_LOCADORA_ROTULO_NOME_FANTASIA}
        iconeInicial="storefront"
        dica={MENSAGEM_LOCADORA_DICA_NOME_FANTASIA}
        obrigatorio
        erro={erros.nomeFantasia?.message}
        {...registrar('nomeFantasia')}
      />
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_LOCADORA_ROTULO_CNPJ}
        iconeInicial="badge"
        obrigatorio
        inputMode="numeric"
        autoComplete="off"
        mascara={formatarCnpj}
        valido={cnpjConferido}
        erro={erros.cnpj?.message}
        {...registrar('cnpj', opcoesAoSair(aoSairCnpj))}
      />
      <Campo
        variante="cadastro"
        rotulo={MENSAGEM_LOCADORA_ROTULO_TELEFONE_COMERCIAL}
        iconeInicial="call"
        obrigatorio
        type="tel"
        inputMode="tel"
        autoComplete="tel"
        mascara={formatarTelefone}
        erro={erros.telefone?.message}
        {...registrar('telefone')}
      />
      <div className="md:col-span-2">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_LOCADORA_ROTULO_EMAIL_COMERCIAL}
          iconeInicial="mail"
          obrigatorio
          type="email"
          autoComplete="email"
          erro={erros.email?.message}
          {...registrar('email', opcoesAoSair(aoSairEmail))}
        />
      </div>
    </div>
  );
}

/** Etapa 1: endereço da sede operacional. Complemento é opcional; os demais campos são obrigatórios. */
export function CamposEnderecoSede({ registrar, erros }: Pick<CamposEmpresaProps, 'registrar' | 'erros'>) {
  return (
    <div className="grid grid-cols-1 gap-x-6 gap-y-4 md:grid-cols-6">
      <div className="md:col-span-6">
        <h2 className="flex items-center gap-2 text-corpo text-navy">
          <Icone nome="pin_drop" tamanho={20} className="text-status-pendente" />
          {MENSAGEM_LOCADORA_TITULO_ENDERECO}
        </h2>
      </div>
      <div className="md:col-span-2">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_LOCADORA_ROTULO_CEP}
          iconeInicial="pin_drop"
          obrigatorio
          inputMode="numeric"
          autoComplete="postal-code"
          mascara={formatarCep}
          erro={erros.cep?.message}
          {...registrar('cep')}
        />
      </div>
      <div className="md:col-span-4">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_LOCADORA_ROTULO_LOGRADOURO}
          iconeInicial="location_on"
          obrigatorio
          autoComplete="address-line1"
          erro={erros.logradouro?.message}
          {...registrar('logradouro')}
        />
      </div>
      <div className="md:col-span-2">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_LOCADORA_ROTULO_NUMERO}
          iconeInicial="home"
          obrigatorio
          autoComplete="off"
          erro={erros.numero?.message}
          {...registrar('numero')}
        />
      </div>
      <div className="md:col-span-4">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_LOCADORA_ROTULO_COMPLEMENTO}
          iconeInicial="apartment"
          autoComplete="address-line2"
          erro={erros.complemento?.message}
          {...registrar('complemento')}
        />
      </div>
      <div className="md:col-span-2">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_LOCADORA_ROTULO_BAIRRO}
          iconeInicial="map"
          obrigatorio
          erro={erros.bairro?.message}
          {...registrar('bairro')}
        />
      </div>
      <div className="md:col-span-3">
        <Campo
          variante="cadastro"
          rotulo={MENSAGEM_LOCADORA_ROTULO_CIDADE}
          iconeInicial="location_city"
          obrigatorio
          autoComplete="address-level2"
          erro={erros.cidade?.message}
          {...registrar('cidade')}
        />
      </div>
      <div className="md:col-span-1">
        <Select
          rotulo={MENSAGEM_LOCADORA_ROTULO_UF}
          opcoes={OPCOES_UF}
          obrigatorio
          autoComplete="address-level1"
          opcaoVazia="UF"
          erro={erros.uf?.message}
          {...registrar('uf')}
        />
      </div>
    </div>
  );
}
