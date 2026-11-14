using AutoMapper;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento do vinculo cliente x locadora para o VO de saida (T-BE-06). Traz o nome fantasia da locadora e as datas
    /// formatadas. A justificativa da recusa nao e mapeada: ela e interna da locadora.
    /// </summary>
    public class ClienteLocadoraProfile : Profile
    {
        public ClienteLocadoraProfile()
        {
            CreateMap<ClienteLocadora, ReturnClienteLocadoraVO>()
                .ForMember(dest => dest.IdEmpresa, opt => opt.MapFrom(src => src.IdEmpresa))
                .ForMember(dest => dest.NomeFantasia, opt => opt.MapFrom(src => src.Empresa.NomeFantasia))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.DataSolicitacaoStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataSolicitacao)))
                .ForMember(dest => dest.DataVerificacaoStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataVerificacao)));
        }
    }
}
