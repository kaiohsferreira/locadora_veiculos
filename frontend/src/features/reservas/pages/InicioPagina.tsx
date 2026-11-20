import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { CAMINHOS } from '@/routes/caminhos';
import { Botao, Campo, Icone } from '@/components/ui';

interface PeriodoBusca {
  localRetirada: string;
  dataRetirada: string;
  dataDevolucao: string;
}

function dataLocalISO(data: Date): string {
  const ano = data.getFullYear();
  const mes = String(data.getMonth() + 1).padStart(2, '0');
  const dia = String(data.getDate()).padStart(2, '0');
  return `${ano}-${mes}-${dia}`;
}

function diaSeguinteISO(dataISO: string): string {
  const data = new Date(`${dataISO}T00:00:00`);
  data.setDate(data.getDate() + 1);
  return dataLocalISO(data);
}

/** P3-FE-01 · Página inicial com busca de veículos por período. */
export default function InicioPagina() {
  useTituloDocumento('Alugue seu próximo carro');
  const navegar = useNavigate();
  const [hoje] = useState(() => dataLocalISO(new Date()));
  const [periodo, setPeriodo] = useState<PeriodoBusca>({ localRetirada: '', dataRetirada: '', dataDevolucao: '' });
  const [erroPeriodo, setErroPeriodo] = useState('');
  const [erroLocalRetirada, setErroLocalRetirada] = useState('');

  function buscarVeiculos(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();

    if (periodo.localRetirada.trim() === '') {
      setErroLocalRetirada('Informe a unidade de retirada.');
      return;
    }
    if (!periodo.dataRetirada || !periodo.dataDevolucao) {
      setErroPeriodo('Informe as datas de retirada e devolução.');
      return;
    }
    if (periodo.dataRetirada >= periodo.dataDevolucao) {
      setErroPeriodo('A devolução deve acontecer depois da retirada.');
      return;
    }

    setErroPeriodo('');
    setErroLocalRetirada('');
    const parametros = new URLSearchParams({
      localRetirada: periodo.localRetirada.trim(),
      dataRetirada: periodo.dataRetirada,
      dataDevolucao: periodo.dataDevolucao,
    });

    navegar({ pathname: CAMINHOS.resultadosBusca, search: parametros.toString() });
  }

  return (
    <div className="overflow-hidden">
      <section aria-labelledby="titulo-inicio" className="relative isolate bg-navy">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -right-40 -top-48 -z-10 h-[34rem] w-[34rem] rounded-full bg-navy-claro/70 blur-3xl"
        />
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-12 sm:px-6 sm:py-16 lg:grid-cols-[1.05fr_0.95fr] lg:items-center lg:gap-14 lg:px-8 lg:py-24">
          <div className="max-w-2xl text-branco">
            <p className="mb-4 inline-flex items-center gap-2 rounded-full border border-branco/20 bg-branco/10 px-3 py-1.5 text-corpo-denso font-medium text-branco">
              <Icone nome="directions_car" tamanho={18} />
              Seu caminho começa aqui
            </p>
            <h1 id="titulo-inicio" className="text-4xl font-bold leading-tight tracking-tight sm:text-5xl lg:text-6xl">
              Liberdade para ir mais longe.
            </h1>
            <p className="mt-5 max-w-xl text-lg leading-8 text-branco/80">
              Escolha as datas da sua viagem e encontre o carro ideal para cada caminho.
            </p>
            <div className="mt-8 flex flex-wrap gap-x-6 gap-y-3 text-corpo-denso text-branco/90">
              <span className="inline-flex items-center gap-2"><Icone nome="verified_user" tamanho={19} /> Veículos selecionados</span>
              <span className="inline-flex items-center gap-2"><Icone nome="event_available" tamanho={19} /> Datas flexíveis</span>
            </div>
          </div>

          <div className="rounded-xl bg-branco p-5 shadow-xl sm:p-7">
            <div className="mb-6">
              <p className="text-legenda font-semibold uppercase tracking-wider text-orange-escuro">Planeje sua viagem</p>
              <h2 className="mt-1 text-2xl font-bold text-navy">Quando você precisa do carro?</h2>
              <p className="mt-2 text-corpo-denso text-texto-suave">Informe a retirada e a devolução para consultar a disponibilidade.</p>
            </div>

            <form className="space-y-5" onSubmit={buscarVeiculos} noValidate>
              <Campo
                rotulo="Unidade de retirada"
                name="localRetirada"
                type="text"
                autoComplete="off"
                maxLength={100}
                required
                placeholder="Digite o nome da unidade"
                dica="Este local é mantido como contexto da retirada; a disponibilidade consulta a frota global."
                value={periodo.localRetirada}
                erro={erroLocalRetirada}
                onChange={(evento) => {
                  setPeriodo((atual) => ({ ...atual, localRetirada: evento.target.value }));
                  setErroLocalRetirada('');
                }}
              />
              <div className="grid gap-5 sm:grid-cols-2">
                <Campo
                  rotulo="Data de retirada"
                  type="date"
                  name="dataRetirada"
                  autoComplete="off"
                  required
                  min={hoje}
                  value={periodo.dataRetirada}
                  onChange={(evento) => {
                    setPeriodo((atual) => ({ ...atual, dataRetirada: evento.target.value }));
                    setErroPeriodo('');
                  }}
                />
                <Campo
                  rotulo="Data de devolução"
                  type="date"
                  name="dataDevolucao"
                  autoComplete="off"
                  required
                  min={periodo.dataRetirada ? diaSeguinteISO(periodo.dataRetirada) : hoje}
                  value={periodo.dataDevolucao}
                  erro={erroPeriodo}
                  onChange={(evento) => {
                    setPeriodo((atual) => ({ ...atual, dataDevolucao: evento.target.value }));
                    setErroPeriodo('');
                  }}
                />
              </div>
              <Botao type="submit" tamanho="grande" larguraTotal iconeDireita="arrow_forward">
                Buscar veículos
              </Botao>
              <p className="text-center text-legenda text-texto-suave">
                A consulta é gratuita. Você escolhe e confirma a reserva na próxima etapa.
              </p>
            </form>
          </div>
        </div>
      </section>

      <section aria-label="Vantagens AutoVia" className="mx-auto grid w-full max-w-7xl gap-6 px-4 py-10 sm:grid-cols-3 sm:px-6 lg:px-8 lg:py-14">
        <article className="flex gap-4 rounded-lg border border-borda bg-branco p-5">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-steel-claro text-navy"><Icone nome="search" tamanho={22} /></span>
          <div><h2 className="font-semibold text-navy">Encontre seu carro</h2><p className="mt-1 text-corpo-denso text-texto-suave">Compare opções disponíveis para as datas escolhidas.</p></div>
        </article>
        <article className="flex gap-4 rounded-lg border border-borda bg-branco p-5">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-steel-claro text-navy"><Icone nome="event" tamanho={22} /></span>
          <div><h2 className="font-semibold text-navy">Escolha seu período</h2><p className="mt-1 text-corpo-denso text-texto-suave">Veja veículos livres durante toda a sua viagem.</p></div>
        </article>
        <article className="flex gap-4 rounded-lg border border-borda bg-branco p-5">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-steel-claro text-navy"><Icone nome="check_circle" tamanho={22} /></span>
          <div><h2 className="font-semibold text-navy">Reserve com tranquilidade</h2><p className="mt-1 text-corpo-denso text-texto-suave">A reserva segura o veículo para o período escolhido.</p></div>
        </article>
      </section>
    </div>
  );
}
