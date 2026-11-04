import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui/aviso/AvisoProvider';
import { queryClient } from '@/lib/queryClient';
import { AutenticacaoProvider } from '@/routes/autenticacao/AutenticacaoProvider';
import { router } from '@/routes/rotas';
import './index.css';

const elementoRaiz = document.getElementById('root');
if (!elementoRaiz) {
  throw new Error('Elemento #root não encontrado no index.html.');
}

createRoot(elementoRaiz).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AvisoProvider>
        <AutenticacaoProvider>
          <RouterProvider router={router} future={{ v7_startTransition: true }} />
        </AutenticacaoProvider>
      </AvisoProvider>
    </QueryClientProvider>
  </StrictMode>,
);
