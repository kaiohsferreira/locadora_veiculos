using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da troca manual de status do veiculo (P1-BE-04, RF-04). Sem banco, sem log e sem relogio: o repositorio
    /// so trava e le se ha reserva e locacao em aberto, e aplica a decisao daqui; o servico converte o nome e registra o log.
    /// A P8-BE-01 (manutencao) e a venda reutilizam esta regra. Testada direto em RegrasStatusVeiculoTests.
    /// </summary>
    public static class RegrasStatusVeiculo
    {
        /// <summary>
        /// Resultado da avaliacao: SemAlteracao (mesmo status manual, responde sem gravar), Permitida (grava) ou recusa, com
        /// MensagemRecusa (sempre uma constante de ConstantsMessage). Quando SemAlteracao e verdadeiro, Permitida tambem e.
        /// </summary>
        public sealed class ResultadoTrocaStatus
        {
            /// <summary>Monta o resultado da avaliacao.</summary>
            public ResultadoTrocaStatus(bool semAlteracao, bool permitida, string? mensagemRecusa)
            {
                SemAlteracao = semAlteracao;
                Permitida = permitida;
                MensagemRecusa = mensagemRecusa;
            }

            /// <summary>Verdadeiro quando o destino e igual ao status atual e o destino e manual: nada e gravado.</summary>
            public bool SemAlteracao { get; }

            /// <summary>Verdadeiro quando a troca pode ser gravada.</summary>
            public bool Permitida { get; }

            /// <summary>Mensagem da recusa (constante de ConstantsMessage); nula quando nao ha recusa.</summary>
            public string? MensagemRecusa { get; }
        }

        /// <summary>Status de reserva que contam como reserva em aberto: ATIVA e CONFIRMADA.</summary>
        public static StatusReserva[] StatusReservasEmAberto()
        {
            return new StatusReserva[] { StatusReserva.ATIVA, StatusReserva.CONFIRMADA };
        }

        /// <summary>Status de locacao que contam como locacao em aberto: EM_ANDAMENTO e ATRASADA.</summary>
        public static StatusLocacao[] StatusLocacoesEmAberto()
        {
            return new StatusLocacao[] { StatusLocacao.EM_ANDAMENTO, StatusLocacao.ATRASADA };
        }

        /// <summary>Destinos que a troca manual aceita: DISPONIVEL, MANUTENCAO e BAIXADO. Os demais so mudam pelo fluxo.</summary>
        public static bool EhDestinoManual(StatusVeiculo destino)
        {
            return destino == StatusVeiculo.DISPONIVEL || destino == StatusVeiculo.MANUTENCAO || destino == StatusVeiculo.BAIXADO;
        }

        /// <summary>
        /// Converte o texto no StatusVeiculo pelo NOME exato, depois do Trim e em comparacao ordinal. Numero, minusculas,
        /// caractere nulo e texto vazio nao convertem.
        /// </summary>
        public static bool TentarLerStatusPorNome(string? texto, out StatusVeiculo status)
        {
            status = StatusVeiculo.DISPONIVEL;

            if (texto == null)
            {
                return false;
            }

            string nome = texto.Trim();

            foreach (StatusVeiculo candidato in Enum.GetValues<StatusVeiculo>())
            {
                if (string.Equals(candidato.ToString(), nome, StringComparison.Ordinal))
                {
                    status = candidato;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Decide a troca, na ordem do contrato: (1) destino nao manual recusa com ErroVeiculoStatusSomentePorFluxo, mesmo que
        /// o status ja seja esse; (2) destino manual igual ao atual nao altera; (3) locacao em aberto recusa, antes da reserva;
        /// (4) reserva em aberto recusa; (5) origem VENDIDO ou BAIXADO recusa, e origem RESERVADO ou LOCADO recusa como origem
        /// por fluxo; (6) sobra a troca permitida (origem DISPONIVEL ou MANUTENCAO).
        /// </summary>
        public static ResultadoTrocaStatus Avaliar(StatusVeiculo origem, StatusVeiculo destino, bool temReservaEmAberto, bool temLocacaoEmAberto)
        {
            if (!EhDestinoManual(destino))
            {
                return Recusar(ConstantsMessage.ErroVeiculoStatusSomentePorFluxo);
            }

            if (origem == destino)
            {
                return new ResultadoTrocaStatus(true, true, null);
            }

            if (temLocacaoEmAberto)
            {
                return Recusar(ConstantsMessage.ErroVeiculoStatusComLocacaoEmAberto);
            }

            if (temReservaEmAberto)
            {
                return Recusar(ConstantsMessage.ErroVeiculoStatusComReservaEmAberto);
            }

            if (origem == StatusVeiculo.VENDIDO || origem == StatusVeiculo.BAIXADO)
            {
                return Recusar(ConstantsMessage.ErroVeiculoStatusVendidoOuBaixado);
            }

            if (origem == StatusVeiculo.RESERVADO || origem == StatusVeiculo.LOCADO)
            {
                return Recusar(ConstantsMessage.ErroVeiculoStatusOrigemPorFluxo);
            }

            return new ResultadoTrocaStatus(false, true, null);
        }

        /// <summary>Monta a recusa com a mensagem informada.</summary>
        private static ResultadoTrocaStatus Recusar(string mensagem)
        {
            return new ResultadoTrocaStatus(false, false, mensagem);
        }
    }
}
