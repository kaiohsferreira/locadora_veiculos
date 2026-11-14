import { useRef, useState, type MutableRefObject } from 'react';
import { EstadoErro, Esqueleto, useAviso } from '@/components/ui';
import { obterMensagemErro } from '@/lib/erros';
import {
  MENSAGEM_EQUIPE_CARREGANDO_FUNCIONARIO,
  MENSAGEM_EQUIPE_ERRO_CONSULTA,
  MENSAGEM_EQUIPE_PAINEL_ADICIONAR,
  MENSAGEM_EQUIPE_PAINEL_EDITAR,
  MENSAGEM_EQUIPE_SUCESSO_CADASTRO,
  MENSAGEM_EQUIPE_SUCESSO_EDICAO,
} from '@/lib/mensagens';
import {
  montarRequisicaoCadastro,
  montarRequisicaoEdicao,
  valoresIniciaisCadastro,
  valoresIniciaisEdicao,
} from '../formulario/conversaoFuncionario';
import type { FormularioFuncionario } from '../formulario/esquemaFuncionario';
import { interpretarErroFuncionario, type ErrosCamposFuncionario } from '../erros/mapeamentoErroEquipe';
import { useCadastrarFuncionario } from '../hooks/useCadastrarFuncionario';
import { useEditarFuncionario } from '../hooks/useEditarFuncionario';
import { useFuncionarioEquipe } from '../hooks/useEquipe';
import { FormularioFuncionario as Formulario } from './FormularioFuncionario';
import { GavetaFuncionario } from './GavetaFuncionario';

/** O que a gaveta faz: adicionar uma pessoa nova, ou editar a de id informado. */
export type ModoPainelFuncionario = { tipo: 'cadastro' } | { tipo: 'edicao'; idFuncionario: number };

export interface PainelFuncionarioProps {
  modo: ModoPainelFuncionario;
  /** Botão que abriu a gaveta: recebe o foco de volta ao fechar. */
  refGatilho: MutableRefObject<HTMLElement | null>;
  aoFechar: () => void;
}

/**
 * Gaveta de adicionar e de editar, com o envio. Fica montada só enquanto está aberta: ao fechar, o formulário, a senha
 * digitada e as mutações saem da memória. Erros de campo chegam ao formulário; o aviso geral aparece no topo.
 */
export function PainelFuncionario({ modo, refGatilho, aoFechar }: PainelFuncionarioProps) {
  const aviso = useAviso();
  const edicao = modo.tipo === 'edicao';
  const consulta = useFuncionarioEquipe(modo.tipo === 'edicao' ? modo.idFuncionario : null);
  const cadastrar = useCadastrarFuncionario();
  const editar = useEditarFuncionario();
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const [errosCampos, setErrosCampos] = useState<ErrosCamposFuncionario>({});
  /** Ref síncrona: impede um segundo envio antes do próximo render (duplo clique ou Enter repetido). */
  const emEnvio = useRef(false);

  const enviando = cadastrar.isPending || editar.isPending;
  const funcionario = consulta.data;
  const ehVoce = funcionario?.ehVoce ?? false;

  function fechar() {
    if (!enviando) aoFechar();
  }

  async function enviar(valores: FormularioFuncionario) {
    if (emEnvio.current) return;
    emEnvio.current = true;
    setErroGeral(null);
    setErrosCampos({});
    try {
      if (modo.tipo === 'cadastro') {
        await cadastrar.mutateAsync(montarRequisicaoCadastro(valores));
        aviso.sucesso(MENSAGEM_EQUIPE_SUCESSO_CADASTRO);
      } else {
        await editar.mutateAsync({
          idFuncionario: modo.idFuncionario,
          requisicao: montarRequisicaoEdicao(valores),
        });
        aviso.sucesso(MENSAGEM_EQUIPE_SUCESSO_EDICAO);
      }
      aoFechar();
    } catch (erro) {
      const resultado = interpretarErroFuncionario(erro, { ehPropriaConta: ehVoce });
      setErrosCampos(resultado.campos);
      setErroGeral(resultado.aviso);
    } finally {
      emEnvio.current = false;
    }
  }

  const titulo = edicao ? MENSAGEM_EQUIPE_PAINEL_EDITAR : MENSAGEM_EQUIPE_PAINEL_ADICIONAR;
  const icone = edicao ? 'edit' : 'person_add';

  return (
    <GavetaFuncionario titulo={titulo} icone={icone} bloqueada={enviando} aoFechar={fechar} refGatilho={refGatilho}>
      {edicao && consulta.isPending && (
        <div className="flex flex-col gap-4 px-6 py-4" aria-busy="true">
          <Esqueleto className="h-10 w-full" />
          <Esqueleto className="h-10 w-full" />
          <Esqueleto className="h-10 w-full" />
          <p className="sr-only">{MENSAGEM_EQUIPE_CARREGANDO_FUNCIONARIO}</p>
        </div>
      )}

      {edicao && consulta.isError && (
        <EstadoErro
          titulo={MENSAGEM_EQUIPE_ERRO_CONSULTA}
          mensagem={interpretarErroFuncionario(consulta.error, { ehPropriaConta: false }).aviso ?? obterMensagemErro(consulta.error)}
          aoTentarNovamente={() => void consulta.refetch()}
          tentando={consulta.isFetching}
        />
      )}

      {!edicao && (
        <Formulario
          modo="cadastro"
          valoresIniciais={valoresIniciaisCadastro()}
          ehVoce={false}
          enviando={enviando}
          erroGeral={erroGeral}
          errosCampos={errosCampos}
          aoEnviar={enviar}
          aoCancelar={fechar}
        />
      )}

      {edicao && funcionario !== undefined && (
        <Formulario
          key={funcionario.idFuncionario}
          modo="edicao"
          valoresIniciais={valoresIniciaisEdicao(funcionario)}
          cpfMascarado={funcionario.cpfMascarado}
          ehVoce={funcionario.ehVoce}
          enviando={enviando}
          erroGeral={erroGeral}
          errosCampos={errosCampos}
          aoEnviar={enviar}
          aoCancelar={fechar}
        />
      )}
    </GavetaFuncionario>
  );
}
