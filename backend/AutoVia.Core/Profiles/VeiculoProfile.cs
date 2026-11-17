using AutoMapper;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento do veiculo para o VO de retorno: nome da categoria e dados da locadora (nome fantasia, cidade e UF)
    /// achatados das navegacoes (sem quebrar se a navegacao vier nula), anos convertidos para int e valores e datas formatados.
    /// </summary>
    public class VeiculoProfile : Profile
    {
        public VeiculoProfile()
        {
            CreateMap<Veiculo, ReturnVeiculoVO>()
                .ForMember(dest => dest.CategoriaNome, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nome : string.Empty))
                .ForMember(dest => dest.LocadoraNomeFantasia, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.NomeFantasia : string.Empty))
                .ForMember(dest => dest.LocadoraCidade, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.Cidade : string.Empty))
                .ForMember(dest => dest.LocadoraUf, opt => opt.MapFrom(src => src.Empresa != null ? src.Empresa.Uf : string.Empty))
                .ForMember(dest => dest.AnoFabricacao, opt => opt.MapFrom(src => (int)src.AnoFabricacao))
                .ForMember(dest => dest.AnoModelo, opt => opt.MapFrom(src => (int)src.AnoModelo))
                .ForMember(dest => dest.ValorDiariaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorDiaria)))
                .ForMember(dest => dest.ValorVendaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorVenda)))
                .ForMember(dest => dest.DataAquisicaoStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataAquisicao)))
                .ForMember(dest => dest.FotoCapa, opt => opt.Ignore())
                .ForMember(dest => dest.ErroFotoCapa, opt => opt.Ignore());
        }
    }
}
