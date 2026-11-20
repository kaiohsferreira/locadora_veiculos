using AutoMapper;
using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento da ordem de manutencao para o VO de retorno: placa achatada (sem quebrar se a navegacao vier nula),
    /// datas e custo formatados pelos StaticMethods e Aberta calculado pela data de saida.
    /// </summary>
    public class ManutencaoProfile : Profile
    {
        public ManutencaoProfile()
        {
            CreateMap<Manutencao, ReturnManutencaoVO>()
                .ForMember(dest => dest.Placa, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.Placa : string.Empty))
                .ForMember(dest => dest.DataEntradaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataEntrada)))
                .ForMember(dest => dest.DataSaidaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataSaida)))
                .ForMember(dest => dest.CustoStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.Custo)))
                .ForMember(dest => dest.Aberta, opt => opt.MapFrom(src => src.DataSaida == null));

            CreateMap<Manutencao, ReturnManutencaoListaVO>()
                .ForMember(dest => dest.Placa, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.Placa : string.Empty))
                .ForMember(dest => dest.Marca, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.Marca : string.Empty))
                .ForMember(dest => dest.Modelo, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.Modelo : string.Empty))
                .ForMember(dest => dest.FotoCapa, opt => opt.Ignore())
                .ForMember(dest => dest.ErroFotoCapa, opt => opt.Ignore())
                .ForMember(dest => dest.DataEntradaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataEntrada)))
                .ForMember(dest => dest.DataSaidaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataSaida)))
                .ForMember(dest => dest.CustoStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.Custo)))
                .ForMember(dest => dest.Aberta, opt => opt.MapFrom(src => src.DataSaida == null));
        }
    }
}
