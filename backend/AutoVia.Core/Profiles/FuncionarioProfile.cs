using AutoMapper;
using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento do funcionario para o VO de saida da equipe (P10-BE-03). Formata o CPF (mascarado no servidor) e a
    /// data de admissao pelos helpers estaticos. Senha e hash nao tem campo no VO. EhVoce depende do token e e
    /// preenchido pelo servico, por isso e ignorado aqui.
    /// </summary>
    public class FuncionarioProfile : Profile
    {
        public FuncionarioProfile()
        {
            CreateMap<Funcionario, ReturnFuncionarioVO>()
                .ForMember(dest => dest.CpfMascarado, opt => opt.MapFrom(src => StaticMethods.MascararCpf(src.Cpf)))
                .ForMember(dest => dest.DataAdmissaoStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataAdmissao)))
                .ForMember(dest => dest.EhVoce, opt => opt.Ignore());
        }
    }
}
