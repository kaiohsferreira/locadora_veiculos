using AutoMapper;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamentos da fila e do detalhe de verificacao (P2-BE-05). A entidade e o vinculo com o cliente anexado; status,
    /// datas e justificativa vem do vinculo. Datas pelo StaticMethods (dd/MM/yyyy e dd/MM/yyyy HH:mm) e CPF formatado.
    /// </summary>
    public class ClienteVerificacaoProfile : Profile
    {
        public ClienteVerificacaoProfile()
        {
            CreateMap<ClienteLocadora, ReturnClienteFilaVO>()
                .ForMember(dest => dest.IdCliente, opt => opt.MapFrom(src => src.IdCliente))
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Cliente.Nome))
                .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => StaticMethods.FormateCpf(src.Cliente.Cpf)))
                .ForMember(dest => dest.DataCadastro, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.Cliente.DataCadastro)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.DataSolicitacao, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataSolicitacao)))
                .ForMember(dest => dest.DataVerificacao, opt => opt.MapFrom(src => src.DataVerificacao.HasValue ? StaticMethods.FormateDateTimeToPTBR(src.DataVerificacao.Value) : null))
                .ForMember(dest => dest.JustificativaRecusa, opt => opt.MapFrom(src => src.JustificativaRecusa));

            CreateMap<ClienteLocadora, ReturnClienteVerificacaoVO>()
                .ForMember(dest => dest.IdCliente, opt => opt.MapFrom(src => src.IdCliente))
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Cliente.Nome))
                .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => StaticMethods.FormateCpf(src.Cliente.Cpf)))
                .ForMember(dest => dest.DataCadastro, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.Cliente.DataCadastro)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.DataSolicitacao, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataSolicitacao)))
                .ForMember(dest => dest.DataVerificacao, opt => opt.MapFrom(src => src.DataVerificacao.HasValue ? StaticMethods.FormateDateTimeToPTBR(src.DataVerificacao.Value) : null))
                .ForMember(dest => dest.JustificativaRecusa, opt => opt.MapFrom(src => src.JustificativaRecusa))
                .ForMember(dest => dest.DataNascimento, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.Cliente.DataNascimento)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Cliente.Email))
                .ForMember(dest => dest.Telefone, opt => opt.MapFrom(src => src.Cliente.Telefone))
                .ForMember(dest => dest.CnhNumero, opt => opt.MapFrom(src => src.Cliente.CnhNumero))
                .ForMember(dest => dest.CnhCategoria, opt => opt.MapFrom(src => src.Cliente.CnhCategoria))
                .ForMember(dest => dest.CnhValidade, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.Cliente.CnhValidade)))
                .ForMember(dest => dest.Logradouro, opt => opt.MapFrom(src => src.Cliente.Logradouro))
                .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => src.Cliente.Numero))
                .ForMember(dest => dest.Complemento, opt => opt.MapFrom(src => src.Cliente.Complemento))
                .ForMember(dest => dest.Bairro, opt => opt.MapFrom(src => src.Cliente.Bairro))
                .ForMember(dest => dest.Cidade, opt => opt.MapFrom(src => src.Cliente.Cidade))
                .ForMember(dest => dest.Uf, opt => opt.MapFrom(src => src.Cliente.Uf))
                .ForMember(dest => dest.Cep, opt => opt.MapFrom(src => src.Cliente.Cep));
        }
    }
}
