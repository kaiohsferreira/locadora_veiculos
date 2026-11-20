using AutoMapper;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles;

public class VistoriaSaidaProfile : Profile
{
    public VistoriaSaidaProfile()
    {
        CreateMap<Avaria, AvariaComparacaoVO>()
            .ForMember(d => d.ValorEstimadoStr, o => o.MapFrom(s => StaticMethods.FormateToMonetary(s.ValorEstimado)));
    }
}
