import { Select } from '@/components/ui';
import { MENSAGEM_VEICULO_ROTULO_STATUS_INICIAL } from '@/lib/mensagensVeiculos';
import { STATUS_INICIAL_VEICULO_TRAVADO } from '@/lib/listasVeiculo';

/**
 * Status inicial de entrada da tela 05, travado: o veículo nasce disponível (RF-02). A lista tem uma opção só e fica
 * desabilitada, então nada é escolhido nem enviado: o servidor decide o status no cadastro.
 */
export function SeletorStatusInicialVeiculo() {
  return (
    <Select
      variante="desenho"
      destaque
      name="statusInicial"
      rotulo={MENSAGEM_VEICULO_ROTULO_STATUS_INICIAL}
      opcoes={[STATUS_INICIAL_VEICULO_TRAVADO]}
      opcaoVazia={false}
      defaultValue={STATUS_INICIAL_VEICULO_TRAVADO.valor}
      disabled
      ocultarAsterisco
    />
  );
}
