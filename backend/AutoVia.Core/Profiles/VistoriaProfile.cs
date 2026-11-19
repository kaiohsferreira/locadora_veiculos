using AutoMapper;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    public class VistoriaProfile : Profile
    {
        public VistoriaProfile()
        {
            CreateMap<Vistoria, ReturnVistoriaVO>()
                .ForMember(dest => dest.DataHoraStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataHora)));
        }
    }
}
