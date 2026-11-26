import { createBrowserRouter, Navigate, type RouteObject } from 'react-router-dom';
import { LayoutAcesso } from '@/layouts/LayoutAcesso';
import { LayoutBackOffice } from '@/layouts/LayoutBackOffice';
import { LayoutPublico } from '@/layouts/LayoutPublico';
import { ProvedorCadastro } from '@/features/clientes/cadastro/ProvedorCadastro';
import { ExigirCadastroConcluido } from '@/features/locadoras/cadastro/ExigirCadastroConcluido';
import { ExigirDadosEmpresa } from '@/features/locadoras/cadastro/ExigirDadosEmpresa';
import { ProvedorCadastroLocadora } from '@/features/locadoras/cadastro/ProvedorCadastroLocadora';
import { ProvedorFiltrosFrota } from '@/features/frota/listagem/ProvedorFiltrosFrota';
import { CARGOS_OPERAR_MANUTENCAO } from '@/features/manutencao/permissoes';
import { CARGOS_VER_PAINEL_FINANCEIRO } from '@/features/financeiro/permissoes';
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
  CadastroLocadoraConfirmacaoPagina,
  CadastroLocadoraEmpresaPagina,
  CadastroLocadoraResponsavelPagina,
  CheckoutReservaPagina,
  ConfirmacaoReservaPagina,
  ContratoDigitalPagina,
  CustoResultadoFrotaPagina,
  DetalheSinistroPagina,
  DetalheVeiculoPagina,
  EquipePagina,
  ConfiguracoesCobrancasPagina,
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
  RecuperarSenhaPagina,
  RedefinirSenhaPagina,
  ResultadosBuscaPagina,
  EntradaRetiradaPagina,
  RetiradaVeiculoPagina,
  TipoContaPagina,
  VerificacaoCadastrosPagina,
  VincularGooglePagina,
  VisaoGeralPagina,
  VistoriaAvariasPagina,
} from './paginas';
import { RotaPorCargo } from './RotaPorCargo';
import { RotaProtegida } from './RotaProtegida';

