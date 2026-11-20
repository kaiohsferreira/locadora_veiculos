using AutoMapper;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Core.Regras;
using AutoVia.Communication.Enums;
using System.Linq;
using System;

namespace AutoVia.Core.Profiles
{
    /// <summary>Mapeamento de locacao para o objeto publico de retorno, com os campos Str formatados por StaticMethods.</summary>
    public class LocacaoProfile : Profile
    {
        public LocacaoProfile()
        {
            CreateMap<Reserva, ReturnRetiradaVO>()
                .ForMember(d => d.CodigoReserva, o => o.MapFrom(s => RegrasReserva.FormateCodigoReserva(s.IdReserva)))
                .ForMember(d => d.StatusReserva, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.DataRetiradaPrevistaStr, o => o.MapFrom(s => StaticMethods.FormateDateTimeToPTBR(s.DataRetiradaPrevista)))
                .ForMember(d => d.DataDevolucaoPrevistaStr, o => o.MapFrom(s => StaticMethods.FormateDateTimeToPTBR(s.DataDevolucaoPrevista)))
                .ForMember(d => d.ValorEstimadoStr, o => o.MapFrom(s => StaticMethods.FormateToMonetary(s.ValorEstimado)))
                .ForMember(d => d.ClienteNome, o => o.MapFrom(s => s.Cliente.Nome))
                .ForMember(d => d.ClienteCpfMascarado, o => o.MapFrom(s => StaticMethods.MascararCpf(s.Cliente.Cpf)))
                .ForMember(d => d.CnhNumero, o => o.MapFrom(s => s.Cliente.CnhNumero))
                .ForMember(d => d.CnhCategoria, o => o.MapFrom(s => s.Cliente.CnhCategoria))
                .ForMember(d => d.CnhValidadeStr, o => o.MapFrom(s => StaticMethods.FormateDateToPTBR(s.Cliente.CnhValidade)))
                .ForMember(d => d.StatusCadastroLocadora, o => o.MapFrom(s => s.Cliente.Locadoras
                    .Where(v => v.DisabledAt == null && v.IdCliente == s.IdCliente && v.IdEmpresa == s.Veiculo.IdEmpresa)
                    .OrderBy(v => v.IdClienteLocadora).Select(v => (StatusCadastroCliente?)v.Status).FirstOrDefault()))
                .ForMember(d => d.Placa, o => o.MapFrom(s => s.Veiculo.Placa))
                .ForMember(d => d.Marca, o => o.MapFrom(s => s.Veiculo.Marca))
                .ForMember(d => d.Modelo, o => o.MapFrom(s => s.Veiculo.Modelo))
                .ForMember(d => d.StatusVeiculo, o => o.MapFrom(s => s.Veiculo.Status))
                .ForMember(d => d.KmAtual, o => o.MapFrom(s => s.Veiculo.KmAtual))
                .ForMember(d => d.ValorDiariaStr, o => o.MapFrom(s => StaticMethods.FormateToMonetary(s.Veiculo.ValorDiaria)))
                .ForMember(d => d.IdLocacaoExistente, o => o.MapFrom(s => s.Locacoes
                    .Where(l => l.DisabledAt == null && l.IdReserva == s.IdReserva)
                    .OrderBy(l => l.IdLocacao).Select(l => (int?)l.IdLocacao).FirstOrDefault()));

            CreateMap<Locacao, ReturnLocacaoVO>()
                .ForMember(dest => dest.IdReserva, opt => opt.MapFrom(src => src.IdReserva ?? 0))
                .ForMember(dest => dest.DataRetiradaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateTimeToPTBR(src.DataRetirada)))
                .ForMember(dest => dest.DataDevolucaoPrevistaStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(src.DataDevolucaoPrevista))))
                .ForMember(dest => dest.ValorDiariaStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorDiaria)))
                .ForMember(dest => dest.ValorCaucaoStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.ValorCaucao)));
        }
    }
}
