import { forwardRef, useId, type InputHTMLAttributes } from 'react';
import { ACESSO_LEMBRAR_DICA, ACESSO_LEMBRAR_ROTULO } from '@/lib/mensagens';

export type CampoLembrarDeMimProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>;

/**
 * Caixa "Lembrar de mim por 30 dias" (RF-50). Checkbox nativo dentro de um label clicável,
 * com a dica ligada por aria-describedby. Compatível com `register('lembrar')`.
 */
export const CampoLembrarDeMim = forwardRef<HTMLInputElement, CampoLembrarDeMimProps>(function CampoLembrarDeMim(
  props,
  ref,
) {
  const idGerado = useId();
  const idDica = `${idGerado}-dica`;

  return (
    <div className="flex flex-col pt-1">
      <label className="flex min-h-8 cursor-pointer select-none items-center gap-2.5">
        <input
          {...props}
          ref={ref}
          type="checkbox"
          aria-describedby={idDica}
          className="h-4 w-4 shrink-0 cursor-pointer rounded accent-status-pendente focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel disabled:cursor-not-allowed"
        />
        <span className="text-corpo-denso text-status-neutro">{ACESSO_LEMBRAR_ROTULO}</span>
      </label>
      <p id={idDica} className="pl-[26px] text-legenda text-status-neutro">
        {ACESSO_LEMBRAR_DICA}
      </p>
    </div>
  );
});
