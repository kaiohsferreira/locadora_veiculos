import { NavLink } from 'react-router-dom';
import {
  MENSAGEM_FROTA_ALTERNANCIA_GRUPO,
  MENSAGEM_FROTA_ALTERNANCIA_LISTA,
  MENSAGEM_FROTA_ALTERNANCIA_PAINEL,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';

/** Opção do controle: a marcada tem fundo claro e `aria-current="page"` (o NavLink põe o atributo sozinho). */
const CLASSES_OPCAO =
  'inline-flex h-8 items-center rounded px-4 text-corpo-denso transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao';

/**
 * Alternância "Painel | Lista" do cabeçalho das telas 11 e 12, no lugar do "Exportar Relatório" do desenho. Cada opção é
 * um link de rota; a troca mantém os filtros (ver ProvedorFiltrosFrota).
 */
export function AlternanciaVisaoFrota() {
  return (
    <fieldset className="inline-flex h-10 items-center gap-1 rounded bg-steel-fundo p-1">
      <legend className="sr-only">{MENSAGEM_FROTA_ALTERNANCIA_GRUPO}</legend>
      <NavLink
        to={CAMINHOS.frota}
        end
        className={({ isActive }) =>
          `${CLASSES_OPCAO} ${isActive ? 'bg-branco font-semibold text-texto-desenho shadow-sm' : 'font-medium text-texto-apoio hover:bg-steel-suave hover:text-texto-desenho'}`
        }
      >
        {MENSAGEM_FROTA_ALTERNANCIA_PAINEL}
      </NavLink>
      <NavLink
        to={CAMINHOS.frotaVeiculos}
        end
        className={({ isActive }) =>
          `${CLASSES_OPCAO} ${isActive ? 'bg-branco font-semibold text-texto-desenho shadow-sm' : 'font-medium text-texto-apoio hover:bg-steel-suave hover:text-texto-desenho'}`
        }
      >
        {MENSAGEM_FROTA_ALTERNANCIA_LISTA}
      </NavLink>
    </fieldset>
  );
}
