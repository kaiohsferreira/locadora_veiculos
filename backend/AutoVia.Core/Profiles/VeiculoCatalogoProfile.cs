using AutoMapper;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento do veiculo para o VO publico do catalogo de seminovos (P6-BE-01): nome da
    /// categoria achatado (sem quebrar se a navegacao vier nula), anos em int, preco formatado e
    /// dados da locadora (nome fantasia, cidade e UF, sem quebrar se a navegacao vier nula).
    /// O catalogo so traz veiculos com valor de venda, entao o nulo vira 0 apenas por seguranca.
    /// </summary>
    public class VeiculoCatalogoProfile : Profile
    {
        public VeiculoCatalogoProfile()
        {
            CreateMap<Veiculo, ReturnVeiculoCatalogoVO>()
                .ForMember(dest => dest.CategoriaNome, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nome : string.Empty))
                .ForMember(dest => dest.AnoFabricacao, opt => opt.MapFrom(src => (int)src.AnoFabricacao))
                .ForMember(dest => dest.AnoModelo, opt => opt.MapFrom(src => (int)src.AnoModelo))
                .ForMember(dest => dest.ValorVenda, opt => opt.MapFrom(src => src.ValorVenda ?? 0m))
                .ForMember(dest => dest.ValorVendaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorVenda)))
                .ForMember(dest => dest.LocadoraNomeFantasia, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.NomeFantasia : string.Empty))
                .ForMember(dest => dest.LocadoraCidade, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.Cidade : string.Empty))
                .ForMember(dest => dest.LocadoraUf, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.Uf : string.Empty))
                .ForMember(dest => dest.FotoCapa, opt => opt.Ignore())
                .ForMember(dest => dest.ErroFotoCapa, opt => opt.Ignore());
        }
    }
}
