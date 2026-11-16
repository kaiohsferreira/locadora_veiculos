using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da abertura de locacao a partir de reserva (RF-17). Sem banco, sem log e sem relogio: o servico e o
    /// repositorio usam as mesmas funcoes, e os testes as conferem direto.
    /// </summary>
    public static class RegrasLocacao
    {
        /// <summary>Verdadeiro quando a reserva pode dar origem a uma locacao: somente ATIVA ou CONFIRMADA.</summary>
        public static bool ReservaPermiteAbrirLocacao(StatusReserva status)
        {
            bool permite = status == StatusReserva.ATIVA || status == StatusReserva.CONFIRMADA;

            return permite;
        }

        /// <summary>Verdadeiro quando o veiculo pode ser entregue na abertura: somente RESERVADO.</summary>
        public static bool VeiculoPermiteAbrirLocacao(StatusVeiculo status)
        {
            bool permite = status == StatusVeiculo.RESERVADO;

            return permite;
        }

        /// <summary>Status da reserva depois da abertura da locacao.</summary>
        public static StatusReserva StatusReservaAposAbertura()
        {
            StatusReserva status = StatusReserva.CONCLUIDA;

            return status;
        }

        /// <summary>Status do veiculo depois da abertura da locacao.</summary>
        public static StatusVeiculo StatusVeiculoAposAbertura()
        {
            StatusVeiculo status = StatusVeiculo.LOCADO;

            return status;
        }

        /// <summary>
        /// Monta a locacao da abertura: quilometragem de saida = km atual do veiculo, diaria = diaria atual do veiculo,
        /// caucao 0, status EM_ANDAMENTO. A retirada vem do servico (IRelogio); CreatedAt e UpdatedAt vem do momento do registro.
        /// </summary>
        public static Locacao CriarLocacaoDeAbertura(Reserva reserva, Veiculo veiculo, int idFuncionario, DateTime dataRetirada, DateTime momentoRegistro)
        {
            Locacao locacao = new Locacao
            {
                IdReserva = reserva.IdReserva,
                IdCliente = reserva.IdCliente,
                IdVeiculo = veiculo.IdVeiculo,
                IdFuncionario = idFuncionario,
                DataRetirada = dataRetirada,
                DataDevolucaoPrevista = reserva.DataDevolucaoPrevista,
                KmSaida = veiculo.KmAtual,
                ValorDiaria = veiculo.ValorDiaria,
                ValorCaucao = 0m,
                Status = StatusLocacao.EM_ANDAMENTO,
                CreatedAt = momentoRegistro,
                UpdatedAt = momentoRegistro,
                DisabledAt = null
            };

            return locacao;
        }
    }
}
