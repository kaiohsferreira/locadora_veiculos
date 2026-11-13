import { useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { FaixaInferiorAcesso, FaixaSuperiorAcesso } from '@/features/clientes/acesso/FaixasAcesso';
import { FormularioAcesso } from '@/features/clientes/acesso/FormularioAcesso';
import { PainelMarcaAcesso } from '@/features/clientes/acesso/PainelMarcaAcesso';
import { perfilInicialPara, resolverDestinoAposLogin } from '@/features/clientes/acesso/destinoAposLogin';
import type { ValoresAcesso } from '@/features/clientes/acesso/esquemaAcesso';
import { montarRequisicaoLogin } from '@/features/clientes/acesso/montarRequisicaoLogin';
import { useEntrar } from '@/features/clientes/hooks/useEntrar';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { ACESSO_TITULO_PAGINA } from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';

/**
 * P2-FE-01 · Acesso ao portal (RF-06): login de cliente e da equipe, em /entrar.
 * Depois do login, a sessão abre e a guarda abaixo leva a pessoa ao destino do seu perfil.
 * Tela cheia (layout próprio): painel da marca à esquerda e formulário à direita.
 */
export default function AcessoPortalPagina() {
  useTituloDocumento(ACESSO_TITULO_PAGINA);
  const { usuario, autenticado } = useAutenticacao();
  const localizacao = useLocation();
  const entrar = useEntrar();
  const { reset } = entrar;

  // Ao sair da tela, descarta o estado da mutação (a senha não fica guardada depois de a tela fechar).
  useEffect(() => () => reset(), [reset]);

  if (autenticado && usuario) {
    return <Navigate to={resolverDestinoAposLogin(usuario.perfil, localizacao.state)} replace />;
  }

  function aoEnviar(valores: ValoresAcesso) {
    entrar.mutate(montarRequisicaoLogin(valores));
  }

  return (
    <div className="grid min-h-screen w-full grid-cols-1 bg-offwhite lg:grid-cols-12">
      <PainelMarcaAcesso />

      <section className="flex flex-col justify-between bg-offwhite p-6 sm:p-12 lg:col-span-6 lg:p-20">
        <FaixaSuperiorAcesso />

        <div className="mx-auto my-auto w-full max-w-md py-8">
          <FormularioAcesso
            perfilInicial={perfilInicialPara(localizacao.state)}
            enviando={entrar.isPending}
            erroEnvio={entrar.error}
            aoEnviar={aoEnviar}
          />
        </div>

        <FaixaInferiorAcesso />
      </section>
    </div>
  );
}
