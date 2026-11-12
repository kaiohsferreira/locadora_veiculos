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
    <div className="flex flex-col gap-1">
      <label className="flex min-h-11 cursor-pointer items-center gap-3">
        <input
          {...props}
          ref={ref}
          type="checkbox"
          aria-describedby={idDica}
          className="h-5 w-5 shrink-0 cursor-pointer accent-navy focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel disabled:cursor-not-allowed"
        />
        <span className="text-corpo-denso font-medium text-texto">{ACESSO_LEMBRAR_ROTULO}</span>
      </label>
      <p id={idDica} className="pl-8 text-legenda text-texto-suave">
        {ACESSO_LEMBRAR_DICA}
      </p>
    </div>
  );
});
