import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { ErroApi } from '@/types/api';
import type { ContextoRetirada } from '@/types/retirada';
import { useAbrirLocacao, useRetirada } from '../hooks/useRetirada';
import { contextoRetiradaTeste as contexto } from '../retirada/contextoRetirada.fixture';
import { termosCortadosPresentesRetirada } from '../retirada/termosCortadosRetirada';
import type { ConferenciaRetirada } from '../retirada/validacaoRetirada';
import RetiradaVeiculoPagina, { ConteudoRetirada } from './RetiradaVeiculoPagina';

vi.mock('../hooks/useRetirada', () => ({ useRetirada: vi.fn(), useAbrirLocacao: vi.fn() }));
// Sem AutenticacaoProvider nos testes: o nome do funcionário logado vem de um dublê.
vi.mock('@/routes/autenticacao/useAutenticacao', () => ({ useAutenticacao: () => ({ usuario: { nome: 'Ana Souza' } }) }));

type Consulta = ReturnType<typeof useRetirada>;
type Registro = ReturnType<typeof useAbrirLocacao>;

const NADA: ConferenciaRetirada = { cnhOriginal: false, comprovanteResidencia: false, termoMultas: false };
const TUDO: ConferenciaRetirada = { cnhOriginal: true, comprovanteResidencia: true, termoMultas: true };
const TEXTO_BOTAO = 'Iniciar locação e ir para vistoria';
const TEXTO_INDISPONIVEL = 'A retirada exige uma reserva ativa ou confirmada e um veículo reservado. Atualize os dados ou volte às retiradas.';
const VOLTAR = 'Voltar às retiradas';
const ENDERECO_ENTRADA = 'href="/back-office/locacoes/retirada"';

function mockarHooks(parcial: Partial<Consulta>, registro: Partial<Registro>) {
  vi.mocked(useRetirada).mockReturnValue({ isPending: false, isError: false, isFetching: false, refetch: vi.fn(), data: contexto, ...parcial } as unknown as Consulta);
  vi.mocked(useAbrirLocacao).mockReturnValue({ isPending: false, isError: false, mutateAsync: vi.fn(), ...registro } as unknown as Registro);
}

function renderizarPagina(parcial: Partial<Consulta> = {}, id = '8', registro: Partial<Registro> = {}): string {
  mockarHooks(parcial, registro);
  return renderToStaticMarkup(
    <AvisoProvider><MemoryRouter initialEntries={[`/retirada/${id}`]}><Routes><Route path="/retirada/:reservaId" element={<RetiradaVeiculoPagina />} /></Routes></MemoryRouter></AvisoProvider>,
  );
}

function renderizarConteudo(
  dados: Partial<ContextoRetirada> = {},
  valoresIniciais: ConferenciaRetirada = NADA,
  registro: Partial<Registro> = {},
  atualizando = false,
): string {
  mockarHooks({}, registro);
  return renderToStaticMarkup(
    <AvisoProvider><MemoryRouter><ConteudoRetirada contexto={{ ...contexto, ...dados }} atualizando={atualizando} valoresIniciais={valoresIniciais} /></MemoryRouter></AvisoProvider>,
  );
}

/** Abertura da tag do botão que contém o texto (o texto vem antes do ícone da direita). */
function botaoComTexto(html: string, texto: string): string {
  const fim = html.indexOf(texto);
  const inicio = html.lastIndexOf('<button', fim);
  return html.slice(inicio, fim);
}

/** Abertura da tag do link que contém o texto, com o ícone da esquerda (o ícone vem antes do texto). */
function linkComTexto(html: string, texto: string): string {
  const fim = html.indexOf(texto);
  const inicio = html.lastIndexOf('<a ', fim);
  return html.slice(inicio, fim);
}

