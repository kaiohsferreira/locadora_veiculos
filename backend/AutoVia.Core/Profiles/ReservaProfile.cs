using AutoMapper;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using System;

namespace AutoVia.Core.Profiles
{
    /// <summary>Mapas da reserva. Datas e valores saem com o campo bruto e o campo Str ao lado, formatados por StaticMethods.</summary>
    public class ReservaProfile : Profile
    {
        public ReservaProfile()
        {
            CreateMap<Reserva, ReturnReservaCriadaVO>()
                .ForMember(destino => destino.Codigo, opcoes => opcoes.MapFrom(origem => RegrasReserva.FormateCodigoReserva(origem.IdReserva)))
                .ForMember(destino => destino.DataRetirada, opcoes => opcoes.MapFrom(origem => DateOnly.FromDateTime(origem.DataRetiradaPrevista)))
                .ForMember(destino => destino.DataRetiradaStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(origem.DataRetiradaPrevista))))
                .ForMember(destino => destino.DataDevolucao, opcoes => opcoes.MapFrom(origem => DateOnly.FromDateTime(origem.DataDevolucaoPrevista)))
                .ForMember(destino => destino.DataDevolucaoStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(origem.DataDevolucaoPrevista))))
                .ForMember(destino => destino.Diarias, opcoes => opcoes.MapFrom(origem => RegrasReserva.CalcularDiarias(DateOnly.FromDateTime(origem.DataRetiradaPrevista), DateOnly.FromDateTime(origem.DataDevolucaoPrevista))))
                .ForMember(destino => destino.ValorEstimadoStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateToMonetary(origem.ValorEstimado)));

            CreateMap<Reserva, ReturnReservaVO>()
                .ForMember(destino => destino.Codigo, opcoes => opcoes.MapFrom(origem => RegrasReserva.FormateCodigoReserva(origem.IdReserva)))
                .ForMember(destino => destino.DataReservaStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateTimeToPTBR(origem.DataReserva)))
                .ForMember(destino => destino.DataRetirada, opcoes => opcoes.MapFrom(origem => DateOnly.FromDateTime(origem.DataRetiradaPrevista)))
                .ForMember(destino => destino.DataRetiradaStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(origem.DataRetiradaPrevista))))
                .ForMember(destino => destino.DataDevolucao, opcoes => opcoes.MapFrom(origem => DateOnly.FromDateTime(origem.DataDevolucaoPrevista)))
                .ForMember(destino => destino.DataDevolucaoStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateDateToPTBR(DateOnly.FromDateTime(origem.DataDevolucaoPrevista))))
                .ForMember(destino => destino.Diarias, opcoes => opcoes.MapFrom(origem => RegrasReserva.CalcularDiarias(DateOnly.FromDateTime(origem.DataRetiradaPrevista), DateOnly.FromDateTime(origem.DataDevolucaoPrevista))))
                .ForMember(destino => destino.Status, opcoes => opcoes.MapFrom(origem => origem.Status.ToString()))
                .ForMember(destino => destino.ValorEstimadoStr, opcoes => opcoes.MapFrom(origem => StaticMethods.FormateToMonetary(origem.ValorEstimado)))
                .ForMember(destino => destino.CategoriaNome, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Categoria.Nome))
                .ForMember(destino => destino.Marca, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Marca))
                .ForMember(destino => destino.Modelo, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Modelo))
                .ForMember(destino => destino.AnoModelo, opcoes => opcoes.MapFrom(origem => (int)origem.Veiculo.AnoModelo))
                .ForMember(destino => destino.Cor, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Cor))
                .ForMember(destino => destino.Placa, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Placa))
                .ForMember(destino => destino.LocadoraNomeFantasia, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Empresa.NomeFantasia))
                .ForMember(destino => destino.LocadoraLogradouro, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Empresa.Logradouro))
                .ForMember(destino => destino.LocadoraNumero, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Empresa.Numero))
                .ForMember(destino => destino.LocadoraBairro, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Empresa.Bairro))
                .ForMember(destino => destino.LocadoraCidade, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Empresa.Cidade))
                .ForMember(destino => destino.LocadoraUf, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Empresa.Uf));
        }
    }
}
