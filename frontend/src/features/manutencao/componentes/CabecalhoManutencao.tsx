import { Botao, Icone } from '@/components/ui';
import {
  MENSAGEM_MANUTENCAO_BOTAO_ABRIR_ORDEM,
  MENSAGEM_MANUTENCAO_DESCRICAO,
  MENSAGEM_MANUTENCAO_TITULO,
  MENSAGEM_MANUTENCAO_TRILHA_CONTROLE,
  MENSAGEM_MANUTENCAO_TRILHA_OPERACOES,
} from '@/lib/mensagens';

export interface CabecalhoManutencaoProps {
  /** Abre o modal de abrir ordem sem veículo escolhido. */
  aoAbrirOrdem: () => void;
}

/**
 * Cabeçalho da tela 14: trilha, título, descrição e o botão "Abrir ordem" (laranja, o do desenho). "Exportar Relatório"
 * e "Agendar manutenção" foram cortados pelo P7 (o último virou "Abrir ordem").
 */
export function CabecalhoManutencao({ aoAbrirOrdem }: CabecalhoManutencaoProps) {
  return (
    <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
      <div className="flex min-w-0 flex-col gap-1">
        <p className="flex flex-wrap items-center gap-2 text-micro uppercase tracking-wider text-contorno">
          <span>{MENSAGEM_MANUTENCAO_TRILHA_OPERACOES}</span>
          <Icone nome="chevron_right" tamanho={14} />
          <span>{MENSAGEM_MANUTENCAO_TRILHA_CONTROLE}</span>
        </p>
        <h1 className="text-titulo-hero font-bold tracking-tight text-texto-desenho">{MENSAGEM_MANUTENCAO_TITULO}</h1>
        <p className="max-w-3xl text-corpo-denso text-texto-apoio">{MENSAGEM_MANUTENCAO_DESCRICAO}</p>
      </div>
      <div className="flex shrink-0 items-center gap-2 self-start md:self-auto">
        <Botao variante="laranjaAcao" icone="add_circle" onClick={aoAbrirOrdem}>
          {MENSAGEM_MANUTENCAO_BOTAO_ABRIR_ORDEM}
        </Botao>
      </div>
    </header>
  );
}
