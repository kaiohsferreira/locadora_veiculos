import { Link } from 'react-router-dom';
import { Icone } from '@/components/ui';
import {
  MENSAGEM_VEICULO_APOIO_CADASTRAR,
  MENSAGEM_VEICULO_APOIO_EDITAR,
  MENSAGEM_VEICULO_TITULO_CADASTRAR,
  MENSAGEM_VEICULO_TITULO_EDITAR,
  MENSAGEM_VEICULO_TRILHA_CADASTRAR,
  MENSAGEM_VEICULO_TRILHA_EDITAR,
  MENSAGEM_VEICULO_TRILHA_FROTA,
  MENSAGEM_VEICULO_TRILHA_NAVEGACAO,
} from '@/lib/mensagensVeiculos';
import type { ModoFormularioVeiculo } from './tiposBlocosVeiculo';

export interface CabecalhoCadastroVeiculoProps {
  modo: ModoFormularioVeiculo;
  /** Destino do item "Frota" da trilha (ex.: a rota da frota no back office). */
  caminhoFrota: string;
  /** Mostra o texto de apoio abaixo do título. Padrão: sim. Não mostra quando não há veículo (não encontrado). */
  mostrarApoio?: boolean;
}

/**
 * Trilha, título e texto de apoio da tela 05. Na edição o título e o último item da trilha mudam para "Editar Veículo", e
 * o texto de apoio passa a ser o da edição (decisão de 17/11/2026). Sem veículo (não encontrado), o texto de apoio não aparece.
 */
export function CabecalhoCadastroVeiculo({ modo, caminhoFrota, mostrarApoio = true }: CabecalhoCadastroVeiculoProps) {
  const edicao = modo === 'edicao';

  return (
    <div className="flex flex-col pb-6">
      <nav aria-label={MENSAGEM_VEICULO_TRILHA_NAVEGACAO} className="mb-1 flex items-center gap-1 text-rotulo text-texto-apoio">
        <Link to={caminhoFrota} className="transition-colors hover:text-navy-profundo">
          {MENSAGEM_VEICULO_TRILHA_FROTA}
        </Link>
        <Icone nome="chevron_right" tamanho={16} className="text-contorno" />
        <span aria-current="page" className="font-semibold text-status-pendente">
          {edicao ? MENSAGEM_VEICULO_TRILHA_EDITAR : MENSAGEM_VEICULO_TRILHA_CADASTRAR}
        </span>
      </nav>
      <h1 className="text-titulo-acesso tracking-tight text-texto-desenho">
        {edicao ? MENSAGEM_VEICULO_TITULO_EDITAR : MENSAGEM_VEICULO_TITULO_CADASTRAR}
      </h1>
      {mostrarApoio && (
        <p className="mt-0.5 text-corpo-denso text-texto-apoio">
          {edicao ? MENSAGEM_VEICULO_APOIO_EDITAR : MENSAGEM_VEICULO_APOIO_CADASTRAR}
        </p>
      )}
    </div>
  );
}
