using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    public partial class VeiculoService
    {
        public async Task<PaginaVO<ReturnVeiculoDisponivelVO>> GetDisponiveisAsync(FiltroVeiculoDisponibilidadeVO filtro)
        {
            try
            {
                int total = await _veiculoRepository.CountDisponiveisAsync(filtro.DataRetirada, filtro.DataDevolucao, filtro.IdCategoria, filtro.ValorDiariaMin, filtro.ValorDiariaMax);
                List<Veiculo> veiculos = await _veiculoRepository.FindDisponiveisAsync(filtro.DataRetirada, filtro.DataDevolucao, filtro.IdCategoria, filtro.ValorDiariaMin, filtro.ValorDiariaMax, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnVeiculoDisponivelVO> itens = _mapper.Map<List<ReturnVeiculoDisponivelVO>>(veiculos);
                PaginaVO<ReturnVeiculoDisponivelVO> pagina = new PaginaVO<ReturnVeiculoDisponivelVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }
    }
}
