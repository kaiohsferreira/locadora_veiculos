import { Icone } from '@/components/ui';
import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import { MENSAGEM_VISTORIA_TRILHA_ROTULO } from '@/lib/mensagensVistoriaAvarias';
import { etapasDaTrilha, rotuloDaEtapa } from './etapasTrilha';

export interface TrilhaVistoriaProps {
  /** Com a vistoria registrada, a etapa 2 fica concluída e a 3 passa a ser a atual (a tela avança para o contrato). */
  vistoriaRegistrada: boolean;
}

/** Trilha de quatro etapas do desenho 19 (cabeçalho do corpo da tela). */
export function TrilhaVistoria({ vistoriaRegistrada }: TrilhaVistoriaProps) {
  const etapas = etapasDaTrilha(vistoriaRegistrada);
  return (
    <ol aria-label={MENSAGEM_VISTORIA_TRILHA_ROTULO} className="grid grid-cols-1 gap-4 rounded-xl bg-branco p-4 shadow-sm sm:grid-cols-2 lg:grid-cols-4">
      {etapas.map((etapa) => {
        const nome = MENSAGENS_RETIRADA.passos[etapa.numero - 1];
        const concluida = etapa.situacao === 'concluida';
        const atual = etapa.situacao === 'atual';
        return (
          <li
            key={etapa.numero}
            aria-current={atual ? 'step' : undefined}
            className={`flex items-center gap-3 rounded-lg p-2 ${atual ? 'bg-orange-acao/10' : concluida ? 'bg-steel-fundo/60' : 'opacity-60'}`}
          >
            <span
              aria-hidden="true"
              className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-xl font-bold text-dado-tabular ${
                concluida ? 'bg-vistoria-conferido text-branco' : atual ? 'bg-orange-acao text-branco shadow-md ring-4 ring-orange-acao/20' : 'bg-steel-alto text-texto-apoio'
              }`}
            >
              {concluida ? <Icone nome="check" tamanho={18} /> : etapa.numero}
            </span>
            <span className="flex flex-col">
              <span className={`text-micro font-semibold uppercase ${concluida ? 'text-vistoria-conferido' : atual ? 'text-frota-oficina' : 'text-texto-apoio'}`}>
                {rotuloDaEtapa(etapa)}
              </span>
              <span className="text-corpo-denso font-semibold text-texto-desenho">{nome}</span>
            </span>
          </li>
        );
      })}
    </ol>
  );
}
