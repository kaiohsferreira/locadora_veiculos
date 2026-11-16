using AutoVia.Communication.Enums;

namespace AutoVia.Integrations.Helpers.Seguradora
{
    /// <summary>
    /// Caminhos validos do status do sinistro (RF-43): ABERTO -> EM_ANALISE;
    /// EM_ANALISE -> APROVADO ou NEGADO; APROVADO -> ENCERRADO; NEGADO -> ENCERRADO.
    /// ENCERRADO e final. Usado pelo provedor FAKE e disponivel para a P9-BE-03.
    /// </summary>
    public static class TransicoesSinistro
    {
        /// <summary>True quando a mudanca de 'atual' para 'novo' e permitida. Funcao pura.</summary>
        public static bool Permitida(StatusSinistro atual, StatusSinistro novo)
        {
            if (atual == StatusSinistro.ABERTO)
            {
                return novo == StatusSinistro.EM_ANALISE;
            }

            if (atual == StatusSinistro.EM_ANALISE)
            {
                return novo == StatusSinistro.APROVADO || novo == StatusSinistro.NEGADO;
            }

            if (atual == StatusSinistro.APROVADO || atual == StatusSinistro.NEGADO)
            {
                return novo == StatusSinistro.ENCERRADO;
            }

            return false;
        }
    }
}
