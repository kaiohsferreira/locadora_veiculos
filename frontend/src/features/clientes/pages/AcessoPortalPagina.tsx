import { useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { FormularioAcesso } from '@/features/clientes/acesso/FormularioAcesso';
import { PainelMarcaAcesso } from '@/features/clientes/acesso/PainelMarcaAcesso';
import { perfilInicialPara, resolverDestinoAposLogin } from '@/features/clientes/acesso/destinoAposLogin';
import type { ValoresAcesso } from '@/features/clientes/acesso/esquemaAcesso';
import { useEntrar } from '@/features/clientes/hooks/useEntrar';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { somenteDigitos } from '@/lib/formatters';
import { ACESSO_TITULO_PAGINA } from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';

/**
 * P2-FE-01 · Acesso ao portal (RF-06): login de cliente e da equipe, em /entrar.
 * Depois do login, a sessão abre e a guarda abaixo leva a pessoa ao destino do seu perfil.
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
    entrar.mutate({ perfil: valores.perfil, cpf: somenteDigitos(valores.cpf), senha: valores.senha });
  }

  return (
    <div className="grid lg:grid-cols-2">
      <PainelMarcaAcesso />

      <section className="flex justify-center px-4 py-10 sm:px-6 lg:px-12 xl:px-20">
        <div className="w-full max-w-md">
          <FormularioAcesso
            perfilInicial={perfilInicialPara(localizacao.state)}
            enviando={entrar.isPending}
            erroEnvio={entrar.error}
            aoEnviar={aoEnviar}
          />
        </div>
      </section>
    </div>
  );
}
