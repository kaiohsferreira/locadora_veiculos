import { Icone } from './ui/Icone';

export interface LogotipoProps {
  /** "claro" para fundo branco (barra pública), "escuro" para fundo navy (menu do back office). */
  tema?: 'claro' | 'escuro';
  /** Linha menor abaixo do nome (ex.: "Back office"). */
  subtitulo?: string;
}

/** Marca da AutoVia: ícone em destaque laranja + nome. */
export function Logotipo({ tema = 'claro', subtitulo }: LogotipoProps) {
  return (
    <span className="flex items-center gap-3">
      <span className="flex h-9 w-9 items-center justify-center rounded-md bg-orange text-branco">
        <Icone nome="directions_car" tamanho={22} preenchido />
      </span>
      <span className="flex flex-col leading-tight">
        <span className={`text-corpo font-bold ${tema === 'escuro' ? 'text-branco' : 'text-navy'}`}>AutoVia</span>
        {subtitulo && (
          <span
            className={`text-legenda font-medium uppercase tracking-wider ${tema === 'escuro' ? 'text-branco/70' : 'text-texto-suave'}`}
          >
            {subtitulo}
          </span>
        )}
      </span>
    </span>
  );
}
