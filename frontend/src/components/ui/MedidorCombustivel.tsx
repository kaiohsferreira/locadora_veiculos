import { SEGMENTOS_COMBUSTIVEL, SEPARADOR_COMBUSTIVEL_PADRAO, descreverNivelCombustivel } from './calculoCombustivel';

export interface MedidorCombustivelProps {
  /** Nível exato de 0 a 1 (o valor gravado). O medidor mostra o oitavo mais próximo. */
  nivel: number;
  /** Separador entre o oitavo e o nome do ponto: ' • ' (tela 19, padrão) ou ' - ' (tela 06). */
  separador?: string;
  className?: string;
}

/**
 * Medidor de combustível em oitavos (desenho 06 e 19). É só visual: os 8 segmentos são decorativos (aria-hidden) e o
 * texto ao lado (oitavo, nome do ponto e percentual exato) é o que o leitor de tela anuncia.
 * Sem animação; a transição de cor é desligada com motion-reduce.
 */
export function MedidorCombustivel({
  nivel,
  separador = SEPARADOR_COMBUSTIVEL_PADRAO,
  className = '',
}: MedidorCombustivelProps) {
  const descricao = descreverNivelCombustivel(nivel, separador);
  const segmentos = Array.from({ length: SEGMENTOS_COMBUSTIVEL }, (_, indice) => indice < descricao.oitavos);

  return (
    <div className={`flex flex-col gap-2 ${className}`}>
      <div className="grid h-3 grid-cols-8 gap-1.5" aria-hidden="true">
        {segmentos.map((cheio, indice) => (
          <span
            key={indice}
            className={`transition-colors motion-reduce:transition-none ${cheio ? 'bg-navy-profundo' : 'bg-steel-alto'}`}
          />
        ))}
      </div>
      <p className="text-dado-tabular font-semibold text-texto-desenho">{descricao.texto}</p>
    </div>
  );
}
