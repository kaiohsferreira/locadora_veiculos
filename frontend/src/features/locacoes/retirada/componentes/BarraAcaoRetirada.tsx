import { Botao, Icone } from '@/components/ui';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { ID_FORMULARIO_RETIRADA } from './ConferenciaRetirada';

export interface BarraAcaoRetiradaProps {
  /** Mostra "Pronto para despacho de pátio" (só com os três itens e a retirada disponível). */
  pronto: boolean;
  /** Liga o botão principal. */
  ligado: boolean;
  carregando: boolean;
}

/**
 * Barra fixa do rodapé do desenho 16. O botão principal grava a locação (POST) e segue para a vistoria. O botão de
 * "Salvar Rascunho" do desenho foi cortado: a conferência não é gravada.
 */
export function BarraAcaoRetirada({ pronto, ligado, carregando }: BarraAcaoRetiradaProps) {
  return (
    <div className="sticky bottom-0 z-30 -mx-8 mt-8 flex flex-col items-center justify-between gap-4 bg-superficie/95 px-8 py-4 shadow-lg backdrop-blur-md sm:flex-row">
      <div className="flex items-center gap-2">
        {pronto && (
          <>
            <Icone nome="task_alt" tamanho={18} className="text-frota-disponivel" />
            <span className="text-rotulo font-semibold text-texto-apoio">{M.prontoParaDespacho}</span>
          </>
        )}
      </div>
      <div className="flex w-full justify-end sm:w-auto">
        <Botao
          variante="laranjaAcao"
          type="submit"
          form={ID_FORMULARIO_RETIRADA}
          disabled={!ligado}
          carregando={carregando}
          iconeDireita="arrow_forward"
          className="w-full gap-2 !shadow-md active:scale-95 enabled:hover:!bg-frota-oficina disabled:opacity-60 sm:w-auto"
        >
          {M.botaoPrincipal}
        </Botao>
      </div>
    </div>
  );
}
