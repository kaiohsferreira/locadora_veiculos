using AutoMapper;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    public class VeiculoLocacaoProfile : Profile
    {
        public VeiculoLocacaoProfile()
        {
            CreateMap<Veiculo, ReturnVeiculoLocacaoVO>()
                .ForMember(dest => dest.CategoriaNome, opt => opt.MapFrom(src => src.Categoria.Nome))
                .ForMember(dest => dest.AnoFabricacao, opt => opt.MapFrom(src => (int)src.AnoFabricacao))
                .ForMember(dest => dest.AnoModelo, opt => opt.MapFrom(src => (int)src.AnoModelo))
                .ForMember(dest => dest.ValorDiariaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorDiaria)))
                .ForMember(dest => dest.LocadoraNomeFantasia, opt => opt.MapFrom(src => src.Empresa.NomeFantasia))
                .ForMember(dest => dest.LocadoraCidade, opt => opt.MapFrom(src => src.Empresa.Cidade))
                .ForMember(dest => dest.LocadoraUf, opt => opt.MapFrom(src => src.Empresa.Uf))
                .ForMember(dest => dest.DisponivelNoPeriodo, opt => opt.Ignore())
                .ForMember(dest => dest.Fotos, opt => opt.Ignore())
                .ForMember(dest => dest.ErroFotos, opt => opt.Ignore());
        }
    }
}
