using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    public partial class VeiculoService
    {
        public async Task<ReturnVeiculoLocacaoVO> GetParaLocacaoAsync(int idVeiculo, FiltroVeiculoLocacaoVO filtro, CancellationToken cancellationToken = default)
        {
            try
            {
                VeiculoLocacaoConsulta? consulta = await _veiculoRepository.FindParaLocacaoAsync(idVeiculo,
                    filtro.DataRetirada, filtro.DataDevolucao, cancellationToken);
                if (consulta == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                ReturnVeiculoLocacaoVO ficha = _mapper.Map<ReturnVeiculoLocacaoVO>(consulta.Veiculo);
                ficha.DisponivelNoPeriodo = consulta.DisponivelNoPeriodo;
                await _fotoCapaVeiculoService.PreencherGaleriaAsync(consulta.Veiculo, ficha, cancellationToken);
                return ficha;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarVeiculoLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
