import { useRef, useState, type FormEvent } from 'react';
import { Botao, Campo } from '@/components/ui';
import {
  GOOGLE_TESTE_BOTAO,
  GOOGLE_TESTE_DICA,
  GOOGLE_TESTE_ROTULO_EMAIL,
  GOOGLE_TESTE_TITULO,
} from '@/lib/mensagens';
import { montarCredencialTeste } from './credencialTeste';

export interface BlocoGoogleTesteProps {
  /** Envio em andamento: bloqueia o campo e mostra o carregamento no botão. */
  ocupado: boolean;
  /** Recebe a credencial `FAKE:<e-mail>` quando o e-mail é válido. */
  aoCredencial: (credencial: string) => void;
}

/**
 * Bloco "Continuar com o Google (teste)" (G4): só aparece com `modoTeste` (provedor FAKE, só em
 * desenvolvimento). Não carrega script externo. Usa nossos tokens, e o rótulo deixa claro que é teste.
 */
export function BlocoGoogleTeste({ ocupado, aoCredencial }: BlocoGoogleTesteProps) {
  const [email, setEmail] = useState('');
  const [erro, setErro] = useState<string | null>(null);
  const campoRef = useRef<HTMLInputElement>(null);

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const resultado = montarCredencialTeste(email);
    if (!resultado.ok) {
      setErro(resultado.mensagem);
      campoRef.current?.focus();
      return;
    }
    setErro(null);
    aoCredencial(resultado.credencial);
  }

  return (
    <form
      noValidate
      onSubmit={enviar}
      className="flex flex-col gap-3 rounded-md border border-dashed border-borda-forte bg-offwhite p-4"
    >
      <p className="text-corpo-denso font-semibold text-navy">{GOOGLE_TESTE_TITULO}</p>
      <fieldset disabled={ocupado} className="flex min-w-0 flex-col gap-3 border-0 p-0">
        <Campo
          ref={campoRef}
          rotulo={GOOGLE_TESTE_ROTULO_EMAIL}
          dica={GOOGLE_TESTE_DICA}
          type="email"
          autoComplete="off"
          obrigatorio
          value={email}
          erro={erro ?? undefined}
          onChange={(evento) => {
            setEmail(evento.target.value);
            if (erro !== null) setErro(null);
          }}
        />
        <Botao type="submit" variante="contorno" larguraTotal carregando={ocupado}>
          {GOOGLE_TESTE_BOTAO}
        </Botao>
      </fieldset>
    </form>
  );
}
