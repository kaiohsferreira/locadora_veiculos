import { useLocation } from 'react-router-dom';
import { BotaoLink } from '@/components/ui/Botao';
import { Icone } from '@/components/ui/Icone';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { MENSAGEM_PAGINA_NAO_ENCONTRADA } from '@/lib/mensagens';
import { CAMINHOS } from './caminhos';

/** Rota 404, dentro do layout da área em que o endereço foi digitado. */
export function PaginaNaoEncontrada() {
  useTituloDocumento('Página não encontrada');
  const { pathname } = useLocation();
  const noBackOffice = pathname.startsWith(CAMINHOS.backOffice);

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-3 px-4 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-steel-claro text-steel">
        <Icone nome="search_off" tamanho={28} />
      </span>
      <p className="text-legenda font-semibold uppercase tracking-wider text-texto-suave">Erro 404</p>
      <h1 className="text-titulo text-navy">Página não encontrada</h1>
      <p className="max-w-md text-corpo text-texto-suave">{MENSAGEM_PAGINA_NAO_ENCONTRADA}</p>
      <BotaoLink
        to={noBackOffice ? CAMINHOS.frota : CAMINHOS.inicio}
        variante="secundario"
        icone="arrow_back"
        className="mt-4"
      >
        {noBackOffice ? 'Voltar ao painel da frota' : 'Voltar para o início'}
      </BotaoLink>
    </div>
  );
}
