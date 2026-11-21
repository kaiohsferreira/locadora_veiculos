using AutoMapper;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamentos da proposta de compra (P6-BE-02): o parcelamento calculado vira o VO da simulacao e a proposta gravada
    /// vira o VO de retorno, com os valores formatados em reais e o veiculo resumido (sem quebrar se a navegacao vier nula).
    /// O IdVeiculo da simulacao e preenchido pelo servico.
    /// </summary>
    public class PropostaCompraProfile : Profile
    {
        public PropostaCompraProfile()
        {
            CreateMap<ParcelamentoCalculado, ReturnSimulacaoParcelamentoVO>()
                .ForMember(dest => dest.IdVeiculo, opt => opt.Ignore())
                .ForMember(dest => dest.ValorVendaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorVenda)))
                .ForMember(dest => dest.ValorEntradaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorEntrada)))
                .ForMember(dest => dest.ValorFinanciadoStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorFinanciado)))
                .ForMember(dest => dest.ValorParcelaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorParcela)))
                .ForMember(dest => dest.ValorUltimaParcelaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorUltimaParcela)));

            CreateMap<PropostaCompra, ReturnPropostaCompraVO>()
                .ForMember(dest => dest.ValorVendaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorVenda)))
                .ForMember(dest => dest.ValorEntradaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorEntrada)))
                .ForMember(dest => dest.ValorFinanciado, opt => opt.MapFrom(src => src.ValorVenda - src.ValorEntrada))
                .ForMember(dest => dest.ValorFinanciadoStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorVenda - src.ValorEntrada)))
                .ForMember(dest => dest.ValorParcelaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorParcela)))
                .ForMember(dest => dest.ValorUltimaParcelaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorUltimaParcela)))
                .ForMember(dest => dest.Marca, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.Marca : string.Empty))
                .ForMember(dest => dest.Modelo, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.Modelo : string.Empty))
                .ForMember(dest => dest.AnoModelo, opt => opt.MapFrom(src => src.Veiculo != null ? (int)src.Veiculo.AnoModelo : 0))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.DataProposta, opt => opt.MapFrom(src => src.CreatedAt));
        }
    }
}
