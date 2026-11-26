using AutoMapper;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using System;
using System.Globalization;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapa da locacao do cliente (tela 27). Datas e valores saem com o campo bruto e o campo Str ao lado, formatados por
    /// StaticMethods. Atrasada e capa nao vem daqui: dependem do instante e do armazenamento, e o servico preenche.
    /// </summary>
    public class MinhasLocacoesProfile : Profile
    {
        public MinhasLocacoesProfile()
        {
            CreateMap<LocacaoClienteRegistro, ReturnMinhaLocacaoVO>()
                .ForMember(destino => destino.CodigoReserva, opcoes => opcoes.MapFrom(origem => origem.IdReserva.HasValue ? RegrasReserva.FormateCodigoReserva(origem.IdReserva.Value) : null))
                .ForMember(destino => destino.Status, opcoes => opcoes.MapFrom(origem => origem.Status.ToString()))
                .ForMember(destino => destino.Atrasada, opcoes => opcoes.Ignore())
                .ForMember(destino => destino.DataRetiradaStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateTimeToPTBR(origem.DataRetirada)))
                .ForMember(destino => destino.DataDevolucaoPrevistaStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateTimeToPTBR(origem.DataDevolucaoPrevista)))
                .ForMember(destino => destino.DevolucaoPrevistaEfetiva, opcoes => opcoes.MapFrom(origem => RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(origem.DataRetirada, origem.DataDevolucaoPrevista)))
                .ForMember(destino => destino.DevolucaoPrevistaEfetivaStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateTimeToPTBR(RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(origem.DataRetirada, origem.DataDevolucaoPrevista))))
                .ForMember(destino => destino.DataDevolucaoEfetivaStr, opcoes => opcoes.MapFrom(origem => origem.DataDevolucaoEfetiva.HasValue ? StaticMethods.FormateDateTimeToPTBR(origem.DataDevolucaoEfetiva.Value) : null))
                .ForMember(destino => destino.Diarias, opcoes => opcoes.MapFrom(origem => RegrasReserva.CalcularDiarias(DateOnly.FromDateTime(origem.DataRetirada), DateOnly.FromDateTime(origem.DataDevolucaoPrevista))))
                .ForMember(destino => destino.ValorDiariaStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateToMonetary(origem.ValorDiaria)))
                .ForMember(destino => destino.ValorTotalStr, opcoes => opcoes.MapFrom(origem => origem.ValorTotal.HasValue ? StaticMethods.FormateToMonetary(origem.ValorTotal.Value) : null))
                .ForMember(destino => destino.CombustivelRetiradaStr, opcoes => opcoes.MapFrom(origem => origem.CombustivelRetirada.HasValue ? origem.CombustivelRetirada.Value.ToString("0.00", CultureInfo.InvariantCulture) : null))
                .ForMember(destino => destino.FotoCapa, opcoes => opcoes.Ignore())
                .ForMember(destino => destino.ErroFotoCapa, opcoes => opcoes.Ignore());
        }
    }
}
