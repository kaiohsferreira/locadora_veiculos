import { Botao } from '@/components/ui';
import {
  MENSAGEM_VEICULO_ACOES_ARIA,
  MENSAGEM_VEICULO_BOTAO_ATIVADO,
  MENSAGEM_VEICULO_BOTAO_CANCELAR,
  MENSAGEM_VEICULO_BOTAO_GRAVANDO,
  MENSAGEM_VEICULO_BOTAO_RASCUNHO,
  MENSAGEM_VEICULO_BOTAO_SALVAR_ATIVAR,
  MENSAGEM_VEICULO_BOTAO_SALVAR_EDICAO,
} from '@/lib/mensagensVeiculos';
import type { ModoFormularioVeiculo } from './tiposBlocosVeiculo';

export interface BarraAcoesVeiculoProps {
  modo: ModoFormularioVeiculo;
  /** Id do formulário que o botão principal envia (o botão fica fora do <form>, na barra). */
  formId: string;
  /** Envio em andamento: o principal mostra "Gravando..." e fica bloqueado. */
  enviando: boolean;
  /** Veículo gravado com as fotos (estado "Veículo Ativado!" do desenho): o principal fica bloqueado. */
  concluido?: boolean;
  aoCancelar: () => void;
  /** Só no cadastro: não existe rascunho no servidor; o rascunho fica neste navegador (quem chama decide). */
  aoSalvarRascunho?: () => void;
}

/**
 * Barra fixa inferior da tela 05, com três botões alinhados à direita (o texto de auditoria saiu). No cadastro há
 * "Salvar como Rascunho"; na edição, o principal vira "Salvar Alterações" e o rascunho some.
 */
export function BarraAcoesVeiculo({
  modo,
  formId,
  enviando,
  concluido = false,
  aoCancelar,
  aoSalvarRascunho,
}: BarraAcoesVeiculoProps) {
  const cadastro = modo === 'cadastro';
  let rotuloPrincipal = MENSAGEM_VEICULO_BOTAO_SALVAR_EDICAO;
  if (cadastro) rotuloPrincipal = MENSAGEM_VEICULO_BOTAO_SALVAR_ATIVAR;
  if (concluido) rotuloPrincipal = MENSAGEM_VEICULO_BOTAO_ATIVADO;
  if (enviando) rotuloPrincipal = MENSAGEM_VEICULO_BOTAO_GRAVANDO;

  return (
    <aside
      aria-label={MENSAGEM_VEICULO_ACOES_ARIA}
      className="sticky bottom-0 z-10 -mx-8 flex flex-col items-center justify-end gap-4 bg-superficie/95 px-8 py-4 shadow-barra-inferior backdrop-blur-md sm:flex-row"
    >
      <div className="flex w-full items-center justify-end gap-2 sm:w-auto">
        <Botao variante="cinzaAzulado" icone="arrow_back" onClick={aoCancelar} disabled={enviando}>
          {MENSAGEM_VEICULO_BOTAO_CANCELAR}
        </Botao>
        {cadastro && (
          <Botao variante="azulMarinho" icone="bookmark" onClick={aoSalvarRascunho}>
            {MENSAGEM_VEICULO_BOTAO_RASCUNHO}
          </Botao>
        )}
        <Botao
          type="submit"
          form={formId}
          variante={concluido ? 'azulProfundo' : 'laranjaAcao'}
          icone={concluido ? 'done_all' : 'check_circle'}
          carregando={enviando}
          disabled={concluido}
        >
          {rotuloPrincipal}
        </Botao>
      </div>
    </aside>
  );
}
