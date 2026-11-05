using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de cadastro e edicao de veiculos (P1-BE-02): categoria ativa (404), placa,
    /// chassi e renavam unicos entre os ativos (409, nesta ordem), normalizacao dos
    /// dados e status DISPONIVEL no cadastro. Na edicao o status nunca muda aqui.
    /// </summary>
    public partial class VeiculoService
    {
        /// <summary>Consulta um veiculo ativo pelo id; lanca NotFoundException se nao existir ou estiver desativado.</summary>
        public async Task<ReturnVeiculoVO> GetByIdAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await _veiculoRepository.FindByIdAsync(idVeiculo);

                if (veiculo == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                ReturnVeiculoVO retorno = _mapper.Map<ReturnVeiculoVO>(veiculo);

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Cadastra um veiculo como DISPONIVEL; lanca NotFoundException (categoria) ou BusinessException (duplicidade).</summary>
        public async Task<ReturnVeiculoVO> InsertAsync(VeiculoVO veiculoVO)
        {
            try
            {
                Categoria? categoria = await _categoriaRepository.FindByIdAsync(veiculoVO.IdCategoria);

                if (categoria == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroCategoriaNaoEncontrada);
                }

                Veiculo veiculo = _mapper.Map<Veiculo>(veiculoVO);
                AplicarNormalizacao(veiculo, veiculoVO);

                await ValidarUnicidadeAsync(veiculo, null);

                veiculo.Status = StatusVeiculo.DISPONIVEL;

                Veiculo gravado = await _veiculoRepository.InsertAsync(veiculo);
                ReturnVeiculoVO retorno = _mapper.Map<ReturnVeiculoVO>(gravado);
                retorno.CategoriaNome = categoria.Nome;

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Altera os dados cadastrais de um veiculo ativo, sem alterar o status. Ordem: veiculo (404),
        /// categoria (404), duplicidades ignorando o proprio veiculo (409); falha ao gravar vira 404.
        /// </summary>
        public async Task UpdateAsync(int idVeiculo, VeiculoVO veiculoVO)
        {
            try
            {
                Veiculo? existente = await _veiculoRepository.FindByIdAsync(idVeiculo);

                if (existente == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                Categoria? categoria = await _categoriaRepository.FindByIdAsync(veiculoVO.IdCategoria);

                if (categoria == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroCategoriaNaoEncontrada);
                }

                Veiculo veiculo = _mapper.Map<Veiculo>(veiculoVO);
                veiculo.IdVeiculo = idVeiculo;
                AplicarNormalizacao(veiculo, veiculoVO);

                await ValidarUnicidadeAsync(veiculo, idVeiculo);

                bool atualizou = await _veiculoRepository.UpdateAsync(veiculo);

                if (!atualizou)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Verifica, nesta ordem, se placa, chassi e renavam ja existem em outro veiculo ativo.
        /// Lanca BusinessException na primeira duplicidade encontrada.
        /// </summary>
        private async Task ValidarUnicidadeAsync(Veiculo veiculo, int? idVeiculoIgnorado)
        {
            try
            {
                bool placaDuplicada = await _veiculoRepository.ExistsByPlacaAsync(veiculo.Placa, idVeiculoIgnorado);

                if (placaDuplicada)
                {
                    throw new BusinessException(ConstantsMessage.ErroVeiculoPlacaDuplicada);
                }

                bool chassiDuplicado = await _veiculoRepository.ExistsByChassiAsync(veiculo.Chassi, idVeiculoIgnorado);

                if (chassiDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroVeiculoChassiDuplicado);
                }

                bool renavamDuplicado = await _veiculoRepository.ExistsByRenavamAsync(veiculo.Renavam, idVeiculoIgnorado);

                if (renavamDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroVeiculoRenavamDuplicado);
                }
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Grava na entidade os textos normalizados: placa sem hifen e em maiusculas, chassi
        /// em maiusculas, renavam, marca e modelo sem espacos das pontas e cor vazia vira nula.
        /// Funcao pura, sem acesso a dados.
        /// </summary>
        private static void AplicarNormalizacao(Veiculo veiculo, VeiculoVO veiculoVO)
        {
            veiculo.Placa = StaticMethods.NormalizePlaca(veiculoVO.Placa);
            veiculo.Chassi = veiculoVO.Chassi.Trim().ToUpperInvariant();
            veiculo.Renavam = veiculoVO.Renavam.Trim();
            veiculo.Marca = veiculoVO.Marca.Trim();
            veiculo.Modelo = veiculoVO.Modelo.Trim();
            veiculo.Cor = NormalizarCor(veiculoVO.Cor);
        }

        /// <summary>Cor sem espacos das pontas; se vazia ou so espacos, vira null.</summary>
        private static string? NormalizarCor(string? cor)
        {
            if (cor == null)
            {
                return null;
            }

            string corSemEspacos = cor.Trim();

            if (corSemEspacos.Length == 0)
            {
                return null;
            }

            return corSemEspacos;
        }
    }
}
