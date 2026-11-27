using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Detalhe publico do seminovo a venda (P6-FE-02). Arquivo parcial do VeiculoService: mesmo recorte e mesmo VO do
    /// catalogo de seminovos, com a capa preenchida pelo IFotoCapaVeiculoService (url nula e erroFotoCapa quando o
    /// armazenamento falha).
    /// </summary>
    public partial class VeiculoService
    {
        public async Task<ReturnVeiculoCatalogoVO> GetAVendaByIdAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await _veiculoRepository.FindAVendaByIdAsync(idVeiculo);

                if (veiculo == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroPropostaVeiculoNaoDisponivel);
                }

                ReturnVeiculoCatalogoVO retorno = _mapper.Map<ReturnVeiculoCatalogoVO>(veiculo);
                await _fotoCapaVeiculoService.PreencherAsync(new List<Veiculo?> { veiculo }, new List<ReturnVeiculoCatalogoVO> { retorno });

                return retorno;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPropostaBuscarVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
