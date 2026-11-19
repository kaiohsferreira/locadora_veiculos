import { BotaoLink } from '@/components/ui';
import {
  MENSAGEM_FROTA_BOTAO_CADASTRAR,
  MENSAGEM_FROTA_DESCRICAO,
  MENSAGEM_FROTA_KICKER,
  MENSAGEM_FROTA_TITULO_PAGINA,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import { AlternanciaVisaoFrota } from './AlternanciaVisaoFrota';

/**
 * Cabeçalho das duas telas da frota (11 e 12): kicker, título e descrição, à esquerda; a alternância Painel | Lista e o
 * botão de cadastrar, à direita. Textos iguais nas duas telas (mesmas constantes).
 */
export function CabecalhoFrota() {
  return (
    <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
      <div className="flex flex-col">
        <p className="mb-1 text-micro uppercase tracking-wider text-texto-apoio">
          <span className="font-semibold text-orange-acao">{MENSAGEM_FROTA_KICKER}</span>
        </p>
        <h1 className="text-titulo-acesso font-bold tracking-tight text-texto-desenho">{MENSAGEM_FROTA_TITULO_PAGINA}</h1>
        <p className="mt-0.5 text-corpo-denso text-texto-apoio">{MENSAGEM_FROTA_DESCRICAO}</p>
      </div>
      <div className="flex shrink-0 items-center gap-2 self-start md:self-auto">
        <AlternanciaVisaoFrota />
        <BotaoLink to={CAMINHOS.frotaNovoVeiculo} icone="add" variante="frota">
          {MENSAGEM_FROTA_BOTAO_CADASTRAR}
        </BotaoLink>
      </div>
    </header>
  );
}
