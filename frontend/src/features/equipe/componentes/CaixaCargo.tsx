import { Icone } from '@/components/ui';
import { MENSAGEM_EQUIPE_CAIXA_CARGO } from '@/lib/mensagens';
import type { CargoFuncionario } from '@/types/enums';
import { RESPONSABILIDADES_CARGO } from '../textosCargo';

export interface CaixaCargoProps {
  cargo: CargoFuncionario;
}

/** "O que este cargo pode fazer": lista fixa por cargo, como no desenho 41. */
export function CaixaCargo({ cargo }: CaixaCargoProps) {
  return (
    <section className="rounded-cartao bg-steel-fundo p-4">
      <p className="flex items-center gap-2 text-corpo-denso font-semibold text-navy">
        <Icone nome="verified_user" tamanho={18} className="text-orange-escuro" />
        {MENSAGEM_EQUIPE_CAIXA_CARGO}
      </p>
      <ul className="mt-3 space-y-2 text-corpo-denso text-texto-suave">
        {RESPONSABILIDADES_CARGO[cargo].map((responsabilidade) => (
          <li key={responsabilidade} className="flex items-center gap-2">
            <span className="flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-orange-claro text-orange-escuro">
              <Icone nome="check" tamanho={13} />
            </span>
            <span>{responsabilidade}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}
