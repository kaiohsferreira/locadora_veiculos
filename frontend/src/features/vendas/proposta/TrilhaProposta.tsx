import { Link } from 'react-router-dom';
import { Icone } from '@/components/ui';
import {
  PROPOSTA_TRILHA_ATUAL,
  PROPOSTA_TRILHA_CATALOGO,
  PROPOSTA_TRILHA_INICIO,
  PROPOSTA_TRILHA_ROTULO,
} from '@/lib/mensagensProposta';
import { CAMINHOS } from '@/routes/caminhos';

export interface TrilhaPropostaProps {
  /** Marca e modelo do carro; sem carro (carregando ou fora do catálogo), o item some. */
  nomeVeiculo: string | null;
}

function Separador() {
  return <Icone nome="chevron_right" tamanho={14} className="text-contorno" />;
}

/** Trilha do desenho 31: Início > Carros à venda > (carro) > Enviar Proposta de Compra. */
export function TrilhaProposta({ nomeVeiculo }: TrilhaPropostaProps) {
  return (
    <nav aria-label={PROPOSTA_TRILHA_ROTULO} className="flex flex-wrap items-center gap-1 text-legenda text-texto-suave">
      <Link to={CAMINHOS.inicio} className="hover:text-navy">
        {PROPOSTA_TRILHA_INICIO}
      </Link>
      <Separador />
      <Link to={CAMINHOS.seminovos} className="hover:text-navy">
        {PROPOSTA_TRILHA_CATALOGO}
      </Link>
      {nomeVeiculo && (
        <>
          <Separador />
          <span className="font-medium text-texto">{nomeVeiculo}</span>
        </>
      )}
      <Separador />
      <span aria-current="page" className="font-semibold text-orange-escuro">
        {PROPOSTA_TRILHA_ATUAL}
      </span>
    </nav>
  );
}
