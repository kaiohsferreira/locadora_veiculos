using AutoMapper;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    public class VeiculoDisponivelProfile : Profile
    {
        public VeiculoDisponivelProfile()
        {
            CreateMap<Veiculo, ReturnVeiculoDisponivelVO>()
                .ForMember(dest => dest.CategoriaNome, opt => opt.MapFrom(src => src.Categoria.Nome))
                .ForMember(dest => dest.AnoModelo, opt => opt.MapFrom(src => (int)src.AnoModelo))
                .ForMember(dest => dest.ValorDiariaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorDiaria)));
        }
    }
}
