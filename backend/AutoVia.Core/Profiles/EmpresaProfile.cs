using AutoMapper;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System.Linq;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamentos do cadastro publico de locadora: VO de entrada para empresa e para o responsavel
    /// (sem id, flags, cargo, matricula, hash e auditoria, que o servico define) e empresa para o VO de
    /// retorno, com CNPJ e data formatados e os dados do responsavel achatados. Nunca vai senha nem hash.
    /// </summary>
    public class EmpresaProfile : Profile
    {
        public EmpresaProfile()
        {
            // VO de entrada -> empresa. Id, flags (ativo e operadora), auditoria e navegacoes nao vem do VO.
            CreateMap<CadastroLocadoraVO, Empresa>()
                .ForMember(dest => dest.IdEmpresa, opt => opt.Ignore())
                .ForMember(dest => dest.Ativo, opt => opt.Ignore())
                .ForMember(dest => dest.Operadora, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DisabledAt, opt => opt.Ignore())
                .ForMember(dest => dest.Funcionarios, opt => opt.Ignore())
                .ForMember(dest => dest.Veiculos, opt => opt.Ignore())
                .ForMember(dest => dest.Clientes, opt => opt.Ignore());

            // VO de entrada -> funcionario responsavel. Cargo, matricula, senha (hash), ativo e data de admissao
            // sao definidos no servico; id, empresa e auditoria nao vem do VO.
            CreateMap<CadastroLocadoraVO, Funcionario>()
                .ForMember(dest => dest.IdFuncionario, opt => opt.Ignore())
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.ResponsavelNome))
                .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.ResponsavelCpf))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ResponsavelEmail))
                .ForMember(dest => dest.Matricula, opt => opt.Ignore())
                .ForMember(dest => dest.Cargo, opt => opt.Ignore())
                .ForMember(dest => dest.DataAdmissao, opt => opt.Ignore())
                .ForMember(dest => dest.Ativo, opt => opt.Ignore())
                .ForMember(dest => dest.SenhaHash, opt => opt.Ignore())
                .ForMember(dest => dest.IdEmpresa, opt => opt.Ignore())
                .ForMember(dest => dest.Empresa, opt => opt.Ignore())
                .ForMember(dest => dest.Locacoes, opt => opt.Ignore())
                .ForMember(dest => dest.Vistorias, opt => opt.Ignore())
                .ForMember(dest => dest.Vendas, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DisabledAt, opt => opt.Ignore());

            // Empresa -> VO de retorno: CNPJ formatado, data de cadastro e dados do responsavel achatados.
            CreateMap<Empresa, ReturnEmpresaVO>()
                .ForMember(dest => dest.CnpjStr, opt => opt.MapFrom(src => StaticMethods.FormateCnpj(src.Cnpj)))
                .ForMember(dest => dest.DataCadastroStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.CreatedAt)))
                .ForMember(dest => dest.IdFuncionarioResponsavel, opt => opt.MapFrom(src => ObterResponsavel(src).IdFuncionario))
                .ForMember(dest => dest.ResponsavelNome, opt => opt.MapFrom(src => ObterResponsavel(src).Nome))
                .ForMember(dest => dest.ResponsavelEmail, opt => opt.MapFrom(src => ObterResponsavel(src).Email ?? string.Empty))
                .ForMember(dest => dest.MatriculaResponsavel, opt => opt.MapFrom(src => ObterResponsavel(src).Matricula));
        }

        /// <summary>Responsavel da empresa (o primeiro funcionario gravado no cadastro). Sem funcionario, devolve um vazio.</summary>
        private static Funcionario ObterResponsavel(Empresa empresa)
        {
            Funcionario? responsavel = empresa.Funcionarios.FirstOrDefault();

            return responsavel ?? new Funcionario();
        }
    }
}
