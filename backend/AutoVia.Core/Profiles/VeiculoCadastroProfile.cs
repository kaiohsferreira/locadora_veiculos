using AutoMapper;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento do VO de entrada do veiculo para a entidade (cadastro e edicao). Id,
    /// status, auditoria e navegacoes nao vem do VO e ficam fora do mapa; os anos
    /// (int no VO) viram SMALLINT na entidade. O VO de retorno esta no VeiculoProfile.
    /// </summary>
    public class VeiculoCadastroProfile : Profile
    {
        public VeiculoCadastroProfile()
        {
            CreateMap<VeiculoVO, Veiculo>()
                .ForMember(dest => dest.IdVeiculo, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.AnoFabricacao, opt => opt.MapFrom(src => (short)src.AnoFabricacao))
                .ForMember(dest => dest.AnoModelo, opt => opt.MapFrom(src => (short)src.AnoModelo))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DisabledAt, opt => opt.Ignore())
                .ForMember(dest => dest.Categoria, opt => opt.Ignore())
                .ForMember(dest => dest.Reservas, opt => opt.Ignore())
                .ForMember(dest => dest.Locacoes, opt => opt.Ignore())
                .ForMember(dest => dest.Manutencoes, opt => opt.Ignore())
                .ForMember(dest => dest.Seguros, opt => opt.Ignore())
                .ForMember(dest => dest.Sinistros, opt => opt.Ignore())
                .ForMember(dest => dest.Vendas, opt => opt.Ignore());
        }
    }
}
