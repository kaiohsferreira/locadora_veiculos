import { useEffect, useRef } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Campo, CampoSenha, Icone, Select } from '@/components/ui';
import { formatarCpf } from '@/lib/formatters';
import { mascararDataBr } from '@/lib/datasBr';
import {
  MENSAGEM_EQUIPE_AVISO_SENHA,
  MENSAGEM_EQUIPE_BOTAO_ADICIONAR,
  MENSAGEM_EQUIPE_CANCELAR,
  MENSAGEM_EQUIPE_DICA_CARGO_PROPRIO,
  MENSAGEM_EQUIPE_DICA_CPF_SOMENTE_LEITURA,
  MENSAGEM_EQUIPE_DICA_SENHA,
  MENSAGEM_EQUIPE_PLACEHOLDER_ADMISSAO,
  MENSAGEM_EQUIPE_PLACEHOLDER_CPF,
  MENSAGEM_EQUIPE_PLACEHOLDER_EMAIL,
  MENSAGEM_EQUIPE_PLACEHOLDER_MATRICULA,
  MENSAGEM_EQUIPE_PLACEHOLDER_NOME,
  MENSAGEM_EQUIPE_ROTULO_ADMISSAO,
  MENSAGEM_EQUIPE_ROTULO_CARGO,
  MENSAGEM_EQUIPE_ROTULO_CONFIRMAR_SENHA,
  MENSAGEM_EQUIPE_ROTULO_CPF,
  MENSAGEM_EQUIPE_ROTULO_EMAIL,
  MENSAGEM_EQUIPE_ROTULO_MATRICULA,
  MENSAGEM_EQUIPE_ROTULO_NOME,
  MENSAGEM_EQUIPE_ROTULO_SENHA,
  MENSAGEM_EQUIPE_SALVAR_EDICAO,
} from '@/lib/mensagens';
import { CARGO_FUNCIONARIO, ROTULO_CARGO_FUNCIONARIO } from '@/types/enums';
import {
  CAMPOS_FORMULARIO_FUNCIONARIO,
  ESQUEMA_CADASTRO_FUNCIONARIO,
  ESQUEMA_EDICAO_FUNCIONARIO,
  type CampoFormularioFuncionario,
  type FormularioFuncionario,
  type ModoFormularioFuncionario,
} from '../formulario/esquemaFuncionario';
import { primeiroCampoComErro, type ErrosCamposFuncionario } from '../erros/mapeamentoErroEquipe';
import { CaixaCargo } from './CaixaCargo';

const OPCOES_CARGO = CARGO_FUNCIONARIO.map((cargo) => ({ valor: cargo, rotulo: ROTULO_CARGO_FUNCIONARIO[cargo] }));

export interface FormularioFuncionarioProps {
  modo: ModoFormularioFuncionario;
  valoresIniciais: FormularioFuncionario;
  /** Edição: CPF mascarado vindo do servidor, mostrado só como leitura. */
  cpfMascarado?: string;
  /** Edição: a linha é a do próprio usuário, então o cargo não é trocado aqui. */
  ehVoce: boolean;
  enviando: boolean;
  /** Aviso geral do envio (ex.: último administrador), mostrado no topo do formulário. */
  erroGeral: string | null;
  /** Erros por campo vindos do servidor; cada envio com erro traz um objeto novo. */
  errosCampos: ErrosCamposFuncionario;
  aoEnviar: (valores: FormularioFuncionario) => void;
  aoCancelar: () => void;
}

/**
 * Corpo e rodapé da gaveta. No cadastro: CPF, senha inicial e confirmação. Na edição: CPF só leitura e sem senha.
 * A senha vive só no estado deste formulário, que some quando a gaveta fecha.
 */
