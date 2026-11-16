using AutoMapper;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.Profiles
{
    /// <summary>Mapeamento de locacao para o objeto publico de retorno.</summary>
    public class LocacaoProfile : Profile
    {
        public LocacaoProfile()
        {
            CreateMap<Locacao, ReturnLocacaoVO>()
                .ForMember(dest => dest.IdReserva, opt => opt.MapFrom(src => src.IdReserva ?? 0));
        }
    }
}
