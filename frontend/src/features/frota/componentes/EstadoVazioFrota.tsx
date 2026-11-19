import { Botao, BotaoLink, EstadoVazio } from '@/components/ui';
import {
  MENSAGEM_FROTA_BOTAO_CADASTRAR,
  MENSAGEM_FROTA_LIMPAR_FILTROS,
  MENSAGEM_FROTA_VAZIA_COM_FILTRO_DESCRICAO,
  MENSAGEM_FROTA_VAZIA_COM_FILTRO_TITULO,
  MENSAGEM_FROTA_VAZIA_SEM_FILTRO_DESCRICAO,
  MENSAGEM_FROTA_VAZIA_SEM_FILTRO_TITULO,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';

export interface EstadoVazioFrotaProps {
  /** Há busca ou filtro aplicado: a mensagem diz para ajustar, e o botão limpa os filtros. */
  comFiltro: boolean;
  aoLimpar: () => void;
}

/** Lista vazia da frota: sem filtro, convida a cadastrar; com filtro, oferece limpar os filtros. */
export function EstadoVazioFrota({ comFiltro, aoLimpar }: EstadoVazioFrotaProps) {
  if (comFiltro) {
    return (
      <EstadoVazio
        titulo={MENSAGEM_FROTA_VAZIA_COM_FILTRO_TITULO}
        descricao={MENSAGEM_FROTA_VAZIA_COM_FILTRO_DESCRICAO}
        acao={
          <Botao variante="contorno" onClick={aoLimpar}>
            {MENSAGEM_FROTA_LIMPAR_FILTROS}
          </Botao>
        }
      />
    );
  }

  return (
    <EstadoVazio
      titulo={MENSAGEM_FROTA_VAZIA_SEM_FILTRO_TITULO}
      descricao={MENSAGEM_FROTA_VAZIA_SEM_FILTRO_DESCRICAO}
      acao={
        <BotaoLink to={CAMINHOS.frotaNovoVeiculo} icone="add">
          {MENSAGEM_FROTA_BOTAO_CADASTRAR}
        </BotaoLink>
      }
    />
  );
}