export function FormularioFuncionario({
  modo,
  valoresIniciais,
  cpfMascarado,
  ehVoce,
  enviando,
  erroGeral,
  errosCampos,
  aoEnviar,
  aoCancelar,
}: FormularioFuncionarioProps) {
  const cadastro = modo === 'cadastro';
  const esquema = cadastro ? ESQUEMA_CADASTRO_FUNCIONARIO : ESQUEMA_EDICAO_FUNCIONARIO;

  const {
    register,
    handleSubmit,
    control,
    setError,
    setFocus,
    formState: { errors },
  } = useForm<FormularioFuncionario>({
    defaultValues: valoresIniciais,
    resolver: zodResolver(esquema),
  });

  const cargoEscolhido = useWatch({ control, name: 'cargo' });
  const cargoBloqueado = !cadastro && ehVoce;
  /** Campo com erro do servidor que ainda espera o foco (ver o efeito abaixo). */
  const focoPendente = useRef<CampoFormularioFuncionario | null>(null);

  // O campo de nome recebe o foco ao abrir (na edição, quando a consulta termina e o formulário aparece).
  useEffect(() => {
    setFocus('nome');
  }, [setFocus]);

  // Erros do servidor viram erro do campo; o primeiro (na ordem da tela) fica pendente para receber o foco.
  useEffect(() => {
    for (const campo of CAMPOS_FORMULARIO_FUNCIONARIO) {
      const mensagem = errosCampos[campo];
      if (mensagem === undefined) continue;
      setError(campo, { type: 'server', message: mensagem });
    }
    focoPendente.current = primeiroCampoComErro(errosCampos) ?? null;
  }, [errosCampos, setError]);

  // O foco só vai para o campo depois que o envio termina. Enquanto ele dura, o <fieldset> está desabilitado e o
  // navegador não dá foco a campo desabilitado. Quando `enviando` vira falso, o fieldset já está habilitado: o foco vai.
  useEffect(() => {
    const campo = focoPendente.current;
    if (enviando || campo === null) return;
    focoPendente.current = null;
    setFocus(campo);
  }, [enviando, errosCampos, setFocus]);

  const cargoExibido = cargoBloqueado ? valoresIniciais.cargo : cargoEscolhido;


  return (
    <form onSubmit={handleSubmit(aoEnviar)} noValidate className="flex min-h-0 flex-1 flex-col" aria-busy={enviando || undefined}>
      <fieldset disabled={enviando} className="min-w-0 flex-1 space-y-4 overflow-y-auto border-0 px-6 py-4">
        {erroGeral !== null && (
          <div role="alert" className="flex items-start gap-2 rounded-lg bg-status-perigo-fundo p-3 text-corpo-denso text-status-perigo">
            <Icone nome="error" tamanho={18} className="mt-0.5" />
            <span>{erroGeral}</span>
          </div>
        )}

        <Campo
          rotulo={MENSAGEM_EQUIPE_ROTULO_NOME}
          obrigatorio
          iconeInicial="person"
          placeholder={MENSAGEM_EQUIPE_PLACEHOLDER_NOME}
          autoComplete="off"
          data-foco-inicial
          erro={errors.nome?.message}
          {...register('nome')}
        />

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {cadastro ? (
            <Campo
              rotulo={MENSAGEM_EQUIPE_ROTULO_CPF}
              obrigatorio
              iconeInicial="badge"
              mascara={formatarCpf}
              inputMode="numeric"
              placeholder={MENSAGEM_EQUIPE_PLACEHOLDER_CPF}
              autoComplete="off"
              erro={errors.cpf?.message}
              {...register('cpf')}
            />
          ) : (
            <Campo
              rotulo={MENSAGEM_EQUIPE_ROTULO_CPF}
              iconeInicial="badge"
              value={cpfMascarado ?? ''}
              readOnly
              dica={MENSAGEM_EQUIPE_DICA_CPF_SOMENTE_LEITURA}
            />
          )}
          <Campo
            rotulo={MENSAGEM_EQUIPE_ROTULO_MATRICULA}
            obrigatorio
            iconeInicial="tag"
            placeholder={MENSAGEM_EQUIPE_PLACEHOLDER_MATRICULA}
            autoComplete="off"
            erro={errors.matricula?.message}
            {...register('matricula')}
          />
        </div>

        <Campo
          rotulo={MENSAGEM_EQUIPE_ROTULO_EMAIL}
          obrigatorio
          type="email"
          iconeInicial="mail"
          placeholder={MENSAGEM_EQUIPE_PLACEHOLDER_EMAIL}
          autoComplete="off"
          erro={errors.email?.message}
          {...register('email')}
        />

        <Campo
          rotulo={MENSAGEM_EQUIPE_ROTULO_ADMISSAO}
          obrigatorio
          iconeInicial="calendar_today"
          mascara={mascararDataBr}
          inputMode="numeric"
          placeholder={MENSAGEM_EQUIPE_PLACEHOLDER_ADMISSAO}
          maxLength={10}
          autoComplete="off"
          erro={errors.dataAdmissao?.message}
          {...register('dataAdmissao')}
        />

        {cargoBloqueado ? (
          <>
            <input type="hidden" {...register('cargo')} />
            <Campo
              rotulo={MENSAGEM_EQUIPE_ROTULO_CARGO}
              value={ROTULO_CARGO_FUNCIONARIO[valoresIniciais.cargo]}
              readOnly
              dica={MENSAGEM_EQUIPE_DICA_CARGO_PROPRIO}
            />
          </>
        ) : (
          <Select
            rotulo={MENSAGEM_EQUIPE_ROTULO_CARGO}
            obrigatorio
            opcoes={OPCOES_CARGO}
            opcaoVazia={false}
            erro={errors.cargo?.message}
            {...register('cargo')}
          />
        )}

        <CaixaCargo cargo={cargoExibido} />

        {cadastro && (
          <>
            <CampoSenha
              rotulo={MENSAGEM_EQUIPE_ROTULO_SENHA}
              obrigatorio
              autoComplete="new-password"
              dica={MENSAGEM_EQUIPE_DICA_SENHA}
              erro={errors.senhaInicial?.message}
              {...register('senhaInicial')}
            />
            <CampoSenha
              rotulo={MENSAGEM_EQUIPE_ROTULO_CONFIRMAR_SENHA}
              obrigatorio
              autoComplete="new-password"
              erro={errors.confirmarSenha?.message}
              {...register('confirmarSenha')}
            />
            <p className="flex items-start gap-2 rounded-lg bg-orange-claro p-3 text-corpo-denso text-status-pendente">
              <Icone nome="info" tamanho={18} className="mt-0.5" />
              <span>{MENSAGEM_EQUIPE_AVISO_SENHA}</span>
            </p>
          </>
        )}
      </fieldset>

      <footer className="flex flex-wrap items-center justify-end gap-2 border-t border-borda bg-steel-fundo px-6 py-4">
        <Botao variante="contorno" onClick={aoCancelar} disabled={enviando}>
          {MENSAGEM_EQUIPE_CANCELAR}
        </Botao>
        <Botao type="submit" icone={cadastro ? 'person_add' : 'save'} carregando={enviando}>
          {cadastro ? MENSAGEM_EQUIPE_BOTAO_ADICIONAR : MENSAGEM_EQUIPE_SALVAR_EDICAO}
        </Botao>
      </footer>
    </form>
  );
}
