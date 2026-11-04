import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGEM_TELA_EM_CONSTRUCAO } from '@/lib/mensagens';
import { Cartao } from './ui/Cartao';
import { Etiqueta } from './ui/Etiqueta';
import { Icone } from './ui/Icone';

export interface ReferenciaPrototipo {
  /** Código da tarefa no plano (ex.: "P3-FE-07"). */
  codigoTarefa: string;
  /** Arquivo do protótipo (ex.: "02_aluguel_de_carros.png"). */
  prototipo: string;
}

export interface PaginaEsqueletoProps extends ReferenciaPrototipo {
  /** Título da tela. */
  titulo: string;
  /** Descrição da tela, como está no plano. */
  descricao?: string;
  /** Área da aplicação: define o espaçamento da página. */
  area: 'publica' | 'back-office';
  /** Versão mobile da mesma tela (mesma rota, layout responsivo). */
  versaoMobile?: ReferenciaPrototipo;
  /** Parâmetros da rota, exibidos para conferência durante o desenvolvimento. */
  parametros?: Readonly<Record<string, string | undefined>>;
}

/**
 * Página provisória de uma tela ainda não implementada: mostra título, código
 * da tarefa e protótipo de referência. Será substituída pela tela real.
 */
export function PaginaEsqueleto({
  titulo,
  descricao,
  codigoTarefa,
  prototipo,
  area,
  versaoMobile,
  parametros,
}: PaginaEsqueletoProps) {
  useTituloDocumento(titulo);
  const listaParametros = parametros ? Object.entries(parametros) : [];

  return (
    <div className={area === 'publica' ? 'mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 lg:px-8' : 'w-full'}>
      <Etiqueta rotulo="Em construção" tom="pendente" />
      <h1 className="mt-3 text-titulo text-navy">{titulo}</h1>
      {descricao && <p className="mt-2 max-w-3xl text-corpo text-texto-suave">{descricao}</p>}

      <Cartao className="mt-6" titulo="Referência da tela">
        <dl className="grid gap-4 text-corpo-denso sm:grid-cols-[max-content_1fr] sm:gap-x-8">
          <dt className="font-medium text-texto-suave">Tarefa e protótipo</dt>
          <dd className="break-words font-semibold text-navy">
            {codigoTarefa} · {prototipo}
          </dd>

          {versaoMobile && (
            <>
              <dt className="font-medium text-texto-suave">Versão mobile (mesma rota)</dt>
              <dd className="break-words font-semibold text-navy">
                {versaoMobile.codigoTarefa} · {versaoMobile.prototipo}
              </dd>
            </>
          )}

          {listaParametros.map(([nome, valor]) => (
            <div key={nome} className="contents">
              <dt className="font-medium text-texto-suave">Parâmetro {nome}</dt>
              <dd className="break-words font-semibold text-navy">{valor ?? VALOR_AUSENTE}</dd>
            </div>
          ))}
        </dl>

        <p className="mt-6 flex items-start gap-2 rounded-md bg-steel-claro px-4 py-3 text-corpo-denso text-navy">
          <Icone nome="info" tamanho={18} className="mt-px text-steel" />
          {MENSAGEM_TELA_EM_CONSTRUCAO}
        </p>
      </Cartao>
    </div>
  );
}
