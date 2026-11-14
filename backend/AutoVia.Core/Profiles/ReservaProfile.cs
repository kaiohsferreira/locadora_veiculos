using AutoMapper;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.Profiles
{
    public class ReservaProfile : Profile
    {
        public ReservaProfile()
        {
            CreateMap<Reserva, ReturnReservaCriadaVO>()
                .ForMember(destino => destino.DataRetirada, opcoes => opcoes.MapFrom(origem => DateOnly.FromDateTime(origem.DataRetiradaPrevista)))
                .ForMember(destino => destino.DataDevolucao, opcoes => opcoes.MapFrom(origem => DateOnly.FromDateTime(origem.DataDevolucaoPrevista)));
        }
    }
}
