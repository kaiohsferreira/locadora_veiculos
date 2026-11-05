using AutoMapper;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamentos do cadastro do cliente: VO de entrada para entidade (sem id, status,
    /// senha, datas de cadastro, auditoria e navegacoes, que o servico define) e entidade
    /// para VO de retorno, com CPF, datas e data de cadastro formatados.
    /// </summary>
    public class ClienteProfile : Profile
    {
        public ClienteProfile()
        {
            // VO de entrada -> entidade. Id, status, senha (hash e definido no servico),
            // datas de cadastro, auditoria e navegacoes nao vem do VO.
            CreateMap<ClienteVO, Cliente>()
                .ForMember(dest => dest.IdCliente, opt => opt.Ignore())
                .ForMember(dest => dest.StatusCadastro, opt => opt.Ignore())
                .ForMember(dest => dest.SenhaHash, opt => opt.Ignore())
                .ForMember(dest => dest.DataCadastro, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DisabledAt, opt => opt.Ignore())
                .ForMember(dest => dest.Reservas, opt => opt.Ignore())
                .ForMember(dest => dest.Locacoes, opt => opt.Ignore())
                .ForMember(dest => dest.Vendas, opt => opt.Ignore());

            // Entidade -> VO de retorno, com os campos formatados. Nao ha senha no retorno.
            CreateMap<Cliente, ReturnClienteVO>()
                .ForMember(dest => dest.CpfStr, opt => opt.MapFrom(src => StaticMethods.FormateCpf(src.Cpf)))
                .ForMember(dest => dest.DataNascimentoStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataNascimento)))
                .ForMember(dest => dest.CnhValidadeStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.CnhValidade)))
                .ForMember(dest => dest.DataCadastroStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataCadastro)));
        }
    }
}
