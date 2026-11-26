import { BotaoLink, Icone } from '@/components/ui';
import { MENSAGEM_PAGAMENTOS_MIGALHA_INICIO, MENSAGEM_PAGAMENTOS_NAVEGACAO_CAMINHO } from '@/lib/mensagensPagamentosCliente';
import {
  MENSAGEM_LOCACOES_DESCRICAO,
  MENSAGEM_LOCACOES_MIGALHA_ATUAL,
  MENSAGEM_LOCACOES_NOVA_RESERVA,
  MENSAGEM_LOCACOES_TITULO_PAGINA,
} from '@/lib/mensagensLocacoesCliente';
import { CAMINHOS } from '@/routes/caminhos';

/**
 * Migalha, título, descrição e o botão "Nova Reserva" (desenho 27). A migalha fica só a partir de sm; no celular o
 * botão ocupa a largura toda, abaixo do texto, com altura mínima de 40 px.
 */
export function CabecalhoLocacoes() {
  return (
    <header className="flex flex-col gap-2">
      <nav
        aria-label={MENSAGEM_PAGAMENTOS_NAVEGACAO_CAMINHO}
        className="hidden items-center gap-2 text-rotulo font-semibold text-texto-apoio sm:flex"
      >
        <span className="flex items-center gap-1">
          <Icone nome="home" tamanho={16} />
          {MENSAGEM_PAGAMENTOS_MIGALHA_INICIO}
        </span>
        <span aria-hidden="true" className="font-bold text-contorno-suave">
          /
        </span>
        <span aria-current="page" className="font-semibold text-navy-profundo">
          {MENSAGEM_LOCACOES_MIGALHA_ATUAL}
        </span>
      </nav>

      <h1 className="break-words text-titulo tracking-tight text-navy-profundo sm:text-titulo-hero">
        {MENSAGEM_LOCACOES_TITULO_PAGINA}
      </h1>

      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <p className="max-w-2xl text-corpo-denso text-texto-apoio sm:text-corpo">{MENSAGEM_LOCACOES_DESCRICAO}</p>
        <BotaoLink
          to={CAMINHOS.inicio}
          variante="secundario"
          icone="add_circle"
          className="min-h-10 w-full sm:w-auto"
        >
          {MENSAGEM_LOCACOES_NOVA_RESERVA}
        </BotaoLink>
      </div>
    </header>
  );
}
