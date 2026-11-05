using AutoMapper;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamentos do modulo Frota para a categoria: VO de entrada para entidade e
    /// entidade para VO de retorno, com a diaria base formatada em moeda.
    /// </summary>
    public class CategoriaProfile : Profile
    {
        public CategoriaProfile()
        {
            // VO de entrada -> entidade. Id, auditoria e navegacoes nao vem do VO.
            CreateMap<CategoriaVO, Categoria>()
                .ForMember(dest => dest.IdCategoria, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DisabledAt, opt => opt.Ignore())
                .ForMember(dest => dest.Veiculos, opt => opt.Ignore());

            // Entidade -> VO de retorno, com o valor formatado em moeda.
            CreateMap<Categoria, ReturnCategoriaVO>()
                .ForMember(dest => dest.ValorDiariaBaseStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorDiariaBase)));
        }
    }
}
