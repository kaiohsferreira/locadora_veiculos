using AutoVia.Communication.ViewObjects.PainelOperacional;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoMapper;
using System;

namespace AutoVia.Core.Profiles
{
    public class PainelOperacionalProfile : Profile
    {
        public PainelOperacionalProfile()
        {
            CreateMap<Reserva, ItemPainelOperacionalVO>()
                .ForMember(destino => destino.Origem, opcoes => opcoes.MapFrom(_ => nameof(Reserva).ToUpperInvariant()))
                .ForMember(destino => destino.IdReserva, opcoes => opcoes.MapFrom(origem => origem.IdReserva))
                .ForMember(destino => destino.IdLocacao, opcoes => opcoes.Ignore())
                .ForMember(destino => destino.CodigoReserva, opcoes => opcoes.MapFrom(origem => RegrasReserva.FormateCodigoReserva(origem.IdReserva)))
                .ForMember(destino => destino.DataHora, opcoes => opcoes.MapFrom((origem, destino, membro, contexto) =>
                    StaticMethods.FormateDateTimeToPTBR((DateTime)contexto.Items[nameof(ItemPainelOperacionalVO.DataHora)])))
                .ForMember(destino => destino.Status, opcoes => opcoes.MapFrom(origem => origem.Status.ToString()))
                .ForMember(destino => destino.NomeCliente, opcoes => opcoes.MapFrom(origem => origem.Cliente.Nome))
                .ForMember(destino => destino.Placa, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Placa))
                .ForMember(destino => destino.Marca, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Marca))
                .ForMember(destino => destino.Modelo, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Modelo));

            CreateMap<Locacao, ItemPainelOperacionalVO>()
                .ForMember(destino => destino.Origem, opcoes => opcoes.MapFrom(_ => nameof(Locacao).ToUpperInvariant()))
                .ForMember(destino => destino.IdReserva, opcoes => opcoes.Ignore())
                .ForMember(destino => destino.CodigoReserva, opcoes => opcoes.MapFrom(origem => origem.IdReserva.HasValue ? RegrasReserva.FormateCodigoReserva(origem.IdReserva.Value) : null))
                .ForMember(destino => destino.IdLocacao, opcoes => opcoes.MapFrom(origem => origem.IdLocacao))
                .ForMember(destino => destino.DataHora, opcoes => opcoes.MapFrom((origem, destino, membro, contexto) =>
                    StaticMethods.FormateDateTimeToPTBR((DateTime)contexto.Items[nameof(ItemPainelOperacionalVO.DataHora)])))
                .ForMember(destino => destino.Status, opcoes => opcoes.MapFrom(origem => origem.Status.ToString()))
                .ForMember(destino => destino.NomeCliente, opcoes => opcoes.MapFrom(origem => origem.Cliente.Nome))
                .ForMember(destino => destino.Placa, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Placa))
                .ForMember(destino => destino.Marca, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Marca))
                .ForMember(destino => destino.Modelo, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Modelo));
        }
    }
}
