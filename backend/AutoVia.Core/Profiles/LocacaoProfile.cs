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
                .ForMember(d => d.DataRetiradaPrevista, o => o.MapFrom(s => DateOnly.FromDateTime(s.DataRetiradaPrevista)))
                .ForMember(d => d.DataRetiradaPrevistaStr, o => o.MapFrom(s => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(s.DataRetiradaPrevista))))
                .ForMember(d => d.DataDevolucaoPrevista, o => o.MapFrom(s => DateOnly.FromDateTime(s.DataDevolucaoPrevista)))
                .ForMember(d => d.DataDevolucaoPrevistaStr, o => o.MapFrom(s => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(s.DataDevolucaoPrevista))))
                .ForMember(d => d.DiariasPrevistas, o => o.MapFrom(s => RegrasReserva.CalcularDiarias(DateOnly.FromDateTime(s.DataRetiradaPrevista), DateOnly.FromDateTime(s.DataDevolucaoPrevista))))
                .ForMember(d => d.ValorEstimado, o => o.MapFrom(s => s.ValorEstimado))
                .ForMember(d => d.ValorEstimadoStr, o => o.MapFrom(s => StaticMethods.FormateToMonetary(s.ValorEstimado)))
                .ForMember(d => d.ClienteNome, o => o.MapFrom(s => s.Cliente.Nome))
                .ForMember(d => d.ClienteCpfMascarado, o => o.MapFrom(s => StaticMethods.MascararCpf(s.Cliente.Cpf)))
                .ForMember(d => d.CnhNumero, o => o.MapFrom(s => s.Cliente.CnhNumero))
                .ForMember(d => d.CnhCategoria, o => o.MapFrom(s => s.Cliente.CnhCategoria))
                .ForMember(d => d.CnhValidade, o => o.MapFrom(s => s.Cliente.CnhValidade))
                .ForMember(d => d.CnhValidadeStr, o => o.MapFrom(s => StaticMethods.FormateDateToPTBR(s.Cliente.CnhValidade)))
                .ForMember(d => d.CnhVencida, o => o.Ignore())
                .ForMember(d => d.CnhEnviadaEm, o => o.Ignore())
                .ForMember(d => d.CnhEnviadaEmStr, o => o.Ignore())
                .ForMember(d => d.ComprovanteResidenciaEnviadoEm, o => o.Ignore())
                .ForMember(d => d.ComprovanteResidenciaEnviadoEmStr, o => o.Ignore())
                .ForMember(d => d.FotoCapa, o => o.Ignore())
                .ForMember(d => d.ErroFotoCapa, o => o.Ignore())
                .ForMember(d => d.StatusCadastroLocadora, o => o.MapFrom(s => s.Cliente.Locadoras
                    .Where(v => v.DisabledAt == null && v.IdCliente == s.IdCliente && v.IdEmpresa == s.Veiculo.IdEmpresa)
                    .OrderBy(v => v.IdClienteLocadora).Select(v => (StatusCadastroCliente?)v.Status).FirstOrDefault()))
                .ForMember(d => d.Placa, o => o.MapFrom(s => s.Veiculo.Placa))
                .ForMember(d => d.Marca, o => o.MapFrom(s => s.Veiculo.Marca))
                .ForMember(d => d.Modelo, o => o.MapFrom(s => s.Veiculo.Modelo))
                .ForMember(d => d.CategoriaNome, o => o.MapFrom(s => s.Veiculo.Categoria.Nome))
                .ForMember(d => d.AnoModelo, o => o.MapFrom(s => (int)s.Veiculo.AnoModelo))
                .ForMember(d => d.StatusVeiculo, o => o.MapFrom(s => s.Veiculo.Status))
                .ForMember(d => d.KmAtual, o => o.MapFrom(s => s.Veiculo.KmAtual))
                .ForMember(d => d.ValorDiaria, o => o.MapFrom(s => s.Veiculo.ValorDiaria))
                .ForMember(d => d.ValorDiariaStr, o => o.MapFrom(s => StaticMethods.FormateToMonetary(s.Veiculo.ValorDiaria)))
                .ForMember(d => d.LocalRetiradaNome, o => o.MapFrom(s => s.Veiculo.Empresa.NomeFantasia))
                .ForMember(d => d.LocalRetiradaEndereco, o => o.MapFrom(s => RegrasLocacao.MontarEnderecoLocadora(s.Veiculo.Empresa.Logradouro, s.Veiculo.Empresa.Numero, s.Veiculo.Empresa.Bairro, s.Veiculo.Empresa.Cidade, s.Veiculo.Empresa.Uf)))
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
