using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public class FakeVistoriaRepository : IVistoriaRepository
    {
        private readonly SemaphoreSlim _trava = new SemaphoreSlim(1, 1);
        public List<Vistoria> Vistorias { get; } = new List<Vistoria>();
        public bool SobTrava { get; private set; }
        public bool LeituraSemTrava { get; private set; }

        public Task<Vistoria?> FindEntradaByLocacaoAsync(int idLocacao)
        {
            LeituraSemTrava |= !SobTrava;
            Vistoria? vistoria = Vistorias.FirstOrDefault(x => x.IdLocacao == idLocacao && x.Tipo == TipoVistoria.ENTRADA && x.DisabledAt == null);
            return Task.FromResult(vistoria);
        }

        public Task<Vistoria> InsertAsync(Vistoria vistoria)
        {
            Assert.True(SobTrava);
            vistoria.IdVistoria = Vistorias.Count + 1;
            Vistorias.Add(vistoria);
            return Task.FromResult(vistoria);
        }

        public async Task<Vistoria> ExecutarComLocacaoBloqueadaAsync(int idLocacao, Func<Task<Vistoria>> operacao)
        {
            await _trava.WaitAsync();
            try
            {
                SobTrava = true;
                await Task.Yield();
                return await operacao();
            }
            finally
            {
                SobTrava = false;
                _trava.Release();
            }
        }
    }
}
