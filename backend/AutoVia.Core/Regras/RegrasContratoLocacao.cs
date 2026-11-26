using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Contratos;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras do contrato digital da locacao (contrato-locacao v5; RP-06, RN-1 a RN-12). Sem banco, sem log e sem relogio:
    /// o servico passa as entidades ja lidas e a data de negocio. A montagem do documento e o hash ficam aqui para serem testados
    /// direto. O formato do nome e da imagem fica em RegrasFormatoAssinaturaContrato (Communication, junto do VO).
    /// </summary>
    public static class RegrasContratoLocacao
    {
        /// <summary>
        /// Vistoria de ENTRADA ativa da locacao (RN-1, RP-06). A vistoria de SAIDA nao conta. Recebe a entrada ja lida pelo repositorio
        /// (filtro de empresa e DisabledAt nulo) e confere o tipo, a locacao e a exclusao logica.
        /// </summary>
        public static bool TemVistoriaEntradaAtiva(Vistoria? entrada, int idLocacao)
        {
            bool tem = entrada != null && entrada.DisabledAt == null && entrada.IdLocacao == idLocacao && entrada.Tipo == TipoVistoria.ENTRADA;

            return tem;
        }

        /// <summary>Locacao em aberto (RN-2): a mesma fonte da vistoria, EM_ANDAMENTO ou ATRASADA.</summary>
        public static bool LocacaoEmAberto(StatusLocacao status)
        {
            bool aberta = RegrasVistoria.LocacaoAceitaVistoria(status);

            return aberta;
        }

        /// <summary>
        /// Clausula 5 (politica de combustivel) consta do documento quando ha configuracao vigente da categoria (RN-12). Sem ela,
        /// o aceite de combustivel sai junto (O-6).
        /// </summary>
        public static bool ClausulaPoliticaCombustivelConsta(ConfiguracaoCobrancaExtra? configuracao)
        {
            bool consta = configuracao != null;

            return consta;
        }

        /// <summary>
        /// Mensagem do aceite recusado, ou nulo quando os aceites estao certos (RN-5, O-6). O aceite das condicoes gerais e sempre
        /// exigido; o de combustivel so quando a clausula 5 consta do documento (a configuracao da categoria existe).
        /// </summary>
        public static string? ErroAceites(bool aceiteCondicoesGerais, bool? aceitePoliticaCombustivel, bool exigePoliticaCombustivel)
        {
            string? erro = null;
            if (!aceiteCondicoesGerais || (exigePoliticaCombustivel && aceitePoliticaCombustivel != true))
            {
                erro = ConstantsMessage.ErroContratoAceiteObrigatorio;
            }

            return erro;
        }

        /// <summary>
        /// Nome do campo do aceite recusado, para o ValidationFailure. Quando as condicoes gerais nao foram aceitas, o campo e o delas;
        /// do contrario (so o de combustivel falhou) o campo e o de combustivel.
        /// </summary>
        public static string NomeCampoAceiteRecusado(bool aceiteCondicoesGerais)
        {
            string campo = aceiteCondicoesGerais ? nameof(AssinarContratoLocacaoVO.AceitePoliticaCombustivel) : nameof(AssinarContratoLocacaoVO.AceiteCondicoesGerais);

            return campo;
        }

        /// <summary>SHA-256 dos bytes UTF-8 exatos do texto, em hexadecimal minusculo (64 caracteres).</summary>
        public static string CalcularHashSha256(string texto)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(texto);
            byte[] hash = SHA256.HashData(bytes);
            string hexadecimal = Convert.ToHexString(hash).ToLowerInvariant();

            return hexadecimal;
        }

        /// <summary>
        /// Monta o documento da locacao (RN-11). Diarias previstas e valor previsto pela regra da apuracao (O-12); clausulas sem lastro
        /// saem e as demais sao renumeradas (RN-6 e RN-12). A configuracao nula significa sem politica de cobrancas da categoria.
        /// </summary>
        public static ContratoDocumentoVO MontarDocumento(Empresa empresa, Locacao locacao, Cliente cliente, Veiculo veiculo, Vistoria entrada, ConfiguracaoCobrancaExtra? configuracao)
        {
            bool temCaucao = locacao.ValorCaucao > 0m;
            bool temConfiguracao = ClausulaPoliticaCombustivelConsta(configuracao);
            int diarias = RegrasDevolucao.CalcularDiarias(locacao.DataRetirada, locacao.DataDevolucaoPrevista);
            decimal valorTotalPrevisto = RegrasDevolucao.ValorDaDiaria(diarias, locacao.ValorDiaria);

            ContratoDocumentoVO documento = new ContratoDocumentoVO
            {
                VersaoDocumento = ConstantsValues.ContratoVersaoDocumento,
                Locadora = MontarLocadora(empresa),
                Locatario = MontarLocatario(cliente),
                Veiculo = MontarVeiculo(veiculo),
                Locacao = new ContratoLocacaoDocumentoVO
                {
                    Numero = locacao.IdLocacao,
                    DataRetiradaStr = StaticMethods.FormateDateTimeToPTBR(locacao.DataRetirada),
                    DataDevolucaoPrevistaStr = StaticMethods.FormateDateTimeToPTBR(locacao.DataDevolucaoPrevista),
                    ValorDiariaStr = StaticMethods.FormateToMonetary(locacao.ValorDiaria),
                    ValorCaucaoStr = temCaucao ? StaticMethods.FormateToMonetary(locacao.ValorCaucao) : null,
                    DiariasPrevistas = diarias,
                    ValorTotalPrevistoStr = StaticMethods.FormateToMonetary(valorTotalPrevisto)
                },
                VistoriaEntrada = new ContratoVistoriaEntradaVO
                {
                    DataHoraStr = StaticMethods.FormateDateTimeToPTBR(entrada.DataHora),
                    KmRegistrado = entrada.KmRegistrado,
                    NivelCombustivel = entrada.NivelCombustivel
                },
                TabelaCobrancas = configuracao == null ? null : MontarTabelaCobrancas(configuracao),
                Clausulas = MontarClausulas(temCaucao, temConfiguracao)
            };

            return documento;
        }

        private static ContratoLocadoraVO MontarLocadora(Empresa empresa)
        {
            ContratoLocadoraVO locadora = new ContratoLocadoraVO
            {
                RazaoSocial = empresa.RazaoSocial,
                NomeFantasia = empresa.NomeFantasia,
                Cnpj = empresa.Cnpj,
                Telefone = empresa.Telefone,
                Email = empresa.Email,
                Cep = empresa.Cep,
                Logradouro = empresa.Logradouro,
                Numero = empresa.Numero,
                Complemento = empresa.Complemento,
                Bairro = empresa.Bairro,
                Cidade = empresa.Cidade,
                Uf = empresa.Uf
            };

            return locadora;
        }

        private static ContratoLocatarioVO MontarLocatario(Cliente cliente)
        {
            ContratoLocatarioVO locatario = new ContratoLocatarioVO
            {
                Nome = cliente.Nome,
                CpfMascarado = StaticMethods.MascararCpf(cliente.Cpf),
                CnhMascarada = StaticMethods.MascararCnh(cliente.CnhNumero),
                CnhCategoria = cliente.CnhCategoria.ToString(),
                CnhValidadeStr = StaticMethods.FormateDateToPTBR(cliente.CnhValidade)
            };

            return locatario;
        }

        private static ContratoVeiculoVO MontarVeiculo(Veiculo veiculo)
        {
            ContratoVeiculoVO veiculoDocumento = new ContratoVeiculoVO
            {
                Marca = veiculo.Marca,
                Modelo = veiculo.Modelo,
                Placa = veiculo.Placa,
                Chassi = veiculo.Chassi,
                Renavam = veiculo.Renavam,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo,
                Cor = veiculo.Cor
            };

            return veiculoDocumento;
        }

        private static ContratoTabelaCobrancasVO MontarTabelaCobrancas(ConfiguracaoCobrancaExtra configuracao)
        {
            ContratoTabelaCobrancasVO tabela = new ContratoTabelaCobrancasVO
            {
                VersaoConfiguracao = configuracao.Versao,
                FranquiaKm = configuracao.FranquiaKm,
                BaseFranquia = configuracao.BaseFranquia,
                ValorKmExcedenteStr = StaticMethods.FormateToMonetary(configuracao.ValorKmExcedente),
                ValorTanqueCompletoStr = StaticMethods.FormateToMonetary(configuracao.ValorTanqueCompleto),
                ToleranciaAtrasoMinutos = configuracao.ToleranciaAtrasoMinutos,
                UnidadeAtraso = configuracao.UnidadeAtraso,
                FracionamentoAtraso = configuracao.FracionamentoAtraso,
                ValorUnidadeAtrasoStr = StaticMethods.FormateToMonetary(configuracao.ValorUnidadeAtraso),
                BaseAvarias = configuracao.BaseAvarias
            };

            return tabela;
        }

        /// <summary>
        /// Clausulas com lastro, na ordem do contrato. Caucao zero tira a 4; sem configuracao tiram a 2, a 5, a 6 e a 7. Os numeros
        /// seguem em sequencia (a 8 vira 4 quando faltam 2, 5, 6 e 7). Os textos sao os aprovados, sem alteracao.
        /// </summary>
        private static List<ContratoClausulaVO> MontarClausulas(bool temCaucao, bool temConfiguracao)
        {
            List<ContratoClausulaVO> clausulas = new List<ContratoClausulaVO>();
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaObjeto, ConstantsMessage.ContratoTextoClausulaObjeto, true);
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaPrazos, ConstantsMessage.ContratoTextoClausulaPrazos, temConfiguracao);
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaValores, ConstantsMessage.ContratoTextoClausulaValores, true);
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaCaucao, ConstantsMessage.ContratoTextoClausulaCaucao, temCaucao);
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaCombustivel, ConstantsMessage.ContratoTextoClausulaCombustivel, temConfiguracao);
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaQuilometragem, ConstantsMessage.ContratoTextoClausulaQuilometragem, temConfiguracao);
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaAvarias, ConstantsMessage.ContratoTextoClausulaAvarias, temConfiguracao);
            AdicionarClausula(clausulas, ConstantsMessage.ContratoTituloClausulaNatureza, ConstantsMessage.ContratoTextoClausulaNatureza, true);

            return clausulas;
        }

        private static void AdicionarClausula(List<ContratoClausulaVO> clausulas, string titulo, string texto, bool incluir)
        {
            if (!incluir)
            {
                return;
            }

            clausulas.Add(new ContratoClausulaVO { Numero = clausulas.Count + 1, Titulo = titulo, Texto = texto });
        }
    }
}
