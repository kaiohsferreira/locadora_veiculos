import { useId } from 'react';
import { Icone } from '@/components/ui';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { OPCOES_SIMULACAO_CAUCAO, type EscolhaSimulacaoCaucao } from '../regrasCaucaoRetirada';

export interface SimulacaoCaucaoRetiradaProps {
  /** Saída escolhida; nula até o atendente escolher (nenhuma vem marcada). */
  escolha: EscolhaSimulacaoCaucao | null;
  aoEscolher: (escolha: EscolhaSimulacaoCaucao) => void;
}

/**
 * Painel "Simulação do cartão" do item da pré-autorização (locacao.contract.json v4, caucao_na_retirada_v4.tela_16_v4,
 * bloco 42). Não é interruptor: é um grupo de rádios nativos com legenda (setas e Tab funcionam pelo teclado). A escolha
 * só diz o que o provedor de simulação vai responder; a pré-autorização acontece no clique do botão principal. Fica dentro
 * do fieldset da conferência, então é desabilitado junto com ela (envio, recarga ou retirada indisponível). A opção
 * escolhida aparece também pelo ícone, não só pela cor.
 */
export function SimulacaoCaucaoRetirada({ escolha, aoEscolher }: SimulacaoCaucaoRetiradaProps) {
  const nomeGrupo = useId();
  return (
    <fieldset className="mt-2 min-w-0">
      <legend className="text-micro font-semibold uppercase text-texto-apoio">{M.simulacaoTitulo}</legend>
      <div className="mt-2 flex flex-col gap-2">
        <p className="flex items-start gap-2 text-apoio text-texto-apoio">
          <Icone nome="info" tamanho={16} />
          <span>{M.simulacaoAviso}</span>
        </p>
        <div className="flex flex-wrap gap-2">
          {OPCOES_SIMULACAO_CAUCAO.map((opcao) => {
            const marcada = escolha === opcao.valor;
            return (
              <label
                key={opcao.valor}
                className={`flex cursor-pointer items-center gap-1.5 rounded px-3 py-1.5 text-rotulo font-semibold transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-orange-acao has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-60 ${
                  marcada ? 'bg-navy-profundo text-branco' : 'bg-steel-alto text-texto-desenho'
                }`}
              >
                <input
                  type="radio"
                  name={nomeGrupo}
                  value={opcao.valor}
                  checked={marcada}
                  onChange={() => aoEscolher(opcao.valor)}
                  className="sr-only"
                />
                <Icone nome={marcada ? 'radio_button_checked' : 'radio_button_unchecked'} tamanho={16} />
                <span>{opcao.rotulo}</span>
              </label>
            );
          })}
        </div>
      </div>
    </fieldset>
  );
}
