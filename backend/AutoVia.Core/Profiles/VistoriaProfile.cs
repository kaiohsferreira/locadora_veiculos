using AutoMapper;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    public class VistoriaProfile : Profile
    {
        public VistoriaProfile()
        {
            CreateMap<Vistoria, ReturnVistoriaVO>()
                .ForMember(dest => dest.DataHoraStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataHora)))
                .ForMember(dest => dest.OperadorStr, opt => opt.MapFrom(src => RegrasOperadorVistoria.Abreviar(src.Funcionario == null ? null : src.Funcionario.Nome)));
        }
    }
}
