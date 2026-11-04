import { createBrowserRouter, Navigate, type RouteObject } from 'react-router-dom';
import { LayoutBackOffice } from '@/layouts/LayoutBackOffice';
import { LayoutPublico } from '@/layouts/LayoutPublico';
import { CAMINHOS } from './caminhos';
import { ErroInesperado } from './ErroInesperado';
import { PaginaNaoEncontrada } from './PaginaNaoEncontrada';
import {
  AcessoPortalPagina,
  CadastroDadosPessoaisPagina,
  CadastroDocumentosPagina,
  CadastroVeiculoPagina,
  CatalogoSeminovosPagina,
  CheckoutDevolucaoPagina,
  CheckoutReservaPagina,
  ConfirmacaoReservaPagina,
  ContratoDigitalPagina,
  CustoResultadoFrotaPagina,
  DetalheSinistroPagina,
  DetalheVeiculoPagina,
  GestaoSegurosPagina,
  GestaoVendasPagina,
  InicioPagina,
  ListagemFrotaPagina,
  ManutencaoFrotaPagina,
  MinhasLocacoesPagina,
  MinhasReservasPagina,
  PagamentosFaturasPagina,
  PainelFinanceiroPagina,
  PainelFrotaPagina,
  PainelReservasPagina,
  PropostaCompraPagina,
  ResultadosBuscaPagina,
  RetiradaVeiculoPagina,
  VerificacaoCadastrosPagina,
  VistoriaAvariasPagina,
} from './paginas';
import { RotaProtegida } from './RotaProtegida';

/** Todas as rotas da aplicação. Os caminhos vêm de `CAMINHOS` (fonte única). */
export const rotas: RouteObject[] = [
  {
    path: CAMINHOS.inicio,
    element: <LayoutPublico />,
    errorElement: <ErroInesperado />,
    children: [
      // Públicas
      { index: true, element: <InicioPagina /> },
      { path: CAMINHOS.entrar, element: <AcessoPortalPagina /> },
      { path: CAMINHOS.cadastro, element: <Navigate to={CAMINHOS.cadastroDadosPessoais} replace /> },
      { path: CAMINHOS.cadastroDadosPessoais, element: <CadastroDadosPessoaisPagina /> },
      { path: CAMINHOS.cadastroDocumentos, element: <CadastroDocumentosPagina /> },
      { path: CAMINHOS.resultadosBusca, element: <ResultadosBuscaPagina /> },
      { path: CAMINHOS.detalheVeiculo, element: <DetalheVeiculoPagina /> },
      { path: CAMINHOS.seminovos, element: <CatalogoSeminovosPagina /> },
      { path: CAMINHOS.propostaCompra, element: <PropostaCompraPagina /> },

      // Área do cliente (perfil cliente)
      {
        element: <RotaProtegida perfisPermitidos={['cliente']} />,
        children: [
          { path: CAMINHOS.checkoutReserva, element: <CheckoutReservaPagina /> },
          { path: CAMINHOS.confirmacaoReserva, element: <ConfirmacaoReservaPagina /> },
          { path: CAMINHOS.minhasReservas, element: <MinhasReservasPagina /> },
          { path: CAMINHOS.minhasLocacoes, element: <MinhasLocacoesPagina /> },
          { path: CAMINHOS.meusPagamentos, element: <PagamentosFaturasPagina /> },
        ],
      },

      { path: '*', element: <PaginaNaoEncontrada /> },
    ],
  },
  {
    // Back office (perfil funcionário)
    path: CAMINHOS.backOffice,
    element: <RotaProtegida perfisPermitidos={['funcionario']} />,
    errorElement: <ErroInesperado />,
    children: [
      {
        element: <LayoutBackOffice />,
        children: [
          { index: true, element: <Navigate to={CAMINHOS.frota} replace /> },

          // Frota
          { path: CAMINHOS.frota, element: <PainelFrotaPagina /> },
          { path: CAMINHOS.frotaVeiculos, element: <ListagemFrotaPagina /> },
          { path: CAMINHOS.frotaNovoVeiculo, element: <CadastroVeiculoPagina /> },

          // Clientes
          { path: CAMINHOS.clientes, element: <Navigate to={CAMINHOS.verificacaoCadastros} replace /> },
          { path: CAMINHOS.verificacaoCadastros, element: <VerificacaoCadastrosPagina /> },

          // Reservas e locações: o painel P3-FE-06 é o mesmo nos dois itens do menu.
          { path: CAMINHOS.reservas, element: <PainelReservasPagina /> },
          { path: CAMINHOS.locacoes, element: <PainelReservasPagina /> },
          { path: CAMINHOS.retiradaVeiculo, element: <RetiradaVeiculoPagina /> },
          { path: CAMINHOS.vistoriaLocacao, element: <VistoriaAvariasPagina /> },
          { path: CAMINHOS.contratoLocacao, element: <ContratoDigitalPagina /> },
          { path: CAMINHOS.devolucaoLocacao, element: <CheckoutDevolucaoPagina /> },

          // Vendas
          { path: CAMINHOS.vendas, element: <GestaoVendasPagina /> },

          // Financeiro
          { path: CAMINHOS.financeiro, element: <PainelFinanceiroPagina /> },

          // Manutenção
          { path: CAMINHOS.manutencao, element: <ManutencaoFrotaPagina /> },
          { path: CAMINHOS.custoResultadoFrota, element: <CustoResultadoFrotaPagina /> },

          // Seguros
          { path: CAMINHOS.seguros, element: <GestaoSegurosPagina /> },
          { path: CAMINHOS.detalheSinistro, element: <DetalheSinistroPagina /> },

          { path: '*', element: <PaginaNaoEncontrada /> },
        ],
      },
    ],
  },
];

/** Roteador da aplicação (React Router v6, já com os comportamentos da v7 ativados). */
export const router = createBrowserRouter(rotas, {
  future: {
    v7_relativeSplatPath: true,
    v7_fetcherPersist: true,
    v7_normalizeFormMethod: true,
    v7_partialHydration: true,
    v7_skipActionErrorRevalidation: true,
  },
});
