import type { ReactNode } from 'react';
import { Etiqueta, Icone, Tabela, type ColunaTabela } from '@/components/ui';
import { formatarData, VALOR_AUSENTE } from '@/lib/formatters';
import {
  mensagemEquipeAcaoDaLinha,
  MENSAGEM_EQUIPE_ACAO_DESATIVAR,
  MENSAGEM_EQUIPE_ACAO_EDITAR,
  MENSAGEM_EQUIPE_ACAO_REATIVAR,
  MENSAGEM_EQUIPE_COLUNA_ACOES,
  MENSAGEM_EQUIPE_COLUNA_ADMISSAO,
  MENSAGEM_EQUIPE_COLUNA_CARGO,
  MENSAGEM_EQUIPE_COLUNA_CPF,
  MENSAGEM_EQUIPE_COLUNA_FUNCIONARIO,
  MENSAGEM_EQUIPE_COLUNA_MATRICULA,
  MENSAGEM_EQUIPE_COLUNA_SITUACAO,
  MENSAGEM_EQUIPE_SELO_VOCE,
  MENSAGEM_EQUIPE_SITUACAO_ATIVO,
  MENSAGEM_EQUIPE_SITUACAO_INATIVO,
  MENSAGEM_EQUIPE_TABELA_LEGENDA,
} from '@/lib/mensagens';
import { ROTULO_CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';
import type { FuncionarioEquipe } from '@/types/equipe';
import { iniciaisDoNome, podeDesativar, podeReativar } from '../regrasLinha';
import { ICONE_CARGO } from '../textosCargo';

/** Ações da linha. O botão que foi clicado vai junto, para devolver o foco a ele ao fechar a gaveta. */
export interface AcoesTabelaEquipe {
  aoEditar: (funcionario: FuncionarioEquipe, gatilho: HTMLElement) => void;
  aoDesativar: (funcionario: FuncionarioEquipe, gatilho: HTMLElement) => void;
  aoReativar: (funcionario: FuncionarioEquipe, gatilho: HTMLElement) => void;
  /** Id da linha com ação em andamento: os botões dela ficam desabilitados. */
  idEmAndamento: number | null;
}

export interface TabelaEquipeProps extends AcoesTabelaEquipe {
  itens: readonly FuncionarioEquipe[];
  carregando: boolean;
  /** Conteúdo quando não há linhas (padrão da Tabela, ou um estado vazio com filtro). */
  vazio?: ReactNode;
}

/** Cargo como etiqueta com ícone (o ADMINISTRADOR tem fundo escuro, como no desenho). */
function EtiquetaCargo({ cargo }: { cargo: CargoFuncionario }) {
  const administrador = cargo === 'ADMINISTRADOR';
  return (
    <span
      className={`inline-flex items-center gap-1 whitespace-nowrap rounded px-2 py-1 text-legenda font-semibold ${
        administrador ? 'bg-navy text-steel-claro' : 'bg-steel-suave text-navy'
      }`}
    >
      <Icone nome={ICONE_CARGO[cargo]} tamanho={14} />
      {ROTULO_CARGO_FUNCIONARIO[cargo]}
    </span>
  );
}

/** Avatar com as iniciais: navy para o próprio usuário, cinza-azulado para inativos, azul-claro para os demais. */
function Avatar({ funcionario }: { funcionario: FuncionarioEquipe }) {
  const classe = funcionario.ehVoce
    ? 'bg-navy text-branco'
    : funcionario.ativo
      ? 'bg-steel-linha text-navy'
      : 'bg-steel-alto text-texto-suave';
  return (
    <span
      aria-hidden="true"
      className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-corpo-denso font-semibold uppercase ${classe}`}
    >
      {iniciaisDoNome(funcionario.nome)}
    </span>
  );
}

/** Botão de ação da linha. Ícone com rótulo de leitor de tela (nome do funcionário) e dica ao passar o mouse. */
function BotaoAcaoLinha({
  titulo,
  nomeFuncionario,
  icone,
  desabilitado,
  destrutiva,
  aoClicar,
}: {
  titulo: string;
  nomeFuncionario: string;
  icone: string;
  desabilitado: boolean;
  destrutiva?: boolean;
  aoClicar: (gatilho: HTMLElement) => void;
}) {
  const cores = destrutiva
    ? 'hover:bg-status-perigo-fundo hover:text-status-perigo'
    : 'hover:bg-steel-alto hover:text-navy';
  return (
    <button
      type="button"
      title={titulo}
      aria-label={mensagemEquipeAcaoDaLinha(titulo, nomeFuncionario)}
      disabled={desabilitado}
      onClick={(evento) => aoClicar(evento.currentTarget)}
      className={`rounded p-1.5 text-texto-suave transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${cores}`}
    >
      <Icone nome={icone} tamanho={18} />
    </button>
  );
}

/** Tabela da equipe, com as colunas do desenho 41. Paginação e estados ficam com a página. */
export function TabelaEquipe({ itens, carregando, vazio, idEmAndamento, aoEditar, aoDesativar, aoReativar }: TabelaEquipeProps) {
  const colunas: ColunaTabela<FuncionarioEquipe>[] = [
    {
      chave: 'funcionario',
      titulo: MENSAGEM_EQUIPE_COLUNA_FUNCIONARIO,
      renderizar: (funcionario) => (
        <div className="flex min-w-0 items-center gap-3">
          <Avatar funcionario={funcionario} />
          <div className="flex min-w-0 flex-col">
            <div className="flex flex-wrap items-center gap-2">
              <span className={`font-semibold ${funcionario.ativo ? 'text-navy' : 'text-texto-suave'}`}>
                {funcionario.nome}
              </span>
              {funcionario.ehVoce && (
                <span className="rounded bg-navy px-1.5 py-0.5 text-legenda font-semibold uppercase tracking-wide text-branco">
                  {MENSAGEM_EQUIPE_SELO_VOCE}
                </span>
              )}
            </div>
            <span className="truncate text-legenda text-texto-suave">{funcionario.email ?? VALOR_AUSENTE}</span>
          </div>
        </div>
      ),
    },
    {
      chave: 'cpf',
      titulo: MENSAGEM_EQUIPE_COLUNA_CPF,
      renderizar: (funcionario) => <span className="tabular-nums text-texto-suave">{funcionario.cpfMascarado}</span>,
      classe: 'whitespace-nowrap',
    },
    {
      chave: 'matricula',
      titulo: MENSAGEM_EQUIPE_COLUNA_MATRICULA,
      renderizar: (funcionario) => <span className="tabular-nums text-texto-suave">{funcionario.matricula}</span>,
      classe: 'whitespace-nowrap',
    },
    {
      chave: 'cargo',
      titulo: MENSAGEM_EQUIPE_COLUNA_CARGO,
      renderizar: (funcionario) => <EtiquetaCargo cargo={funcionario.cargo} />,
      classe: 'whitespace-nowrap',
    },
    {
      chave: 'admissao',
      titulo: MENSAGEM_EQUIPE_COLUNA_ADMISSAO,
      renderizar: (funcionario) => (
        <span className="tabular-nums text-texto-suave">{formatarData(funcionario.dataAdmissao)}</span>
      ),
      classe: 'whitespace-nowrap',
    },
    {
      chave: 'situacao',
      titulo: MENSAGEM_EQUIPE_COLUNA_SITUACAO,
      renderizar: (funcionario) =>
        funcionario.ativo ? (
          <Etiqueta rotulo={MENSAGEM_EQUIPE_SITUACAO_ATIVO} tom="sucesso" />
        ) : (
          <Etiqueta rotulo={MENSAGEM_EQUIPE_SITUACAO_INATIVO} tom="neutro" />
        ),
      classe: 'whitespace-nowrap',
    },
    {
      chave: 'acoes',
      titulo: MENSAGEM_EQUIPE_COLUNA_ACOES,
      alinhamento: 'direita',
      renderizar: (funcionario) => {
        const emAndamento = idEmAndamento === funcionario.idFuncionario;
        return (
          <div className="inline-flex items-center justify-end gap-1">
            <BotaoAcaoLinha
              titulo={MENSAGEM_EQUIPE_ACAO_EDITAR}
              nomeFuncionario={funcionario.nome}
              icone="edit"
              desabilitado={emAndamento}
              aoClicar={(gatilho) => aoEditar(funcionario, gatilho)}
            />
            {podeDesativar(funcionario) && (
              <BotaoAcaoLinha
                titulo={MENSAGEM_EQUIPE_ACAO_DESATIVAR}
                nomeFuncionario={funcionario.nome}
                icone="person_off"
                desabilitado={emAndamento}
                destrutiva
                aoClicar={(gatilho) => aoDesativar(funcionario, gatilho)}
              />
            )}
            {podeReativar(funcionario) && (
              <BotaoAcaoLinha
                titulo={MENSAGEM_EQUIPE_ACAO_REATIVAR}
                nomeFuncionario={funcionario.nome}
                icone="person"
                desabilitado={emAndamento}
                aoClicar={(gatilho) => aoReativar(funcionario, gatilho)}
              />
            )}
          </div>
        );
      },
    },
  ];

  return (
    <Tabela<FuncionarioEquipe>
      colunas={colunas}
      dados={itens}
      obterChave={(funcionario) => funcionario.idFuncionario}
      legenda={MENSAGEM_EQUIPE_TABELA_LEGENDA}
      carregando={carregando}
      vazio={vazio}
    />
  );
}
