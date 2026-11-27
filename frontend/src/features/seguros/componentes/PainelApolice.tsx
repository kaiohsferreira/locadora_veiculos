import { useRef, useState, type MutableRefObject } from 'react';
import { EstadoErro, Esqueleto, useAviso } from '@/components/ui';
import { GavetaFuncionario as Gaveta } from '@/features/equipe/componentes/GavetaFuncionario';
import { useApolice } from '@/features/seguros/hooks/useApolices';
import { useCadastrarApolice, useEditarApolice } from '@/features/seguros/hooks/useSalvarApolice';
import { interpretarErroApolice, type ResultadoErroApolice } from '@/features/seguros/regras/errosApolice';
import {
  montarRequisicaoApolice,
  valoresIniciaisCadastro,
  valoresIniciaisEdicao,
  type ValoresApolice,
} from '@/features/seguros/regras/formularioApolice';
import { obterMensagemErro } from '@/lib/erros';
import {
  SEGUROS_CARREGANDO_APOLICE,
  SEGUROS_ERRO_CONSULTA,
  SEGUROS_GAVETA_EDITAR,
  SEGUROS_GAVETA_NOVA,
  SEGUROS_SUCESSO_CADASTRO,
  SEGUROS_SUCESSO_EDICAO,
} from '@/lib/mensagensSeguros';
import { FormularioApolice } from './FormularioApolice';

/** O que a gaveta faz: cadastrar uma apólice nova, ou editar a de id informado. */
export type ModoPainelApolice = { tipo: 'cadastro' } | { tipo: 'edicao'; idSeguro: number };

export interface PainelApoliceProps {
  modo: ModoPainelApolice;
  /** Botão que abriu a gaveta: recebe o foco de volta ao fechar. */
  refGatilho: MutableRefObject<HTMLElement | null>;
  aoFechar: () => void;
}

const SEM_ERROS: ResultadoErroApolice = { campos: {}, aviso: null };

/**
 * Gaveta de cadastrar e de editar apólice, com o envio. Usa a gaveta lateral da Equipe (o mesmo molde de back office,
 * sem mudar nada nela). Na edição, carrega a apólice atual antes de mostrar o formulário.
 */
export function PainelApolice({ modo, refGatilho, aoFechar }: PainelApoliceProps) {
  const aviso = useAviso();
  const edicao = modo.tipo === 'edicao';
  const consulta = useApolice(modo.tipo === 'edicao' ? modo.idSeguro : null);
  const cadastrar = useCadastrarApolice();
  const editar = useEditarApolice();
  const [erros, setErros] = useState<ResultadoErroApolice>(SEM_ERROS);
  /** Impede um segundo envio antes do próximo render (duplo clique ou Enter repetido). */
  const emEnvio = useRef(false);
  const enviando = cadastrar.isPending || editar.isPending;

  function fechar() {
    if (!enviando) aoFechar();
  }

  async function enviar(valores: ValoresApolice) {
    if (emEnvio.current) return;
    emEnvio.current = true;
    setErros(SEM_ERROS);
    try {
      const requisicao = montarRequisicaoApolice(valores);
      if (modo.tipo === 'cadastro') {
        await cadastrar.mutateAsync(requisicao);
        aviso.sucesso(SEGUROS_SUCESSO_CADASTRO);
      } else {
        await editar.mutateAsync({ idSeguro: modo.idSeguro, requisicao });
        aviso.sucesso(SEGUROS_SUCESSO_EDICAO);
      }
      aoFechar();
    } catch (erro) {
      setErros(interpretarErroApolice(erro));
    } finally {
      emEnvio.current = false;
    }
  }

  const apolice = consulta.data;

  return (
    <Gaveta
      titulo={edicao ? SEGUROS_GAVETA_EDITAR : SEGUROS_GAVETA_NOVA}
      icone={edicao ? 'edit' : 'shield'}
      bloqueada={enviando}
      aoFechar={fechar}
      refGatilho={refGatilho}
    >
      {edicao && consulta.isPending && (
        <div className="flex flex-col gap-4 px-6 py-4" aria-busy="true">
          <Esqueleto className="h-10 w-full" />
          <Esqueleto className="h-10 w-full" />
          <Esqueleto className="h-10 w-full" />
          <p className="sr-only">{SEGUROS_CARREGANDO_APOLICE}</p>
        </div>
      )}

      {edicao && consulta.isError && (
        <EstadoErro
          titulo={SEGUROS_ERRO_CONSULTA}
          mensagem={obterMensagemErro(consulta.error)}
          aoTentarNovamente={() => void consulta.refetch()}
          tentando={consulta.isFetching}
        />
      )}

      {!edicao && (
        <FormularioApolice
          valoresIniciais={valoresIniciaisCadastro()}
          enviando={enviando}
          erroGeral={erros.aviso}
          errosCampos={erros.campos}
          aoEnviar={(valores) => void enviar(valores)}
          aoCancelar={fechar}
        />
      )}

      {edicao && apolice !== undefined && (
        <FormularioApolice
          key={apolice.idSeguro}
          valoresIniciais={valoresIniciaisEdicao(apolice)}
          enviando={enviando}
          erroGeral={erros.aviso}
          errosCampos={erros.campos}
          aoEnviar={(valores) => void enviar(valores)}
          aoCancelar={fechar}
        />
      )}
    </Gaveta>
  );
}
