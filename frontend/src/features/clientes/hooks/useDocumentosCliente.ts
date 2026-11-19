import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { enviarDocumentoCliente, listarDocumentosCliente, type OpcoesEnvioDocumento } from '@/api/documentosCliente';
import type { DocumentoClienteGravado, TipoDocumentoCliente } from '@/types/documentosCliente';

/** Chave das consultas dos documentos do cliente (a lista é de um cliente só). */
export const CHAVE_DOCUMENTOS_CLIENTE = 'documentos-cliente';

/** Documentos ativos do cliente da sessão (sem o link de leitura). Só consulta quando há um id de cliente. */
export function useDocumentosCliente(idCliente: number | null) {
  return useQuery<DocumentoClienteGravado[]>({
    queryKey: [CHAVE_DOCUMENTOS_CLIENTE, idCliente],
    queryFn: () => listarDocumentosCliente(idCliente ?? 0),
    enabled: idCliente !== null,
  });
}

/** Entrada do envio de um documento. */
export interface EnvioDocumentoVariaveis {
  idCliente: number;
  tipo: TipoDocumentoCliente;
  arquivo: File;
  opcoes?: OpcoesEnvioDocumento;
}

/** Envia (ou substitui) um documento e atualiza a lista do cliente quando dá certo. */
export function useEnviarDocumentoCliente() {
  const queryClient = useQueryClient();
  return useMutation<DocumentoClienteGravado, Error, EnvioDocumentoVariaveis>({
    mutationFn: ({ idCliente, tipo, arquivo, opcoes }) => enviarDocumentoCliente(idCliente, tipo, arquivo, opcoes),
    onSuccess: (_resposta, variaveis) =>
      queryClient.invalidateQueries({ queryKey: [CHAVE_DOCUMENTOS_CLIENTE, variaveis.idCliente] }),
  });
}
