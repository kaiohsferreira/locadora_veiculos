/** Posição padrão da área de avisos (canto inferior direito). Não mudar: as demais telas dependem dela. */
const CLASSE_SECAO_PADRAO =
  'pointer-events-none fixed inset-x-4 bottom-4 z-[60] flex flex-col gap-2 sm:inset-x-auto sm:bottom-6 sm:right-6 sm:w-96';

/** Mesma área, acima da barra de ações fixa das telas que a pedem (72px da barra mais folga). */
const CLASSE_SECAO_ACIMA_DA_BARRA =
  'pointer-events-none fixed inset-x-4 bottom-40 z-[60] flex flex-col gap-2 sm:inset-x-auto sm:bottom-24 sm:right-6 sm:w-96';

/** Classe da área de avisos conforme o pedido da tela. Sem pedido, sai exatamente a marcação de sempre. */
export function classeSecaoAvisos(acimaDaBarra: boolean): string {
  return acimaDaBarra ? CLASSE_SECAO_ACIMA_DA_BARRA : CLASSE_SECAO_PADRAO;
}

export { CLASSE_SECAO_PADRAO, CLASSE_SECAO_ACIMA_DA_BARRA };
