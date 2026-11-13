using AutoMapper;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamentos da apolice: VO de entrada para entidade e entidade para VO de retorno,
    /// com placa achatada (sem quebrar se a navegacao vier nula), datas e premio formatados.
    /// DiasParaVencer e VenceEm60Dias dependem da data de hoje e sao preenchidos no SeguroService.
    /// </summary>
    public class SeguroProfile : Profile
    {
        public SeguroProfile()
        {
            // VO de entrada -> entidade. Id, auditoria e navegacoes nao vem do VO.
            CreateMap<SeguroVO, Seguro>()
                .ForMember(dest => dest.IdSeguro, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DisabledAt, opt => opt.Ignore())
                .ForMember(dest => dest.Veiculo, opt => opt.Ignore())
                .ForMember(dest => dest.Sinistros, opt => opt.Ignore());

            // Entidade -> VO de retorno.
            CreateMap<Seguro, ReturnSeguroVO>()
                .ForMember(dest => dest.PlacaVeiculo, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.Placa : string.Empty))
                .ForMember(dest => dest.DataInicioStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataInicio)))
                .ForMember(dest => dest.DataFimStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataFim)))
                .ForMember(dest => dest.ValorPremioStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorPremio)))
                .ForMember(dest => dest.DiasParaVencer, opt => opt.Ignore())
                .ForMember(dest => dest.VenceEm60Dias, opt => opt.Ignore());
        }
    }
}
