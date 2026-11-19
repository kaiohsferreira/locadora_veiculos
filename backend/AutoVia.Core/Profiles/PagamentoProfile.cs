using AutoMapper;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;

namespace AutoVia.Core.Profiles
{
    /// <summary>
    /// Mapeamento da cobranca para as saidas (ReturnPagamentoVO, ReturnMeuPagamentoVO e ReturnResumoMeusPagamentosVO). Aqui
    /// ficam a formatacao de datas (dd/MM/yyyy) e de moeda (R$), e a descricao do veiculo. O id da transacao nao e mapeado.
    /// Situacao, dias para vencer e a contagem das abas sao preenchidos pelo servico, que tem a data de hoje.
    /// </summary>
    public class PagamentoProfile : Profile
    {
        public PagamentoProfile()
        {
            CreateMap<Pagamento, ReturnPagamentoVO>()
                .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.Tipo.ToString()))
                .ForMember(dest => dest.ValorStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.Valor)))
                .ForMember(dest => dest.DataVencimentoStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataVencimento)))
                .ForMember(dest => dest.DataPagamentoStr, opt => opt.MapFrom(src => src.DataPagamento.HasValue ? StaticMethods.FormateDateToPTBR(src.DataPagamento.Value) : null))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<Pagamento, ReturnMeuPagamentoVO>()
                .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.Tipo.ToString()))
                .ForMember(dest => dest.ValorStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.Valor)))
                .ForMember(dest => dest.DataVencimentoStr, opt => opt.MapFrom(src => StaticMethods.FormateDateToPTBR(src.DataVencimento)))
                .ForMember(dest => dest.DataPagamentoStr, opt => opt.MapFrom(src => src.DataPagamento.HasValue ? StaticMethods.FormateDateToPTBR(src.DataPagamento.Value) : null))
                .ForMember(dest => dest.VeiculoDescricao, opt => opt.MapFrom(src => DescreverVeiculo(src)))
                .ForMember(dest => dest.DiasParaVencer, opt => opt.Ignore())
                .ForMember(dest => dest.Situacao, opt => opt.Ignore());

            CreateMap<ResumoPagamentosClienteRegistro, ReturnResumoMeusPagamentosVO>()
                .ForMember(dest => dest.TotalLiquidadoAnoStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.TotalLiquidadoAno)))
                .ForMember(dest => dest.TotalPendenteStr, opt => opt.MapFrom(src => StaticMethods.FormateToMonetary(src.TotalPendente)))
                .ForMember(dest => dest.AnoExercicio, opt => opt.Ignore())
                .ForMember(dest => dest.QuantidadeAbaPendentes, opt => opt.Ignore())
                .ForMember(dest => dest.QuantidadeAbaPagos, opt => opt.Ignore());
        }

        /// <summary>Marca e modelo do veiculo da locacao ou da venda; nulo quando a cobranca nao tem veiculo carregado.</summary>
        private static string? DescreverVeiculo(Pagamento pagamento)
        {
            Veiculo? veiculo = pagamento.Locacao?.Veiculo ?? pagamento.Venda?.Veiculo;

            if (veiculo == null)
            {
                return null;
            }

            return $"{veiculo.Marca} {veiculo.Modelo}";
        }
    }
}