describe('tela 16: retirada do veículo', () => {
  beforeEach(() => vi.clearAllMocks());

  it('estado inicial: título, subtítulo, trilha, passo a passo, dados reais e conferência desmarcada', () => {
    const html = renderizarPagina();
    for (const texto of [
      'Retirada do veículo',
      'Conferência inicial de documentação e validação de reserva para entrega das chaves.',
      'Despacho', '#AV-00008', 'Reserva Ativa', 'Grupo SUV Médio', 'Jeep Renegade Longitude', '2024', 'BRASIL', 'BRA2E19',
      'Cliente de teste', '123.***.***-09', '*******1234', 'Categoria:', '18/11/2030', 'Cadastro nesta locadora',
      '18/11/2026 → 22/11/2026', '4 diárias completas contratadas', 'AutoVia Congonhas', 'Rua Exemplo, 100 - Centro, São Paulo - SP',
      'Diária:', 'R$ 120,00', 'Total previsto', 'R$ 480,00', 'Checklist de Verificação Documental e Presencial', '0/3 Confirmados',
      'Etapa 1', 'Em andamento', 'Quilometragem atual', '12.000 km', 'Atualizar dados',
    ]) expect(html).toContain(texto);
    expect(html).toContain('href="/back-office/locacoes"');
    expect(html.match(/role="switch"/g)).toHaveLength(3);
    expect(html.match(/aria-checked="false"/g)).toHaveLength(3);
    expect(html).toContain('aria-current="step"');
    expect(html).not.toContain('Pronto para despacho de pátio');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('type="file"');
    expect(html).not.toContain(VOLTAR);
  });

  it('botão principal desligado fica apagado (disabled:opacity-60) em 0/3 e no indisponível, com sombra e hover do desenho', () => {
    const inicial = botaoComTexto(renderizarPagina(), TEXTO_BOTAO);
    const indisponivel = botaoComTexto(renderizarPagina({ data: { ...contexto, statusVeiculo: 'DISPONIVEL' } }), TEXTO_BOTAO);
    for (const botao of [inicial, indisponivel]) {
      expect(botao).toContain('disabled=""');
      expect(botao).toContain('disabled:opacity-60');
    }
    for (const classe of ['gap-2', '!shadow-md', 'active:scale-95', 'enabled:hover:!bg-frota-oficina']) expect(inicial).toContain(classe);
  });

  it('abaixo de 1280 px as colunas empilham e o passo a passo fica 2 por linha (xl e md:grid-cols-2)', () => {
    const html = renderizarPagina();
    for (const classe of ['xl:grid-cols-12', 'xl:col-span-5', 'xl:col-span-7', 'md:grid-cols-2 xl:grid-cols-4']) expect(html).toContain(classe);
    for (const classe of ['lg:grid-cols-12', 'lg:col-span-5', 'lg:col-span-7', 'md:grid-cols-4']) expect(html).not.toContain(classe);
  });

  it('o ponto do selo Em andamento para quando o sistema pede menos movimento', () => {
    const pontos = renderizarPagina().match(/class="[^"]*animate-ping[^"]*"/g) ?? [];
    expect(pontos).toHaveLength(1);
    expect(pontos[0]).toContain('motion-reduce:animate-none');
  });

  it('ocupa a área como as telas 09, 11 e 18: sem a largura antiga e com a barra de ponta a ponta', () => {
    const html = renderizarPagina();
    expect(html).not.toContain('max-w-5xl');
    expect(html).toMatch(/class="sticky bottom-0 [^"]*-mx-8 [^"]*px-8 /);
  });

  it('nenhuma etapa do passo a passo é link', () => {
    const html = renderizarPagina();
    expect(html).not.toMatch(/<a [^>]*>[^<]*(Conferência|Vistoria|Contrato|Entrega)/);
  });

  it('carregando: sem conferência nem dados antigos', () => {
    const html = renderizarPagina({ isPending: true });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Cliente de teste');
  });

  it.each([401, 403, 500, 0])('erro %s: mensagem do servidor, Tentar novamente e Voltar às retiradas, sem conferência nem dados antigos', (status) => {
    const html = renderizarPagina({ isError: true, error: new ErroApi({ status, mensagem: 'Não foi possível consultar.' }) });
    expect(html).toContain('Não foi possível consultar.');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Cliente de teste');
    expect(html).toContain('Tentar novamente');
    const voltar = linkComTexto(html, VOLTAR);
    expect(voltar).toContain(ENDERECO_ENTRADA);
    expect(voltar).toContain('>arrow_back</span>');
  });

  it('erro 404 (reserva não encontrada): mensagem do servidor e só o Voltar às retiradas, sem Tentar novamente', () => {
    const html = renderizarPagina({ isError: true, error: new ErroApi({ status: 404, mensagem: 'A reserva não foi encontrada.' }) });
    expect(html).toContain('A reserva não foi encontrada.');
    expect(html).not.toContain('Tentar novamente');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Cliente de teste');
    expect(linkComTexto(html, VOLTAR)).toContain(ENDERECO_ENTRADA);
  });

  it('id inválido: mensagem própria, Voltar às retiradas e nenhuma consulta com id', () => {
    const html = renderizarPagina({}, '0');
    expect(html).toContain('A reserva informada é inválida. Volte às retiradas e informe o código de novo.');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Tentar novamente');
    expect(linkComTexto(html, VOLTAR)).toContain(ENDERECO_ENTRADA);
    expect(useRetirada).toHaveBeenCalledWith(null);
  });

  it('locação já iniciada: link para a vistoria, sem conferência, sem barra e sem selo Em andamento', () => {
    const html = renderizarPagina({ data: { ...contexto, idLocacaoExistente: 42, statusVeiculo: 'LOCADO' } });
    for (const texto of ['Locação já iniciada', 'Continuar para vistoria', 'Etapa 1']) expect(html).toContain(texto);
    expect(html).toContain('/back-office/locacoes/42/vistoria');
    for (const ausente of ['role="switch"', 'Pronto para despacho de pátio', TEXTO_BOTAO, 'Em andamento', 'aria-current="step"']) expect(html).not.toContain(ausente);
  });

  it('retirada indisponível: texto do estado, botão desligado e sem selo Em andamento', () => {
    const html = renderizarPagina({ data: { ...contexto, statusVeiculo: 'DISPONIVEL' } });
    expect(html).toContain(TEXTO_INDISPONIVEL);
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('Em andamento');
  });

  it('retirada indisponível: o aviso fica no topo, com role="alert", antes da lista e antes do aviso de CNH vencida', () => {
    const html = renderizarConteudo({ statusReserva: 'CANCELADA', cnhVencida: true, cnhValidadeStr: '17/11/2026' }, NADA);
    const posicao = html.indexOf(TEXTO_INDISPONIVEL);
    expect(html.split(TEXTO_INDISPONIVEL).length - 1).toBe(1);
    const abertura = html.slice(html.lastIndexOf('<p ', posicao), posicao);
    expect(abertura).toContain('role="alert"');
    expect(abertura).toContain('bg-status-perigo-fundo');
    expect(abertura).toContain('>block</span>');
    expect(posicao).toBeLessThan(html.indexOf('CNH vencida:'));
    expect(posicao).toBeLessThan(html.indexOf('<ul'));
    expect(posicao).toBeLessThan(html.indexOf('CNH original física ou digital compatível'));
  });

  it('CNH vencida: aviso com a data, e o botão segue a mesma regra', () => {
    const html = renderizarConteudo({ cnhVencida: true, cnhValidadeStr: '17/11/2026' }, TUDO);
    expect(html).toContain('CNH vencida: a validade terminou em 17/11/2026. O sistema não bloqueia a retirada; confira o documento com o cliente.');
    expect(botaoComTexto(html, TEXTO_BOTAO)).not.toContain('disabled=""');
  });

  it('CNH em dia: sem aviso', () => {
    expect(renderizarConteudo({ cnhVencida: false }, NADA)).not.toContain('CNH vencida');
  });

  it('documentos não enviados pelo site: texto de não enviado nas duas linhas', () => {
    const html = renderizarConteudo({ cnhEnviadaEmStr: '', comprovanteResidenciaEnviadoEmStr: '' }, NADA);
    expect(html).toContain('O cliente não enviou a CNH pelo site.');
    expect(html).toContain('O cliente não enviou o comprovante pelo site.');
  });

  it('documentos enviados: data e hora do envio, sem link para o arquivo', () => {
    const html = renderizarConteudo({}, NADA);
    expect(html).toContain('Enviada pelo cliente em 10/11/2026 10:30.');
    expect(html).toContain('Enviado pelo cliente em 12/11/2026 09:15.');
    expect(html).not.toContain('href="http');
  });

  it('veículo sem foto: ícone de carro', () => {
    expect(renderizarConteudo({ fotoCapa: null, erroFotoCapa: null }, NADA)).toContain('directions_car');
  });

  it('veículo com erro na capa: mostra o texto pronto do servidor', () => {
    const texto = 'Não foi possível carregar as fotos agora. Tente novamente em instantes.';
    expect(renderizarConteudo({ fotoCapa: null, erroFotoCapa: texto }, NADA)).toContain(texto);
  });

  it('sem valor estimado: a linha Total previsto não aparece', () => {
    const html = renderizarConteudo({ valorEstimadoStr: '' }, NADA);
    expect(html).not.toContain('Total previsto');
    expect(html).toContain('Diária:');
  });

  it('três itens marcados: contador 3/3, botão ligado e pronto para despacho', () => {
    const html = renderizarConteudo({}, TUDO);
    expect(html).toContain('3/3 Confirmados');
    expect(html).toContain('Conferido nesta tela por Ana Souza');
    expect(html.match(/aria-checked="true"/g)).toHaveLength(3);
    expect(botaoComTexto(html, TEXTO_BOTAO)).not.toContain('disabled=""');
    expect(html).toContain('Pronto para despacho de pátio');
  });

  it('bloqueia durante atualização e envio', () => {
    expect(renderizarConteudo({}, NADA, {}, true)).toContain('<fieldset disabled=""');
    expect(renderizarConteudo({}, NADA, { isPending: true })).toContain('<fieldset disabled=""');
  });

  it('409 ao abrir: texto do servidor e aviso de conflito, sem o aviso de recuperação', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'A reserva não está em um estado que permita iniciar a locação.' });
    const html = renderizarConteudo({}, TUDO, { isError: true, error: erro });
    expect(html).toContain('A reserva não está em um estado que permita iniciar a locação.');
    expect(html).toContain('Os dados da retirada mudaram. Confira os dados atualizados antes de continuar.');
    expect(html).not.toContain('Não foi possível confirmar a abertura.');
  });

  it('outro erro ao abrir (500): texto do servidor e aviso de recuperação, sem o de conflito', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Erro interno.' });
    const html = renderizarConteudo({}, TUDO, { isError: true, error: erro });
    expect(html).toContain('Não foi possível confirmar a abertura.');
    expect(html).not.toContain('Os dados da retirada mudaram.');
  });

  it('escapa nome com marcação e mostra sem cadastro ativo', () => {
    const html = renderizarConteudo({ clienteNome: '<script>alert(1)</script>', statusCadastroLocadora: null }, NADA);
    expect(html).toContain('&lt;script&gt;');
    expect(html).toContain('Sem cadastro ativo nesta locadora');
  });

  it.each([
    ['estado inicial', () => renderizarPagina()],
    ['três itens marcados', () => renderizarConteudo({}, TUDO)],
    ['locação já iniciada', () => renderizarPagina({ data: { ...contexto, idLocacaoExistente: 42 } })],
    ['indisponível', () => renderizarPagina({ data: { ...contexto, statusVeiculo: 'DISPONIVEL' } })],
    ['carregando', () => renderizarPagina({ isPending: true })],
    ['erro 404', () => renderizarPagina({ isError: true, error: new ErroApi({ status: 404, mensagem: 'A reserva não foi encontrada.' }) })],
    ['409 ao abrir', () => renderizarConteudo({}, TUDO, { isError: true, error: new ErroApi({ status: 409, mensagem: 'Estado inválido.' }) })],
    ['CNH vencida e sem documentos', () => renderizarConteudo({ cnhVencida: true, cnhEnviadaEmStr: '', comprovanteResidenciaEnviadoEmStr: '' }, NADA)],
    ['sem valor estimado', () => renderizarConteudo({ valorEstimadoStr: '' }, NADA)],
  ])('nenhum termo cortado do desenho aparece: %s', (_nome, renderizar) => {
    expect(termosCortadosPresentesRetirada(renderizar())).toEqual([]);
  });
});