/** Todas as rotas da aplicação. Os caminhos vêm de `CAMINHOS` (fonte única). */
export const rotas: RouteObject[] = [
  {
    // Tela de acesso: tela cheia, sem menu do topo nem rodapé (layout próprio).
    element: <LayoutAcesso />,
    errorElement: <ErroInesperado />,
    children: [{ path: CAMINHOS.entrar, element: <AcessoPortalPagina /> }],
  },
  {
    path: CAMINHOS.inicio,
    element: <LayoutPublico />,
    errorElement: <ErroInesperado />,
    children: [
      // Públicas
      { index: true, element: <InicioPagina /> },
      { path: CAMINHOS.vincularGoogle, element: <VincularGooglePagina /> },
      { path: CAMINHOS.recuperarSenha, element: <RecuperarSenhaPagina /> },
      { path: CAMINHOS.redefinirSenha, element: <RedefinirSenhaPagina /> },
      // Escolha do tipo de conta (tela 35): cliente segue para o cadastro do cliente; locadora para a etapa 1.
      { path: CAMINHOS.cadastro, element: <TipoContaPagina /> },
      {
        // Rota-pai sem caminho: guarda em memória os dados das duas telas do cadastro do cliente (sem a senha).
        element: <ProvedorCadastro />,
        children: [
          { path: CAMINHOS.cadastroDadosPessoais, element: <CadastroDadosPessoaisPagina /> },
          { path: CAMINHOS.cadastroDocumentos, element: <CadastroDocumentosPagina /> },
        ],
      },
      {
        // Rota-pai sem caminho: guarda em memória os dados da empresa entre as etapas (sem a senha).
        // As etapas 2 e 3 exigem os dados da etapa 1 (sem eles, voltam para a 1); a 3 exige também a conta criada.
        element: <ProvedorCadastroLocadora />,
        children: [
          { path: CAMINHOS.cadastroLocadoraEmpresa, element: <CadastroLocadoraEmpresaPagina /> },
          {
            element: <ExigirDadosEmpresa />,
            children: [
              { path: CAMINHOS.cadastroLocadoraResponsavel, element: <CadastroLocadoraResponsavelPagina /> },
              {
                // A confirmação só abre com a conta criada no envio da etapa 2 em memória.
                element: <ExigirCadastroConcluido />,
                children: [{ path: CAMINHOS.cadastroLocadoraConfirmacao, element: <CadastroLocadoraConfirmacaoPagina /> }],
              },
            ],
          },
        ],
      },
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
          // Entrada do back office: a visão geral (P11-FE-01).
          { index: true, element: <Navigate to={CAMINHOS.visaoGeral} replace /> },

          // Visão geral (P11-FE-01: telas 39 e 40)
          { path: CAMINHOS.visaoGeral, element: <VisaoGeralPagina /> },

          // Frota: Painel (11) e Lista (12) dividem os filtros numa rota-pai sem caminho; novo veículo e edição ficam fora.
          {
            element: <ProvedorFiltrosFrota />,
            children: [
              { path: CAMINHOS.frota, element: <PainelFrotaPagina /> },
              { path: CAMINHOS.frotaVeiculos, element: <ListagemFrotaPagina /> },
            ],
          },
          { path: CAMINHOS.frotaNovoVeiculo, element: <CadastroVeiculoPagina /> },
          // Edição usa a mesma página até a tela de edição existir (P1-FE-03: o formulário é o mesmo).
          { path: CAMINHOS.frotaEditarVeiculo, element: <CadastroVeiculoPagina /> },

          // Clientes: verificação de cadastros só para ATENDENTE e ADMINISTRADOR (o servidor também confere a política).
          { path: CAMINHOS.clientes, element: <Navigate to={CAMINHOS.verificacaoCadastros} replace /> },
          {
            element: <RotaPorCargo cargosPermitidos={['ATENDENTE', 'ADMINISTRADOR']} />,
            children: [{ path: CAMINHOS.verificacaoCadastros, element: <VerificacaoCadastrosPagina /> }],
          },

          // Reservas: o painel P3-FE-06 (do Lucas, semana 4). Locações: o item do menu abre a página de entrada da retirada
          // (até o painel de reservas existir como tela 15 própria); o redirecionamento segue o molde de CAMINHOS.clientes.
          { path: CAMINHOS.reservas, element: <PainelReservasPagina /> },
          { path: CAMINHOS.locacoes, element: <Navigate to={CAMINHOS.retiradas} replace /> },
          { path: CAMINHOS.retiradas, element: <EntradaRetiradaPagina /> },
          { path: CAMINHOS.retiradaVeiculo, element: <RetiradaVeiculoPagina /> },
          // Contrato (tela 08): emitir e assinar só ATENDENTE e ADMINISTRADOR (D6). O servidor confere de novo.
          {
            element: <RotaPorCargo cargosPermitidos={['ATENDENTE', 'ADMINISTRADOR']} />,
            children: [{ path: CAMINHOS.contratoLocacao, element: <ContratoDigitalPagina /> }],
          },
          // Vistoria (tela 19) e devolução (tela 06): abertas a qualquer funcionário (sem cargo nesta rota). A devolução
          // esconde a apuração e o botão Encerrar para cargo sem permissão, e o servidor confere cada ação.
          { path: CAMINHOS.vistoriaLocacao, element: <VistoriaAvariasPagina /> },
          { path: CAMINHOS.devolucaoLocacao, element: <CheckoutDevolucaoPagina /> },

          // Vendas
          { path: CAMINHOS.vendas, element: <GestaoVendasPagina /> },

          // Financeiro: painel (tela 09) só FINANCEIRO e ADMINISTRADOR. O servidor confere de novo (política Financeiro).
          {
            element: <RotaPorCargo cargosPermitidos={CARGOS_VER_PAINEL_FINANCEIRO} />,
            children: [{ path: CAMINHOS.financeiro, element: <PainelFinanceiroPagina /> }],
          },

          // Manutenção: abrir e encerrar ordem só GESTOR_FROTA e ADMINISTRADOR (P13). O servidor confere de novo.
          {
            element: <RotaPorCargo cargosPermitidos={CARGOS_OPERAR_MANUTENCAO} />,
            children: [{ path: CAMINHOS.manutencao, element: <ManutencaoFrotaPagina /> }],
          },
          { path: CAMINHOS.custoResultadoFrota, element: <CustoResultadoFrotaPagina /> },

          // Seguros
          { path: CAMINHOS.seguros, element: <GestaoSegurosPagina /> },
          { path: CAMINHOS.detalheSinistro, element: <DetalheSinistroPagina /> },

          // Administração: só ADMINISTRADOR. O menu esconde o item, mas o servidor é a autoridade (P10-FE-03).
          {
            element: <RotaPorCargo cargosPermitidos={['ADMINISTRADOR']} />,
            children: [
              { path: CAMINHOS.equipe, element: <EquipePagina /> },
              { path: CAMINHOS.configuracoesCobrancasExtras, element: <ConfiguracoesCobrancasPagina /> },
            ],
          },

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
