using AutoMapper;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>Locacao para o item da lista de devolucoes pendentes. A flag de atraso vem do servico (depende do relogio).</summary>
    public class DevolucaoPendenteProfile : Profile
    {
        public DevolucaoPendenteProfile()
        {
            CreateMap<Locacao, ReturnDevolucaoPendenteVO>()
                .ForMember(destino => destino.IdLocacao, opcoes => opcoes.MapFrom(origem => origem.IdLocacao))
                .ForMember(destino => destino.CodigoReserva, opcoes => opcoes.MapFrom(origem => origem.IdReserva.HasValue ? RegrasReserva.FormateCodigoReserva(origem.IdReserva.Value) : null))
                .ForMember(destino => destino.NomeCliente, opcoes => opcoes.MapFrom(origem => origem.Cliente.Nome))
                .ForMember(destino => destino.Placa, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Placa))
                .ForMember(destino => destino.Marca, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Marca))
                .ForMember(destino => destino.Modelo, opcoes => opcoes.MapFrom(origem => origem.Veiculo.Modelo))
                .ForMember(destino => destino.Status, opcoes => opcoes.MapFrom(origem => origem.Status.ToString()))
                .ForMember(destino => destino.Atrasada, opcoes => opcoes.Ignore())
                .ForMember(destino => destino.DevolucaoPrevistaEfetiva, opcoes => opcoes.MapFrom(origem =>
                    RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(origem.DataRetirada, origem.DataDevolucaoPrevista)))
                .ForMember(destino => destino.DevolucaoPrevistaEfetivaStr, opcoes => opcoes.MapFrom(origem =>
                    StaticMethods.FormateDateTimeToPTBR(RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(origem.DataRetirada, origem.DataDevolucaoPrevista))));
        }
    }
}
