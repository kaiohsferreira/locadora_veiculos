import { useId, type Ref } from 'react';
import { Icone } from '@/components/ui';
import { ACESSO_OPCAO_CLIENTE, ACESSO_OPCAO_EQUIPE, ACESSO_ROTULO_PERFIL } from '@/lib/mensagens';
import type { PerfilApi } from '@/types/autenticacao';

export interface SeletorPerfilProps {
  /** Perfil escolhido agora. */
  valor: PerfilApi;
  aoMudar: (perfil: PerfilApi) => void;
  /** Mensagem de erro do campo (ex.: perfil não informado). */
  erro?: string;
  /** Ref do campo para o foco (ex.: `field.ref` do Controller). Vai para a opção marcada. */
  campoRef?: Ref<HTMLInputElement>;
}

interface OpcaoPerfil {
  valor: PerfilApi;
  rotulo: string;
  icone: string;
}

const OPCOES_PERFIL: readonly OpcaoPerfil[] = [
  { valor: 'CLIENTE', rotulo: ACESSO_OPCAO_CLIENTE, icone: 'person' },
  { valor: 'FUNCIONARIO', rotulo: ACESSO_OPCAO_EQUIPE, icone: 'badge' },
];

/**
 * Escolha entre Cliente e Equipe. Grupo de rádios nativos (setas e Tab funcionam pelo
 * teclado). O estado escolhido aparece também pelo ícone e pelo texto, não só pela cor.
 */
export function SeletorPerfil({ valor, aoMudar, erro, campoRef }: SeletorPerfilProps) {
  const idGerado = useId();
  const idRotulo = `${idGerado}-rotulo`;
  const idErro = `${idGerado}-erro`;
  const nomeGrupo = `${idGerado}-perfil`;

  return (
    <div className="flex flex-col gap-1.5">
      <span id={idRotulo} className="block text-corpo-denso font-semibold text-navy">
        {ACESSO_ROTULO_PERFIL}
      </span>

      <div
        role="radiogroup"
        aria-labelledby={idRotulo}
        aria-required="true"
        aria-describedby={erro ? idErro : undefined}
        className="grid grid-cols-2 gap-2"
      >
        {OPCOES_PERFIL.map((opcao) => {
          const selecionado = valor === opcao.valor;
          return (
            <label
              key={opcao.valor}
              className={
                'flex h-11 cursor-pointer items-center gap-2 rounded px-3.5 text-corpo-denso font-medium shadow-sm transition-all ' +
                'focus-within:ring-2 focus-within:ring-steel ' +
                (selecionado
                  ? 'bg-steel-claro text-navy ring-2 ring-navy'
                  : 'bg-branco text-texto hover:bg-steel-claro/50')
              }
            >
              <input
                ref={selecionado ? campoRef : undefined}
                type="radio"
                name={nomeGrupo}
                value={opcao.valor}
                checked={selecionado}
                onChange={() => aoMudar(opcao.valor)}
                className="sr-only"
              />
              <Icone nome={opcao.icone} tamanho={20} className="text-texto-suave" />
              <span>{opcao.rotulo}</span>
              {selecionado && <Icone nome="check_circle" tamanho={18} className="ml-auto text-navy" />}
            </label>
          );
        })}
      </div>

      {erro && (
        <p id={idErro} role="alert" className="flex items-center gap-1 text-legenda font-medium text-status-perigo">
          <Icone nome="error" tamanho={14} />
          {erro}
        </p>
      )}
    </div>
  );
}
