using AutoMapper;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles;

public sealed class CobrancasExtrasProfile : Profile
{
    public CobrancasExtrasProfile()
    {
        CreateMap<ConfiguracaoCobrancaExtra, ReturnConfiguracaoCobrancaExtraVO>()
            .ForMember(x => x.CategoriaNome, x => x.MapFrom(y => y.Categoria.Nome))
            .ForMember(x => x.UpdatedAtStr, x => x.MapFrom(y => StaticMethods.FormateDateTimeToPTBR(y.UpdatedAt)));
    }
}
