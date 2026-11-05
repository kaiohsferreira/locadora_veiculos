using AutoVia.Communication.ViewObjects.Frota;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato de cadastro e edicao de veiculos (P1-BE-02). As regras de existencia,
    /// unicidade de placa, chassi e renavam ficam na implementacao (VeiculoService).
    /// </summary>
    public partial interface IVeiculoService
    {
        /// <summary>Consulta um veiculo ativo pelo id; lanca NotFoundException se nao existir ou estiver desativado.</summary>
        Task<ReturnVeiculoVO> GetByIdAsync(int idVeiculo);

        /// <summary>Cadastra um veiculo como DISPONIVEL; lanca NotFoundException (categoria) ou BusinessException (duplicidade).</summary>
        Task<ReturnVeiculoVO> InsertAsync(VeiculoVO veiculoVO);

        /// <summary>Altera os dados cadastrais de um veiculo ativo (nao altera o status); lanca NotFoundException ou BusinessException.</summary>
        Task UpdateAsync(int idVeiculo, VeiculoVO veiculoVO);
    }
}
