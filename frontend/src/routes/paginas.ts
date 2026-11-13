import { lazy } from 'react';

/*
 * Páginas carregadas sob demanda (um arquivo JS por tela). Enquanto carregam,
 * o layout continua na tela e o conteúdo mostra o esqueleto (Suspense nos layouts).
 */

// Área pública e do cliente ---------------------------------------------------
export const InicioPagina = lazy(() => import('@/features/reservas/pages/InicioPagina'));
export const AcessoPortalPagina = lazy(() => import('@/features/clientes/pages/AcessoPortalPagina'));
export const RecuperarSenhaPagina = lazy(() => import('@/features/clientes/pages/RecuperarSenhaPagina'));
export const RedefinirSenhaPagina = lazy(() => import('@/features/clientes/pages/RedefinirSenhaPagina'));
export const VincularGooglePagina = lazy(() => import('@/features/clientes/pages/VincularGooglePagina'));
export const CadastroDadosPessoaisPagina = lazy(() => import('@/features/clientes/pages/CadastroDadosPessoaisPagina'));
export const CadastroDocumentosPagina = lazy(() => import('@/features/clientes/pages/CadastroDocumentosPagina'));
export const ResultadosBuscaPagina = lazy(() => import('@/features/reservas/pages/ResultadosBuscaPagina'));
export const DetalheVeiculoPagina = lazy(() => import('@/features/reservas/pages/DetalheVeiculoPagina'));
export const CheckoutReservaPagina = lazy(() => import('@/features/reservas/pages/CheckoutReservaPagina'));
export const ConfirmacaoReservaPagina = lazy(() => import('@/features/reservas/pages/ConfirmacaoReservaPagina'));
export const CatalogoSeminovosPagina = lazy(() => import('@/features/vendas/pages/CatalogoSeminovosPagina'));
export const PropostaCompraPagina = lazy(() => import('@/features/vendas/pages/PropostaCompraPagina'));
export const MinhasReservasPagina = lazy(() => import('@/features/area-cliente/pages/MinhasReservasPagina'));
export const MinhasLocacoesPagina = lazy(() => import('@/features/area-cliente/pages/MinhasLocacoesPagina'));
export const PagamentosFaturasPagina = lazy(() => import('@/features/financeiro/pages/PagamentosFaturasPagina'));

// Back office ---------------------------------------------------------------
export const PainelFrotaPagina = lazy(() => import('@/features/frota/pages/PainelFrotaPagina'));
export const ListagemFrotaPagina = lazy(() => import('@/features/frota/pages/ListagemFrotaPagina'));
export const CadastroVeiculoPagina = lazy(() => import('@/features/frota/pages/CadastroVeiculoPagina'));
export const VerificacaoCadastrosPagina = lazy(() => import('@/features/clientes/pages/VerificacaoCadastrosPagina'));
export const PainelReservasPagina = lazy(() => import('@/features/reservas/pages/PainelReservasPagina'));
export const RetiradaVeiculoPagina = lazy(() => import('@/features/locacoes/pages/RetiradaVeiculoPagina'));
export const VistoriaAvariasPagina = lazy(() => import('@/features/locacoes/pages/VistoriaAvariasPagina'));
export const ContratoDigitalPagina = lazy(() => import('@/features/locacoes/pages/ContratoDigitalPagina'));
export const CheckoutDevolucaoPagina = lazy(() => import('@/features/devolucao/pages/CheckoutDevolucaoPagina'));
export const GestaoVendasPagina = lazy(() => import('@/features/vendas/pages/GestaoVendasPagina'));
export const PainelFinanceiroPagina = lazy(() => import('@/features/financeiro/pages/PainelFinanceiroPagina'));
export const ManutencaoFrotaPagina = lazy(() => import('@/features/manutencao/pages/ManutencaoFrotaPagina'));
export const CustoResultadoFrotaPagina = lazy(() => import('@/features/manutencao/pages/CustoResultadoFrotaPagina'));
export const GestaoSegurosPagina = lazy(() => import('@/features/seguros/pages/GestaoSegurosPagina'));
export const DetalheSinistroPagina = lazy(() => import('@/features/seguros/pages/DetalheSinistroPagina'));
