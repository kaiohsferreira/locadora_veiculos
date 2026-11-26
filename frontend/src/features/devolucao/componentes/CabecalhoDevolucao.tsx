import { Link } from 'react-router-dom';
import { Etiqueta, Icone } from '@/components/ui';
import { PlacaRetirada } from '@/features/locacoes/retirada/componentes/PlacaRetirada';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import { CAMINHOS } from '@/routes/caminhos';
import type { ContextoDevolucao } from '@/types/devolucao';
import { estaAtrasadaDevolucao, faseDevolucao, textoEstadoDevolucao } from '../regras/regrasDevolucao';

export interface CabecalhoDevolucaoProps {
  /** Contexto carregado; sem ele, o cabeçalho mostra só a trilha e o título. */
  contexto: ContextoDevolucao | null;
}

/** Cabeçalho da tela 06: trilha, título, identificação da locação e do veículo e o selo de estado (desenho 06, aprovado). */
export function CabecalhoDevolucao({ contexto }: CabecalhoDevolucaoProps) {
  const estado = contexto ? textoEstadoDevolucao(faseDevolucao(contexto)) : null;
  const atrasada = contexto !== null && estaAtrasadaDevolucao(contexto.locacao.status);
  return (
    <header className="space-y-4">
      <nav aria-label={MENSAGENS_RETIRADA.trilhaRotulo} className="flex flex-wrap items-center gap-2 text-legenda font-semibold uppercase tracking-wider text-texto-apoio">
        <Link to={CAMINHOS.locacoes} className="rounded-sm hover:text-navy-profundo focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-navy">{MENSAGENS_RETIRADA.trilhaLocacoes}</Link>
        <Icone nome="chevron_right" tamanho={14} />
        <span>{MENSAGENS_RETIRADA.trilhaDespacho}</span>
        <Icone nome="chevron_right" tamanho={14} />
        <span aria-current="page" className="font-bold text-navy-profundo">{M.titulo}</span>
      </nav>
      <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        {/* H1 e chips na mesma linha que quebra (desenho 06: o primeiro chip fica ao lado do título). */}
        <div className="flex min-w-0 flex-wrap items-center gap-3">
          <h1 className="text-titulo-acesso font-bold tracking-tight text-navy-profundo">{M.titulo}</h1>
          {contexto && (
            <>
              <span className="inline-flex items-center gap-1 rounded-sm bg-steel-alto px-2 py-0.5 text-dado-tabular font-semibold text-navy-profundo">
                <Icone nome="contract" tamanho={16} className="text-orange-acao" />
                <span>{MENSAGENS_RETIRADA.locacaoNumero(contexto.locacao.idLocacao)}</span>
              </span>
              <span className="inline-flex items-center gap-1 rounded-sm bg-navy-profundo px-2 py-0.5 text-rotulo text-branco">
                <Icone nome="directions_car" tamanho={15} className="text-frota-oficina-fundo" />
                <span>{`${contexto.marca} ${contexto.modelo}`}</span>
              </span>
              <PlacaRetirada placa={contexto.placa} />
            </>
          )}
        </div>
        {(estado !== null || atrasada) && (
          <div className="flex flex-wrap items-center gap-2 self-start md:self-auto">
            {estado !== null && (
              <div className="flex items-center gap-2 rounded bg-steel-suave px-4 py-1.5">
                <span aria-hidden="true" className="h-2.5 w-2.5 rounded-full bg-orange-acao motion-safe:animate-pulse motion-reduce:animate-none" />
                <span className="text-rotulo font-semibold text-navy-profundo">{estado}</span>
              </div>
            )}
            {atrasada && <Etiqueta status="ATRASADA" />}
          </div>
        )}
      </div>
      <p className="max-w-4xl text-corpo-denso text-texto-apoio">{M.descricao}</p>
    </header>
  );
}
