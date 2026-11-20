import { BotaoLink, EstadoErro, EstadoVazio, Paginacao } from '@/components/ui';
import { CartaoReservaAtiva } from '@/features/area-cliente/reservas/componentes/CartaoReservaAtiva';
import { EsqueletoReservaAtiva } from '@/features/area-cliente/reservas/componentes/EsqueletoReservaAtiva';
import type { EstadoListaReservas } from '@/features/area-cliente/reservas/estadoLista';
import { TAMANHO_PAGINA_ATIVAS } from '@/features/area-cliente/reservas/parametrosReservas';
import { obterMensagemErro } from '@/lib/erros';
import {
  MENSAGEM_RESERVAS_BUSCAR_VEICULO,
  MENSAGEM_RESERVAS_ERRO_ATUALIZACAO,
  MENSAGEM_RESERVAS_ERRO_TITULO,
  MENSAGEM_RESERVAS_RODAPE_ITENS,
  MENSAGEM_RESERVAS_VAZIO_ATIVAS_DESCRICAO,
  MENSAGEM_RESERVAS_VAZIO_ATIVAS_TITULO,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import type { ReservaCliente } from '@/types/reservas';

export interface PainelReservasAtivasProps {
  estado: EstadoListaReservas;
  pagina: number;
  aoMudarPagina: (pagina: number) => void;
  nomeCondutor: string;
  aoCancelar: (reserva: ReservaCliente) => void;
  /** Foto de capa que falhou ao abrir. Sem ela, a imagem que falha cai direto no ícone de carro. */
  aoFalharFotoCapa?: () => void;
}

/**
 * Aba Ativas e Próximas: cartões das reservas ATIVA e CONFIRMADA, com esqueleto, vazio, erro com nova tentativa
 * e paginação de 5 em 5. Erro com dado já na tela mantém o dado e avisa.
 */
export function PainelReservasAtivas({
  estado,
  pagina,
  aoMudarPagina,
  nomeCondutor,
  aoCancelar,
  aoFalharFotoCapa,
}: PainelReservasAtivasProps) {
  const { dados, carregando, erro, atualizando, aoTentarNovamente } = estado;

  if (carregando) {
    return (
      <div aria-busy="true" className="flex flex-col gap-6">
        <EsqueletoReservaAtiva />
      </div>
    );
  }

  if (erro && !dados) {
    return (
      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        <EstadoErro
          titulo={MENSAGEM_RESERVAS_ERRO_TITULO}
          mensagem={obterMensagemErro(erro)}
          aoTentarNovamente={aoTentarNovamente}
          tentando={atualizando}
        />
      </section>
    );
  }

  if (!dados || dados.total === 0) {
    return (
      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        <EstadoVazio
          titulo={MENSAGEM_RESERVAS_VAZIO_ATIVAS_TITULO}
          descricao={MENSAGEM_RESERVAS_VAZIO_ATIVAS_DESCRICAO}
          acao={
            <BotaoLink to={CAMINHOS.inicio} variante="contorno">
              {MENSAGEM_RESERVAS_BUSCAR_VEICULO}
            </BotaoLink>
          }
        />
      </section>
    );
  }

  return (
    <div aria-busy={atualizando} className="flex flex-col gap-6">
      {erro ? (
        <p role="alert" className="rounded-cartao bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
          {MENSAGEM_RESERVAS_ERRO_ATUALIZACAO}
        </p>
      ) : null}

      {dados.itens.map((reserva) => (
        <CartaoReservaAtiva
          key={reserva.idReserva}
          reserva={reserva}
          nomeCondutor={nomeCondutor}
          aoCancelar={aoCancelar}
          aoFalharFoto={aoFalharFotoCapa}
        />
      ))}

      <div className="overflow-hidden rounded-cartao bg-branco shadow-cartao [&_nav>ul]:flex-wrap">
        <Paginacao
          pagina={pagina}
          tamanhoPagina={TAMANHO_PAGINA_ATIVAS}
          total={dados.total}
          aoMudarPagina={aoMudarPagina}
          desabilitada={atualizando}
          rotuloItens={MENSAGEM_RESERVAS_RODAPE_ITENS}
        />
      </div>
    </div>
  );
}
