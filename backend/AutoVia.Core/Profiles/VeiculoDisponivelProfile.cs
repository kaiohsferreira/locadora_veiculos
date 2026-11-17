using AutoMapper;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento do veiculo para o VO publico da busca de disponiveis: categoria, locadora (nome fantasia, cidade e UF,
    /// sem quebrar se a navegacao vier nula) e valores formatados.
    /// </summary>
    public class VeiculoDisponivelProfile : Profile
    {
        public VeiculoDisponivelProfile()
        {
            CreateMap<Veiculo, ReturnVeiculoDisponivelVO>()
                .ForMember(dest => dest.CategoriaNome, opt => opt.MapFrom(src => src.Categoria.Nome))
                .ForMember(dest => dest.AnoModelo, opt => opt.MapFrom(src => (int)src.AnoModelo))
                .ForMember(dest => dest.ValorDiariaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorDiaria)))
                .ForMember(dest => dest.LocadoraNomeFantasia, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.NomeFantasia : string.Empty))
                .ForMember(dest => dest.LocadoraCidade, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.Cidade : string.Empty))
                .ForMember(dest => dest.LocadoraUf, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.Uf : string.Empty))
                .ForMember(dest => dest.FotoCapa, opt => opt.Ignore())
                .ForMember(dest => dest.ErroFotoCapa, opt => opt.Ignore());
        }
    }
}
