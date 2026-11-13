import { Link } from 'react-router-dom';
import { Icone } from '@/components/ui';
import { ACESSO_FAIXA_ROTULO, ACESSO_FAIXA_VOLTAR, mensagemRodapeAcesso } from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';

/** Ano exibido na faixa inferior (calculado uma vez, ao carregar a aplicação). */
const ANO_ATUAL = new Date().getFullYear();

/** Faixa superior da coluna do formulário: rótulo à esquerda e link para o início à direita. */
export function FaixaSuperiorAcesso() {
  return (
    <div className="flex w-full items-center justify-between gap-4 pb-6">
      <span className="text-micro uppercase tracking-wider text-status-neutro">{ACESSO_FAIXA_ROTULO}</span>
      <Link
        to={CAMINHOS.inicio}
        className="inline-flex items-center gap-1.5 rounded text-rotulo text-navy transition-colors hover:text-status-pendente focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel"
      >
        <Icone nome="arrow_back" tamanho={18} />
        {ACESSO_FAIXA_VOLTAR}
      </Link>
    </div>
  );
}

/** Faixa inferior da coluna do formulário: linha de rodapé. */
export function FaixaInferiorAcesso() {
  return (
    <footer className="w-full pt-6 text-center sm:text-left">
      <p className="text-legenda text-status-neutro">{mensagemRodapeAcesso(ANO_ATUAL)}</p>
    </footer>
  );
}
