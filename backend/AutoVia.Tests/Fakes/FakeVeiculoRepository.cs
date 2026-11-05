using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Base (SEAM) do IVeiculoRepository em memoria. Adicionar() semeia veiculos,
    /// inclusive desativados, sem passar pelo servico. Os metodos da interface entram
    /// nos arquivos parciais de cada tarefa (P1-BE-02 e P1-BE-03).
    /// </summary>
    public partial class FakeVeiculoRepository : IVeiculoRepository
    {
        private readonly List<Veiculo> _veiculos = new List<Veiculo>();
        private int _proximoId = 1;

        /// <summary>Semeia um veiculo (ativo ou desativado) sem passar pelo servico. Se o id for 0, gera um.</summary>
        public void Adicionar(Veiculo veiculo)
        {
            if (veiculo.IdVeiculo == 0)
            {
                veiculo.IdVeiculo = _proximoId;
            }

            if (veiculo.IdVeiculo >= _proximoId)
            {
                _proximoId = veiculo.IdVeiculo + 1;
            }

            _veiculos.Add(Copiar(veiculo));
        }

        /// <summary>Copia os campos do veiculo, inclusive auditoria e a referencia de Categoria (simula o Include).</summary>
        private static Veiculo Copiar(Veiculo origem)
        {
            Veiculo copia = new Veiculo
            {
                IdVeiculo = origem.IdVeiculo,
                IdCategoria = origem.IdCategoria,
                Placa = origem.Placa,
                Chassi = origem.Chassi,
                Renavam = origem.Renavam,
                Marca = origem.Marca,
                Modelo = origem.Modelo,
                AnoFabricacao = origem.AnoFabricacao,
                AnoModelo = origem.AnoModelo,
                Cor = origem.Cor,
                KmAtual = origem.KmAtual,
                ValorDiaria = origem.ValorDiaria,
                ValorVenda = origem.ValorVenda,
                Status = origem.Status,
                DataAquisicao = origem.DataAquisicao,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt,
                Categoria = origem.Categoria
            };

            return copia;
        }
    }
}
