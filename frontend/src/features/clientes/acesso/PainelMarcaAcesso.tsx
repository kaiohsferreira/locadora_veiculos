import { Logotipo } from '@/components/Logotipo';
import { Icone } from '@/components/ui';
import {
  ACESSO_DESCRICAO_PAINEL,
  ACESSO_DESTAQUE_CADASTRO_TEXTO,
  ACESSO_DESTAQUE_CADASTRO_TITULO,
  ACESSO_DESTAQUE_RESERVA_TEXTO,
  ACESSO_DESTAQUE_RESERVA_TITULO,
  ACESSO_DESTAQUE_SEMINOVOS_TEXTO,
  ACESSO_DESTAQUE_SEMINOVOS_TITULO,
  ACESSO_FRASE_PAINEL,
} from '@/lib/mensagens';

interface DestaqueMarca {
  icone: string;
  titulo: string;
  texto: string;
}

const DESTAQUES_MARCA: readonly DestaqueMarca[] = [
  {
    icone: 'event_available',
    titulo: ACESSO_DESTAQUE_RESERVA_TITULO,
    texto: ACESSO_DESTAQUE_RESERVA_TEXTO,
  },
  {
    icone: 'verified_user',
    titulo: ACESSO_DESTAQUE_CADASTRO_TITULO,
    texto: ACESSO_DESTAQUE_CADASTRO_TEXTO,
  },
  {
    icone: 'directions_car',
    titulo: ACESSO_DESTAQUE_SEMINOVOS_TITULO,
    texto: ACESSO_DESTAQUE_SEMINOVOS_TEXTO,
  },
];

/**
 * Painel esquerdo da tela de acesso: marca, frase principal e três destaques.
 * Aparece só em telas grandes (lg+); no celular a tela mostra só o formulário.
 */
export function PainelMarcaAcesso() {
  return (
    <aside
      aria-label={ACESSO_DESCRICAO_PAINEL}
      className="hidden flex-col justify-between gap-12 bg-navy px-12 py-16 text-branco lg:flex"
    >
      <Logotipo tema="escuro" />

      <div className="flex flex-col gap-10">
        <p className="max-w-lg text-titulo text-branco">{ACESSO_FRASE_PAINEL}</p>

        <ul className="flex max-w-lg flex-col gap-4">
          {DESTAQUES_MARCA.map((destaque) => (
            <li
              key={destaque.titulo}
              className="flex items-start gap-4 rounded-cartao border border-branco/15 bg-branco/5 p-4"
            >
              <Icone nome={destaque.icone} tamanho={24} className="mt-0.5 text-orange" />
              <div className="flex flex-col gap-1">
                <p className="text-corpo font-semibold text-branco">{destaque.titulo}</p>
                <p className="text-corpo-denso text-branco/80">{destaque.texto}</p>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </aside>
  );
}
