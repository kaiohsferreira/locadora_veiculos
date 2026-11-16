using AutoMapper;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;

namespace AutoVia.Core.Profiles
{
    /// <summary>Mapeamento de locacao para o objeto publico de retorno, com os campos Str formatados por StaticMethods.</summary>
    public class LocacaoProfile : Profile
    {
        public LocacaoProfile()
        {
            CreateMap<Locacao, ReturnLocacaoVO>()
                .ForMember(dest => dest.IdReserva, opt => opt.MapFrom(src => src.IdReserva ?? 0))
                .ForMember(dest => dest.DataRetiradaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataRetirada)))
                .ForMember(dest => dest.DataDevolucaoPrevistaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(src.DataDevolucaoPrevista))))
                .ForMember(dest => dest.ValorDiariaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorDiaria)))
                .ForMember(dest => dest.ValorCaucaoStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorCaucao)));
        }
    }
}
