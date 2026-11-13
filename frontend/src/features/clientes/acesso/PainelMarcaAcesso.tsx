import { Link } from 'react-router-dom';
import { Icone } from '@/components/ui';
import fundo from '@/assets/acesso-fundo.jpg';
import {
  ACESSO_DESCRICAO_PAINEL,
  ACESSO_DESTAQUE_CADASTRO_TEXTO,
  ACESSO_DESTAQUE_CADASTRO_TITULO,
  ACESSO_DESTAQUE_RESERVA_TEXTO,
  ACESSO_DESTAQUE_RESERVA_TITULO,
  ACESSO_DESTAQUE_SEMINOVOS_TEXTO,
  ACESSO_DESTAQUE_SEMINOVOS_TITULO,
  ACESSO_FRASE_PAINEL,
  ACESSO_LINHA_MARCA,
  ACESSO_MARCA_PREFIXO,
  ACESSO_MARCA_ROTULO_LINK,
  ACESSO_MARCA_SUFIXO,
  ACESSO_PARAGRAFO_PAINEL,
  ACESSO_SELO_PAINEL,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';

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
 * Painel esquerdo da tela de acesso: foto de fundo com degradês escuros, marca, selo, frase principal
 * e três destaques lado a lado. Aparece em todas as larguras (empilhado acima do formulário no celular).
 */
export function PainelMarcaAcesso() {
  return (
    <section
      aria-label={ACESSO_DESCRICAO_PAINEL}
      className="relative flex min-h-[460px] flex-col justify-between overflow-hidden bg-navy-escuro p-8 sm:p-12 lg:col-span-6 lg:min-h-screen lg:p-16"
    >
      {/* Foto decorativa e camadas de degradê escuro (sem texto alternativo). */}
      {/*
        A foto traz um cartão de login falso embutido a partir de ~70% da largura. A caixa da imagem é 150% do
        painel (o contêiner corta o excesso) e a imagem fica em 60% na horizontal: a janela visível termina,
        no máximo, em 66,7% da largura da imagem em qualquer proporção de painel, então o cartão nunca aparece.
      */}
      <img
        src={fundo}
        alt=""
        aria-hidden="true"
        className="absolute inset-y-0 left-0 h-full w-[150%] max-w-none object-cover object-[60%_center]"
      />
      <div
        aria-hidden="true"
        className="absolute inset-0 bg-gradient-to-t from-navy-escuro via-navy/85 to-navy-escuro/90 mix-blend-multiply"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 bg-gradient-to-br from-navy/60 via-transparent to-status-pendente/20"
      />

      <Link
        to={CAMINHOS.inicio}
        aria-label={ACESSO_MARCA_ROTULO_LINK}
        className="relative z-10 flex w-fit items-center gap-3 rounded focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-branco"
      >
        <span className="flex h-11 w-11 items-center justify-center rounded bg-orange text-branco shadow-md">
          <Icone nome="directions_car" tamanho={24} />
        </span>
        <span className="flex flex-col">
          <span className="text-marca text-branco">
            {ACESSO_MARCA_PREFIXO}<span className="text-orange">{ACESSO_MARCA_SUFIXO}</span>
          </span>
          <span className="text-micro uppercase tracking-wider text-steel-neblina">{ACESSO_LINHA_MARCA}</span>
        </span>
      </Link>

      <div className="relative z-10 my-auto max-w-lg py-12">
        <div className="mb-6 inline-flex items-center gap-2 rounded-xl bg-branco/10 px-3 py-1 backdrop-blur-md">
          <span aria-hidden="true" className="h-2 w-2 animate-pulse rounded-full bg-orange motion-reduce:animate-none" />
          <span className="text-rotulo font-medium text-steel-claro">{ACESSO_SELO_PAINEL}</span>
        </div>
        <p className="mb-4 text-titulo-hero leading-tight text-branco drop-shadow-sm">{ACESSO_FRASE_PAINEL}</p>
        <p className="text-corpo leading-relaxed text-steel-palido">{ACESSO_PARAGRAFO_PAINEL}</p>
      </div>

      <div className="relative z-10 pt-6">
        <ul className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          {DESTAQUES_MARCA.map((destaque) => (
            <li key={destaque.titulo} className="flex items-start gap-3 rounded bg-branco/10 p-3.5 backdrop-blur-md">
              <Icone nome={destaque.icone} tamanho={20} className="mt-0.5 text-orange" />
              <div className="min-w-0 break-words">
                <p className="text-corpo-denso font-semibold text-branco">{destaque.titulo}</p>
                <p className="text-legenda text-steel-claro/80">{destaque.texto}</p>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
